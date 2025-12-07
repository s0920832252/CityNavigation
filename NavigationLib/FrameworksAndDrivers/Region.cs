using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using NavigationLib.UseCases;

namespace NavigationLib.FrameworksAndDrivers
{
    /// <summary>
    /// 提供 Region 附加屬性，用於在 XAML 中標記 Region。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 在 FrameworkElement 上設定 Region.Name 會在元素 Loaded 時自動註冊到 RegionStore，
    /// 並在 Unloaded 時（確認離開視覺樹後）解除註冊。
    /// </para>
    /// <para>
    /// 使用 WeakEventManager 管理事件訂閱，避免記憶體洩漏。
    /// 使用 ConditionalWeakTable 管理 FrameworkElement 與 RegionElementAdapter 的關聯，
    /// 確保 Adapter 隨 Element 生命週期自動回收。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// &lt;TabControl nav:Region.Name="Shell"&gt;
    ///   &lt;TabItem Header="Home"&gt;
    ///     &lt;local:HomeView nav:Region.Name="Home" /&gt;
    ///   &lt;/TabItem&gt;
    /// &lt;/TabControl&gt;
    /// </code>
    /// </example>
    public static class Region
    {
        /// <summary>
        /// Region.Name 附加屬性。
        /// </summary>
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.RegisterAttached(
                "Name",
                typeof(string),
                typeof(Region),
                new PropertyMetadata(null, OnNameChanged));

        /// <summary>
        /// 使用 ConditionalWeakTable 管理 FrameworkElement 與 RegionElementAdapter 的關聯。
        /// 當 FrameworkElement 被 GC 回收時，對應的 Adapter 也會自動被回收。
        /// </summary>
        private static readonly ConditionalWeakTable<FrameworkElement, RegionElementAdapter> 
            _adapterTable = new ConditionalWeakTable<FrameworkElement, RegionElementAdapter>();

        /// <summary>
        /// 取得元素的 Region 名稱。
        /// </summary>
        /// <param name="element">目標元素。</param>
        /// <returns>Region 名稱，若未設定則為 null。</returns>
        public static string GetName(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (string)element.GetValue(NameProperty);
        }

        /// <summary>
        /// 設定元素的 Region 名稱。
        /// </summary>
        /// <param name="element">目標元素。</param>
        /// <param name="value">Region 名稱。</param>
        public static void SetName(DependencyObject element, string value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.SetValue(NameProperty, value);
        }

        /// <summary>
        /// Region.Name 屬性變更時的回呼。
        /// </summary>
        private static void OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement element))
            {
                Debug.WriteLine("[Region] Region.Name can only be set on FrameworkElement.");
                return;
            }

            string oldName = e.OldValue as string;
            string newName = e.NewValue as string;

            // 移除舊的事件處理器
            if (!string.IsNullOrEmpty(oldName))
            {
                LoadedEventManager.RemoveHandler(element, OnElementLoaded);
                UnloadedEventManager.RemoveHandler(element, OnElementUnloaded);
            }

            // 若新名稱有效，註冊事件處理器
            if (!string.IsNullOrEmpty(newName))
            {
                LoadedEventManager.AddHandler(element, OnElementLoaded);
                UnloadedEventManager.AddHandler(element, OnElementUnloaded);

                // 若元素已經載入，立即註冊
                if (element.IsLoaded)
                {
                    RegisterRegion(element, newName);
                }
            }
        }

        /// <summary>
        /// 元素 Loaded 時的處理器。
        /// </summary>
        private static void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element))
                return;

            string regionName = GetName(element);
            if (!string.IsNullOrEmpty(regionName))
            {
                RegisterRegion(element, regionName);
            }
        }

        /// <summary>
        /// 元素 Unloaded 時的處理器。
        /// </summary>
        private static void OnElementUnloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element))
                return;

            string regionName = GetName(element);
            if (!string.IsNullOrEmpty(regionName))
            {
                // 使用 PresentationSource.FromVisual 檢查元素是否真的離開視覺樹
                // 這避免了因 TabControl 切換等情況導致的誤解除註冊
                if (PresentationSource.FromVisual(element) == null)
                {
                    UnregisterRegion(element, regionName);
                }
                else
                {
                    Debug.WriteLine(string.Format("[Region] Element '{0}' Unloaded but still in visual tree. Skipping unregistration.", regionName));
                }
            }
        }

        /// <summary>
        /// 註冊 Region 到 RegionStore。
        /// </summary>
        private static void RegisterRegion(FrameworkElement element, string regionName)
        {
            try
            {
                // 嘗試取得現有 Adapter，若不存在則建立新的
                RegionElementAdapter adapter;
                if (!_adapterTable.TryGetValue(element, out adapter))
                {
                    adapter = new RegionElementAdapter(element);
                    _adapterTable.Add(element, adapter);
                }
                
                RegionStore.Instance.Register(regionName, adapter);
                Debug.WriteLine(string.Format("[Region] Registered region '{0}'.", regionName));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("[Region] Failed to register region '{0}': {1}", regionName, ex.Message));
            }
        }

        /// <summary>
        /// 從 RegionStore 解除註冊 Region。
        /// </summary>
        private static void UnregisterRegion(FrameworkElement element, string regionName)
        {
            try
            {
                // 嘗試從 ConditionalWeakTable 取得對應的 Adapter
                if (_adapterTable.TryGetValue(element, out RegionElementAdapter adapter))
                {
                    RegionStore.Instance.Unregister(regionName, adapter);
                    
                    // 從 ConditionalWeakTable 移除關聯
                    _adapterTable.Remove(element);
                    
                    Debug.WriteLine(string.Format("[Region] Unregistered region '{0}'.", regionName));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("[Region] Failed to unregister region '{0}': {1}", regionName, ex.Message));
            }
        }

        /// <summary>
        /// Loaded 事件的 WeakEventManager。
        /// </summary>
        private class LoadedEventManager : WeakEventManager
        {
            private LoadedEventManager()
            {
            }

            private static LoadedEventManager CurrentManager
            {
                get
                {
                    Type managerType = typeof(LoadedEventManager);
                    LoadedEventManager manager = (LoadedEventManager)GetCurrentManager(managerType);
                    if (manager == null)
                    {
                        manager = new LoadedEventManager();
                        SetCurrentManager(managerType, manager);
                    }
                    return manager;
                }
            }

            public static void AddHandler(FrameworkElement element, EventHandler<RoutedEventArgs> handler)
            {
                if (element == null)
                    throw new ArgumentNullException(nameof(element));
                if (handler == null)
                    throw new ArgumentNullException(nameof(handler));

                CurrentManager.ProtectedAddHandler(element, handler);
            }

            public static void RemoveHandler(FrameworkElement element, EventHandler<RoutedEventArgs> handler)
            {
                if (element == null)
                    throw new ArgumentNullException(nameof(element));
                if (handler == null)
                    throw new ArgumentNullException(nameof(handler));

                CurrentManager.ProtectedRemoveHandler(element, handler);
            }

            protected override void StartListening(object source)
            {
                if (source is FrameworkElement element)
                {
                    element.Loaded += DeliverEvent;
                }
            }

            protected override void StopListening(object source)
            {
                if (source is FrameworkElement element)
                {
                    element.Loaded -= DeliverEvent;
                }
            }

            protected override ListenerList NewListenerList()
            {
                return new ListenerList<RoutedEventArgs>();
            }
        }

        /// <summary>
        /// Unloaded 事件的 WeakEventManager。
        /// </summary>
        private class UnloadedEventManager : WeakEventManager
        {
            private UnloadedEventManager()
            {
            }

            private static UnloadedEventManager CurrentManager
            {
                get
                {
                    Type managerType = typeof(UnloadedEventManager);
                    UnloadedEventManager manager = (UnloadedEventManager)GetCurrentManager(managerType);
                    if (manager == null)
                    {
                        manager = new UnloadedEventManager();
                        SetCurrentManager(managerType, manager);
                    }
                    return manager;
                }
            }

            public static void AddHandler(FrameworkElement element, EventHandler<RoutedEventArgs> handler)
            {
                if (element == null)
                    throw new ArgumentNullException(nameof(element));
                if (handler == null)
                    throw new ArgumentNullException(nameof(handler));

                CurrentManager.ProtectedAddHandler(element, handler);
            }

            public static void RemoveHandler(FrameworkElement element, EventHandler<RoutedEventArgs> handler)
            {
                if (element == null)
                    throw new ArgumentNullException(nameof(element));
                if (handler == null)
                    throw new ArgumentNullException(nameof(handler));

                CurrentManager.ProtectedRemoveHandler(element, handler);
            }

            protected override void StartListening(object source)
            {
                if (source is FrameworkElement element)
                {
                    element.Unloaded += DeliverEvent;
                }
            }

            protected override void StopListening(object source)
            {
                if (source is FrameworkElement element)
                {
                    element.Unloaded -= DeliverEvent;
                }
            }

            protected override ListenerList NewListenerList()
            {
                return new ListenerList<RoutedEventArgs>();
            }
        }
    }
}
