using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NavigationLib.UseCases
{
    /// <summary>
    ///     管理 Region 的生命週期，包含自動訂閱 Unloaded 事件與清理資源。
    /// </summary>
    internal sealed class RegionLifecycleManager : IDisposable
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, IDisposable> _subscriptions;
        private bool _disposed;

        /// <summary>
        ///     初始化 RegionLifecycleManager 的新執行個體。
        /// </summary>
        public RegionLifecycleManager() => _subscriptions = new Dictionary<string, IDisposable>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     釋放所有資源並取消所有訂閱。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (_lock)
            {
                foreach (var subscription in _subscriptions.Values)
                {
                    subscription?.Dispose();
                }

                _subscriptions.Clear();
            }

            _disposed = true;
        }

        /// <summary>
        ///     開始管理指定的 region，訂閱其 Unloaded 事件。
        /// </summary>
        /// <param name="regionName">Region 名稱。</param>
        /// <param name="element">Region 元素。</param>
        /// <param name="onUnload">元素離開視覺樹時的回呼（通常是解除註冊）。</param>
        public void ManageRegion(string regionName, IRegionElement element, Action<string> onUnload)
        {
            if (regionName == null)
            {
                throw new ArgumentNullException(nameof(regionName));
            }

            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            if (onUnload == null)
            {
                throw new ArgumentNullException(nameof(onUnload));
            }

            lock (_lock)
            {
                // 若已在管理，先停止舊的訂閱
                if (_subscriptions.ContainsKey(regionName))
                {
                    StopManaging(regionName);
                }

                // 訂閱 Unloaded 事件
                var subscription = SubscribeToUnloaded(regionName, element, onUnload);

                if (subscription != null)
                {
                    _subscriptions[regionName] = subscription;
                    Debug.WriteLine($"[RegionLifecycleManager] Started managing region '{regionName}'.");
                }
            }
        }

        /// <summary>
        ///     停止管理指定的 region，取消事件訂閱。
        /// </summary>
        /// <param name="regionName">Region 名稱。</param>
        public void StopManaging(string regionName)
        {
            if (regionName == null)
            {
                throw new ArgumentNullException(nameof(regionName));
            }

            lock (_lock)
            {
                if (_subscriptions.TryGetValue(regionName, out var subscription))
                {
                    subscription?.Dispose();
                    _subscriptions.Remove(regionName);
                    Debug.WriteLine($"[RegionLifecycleManager] Stopped managing region '{regionName}'.");
                }
            }
        }

        /// <summary>
        ///     訂閱元素的 Unloaded 事件。
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         <strong>記憶體管理說明：</strong>
        ///     </para>
        ///     <para>
        ///         此方法中的 Local function Handler 會捕獲 <paramref name="element"/> 變數（閉包），
        ///         形成 Handler → element 的強引用。同時，element.SubscribeUnloaded(Handler) 
        ///         會在 element 內部透過 WeakEventManager 訂閱 Handler，形成 element → Handler 的弱引用。
        ///     </para>
        ///     <para>
        ///         雖然看似循環引用，但因為其中一邊是弱引用（WeakEventManager），
        ///         當外部移除對 Handler 的強引用時（例如透過 Dispose），
        ///         GC 仍可正常回收這些物件，不會造成記憶體洩漏。
        ///     </para>
        ///     <para>
        ///         引用關係：
        ///         <list type="bullet">
        ///             <item>Local function Handler → element（強引用，閉包捕獲）</item>
        ///             <item>element → Handler（弱引用，透過 WeakEventManager）</item>
        ///             <item>Dispose() 會明確移除訂閱，確保資源釋放</item>
        ///         </list>
        ///     </para>
        /// </remarks>
        private IDisposable SubscribeToUnloaded(string regionName, IRegionElement element, Action<string> onUnload)
        {
            // Local function：處理元素卸載事件
            void Handler(object sender, EventArgs e)
            {
                // 確認元素真的離開視覺樹（避免 TabControl 切換等情況的誤判）
                // 注意：雖然捕獲了 element，但不會造成記憶體洩漏（見上方 remarks）
                if (!element.IsInVisualTree())
                {
                    Debug.WriteLine($"[RegionLifecycleManager] Region '{regionName}' element unloaded, triggering cleanup.");
                    onUnload(regionName);
                }
            }

            return element.SubscribeUnloaded(Handler);
        }
    }
}