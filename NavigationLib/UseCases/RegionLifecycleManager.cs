using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using NavigationLib.Adapters;
using NavigationLib.FrameworksAndDrivers;

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
        private IDisposable SubscribeToUnloaded(string regionName, IRegionElement element, Action<string> onUnload)
        {
            if (element is RegionElementAdapter adapter)
            {
                EventHandler<RoutedEventArgs> handler = (sender, e) =>
                {
                    // 確認元素真的離開視覺樹
                    if (!adapter.IsInVisualTree())
                    {
                        Debug.WriteLine($"[RegionLifecycleManager] Region '{regionName}' element unloaded, triggering cleanup.");
                        onUnload(regionName);
                    }
                };

                // 透過 adapter 的公開方法訂閱
                adapter.SubscribeUnloaded(handler);

                // 返回 IDisposable 以便取消訂閱
                return new UnloadSubscription(adapter, handler);
            }

            return null;
        }

        /// <summary>
        ///     封裝 Unloaded 事件訂閱的 IDisposable 實作。
        /// </summary>
        private class UnloadSubscription : IDisposable
        {
            private readonly RegionElementAdapter _adapter;
            private readonly EventHandler<RoutedEventArgs> _handler;
            private bool _disposed;

            public UnloadSubscription(RegionElementAdapter adapter, EventHandler<RoutedEventArgs> handler)
            {
                _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
                _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _adapter.UnsubscribeUnloaded(_handler);
                _disposed = true;
            }
        }
    }
}