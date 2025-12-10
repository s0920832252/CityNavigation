using System;
using System.Windows;
using NavigationLib.Adapters;
using NavigationLib.UseCases;

namespace NavigationLib.FrameworksAndDrivers
{
    /// <summary>
    ///     IRegionElement 的 WPF 實作，包裝 FrameworkElement。
    ///     訂閱元素的 Unloaded 事件，並在元素離開視覺樹時主動從 RegionStore 解除註冊。
    /// </summary>
    internal class RegionElementAdapter : IRegionElement, IDisposable
    {
        private readonly DispatcherAdapter _dispatcher;
        private bool _disposed;

        /// <summary>
        ///     初始化 RegionElementAdapter 的新執行個體。
        /// </summary>
        /// <param name="element">要包裝的 FrameworkElement。</param>
        public RegionElementAdapter(FrameworkElement element)
        {
            Element     = element ?? throw new ArgumentNullException(nameof(element));
            _dispatcher = new DispatcherAdapter(element.Dispatcher);
        }

        /// <summary>
        ///     取得包裝的 FrameworkElement。
        /// </summary>
        internal FrameworkElement Element { get; }

        /// <summary>
        ///     釋放資源。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // 現階段沒有需要顯式釋放的資源：
            // - Unloaded 訂閱由 RegionLifecycleManager 管理，透過 UnloadedSubscription.Dispose() 移除
            // - DataContextChanged 訂閱透過 WeakEventManager 管理，使用弱引用
            //
            // 保留 IDisposable 介面是為了：
            // - 配合 RegionStore.CleanupElement 的生命週期管理
            // - 未來若 Adapter 新增需釋放資源，可以在此集中處理
        }

        /// <summary>
        ///     取得元素的 DataContext。
        /// </summary>
        /// <returns>DataContext 物件，若未設定則為 null。</returns>
        public object GetDataContext() => Element.DataContext;

        /// <summary>
        ///     訂閱 DataContext 變更事件。
        /// </summary>
        /// <param name="handler">DataContext 變更時的處理常式。</param>
        public void AddDataContextChangedHandler(EventHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            DataContextChangedEventManager.AddHandler(Element, handler);
        }

        /// <summary>
        ///     取消訂閱 DataContext 變更事件。
        /// </summary>
        /// <param name="handler">要移除的處理常式。</param>
        public void RemoveDataContextChangedHandler(EventHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            DataContextChangedEventManager.RemoveHandler(Element, handler);
        }

        /// <summary>
        ///     取得此元素的 Dispatcher。
        /// </summary>
        /// <returns>IDispatcher 介面實例。</returns>
        public IDispatcher GetDispatcher() => _dispatcher;

        /// <summary>
        ///     檢查此元素是否仍在視覺樹中。
        /// </summary>
        /// <returns>若元素仍在視覺樹中則為 true，否則為 false。</returns>
        public bool IsInVisualTree() => PresentationSource.FromVisual(Element) != null;

        /// <summary>
        ///     檢查此 adapter 是否與另一個 adapter 包裝相同的 FrameworkElement。
        /// </summary>
        /// <param name="other">要比對的另一個 Region 元素。</param>
        /// <returns>若兩者包裝相同的 FrameworkElement 實例則為 true，否則為 false。</returns>
        public bool IsSameElement(IRegionElement other)
        {
            if (other is RegionElementAdapter otherAdapter)
            {
                return ReferenceEquals(Element, otherAdapter.Element);
            }

            return false;
        }

        /// <summary>
        ///     訂閱元素離開視覺樹的事件。
        /// </summary>
        /// <param name="handler">元素離開視覺樹時的處理常式。</param>
        /// <returns>IDisposable 實例，用於取消訂閱。</returns>
        public IDisposable SubscribeUnloaded(EventHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            // 將 EventHandler 包裝成 WPF 的 RoutedEventHandler
            EventHandler<RoutedEventArgs> routedHandler = (sender, e) => handler(sender, EventArgs.Empty);

            UnloadedEventManager.AddHandler(Element, routedHandler);

            return new UnloadedSubscription(Element, routedHandler);
        }

        /// <summary>
        ///     封裝 Unloaded 事件訂閱的 IDisposable 實作。
        /// </summary>
        private sealed class UnloadedSubscription : IDisposable
        {
            private readonly FrameworkElement _element;
            private readonly EventHandler<RoutedEventArgs> _handler;
            private bool _disposed;

            public UnloadedSubscription(FrameworkElement element, EventHandler<RoutedEventArgs> handler)
            {
                _element = element ?? throw new ArgumentNullException(nameof(element));
                _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                UnloadedEventManager.RemoveHandler(_element, _handler);
                _disposed = true;
            }
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

        /// <summary>
        ///     DataContextChanged 事件的 WeakEventManager。
        /// </summary>
        private class DataContextChangedEventManager : WeakEventManager
        {
            private DataContextChangedEventManager()
            {
            }

            private static DataContextChangedEventManager CurrentManager
            {
                get
                {
                    var managerType = typeof(DataContextChangedEventManager);
                    var manager = (DataContextChangedEventManager)GetCurrentManager(managerType);

                    if (manager == null)
                    {
                        manager = new DataContextChangedEventManager();
                        SetCurrentManager(managerType, manager);
                    }

                    return manager;
                }
            }

            public static void AddHandler(FrameworkElement element, EventHandler handler)
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

            public static void RemoveHandler(FrameworkElement element, EventHandler handler)
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
                    element.DataContextChanged += OnDataContextChanged;
                }
            }

            protected override void StopListening(object source)
            {
                if (source is FrameworkElement element)
                {
                    element.DataContextChanged -= OnDataContextChanged;
                }
            }

            protected override ListenerList NewListenerList() => new ListenerList();

            private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) =>
                // 轉發事件給所有註冊的 EventHandler（將 DependencyPropertyChangedEventArgs 包裝成 EventArgs）
                DeliverEventToList(sender, EventArgs.Empty, null);
        }
    }
}