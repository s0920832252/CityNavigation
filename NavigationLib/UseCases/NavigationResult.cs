using System;

namespace NavigationLib.UseCases
{
    /// <summary>
    ///     表示導航操作的結果（Use Cases 層輸出模型）。
    ///     此類別為不可變（immutable），所有屬性僅能透過建構子或靜態工廠方法初始化。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         此類別為 Use Cases 層內部使用的結果模型，由 NavigationService 產生。
    ///         外部使用者請透過 NavigationLib.Adapters.NavigationHostResult 接收導航結果。
    ///     </para>
    ///     <para>
    ///         建議使用 <see cref="CreateSuccess" /> 和 <see cref="CreateFailure" /> 靜態方法建立實例。
    ///     </para>
    /// </remarks>
    internal class NavigationResult
    {
        /// <summary>
        ///     初始化 NavigationResult 的新執行個體。
        /// </summary>
        /// <param name="success">導航是否成功。</param>
        /// <param name="failedAtSegment">失敗的段落名稱（成功時為 null）。</param>
        /// <param name="errorMessage">錯誤訊息（成功時為 null）。</param>
        /// <param name="exception">例外物件（若有）。</param>
        public NavigationResult(bool success, string failedAtSegment, string errorMessage, Exception exception)
        {
            Success         = success;
            FailedAtSegment = failedAtSegment;
            ErrorMessage    = errorMessage;
            Exception       = exception;
        }

        /// <summary>
        ///     取得指示導航是否成功的值。
        /// </summary>
        public bool Success { get; }

        /// <summary>
        ///     取得導航失敗時的段落名稱（region 名稱）。
        ///     若導航成功，此值為 null。
        /// </summary>
        public string FailedAtSegment { get; }

        /// <summary>
        ///     取得失敗時的錯誤訊息。
        ///     若導航成功，此值為 null。
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        ///     取得導航過程中拋出的例外（若有）。
        ///     若未發生例外，此值為 null。
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     建立表示成功導航的 NavigationResult。
        /// </summary>
        /// <returns>表示成功的 NavigationResult 執行個體。</returns>
        /// <example>
        ///     <code>
        /// var result = NavigationResult.CreateSuccess();
        /// if (result.Success)
        /// {
        ///     Console.WriteLine("Navigation completed successfully.");
        /// }
        /// </code>
        /// </example>
        public static NavigationResult CreateSuccess() => new NavigationResult(true, null, null, null);

        /// <summary>
        ///     建立表示失敗導航的 NavigationResult。
        /// </summary>
        /// <param name="failedAtSegment">導航失敗的段落名稱。</param>
        /// <param name="errorMessage">描述失敗原因的訊息。</param>
        /// <param name="exception">相關的例外物件（可選）。</param>
        /// <returns>表示失敗的 NavigationResult 執行個體。</returns>
        /// <exception cref="ArgumentNullException">
        ///     當 <paramref name="errorMessage" /> 為 null 時拋出。
        /// </exception>
        /// <example>
        ///     <code>
        /// var result = NavigationResult.CreateFailure("Level1", "Region not found within timeout.");
        /// if (!result.Success)
        /// {
        ///     Console.WriteLine($"Navigation failed at {result.FailedAtSegment}: {result.ErrorMessage}");
        /// }
        /// </code>
        /// </example>
        public static NavigationResult CreateFailure(string failedAtSegment, string errorMessage, Exception exception = null)
        {
            if (errorMessage == null)
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }

            return new NavigationResult(false, failedAtSegment, errorMessage, exception);
        }

        /// <summary>
        ///     傳回代表目前物件的字串。
        /// </summary>
        /// <returns>包含導航結果詳細資訊的字串。</returns>
        public override string ToString()
        {
            if (Success)
            {
                return "NavigationResult: Success";
            }

            return $"NavigationResult: Failed at '{FailedAtSegment ?? "(unknown)"}' - {ErrorMessage ?? "(no message)"}";
        }
    }
}
