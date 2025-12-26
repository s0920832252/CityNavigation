using System;

namespace NavigationLib.Adapters
{
    /// <summary>
    ///     Represents the result of a navigation operation (returned by OHS outer layer).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class is provided by the NavigationHost (Open Host Service) outer layer for navigation results.
    ///         All properties are immutable and can only be initialized through the constructor.
    ///     </para>
    ///     <para>
    ///         External consumers receive this class through the NavigationHost.RequestNavigate callback
    ///         to determine if navigation succeeded and obtain detailed information on failures.
    ///     </para>
    /// </remarks>
    public class NavigationHostResult
    {
        /// <summary>
        ///     Initializes a new instance of NavigationHostResult.
        /// </summary>
        /// <param name="success">Whether the navigation succeeded.</param>
        /// <param name="failedAtSegment">The name of the failed region (null if successful).</param>
        /// <param name="errorMessage">The error message (null if successful).</param>
        /// <param name="exception">The exception thrown (if any).</param>
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
        ///     Gets a value indicating whether the navigation succeeded.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        ///     Gets the name of the region where navigation failed.
        ///     This value is null if navigation succeeded.
        /// </summary>
        public string FailedAtSegment { get; }

        /// <summary>
        ///     Gets the error message when navigation failed.
        ///     This value is null if navigation succeeded.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        ///     Gets the exception thrown during navigation (if any).
        ///     This value is null if no exception occurred.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///     Returns a string representing the current object.
        /// </summary>
        /// <returns>A string containing detailed information about the navigation result.</returns>
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
