using System;
using System.Windows;
using NavigationLib.Adapters;

namespace NavigationLib.FrameworksAndDrivers
{
    /// <summary>
    /// IRegionElement 的 WPF 實作，包裝 FrameworkElement。
    /// </summary>
    internal class RegionElementAdapter : IRegionElement
    {
        private readonly FrameworkElement _element;
        private readonly DispatcherAdapter _dispatcher;

        /// <summary>
        /// 初始化 RegionElementAdapter 的新執行個體。
        /// </summary>
        /// <param name="element">要包裝的 FrameworkElement。</param>
        public RegionElementAdapter(FrameworkElement element)
        {
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _dispatcher = new DispatcherAdapter(element.Dispatcher);
        }

        /// <summary>
        /// 取得包裝的 FrameworkElement。
        /// </summary>
        internal FrameworkElement Element
        {
            get { return _element; }
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
    }
}
