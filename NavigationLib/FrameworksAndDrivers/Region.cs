using System;
using System.Diagnostics;
using System.Windows;
using NavigationLib.UseCases;

namespace NavigationLib.FrameworksAndDrivers
{
    /// <summary>
    ///     提供 Region 附加屬性，用於在 XAML 中標記 Region。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         在 FrameworkElement 上設定 Region.Name 會在元素 Loaded 時自動註冊到 RegionStore，
    ///         並在 Unloaded 時（確認離開視覺樹後）解除註冊。
    ///     </para>
    ///     <para>
    ///         使用 WeakEventManager 管理事件訂閱，避免記憶體洩漏。
    ///         RegionStore 透過強引用管理 RegionElementAdapter 的生命週期，
    ///         並由 RegionLifecycleManager 訂閱 Unloaded 事件以自動清理資源。
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
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
        ///     Region.Name 附加屬性。
        /// </summary>
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.RegisterAttached("Name",
                typeof(string),
                typeof(Region),
                new PropertyMetadata(null, OnNameChanged));

        /// <summary>
        ///     取得元素的 Region 名稱。
        /// </summary>
        /// <param name="element">目標元素。</param>
        /// <returns>Region 名稱，若未設定則為 null。</returns>
        public static string GetName(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return (string)element.GetValue(NameProperty);
        }

        /// <summary>
        ///     設定元素的 Region 名稱。
        /// </summary>
        /// <param name="element">目標元素。</param>
        /// <param name="value">Region 名稱。</param>
        public static void SetName(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(NameProperty, value);
        }

        /// <summary>
        ///     Region.Name 屬性變更時的回呼。
        /// </summary>
        private static void OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement element))
            {
                Debug.WriteLine("[Region] Region.Name can only be set on FrameworkElement.");
                return;
            }

            // 移除舊的事件處理器
            if (e.OldValue is string oldName && !string.IsNullOrEmpty(oldName))
            {
                LoadedEventManager.RemoveHandler(element, OnElementLoaded);
                UnloadedEventManager.RemoveHandler(element, OnElementUnloaded);
            }

            // 若新名稱有效，註冊事件處理器
            if (e.NewValue is string newName && !string.IsNullOrEmpty(newName))
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
        ///     元素 Loaded 時的處理器。
        /// </summary>
        private static void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element))
            {
                return;
            }

            var regionName = GetName(element);

            if (!string.IsNullOrEmpty(regionName))
            {
                RegisterRegion(element, regionName);
            }
        }

        /// <summary>
        ///     元素 Unloaded 時的處理器。
        /// </summary>
        private static void OnElementUnloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element))
            {
                return;
            }

            var regionName = GetName(element);

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
                    Debug.WriteLine($"[Region] Element '{regionName}' Unloaded but still in visual tree. Skipping unregistration.");
                }
            }
        }

        /// <summary>
        ///     註冊 Region 到 RegionStore。
        /// </summary>
        private static void RegisterRegion(FrameworkElement element, string regionName)
        {
            try
            {
                // 每次建立新的 adapter（RegionStore 會透過 IsSameElement 判斷是否重複）
                var adapter = new RegionElementAdapter(element);
                RegionStore.Instance.Register(regionName, adapter);
                Debug.WriteLine($"[Region] Registered region '{regionName}'.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Region] Failed to register region '{0}': {1}", regionName, ex.Message);
            }
        }

        /// <summary>
        ///     從 RegionStore 解除註冊 Region。
        /// </summary>
        private static void UnregisterRegion(FrameworkElement element, string regionName)
        {
            try
            {
                // 直接呼叫 Unregister（RegionStore 會處理清理）
                RegionStore.Instance.Unregister(regionName);
                Debug.WriteLine($"[Region] Unregistered region '{regionName}'.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Region] Failed to unregister region '{0}': {1}", regionName, ex.Message);
            }
        }

        /// <summary>
        ///     Loaded 事件的 WeakEventManager。
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
                    var managerType = typeof(LoadedEventManager);
                    var manager = (LoadedEventManager)GetCurrentManager(managerType);

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
                {
                    throw new ArgumentNullException(nameof(element));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                CurrentManager.ProtectedAddHandler(element, handler);
            }

            public static void RemoveHandler(FrameworkElement element, EventHandler<RoutedEventArgs> handler)
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

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

            protected override ListenerList NewListenerList() => new ListenerList<RoutedEventArgs>();
        }

        /// <summary>
        ///     Unloaded 事件的 WeakEventManager。
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
                    var managerType = typeof(UnloadedEventManager);
                    var manager = (UnloadedEventManager)GetCurrentManager(managerType);

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
                {
                    throw new ArgumentNullException(nameof(element));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                CurrentManager.ProtectedAddHandler(element, handler);
            }

            public static void RemoveHandler(FrameworkElement element, EventHandler<RoutedEventArgs> handler)
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

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

            protected override ListenerList NewListenerList() => new ListenerList<RoutedEventArgs>();
        }
    }
}