using System;

namespace NavigationLib.Adapters
{
    /// <summary>
    ///     隔離 WPF FrameworkElement 依賴的介面，用於 Region 管理。
    ///     此介面允許 UseCases 層在不直接參考 WPF 型別的情況下操作 UI 元素。
    /// </summary>
    public interface IRegionElement
    {
        /// <summary>
        ///     取得元素的 DataContext。
        /// </summary>
        /// <returns>DataContext 物件，若未設定則為 null。</returns>
        object GetDataContext();

        /// <summary>
        ///     訂閱 DataContext 變更事件。
        /// </summary>
        /// <param name="handler">DataContext 變更時的處理常式。</param>
        void AddDataContextChangedHandler(EventHandler handler);

        /// <summary>
        ///     取消訂閱 DataContext 變更事件。
        /// </summary>
        /// <param name="handler">要移除的處理常式。</param>
        void RemoveDataContextChangedHandler(EventHandler handler);

        /// <summary>
        ///     取得此元素的 Dispatcher，用於將操作調度到 UI 執行緒。
        /// </summary>
        /// <returns>IDispatcher 介面實例。</returns>
        IDispatcher GetDispatcher();

        /// <summary>
        ///     檢查此元素是否仍在視覺樹中（用於 Unloaded 檢查）。
        /// </summary>
        /// <returns>若元素仍在視覺樹中則為 true，否則為 false。</returns>
        bool IsInVisualTree();

        /// <summary>
        ///     檢查此 Region 元素是否與另一個 Region 元素包裝相同的底層 UI 元素。
        /// </summary>
        /// <param name="other">要比對的另一個 Region 元素。</param>
        /// <returns>若兩者包裝相同的底層元素則為 true，否則為 false。</returns>
        /// <remarks>
        ///     此方法用於判斷兩個不同的 IRegionElement 實例是否代表同一個 UI 元素，
        ///     避免重複註冊相同的元素到 Region。
        /// </remarks>
        bool IsSameElement(IRegionElement other);
    }
}