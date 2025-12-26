using System;
using System.Diagnostics;
using System.Threading;
using NavigationLib.Entities;
using NavigationLib.Entities.Exceptions;

namespace NavigationLib.UseCases
{
    /// <summary>
    ///     Internal implementation of the navigation service (Use Case layer).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Coordinates the navigation flow including path parsing, region lookup, and ViewModel invocation.
    ///     </para>
    ///     <para>
    ///         Adopts a Prism-style event-driven, non-blocking navigation model.
    ///         The RequestNavigate method returns immediately and reports results via callback.
    ///     </para>
    ///     <para>
    ///         Supports delayed region creation (DataTemplate/ControlTemplate scenarios):
    ///         If a region is not yet registered or the DataContext is not ready, the service waits until ready or timeout.
    ///     </para>
    ///     <para>
    ///         <strong>External users should call navigation functionality through NavigationLib.Adapters.NavigationHost (OHS).</strong>
    ///     </para>
    /// </remarks>
    internal static class NavigationService
    {
        private const int DefaultTimeoutMs = 10000; // 10 seconds

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
            Action<NavigationResult> callback = null,
            int timeoutMs = DefaultTimeoutMs)
        {
            // Validate path
            string[] segments;

            try
            {
                segments = PathValidator.ValidateAndParse(path);
            }
            catch (InvalidPathException ex)
            {
                // Path is invalid, immediately report failure
                InvokeCallback(callback, NavigationResult.CreateFailure(null, ex.Message, ex));
                return;
            }

            // Initialize navigation state and start non-blocking processing
            var state = new NavigationState
            {
                FullPath            = path,
                Segments            = segments,
                CurrentSegmentIndex = 0,
                Parameter           = parameter,
                Callback            = callback,
                TimeoutMs           = timeoutMs,
            };

            // Start processing the first segment
            ProcessNextSegment(state);
        }

        /// <summary>
        ///     Processes the next segment (or completes navigation).
        /// </summary>
        private static void ProcessNextSegment(NavigationState state)
        {
            if (state.CurrentSegmentIndex >= state.Segments.Length)
            {
                // All segments have been successfully processed, navigation complete
                InvokeCallback(state.Callback, NavigationResult.CreateSuccess());
                return;
            }

            var segmentName = state.Segments[state.CurrentSegmentIndex];

            // Try to get region
            if (RegionStore.Instance.TryGetRegion(segmentName, out var element))
            {
                // Region already exists, continue processing
                ProcessSegment(state, element);
            }
            else
            {
                // Region not yet registered, wait for registration
                WaitForRegionRegistration(state, segmentName);
            }
        }

        /// <summary>
        ///     Waits for region registration (using event subscription).
        /// </summary>
        private static void WaitForRegionRegistration(NavigationState state, string segmentName)
        {
            var handlerInvoked = 0; // 0 = false, 1 = true
            EventHandler<RegionEventArgs> handler = null;
            Timer timer = null;

            // Create event handler
            handler = (sender, e) =>
            {
                if (string.Equals(e.RegionName, segmentName, StringComparison.OrdinalIgnoreCase))
                {
                    // Found target region
                    if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
                    {
                        // Cleanup
                        RegionStore.Instance.RegionRegistered -= handler;
                        timer?.Dispose();

                        // Continue processing
                        ProcessSegment(state, e.Element);
                    }
                }
            };

            // Create timeout timer
            timer = new Timer(_ =>
                {
                    if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
                    {
                        // Timeout
                        RegionStore.Instance.RegionRegistered -= handler;
                        timer?.Dispose();
                        var errorMsg = $"Region '{segmentName}' was not registered within timeout ({state.TimeoutMs}ms).";

                        InvokeCallback(state.Callback,
                            NavigationResult.CreateFailure(segmentName, errorMsg));
                    }
                },
                null,
                state.TimeoutMs,
                Timeout.Infinite);

            // Subscribe to event
            RegionStore.Instance.RegionRegistered += handler;

            // Check again (to avoid race condition: region may have been registered before subscription)
            if (RegionStore.Instance.TryGetRegion(segmentName, out var element))
            {
                if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
                {
                    // Cleanup
                    RegionStore.Instance.RegionRegistered -= handler;
                    timer.Dispose();

                    // Continue processing
                    ProcessSegment(state, element);
                }
            }
        }

        /// <summary>
        ///     Processes a single segment: waits for DataContext, validates ViewModel, invokes OnNavigation.
        /// </summary>
        private static void ProcessSegment(NavigationState state, IRegionElement element)
        {
            var segmentName = state.Segments[state.CurrentSegmentIndex];

            // Check DataContext
            var dataContext = element.GetDataContext();

            if (dataContext == null)
            {
                // DataContext not yet set, wait
                WaitForDataContext(state, element, segmentName);
                return;
            }

            // Verify implementation of INavigableViewModel
            if (!(dataContext is INavigableViewModel navigableVm))
            {
                var errorMsg = string.Format("DataContext of region '{0}' does not implement INavigableViewModel. Type: {1}",
                    segmentName,
                    dataContext.GetType().FullName);
                InvokeCallback(state.Callback, NavigationResult.CreateFailure(segmentName, errorMsg));
                return;
            }

            // Create NavigationContext
            var context = new NavigationContext(state.FullPath,
                state.CurrentSegmentIndex,
                segmentName,
                state.Segments,
                state.CurrentSegmentIndex == state.Segments.Length - 1,
                state.Parameter);

            // Invoke OnNavigation on the UI thread
            var dispatcher = element.GetDispatcher();

            try
            {
                dispatcher.Invoke(() =>
                {
                    try
                    {
                        navigableVm.OnNavigation(context);

                        // Success, process next segment
                        state.CurrentSegmentIndex++;
                        ProcessNextSegment(state);
                    }
                    catch (Exception ex)
                    {
                        // OnNavigation threw exception
                        var errorMsg = $"OnNavigation threw an exception at segment '{segmentName}': {ex.Message}";

                        InvokeCallback(state.Callback,
                            NavigationResult.CreateFailure(segmentName, errorMsg, ex));
                    }
                });
            }
            catch (Exception ex)
            {
                // Dispatcher.Invoke failed
                var errorMsg = $"Failed to invoke OnNavigation on UI thread for segment '{segmentName}': {ex.Message}";

                InvokeCallback(state.Callback,
                    NavigationResult.CreateFailure(segmentName, errorMsg, ex));
            }
        }

        /// <summary>
        ///     Waits for DataContext to be set (using DataContextChanged event).
        /// </summary>
        private static void WaitForDataContext(NavigationState state, IRegionElement element, string segmentName)
        {
            var handlerInvoked = 0; // 0 = false, 1 = true
            EventHandler handler = null;
            Timer timer = null;

            // Create event handler
            handler = (sender, e) =>
            {
                if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
                {
                    // Cleanup
                    element.RemoveDataContextChangedHandler(handler);
                    timer?.Dispose();

                    // Re-process this segment
                    ProcessSegment(state, element);
                }
            };

            // Create timeout timer
            timer = new Timer(_ =>
                {
                    if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
                    {
                        // Timeout
                        element.RemoveDataContextChangedHandler(handler);
                        timer?.Dispose();

                        var errorMsg = string.Format("DataContext of region '{0}' was not set within timeout ({1}ms).",
                            segmentName,
                            state.TimeoutMs);

                        InvokeCallback(state.Callback,
                            NavigationResult.CreateFailure(segmentName, errorMsg));
                    }
                },
                null,
                state.TimeoutMs,
                Timeout.Infinite);

            // Subscribe to event
            element.AddDataContextChangedHandler(handler);

            // Check again (to avoid race condition)
            var dataContext = element.GetDataContext();

            if (dataContext != null)
            {
                if (Interlocked.Exchange(ref handlerInvoked, 1) == 0)
                {
                    // Cleanup
                    element.RemoveDataContextChangedHandler(handler);
                    timer.Dispose();

                    // Re-process this segment
                    ProcessSegment(state, element);
                }
            }
        }

        /// <summary>
        ///     Safely invokes the callback.
        /// </summary>
        private static void InvokeCallback(Action<NavigationResult> callback, NavigationResult result)
        {
            if (callback == null)
            {
                return;
            }

            try
            {
                callback(result);
            }
            catch (Exception ex)
            {
                // Callback threw exception, log but don't interrupt flow
                Debug.WriteLine("[NavigationService] Callback threw exception: {0}", ex);
            }
        }

        /// <summary>
        ///     Navigation state (internal use).
        /// </summary>
        private class NavigationState
        {
            public string FullPath { get; set; }
            public string[] Segments { get; set; }
            public int CurrentSegmentIndex { get; set; }
            public object Parameter { get; set; }
            public Action<NavigationResult> Callback { get; set; }
            public int TimeoutMs { get; set; }
        }
    }
}