using System;
using System.Windows;
using System.Windows.Threading;
using NavigationLib.Adapters;

namespace NavigationLib.FrameworksAndDrivers
{
    /// <summary>
    /// IDispatcher 的 WPF 實作，包裝 System.Windows.Threading.Dispatcher。
    /// </summary>
    internal class DispatcherAdapter : IDispatcher
    {
        private readonly Dispatcher _dispatcher;

        /// <summary>
        /// 初始化 DispatcherAdapter 的新執行個體。
        /// </summary>
        /// <param name="dispatcher">要包裝的 WPF Dispatcher。</param>
        public DispatcherAdapter(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        /// <summary>
        /// 檢查當前執行緒是否為 UI 執行緒。
        /// </summary>
        /// <returns>若當前執行緒可存取 UI，則為 true；否則為 false。</returns>
        public bool CheckAccess()
        {
            return _dispatcher.CheckAccess();
        }

        /// <summary>
        /// 在 UI 執行緒上同步執行指定的動作。
        /// </summary>
        /// <param name="action">要執行的動作。</param>
        public void Invoke(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (_dispatcher.CheckAccess())
            {
                // 已在 UI 執行緒，直接執行
                action();
            }
            else
            {
                // 調度到 UI 執行緒
                _dispatcher.Invoke(action);
            }
        }

        /// <summary>
        /// 在 UI 執行緒上非同步執行指定的動作。
        /// </summary>
        /// <param name="action">要執行的動作。</param>
        public void BeginInvoke(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _dispatcher.BeginInvoke(action);
        }
    }
}
