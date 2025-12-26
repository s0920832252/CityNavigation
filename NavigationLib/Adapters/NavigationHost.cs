using System;
using NavigationLib.UseCases;

namespace NavigationLib.Adapters
{
    /// <summary>
    ///     Open Host Service (OHS) for navigation functionality.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         NavigationHost is the outer service layer, injecting interfaces to the Adapter layer,
    ///         conforming to the Open Host Service concept in Clean Architecture / CADDD.
    ///     </para>
    ///     <para>
    ///         External consumers should use this service rather than directly depending on the inner Use Case layer.
    ///     </para>
    ///     <para>
    ///         Follows Prism's pattern of asynchronous navigation with callback services.
    ///         The RequestNavigate method returns immediately, with results delivered via callback.
    ///     </para>
    ///     <para>
    ///         Warning: For lazily-created regions (via DataTemplate/ControlTemplate):
    ///         If a region's DataContext is attached with delay, this may cause timeout or wait until a very short time before timing out.
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
        ///     Issues a non-blocking navigation request.
        /// </summary>
        /// <param name="path">Navigation path (e.g., "Shell/Level1/Level2").</param>
        /// <param name="parameter">Navigation parameter (optional).</param>
        /// <param name="callback">Callback invoked upon completion (optional, null indicates fire-and-forget).</param>
        /// <param name="timeoutMs">Timeout in milliseconds for each segment wait, default is 10000 (10 seconds).</param>
        /// <remarks>
        ///     <para>
        ///         This method returns immediately without blocking the calling thread.
        ///         The navigation flow executes in the background and reports results via <paramref name="callback" />.
        ///     </para>
        ///     <para>
        ///         If path validation fails, failure is immediately reported via callback (synchronously on the calling thread).
        ///     </para>
        /// </remarks>
        public static void RequestNavigate(
            string path,
            object parameter = null,
            Action<NavigationHostResult> callback = null,
            int timeoutMs = DefaultTimeoutMs)
        {
            // Create inner callback to convert NavigationResult to NavigationHostResult
            Action<NavigationResult> innerCallback = null;

            if (callback != null)
            {
                innerCallback = result =>
                {
                    var hostResult = new NavigationHostResult(result.Success,
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