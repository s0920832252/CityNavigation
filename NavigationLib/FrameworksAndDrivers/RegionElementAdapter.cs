using System;
using System.Diagnostics;
using System.Windows;
using NavigationLib.Adapters;
using NavigationLib.UseCases;

namespace NavigationLib.FrameworksAndDrivers
{
    /// <summary>
    /// IRegionElement 的 WPF 實作，包裝 FrameworkElement。
    /// 訂閱元素的 Unloaded 事件，並在元素離開視覺樹時主動從 RegionStore 解除註冊。
    /// </summary>
    internal class RegionElementAdapter : IRegionElement, IDisposable
    {
        private readonly FrameworkElement _element;
        private readonly DispatcherAdapter _dispatcher;
        private string _registeredRegionName; // 記錄註冊的 region 名稱
        private bool _disposed;

        /// <summary>
        /// 初始化 RegionElementAdapter 的新執行個體。
        /// </summary>
        /// <param name="element">要包裝的 FrameworkElement。</param>
        public RegionElementAdapter(FrameworkElement element)
        {
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _dispatcher = new DispatcherAdapter(element.Dispatcher);
            
            // 訂閱 Unloaded 事件（使用弱事件模式）
            UnloadedEventManager.AddHandler(_element, OnElementUnloaded);
        }

        /// <summary>
        /// 取得包裝的 FrameworkElement。
        /// </summary>
        internal FrameworkElement Element
        {
            get { return _element; }
        }

        /// <summary>
        /// 設定此 adapter 已註冊的 region 名稱（供 Unloaded 時自動解除註冊使用）。
        /// </summary>
        internal void SetRegisteredRegionName(string regionName)
        {
            _registeredRegionName = regionName;
        }

        /// <summary>
        /// 元素 Unloaded 時的處理器（自動從 RegionStore 解除註冊）。
        /// </summary>
        private void OnElementUnloaded(object sender, RoutedEventArgs e)
        {
            // 確認真的離開視覺樹
            if (PresentationSource.FromVisual(_element) == null && !string.IsNullOrEmpty(_registeredRegionName))
            {
                Debug.WriteLine(string.Format("[RegionElementAdapter] Element unloaded, auto-unregistering region '{0}'.", _registeredRegionName));
                
                try
                {
                    RegionStore.Instance.Unregister(_registeredRegionName, this);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("[RegionElementAdapter] Failed to auto-unregister region '{0}': {1}", _registeredRegionName, ex.Message));
                }
                
                Dispose();
            }
        }

        /// <summary>
        /// 取得元素的 DataContext。
        /// </summary>
        /// <returns>DataContext 物件，若未設定則為 null。</returns>
        public object GetDataContext()
        {
            return _element.DataContext;
        }

        /// <summary>
        /// 訂閱 DataContext 變更事件。
        /// </summary>
        /// <param name="handler">DataContext 變更時的處理常式。</param>
        public void AddDataContextChangedHandler(EventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            // 包裝成 DependencyPropertyChangedEventHandler
            DependencyPropertyChangedEventHandler wrappedHandler = (s, e) => handler(s, EventArgs.Empty);
            _element.DataContextChanged += wrappedHandler;
        }

        /// <summary>
        /// 取消訂閱 DataContext 變更事件。
        /// </summary>
        /// <param name="handler">要移除的處理常式。</param>
        public void RemoveDataContextChangedHandler(EventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            // 注意：由於包裝的關係，這個實作無法正確移除處理器
            // 需要改用 WeakEventManager 或保存處理器映射
            // 暫時先保留此簽章以符合介面
            DependencyPropertyChangedEventHandler wrappedHandler = (s, e) => handler(s, EventArgs.Empty);
            _element.DataContextChanged -= wrappedHandler;
        }

        /// <summary>
        /// 取得此元素的 Dispatcher。
        /// </summary>
        /// <returns>IDispatcher 介面實例。</returns>
        public IDispatcher GetDispatcher()
        {
            return _dispatcher;
        }

        /// <summary>
        /// 檢查此元素是否仍在視覺樹中。
        /// </summary>
        /// <returns>若元素仍在視覺樹中則為 true，否則為 false。</returns>
        public bool IsInVisualTree()
        {
            return PresentationSource.FromVisual(_element) != null;
        }

        /// <summary>
        /// 釋放資源並解除事件訂閱。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            UnloadedEventManager.RemoveHandler(_element, OnElementUnloaded);
            _registeredRegionName = null;
            _disposed = true;
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
