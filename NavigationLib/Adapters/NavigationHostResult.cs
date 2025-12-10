using System;

namespace NavigationLib.Adapters
{
    /// <summary>
    ///     表示導航操作的結果（OHS 對外契約）。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         此類別是 NavigationHost (Open Host Service) 對外公開的導航結果模型。
    ///         所有屬性為不可變（immutable），僅能透過建構子初始化。
    ///     </para>
    ///     <para>
    ///         外部使用者透過 NavigationHost.RequestNavigate 的 callback 接收此型別，
    ///         用於判斷導航是否成功，以及失敗時的詳細資訊。
    ///     </para>
    /// </remarks>
    public class NavigationHostResult
    {
        /// <summary>
        ///     初始化 NavigationHostResult 的新執行個體。
        /// </summary>
        /// <param name="success">導航是否成功。</param>
        /// <param name="failedAtSegment">失敗的段落名稱（成功時為 null）。</param>
        /// <param name="errorMessage">錯誤訊息（成功時為 null）。</param>
        /// <param name="exception">例外物件（若有）。</param>
        public NavigationHostResult(
            bool success,
            string failedAtSegment,
            string errorMessage,
            Exception exception)
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
        ///     傳回代表目前物件的字串。
        /// </summary>
        /// <returns>包含導航結果詳細資訊的字串。</returns>
        public override string ToString()
        {
            if (Success)
            {
                return "NavigationHostResult: Success";
            }

            return $"NavigationHostResult: Failed at '{FailedAtSegment ?? "(unknown)"}' - {ErrorMessage ?? "(no message)"}";
        }
    }
}
