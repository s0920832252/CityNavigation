namespace NavigationLib.Entities
{
    /// <summary>
    /// ViewModel 實作此介面以接收導航請求。
    /// 當 NavigationService 處理路徑時，會對每個段落對應的 ViewModel 呼叫 OnNavigation。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 此介面是導航系統的核心契約。實作此介面的 ViewModel 將在導航流程中被呼叫，
    /// 以便準備子視圖（例如設定 TabControl 的 SelectedIndex）並執行初始化邏輯。
    /// </para>
    /// <para>
    /// <strong>重要注意事項：</strong>
    /// <list type="bullet">
    /// <item><description>OnNavigation 方法應為輕量且同步執行。</description></item>
    /// <item><description>若需要長時間操作（如載入資料），應在 OnNavigation 中啟動非同步作業，而非阻塞方法。</description></item>
    /// <item><description>此方法會在 UI 執行緒上被呼叫，因此可以安全地更新 UI 相關屬性。</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class ShellViewModel : INavigableViewModel
    /// {
    ///     public void OnNavigation(NavigationContext context)
    ///     {
    ///         // 根據路徑段落準備子視圖
    ///         if (context.SegmentIndex == 0)
    ///         {
    ///             // 這是第一個段落，可能需要設定主選單
    ///             PrepareMainView();
    ///         }
    ///         
    ///         // 如果是最後一個段落，可以處理參數
    ///         if (context.IsLastSegment &amp;&amp; context.Parameter != null)
    ///         {
    ///             ProcessParameter(context.Parameter);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface INavigableViewModel
    {
        /// <summary>
        /// 當導航到此 ViewModel 所屬的 region 時被呼叫。
        /// </summary>
        /// <param name="context">
        /// 導航上下文，包含路徑資訊、段落索引、參數等。
        /// 請參考 <see cref="NavigationContext"/> 以了解可用的屬性。
        /// </param>
        /// <remarks>
        /// <para>
        /// 此方法在 UI 執行緒上同步執行。實作時應保持輕量，避免長時間阻塞。
        /// </para>
        /// <para>
        /// 若此方法拋出例外，導航流程將中斷，並透過 NavigationResult 回報失敗。
        /// </para>
        /// </remarks>
        void OnNavigation(NavigationContext context);
    }
}
