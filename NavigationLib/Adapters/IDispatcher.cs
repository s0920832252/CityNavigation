using System;

namespace NavigationLib.Adapters
{
    /// <summary>
    /// 隔離 WPF Dispatcher 依賴的介面，用於將操作調度到 UI 執行緒。
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// 檢查當前執行緒是否為 UI 執行緒。
        /// </summary>
        /// <returns>若當前執行緒可存取 UI，則為 true；否則為 false。</returns>
        bool CheckAccess();

        /// <summary>
        /// 在 UI 執行緒上同步執行指定的動作。
        /// 若當前已在 UI 執行緒，則直接執行；否則透過 Dispatcher 調度。
        /// </summary>
        /// <param name="action">要執行的動作。</param>
        void Invoke(Action action);

        /// <summary>
        /// 在 UI 執行緒上非同步執行指定的動作。
        /// </summary>
        /// <param name="action">要執行的動作。</param>
        void BeginInvoke(Action action);
    }
}
