using System;
using NavigationLib.UseCases;

namespace NavigationLib.Adapters
{
    /// <summary>
    ///     Open Host Service (OHS) for navigation functionality.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         NavigationHost 是對外暴露的導航服務入口（Adapter 層），
    ///         遵循 Clean Architecture / CADDD 的 Open Host Service 模式。
    ///     </para>
    ///     <para>
    ///         外部使用者應使用此類別進行導航，而非直接存取內部的 Use Case 實作。
    ///     </para>
    ///     <para>
    ///         採用 Prism 風格的事件驅動、非阻塞導航模型。
    ///         RequestNavigate 方法立即返回，並透過 callback 回報結果。
    ///     </para>
    ///     <para>
    ///         支援延遲建立的 region（DataTemplate/ControlTemplate 情境）：
    ///         若 region 尚未註冊或 DataContext 尚未就緒，服務會等待直至就緒或 timeout。
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// NavigationHost.RequestNavigate("Shell/Level1/Level2", 
    ///     parameter: myData,
    ///     callback: result =>
    ///     {
    ///         if (result.Success)
    ///         {
    ///             Console.WriteLine("Navigation succeeded!");
    ///         }
    ///         else
    ///         {
    ///             Console.WriteLine($"Navigation failed: {result.ErrorMessage}");
    ///         }
    ///     });
    /// </code>
    /// </example>
    public static class NavigationHost
    {
        private const int DefaultTimeoutMs = 10000;

        /// <summary>
        ///     發出非阻塞的導航請求。
        /// </summary>
        /// <param name="path">導航路徑（例如："Shell/Level1/Level2"）。</param>
        /// <param name="parameter">導航參數（可選）。</param>
        /// <param name="callback">完成時的回呼（可選，null 表示 fire-and-forget）。</param>
        /// <param name="timeoutMs">每個段落的等待超時時間（毫秒），預設為 10000 (10秒)。</param>
        /// <remarks>
        ///     <para>
        ///         此方法立即返回，不會阻塞呼叫執行緒。
        ///         導航流程在背景執行，完成時會透過 <paramref name="callback" /> 回報結果。
        ///     </para>
        ///     <para>
        ///         若路徑驗證失敗，會立即透過 callback 回報失敗（在呼叫執行緒上同步執行 callback）。
        ///     </para>
        /// </remarks>
        public static void RequestNavigate(
            string path,
            object parameter = null,
            Action<NavigationHostResult> callback = null,
            int timeoutMs = DefaultTimeoutMs)
        {
            // 建立橋接 callback，將內部的 NavigationResult 轉換為對外的 NavigationHostResult
            Action<Entities.NavigationResult> innerCallback = null;

            if (callback != null)
            {
                innerCallback = result =>
                {
                    var hostResult = new NavigationHostResult(
                        result.Success,
                        result.FailedAtSegment,
                        result.ErrorMessage,
                        result.Exception);

                    callback(hostResult);
                };
            }

            NavigationService.RequestNavigate(path, parameter, innerCallback, timeoutMs);
        }
    }
}