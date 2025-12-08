using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NavigationLib.Adapters;

namespace NavigationLib.UseCases
{
    /// <summary>
    /// Region 註冊事件的參數。
    /// </summary>
    public class RegionEventArgs : EventArgs
    {
        /// <summary>
        /// 取得 Region 的名稱。
        /// </summary>
        public string RegionName { get; }

        /// <summary>
        /// 取得相關的 Region 元素。
        /// </summary>
        public IRegionElement Element { get; }

        /// <summary>
        /// 初始化 RegionEventArgs 的新執行個體。
        /// </summary>
        /// <param name="regionName">Region 名稱。</param>
        /// <param name="element">Region 元素。</param>
        public RegionEventArgs(string regionName, IRegionElement element)
        {
            RegionName = regionName;
            Element = element;
        }
    }

    /// <summary>
    /// Region 註冊中心，負責管理所有已註冊的 Region。
    /// 使用強引用儲存 Region 元素，配合事件驅動清理機制避免記憶體洩漏。
    /// 此類別為執行緒安全的 Singleton。
    /// </summary>
    /// <remarks>
    /// <para>
    /// RegionStore 維護全域範圍內的 Region 註冊，每個名稱對應到單一活躍元素。
    /// </para>
    /// <para>
    /// 重複註冊行為：
    /// <list type="bullet">
    /// <item><description>若新註冊的元素與已登記的元素為同一實例，視為 idempotent（忽略重複註冊）</description></item>
    /// <item><description>若為不同的元素，將更新註冊到新的元素，並記錄警告以協助診斷</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// 記憶體管理策略：使用強引用保證導航期間物件存在，但依賴 RegionElementAdapter 
    /// 訂閱 Unloaded 事件並主動通知 RegionStore 移除，避免記憶體洩漏。
    /// </para>
    /// </remarks>
    public sealed class RegionStore
    {
        private static readonly Lazy<RegionStore> _instance = 
            new Lazy<RegionStore>(() => new RegionStore());

        private readonly Dictionary<string, IRegionElement> _regions;
        private readonly object _lock = new object();

        /// <summary>
        /// 取得 RegionStore 的 Singleton 執行個體。
        /// </summary>
        public static RegionStore Instance
        {
            get { return _instance.Value; }
        }

        /// <summary>
        /// 當 Region 註冊時發生。
        /// </summary>
        /// <remarks>
        /// 此事件在 UI 執行緒或呼叫 Register 的執行緒上引發。
        /// 訂閱者應保持處理輕量，避免阻塞。
        /// </remarks>
        public event EventHandler<RegionEventArgs> RegionRegistered;

        /// <summary>
        /// 當 Region 解除註冊時發生。
        /// </summary>
        /// <remarks>
        /// 此事件在 UI 執行緒或呼叫 Unregister 的執行緒上引發。
        /// </remarks>
        public event EventHandler<RegionEventArgs> RegionUnregistered;

        private RegionStore()
        {
            _regions = new Dictionary<string, IRegionElement>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 註冊一個 Region 元素。
        /// </summary>
        /// <param name="regionName">Region 的名稱。</param>
        /// <param name="element">要註冊的元素。</param>
        /// <exception cref="ArgumentNullException">
        /// 當 <paramref name="regionName"/> 或 <paramref name="element"/> 為 null 時拋出。
        /// </exception>
        /// <remarks>
        /// <para>
        /// 若 regionName 已存在：
        /// <list type="bullet">
        /// <item><description>若為相同實例，忽略（idempotent）</description></item>
        /// <item><description>若為不同實例，更新為新實例並記錄警告</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// 此方法會自動清理已失效的弱參考。
        /// </para>
        /// </remarks>
        public void Register(string regionName, IRegionElement element)
        {
            if (regionName == null)
                throw new ArgumentNullException(nameof(regionName));
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            bool shouldRaiseEvent = false;
            RegionEventArgs eventArgs = null;

            lock (_lock)
            {
                // 檢查是否已存在
                if (_regions.TryGetValue(regionName, out IRegionElement existingElement))
                {
                    // 已存在
                    if (ReferenceEquals(existingElement, element))
                    {
                        // 相同實例，忽略（idempotent）
                        Debug.WriteLine(string.Format("[RegionStore] Region '{0}' already registered with the same instance. Ignoring duplicate registration.", regionName));
                        return;
                    }
                    else
                    {
                        // 不同實例，更新並記錄警告
                        Debug.WriteLine(string.Format("[RegionStore] Warning: Region '{0}' is being re-registered with a different element. Updating registration.", regionName));
                        _regions[regionName] = element;
                        shouldRaiseEvent = true;
                        eventArgs = new RegionEventArgs(regionName, element);
                    }
                }
                else
                {
                    // 新註冊
                    _regions[regionName] = element;
                    shouldRaiseEvent = true;
                    eventArgs = new RegionEventArgs(regionName, element);
                }
            }

            // 在鎖外觸發事件
            if (shouldRaiseEvent && eventArgs != null)
            {
                OnRegionRegistered(eventArgs);
            }
        }

        /// <summary>
        /// 解除註冊一個 Region 元素。
        /// </summary>
        /// <param name="regionName">Region 的名稱。</param>
        /// <param name="element">要解除註冊的元素（用於驗證）。</param>
        /// <exception cref="ArgumentNullException">
        /// 當 <paramref name="regionName"/> 或 <paramref name="element"/> 為 null 時拋出。
        /// </exception>
        /// <remarks>
        /// 只有當指定的元素與已註冊的元素為同一實例時，才會解除註冊。
        /// 這避免了誤解除註冊其他元素。
        /// </remarks>
        public void Unregister(string regionName, IRegionElement element)
        {
            if (regionName == null)
                throw new ArgumentNullException(nameof(regionName));
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            bool shouldRaiseEvent = false;
            RegionEventArgs eventArgs = null;

            lock (_lock)
            {
                if (_regions.TryGetValue(regionName, out IRegionElement existingElement))
                {
                    if (ReferenceEquals(existingElement, element))
                    {
                        // 相同實例，移除
                        _regions.Remove(regionName);
                        shouldRaiseEvent = true;
                        eventArgs = new RegionEventArgs(regionName, element);
                    }
                    else
                    {
                        // 不同實例，不移除（避免誤移除其他元素）
                        Debug.WriteLine(string.Format("[RegionStore] Region '{0}' unregister attempt with different element instance. Ignoring.", regionName));
                    }
                }
            }

            // 在鎖外觸發事件
            if (shouldRaiseEvent && eventArgs != null)
            {
                OnRegionUnregistered(eventArgs);
            }
        }

        /// <summary>
        /// 嘗試取得已註冊的 Region 元素。
        /// </summary>
        /// <param name="regionName">Region 的名稱。</param>
        /// <param name="element">若找到，則為對應的元素；否則為 null。</param>
        /// <returns>若找到且元素仍然存活，則為 true；否則為 false。</returns>
        /// <exception cref="ArgumentNullException">
        /// 當 <paramref name="regionName"/> 為 null 時拋出。
        /// </exception>
        public bool TryGetRegion(string regionName, out IRegionElement element)
        {
            if (regionName == null)
                throw new ArgumentNullException(nameof(regionName));

            element = null;

            lock (_lock)
            {
                if (_regions.TryGetValue(regionName, out IRegionElement target))
                {
                    element = target;
                    return true;
                }

                return false;
            }
        }



        /// <summary>
        /// 引發 RegionRegistered 事件。
        /// </summary>
        /// <param name="e">事件參數。</param>
        private void OnRegionRegistered(RegionEventArgs e)
        {
            RegionRegistered?.Invoke(this, e);
        }

        /// <summary>
        /// 引發 RegionUnregistered 事件。
        /// </summary>
        /// <param name="e">事件參數。</param>
        private void OnRegionUnregistered(RegionEventArgs e)
        {
            RegionUnregistered?.Invoke(this, e);
        }

        /// <summary>
        /// 取得目前已註冊的所有 Region 名稱（用於測試/診斷）。
        /// </summary>
        /// <returns>已註冊的 Region 名稱集合。</returns>
        internal IEnumerable<string> GetRegisteredRegionNames()
        {
            lock (_lock)
            {
                return _regions.Keys.ToList();
            }
        }
    }
}
