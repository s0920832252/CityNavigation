using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NavigationLib.UseCases
{
    /// <summary>
    ///     Manages the lifecycle of Regions, including automatic subscription to Unloaded events and resource cleanup.
    /// </summary>
    internal sealed class RegionLifecycleManager : IDisposable
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, IDisposable> _subscriptions;
        private bool _disposed;

        /// <summary>
        ///     Initializes a new instance of RegionLifecycleManager.
        /// </summary>
        public RegionLifecycleManager() => _subscriptions = new Dictionary<string, IDisposable>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Releases all resources and cancels all subscriptions.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (_lock)
            {
                foreach (var subscription in _subscriptions.Values)
                {
                    subscription?.Dispose();
                }

                _subscriptions.Clear();
            }

            _disposed = true;
        }

        /// <summary>
        ///     Starts managing the specified region by subscribing to its Unloaded event.
        /// </summary>
        /// <param name="regionName">Region name.</param>
        /// <param name="element">Region element.</param>
        /// <param name="onUnload">Callback invoked when the element leaves the visual tree (typically for unregistration).</param>
        public void ManageRegion(string regionName, IRegionElement element, Action<string> onUnload)
        {
            if (regionName == null)
            {
                throw new ArgumentNullException(nameof(regionName));
            }

            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            if (onUnload == null)
            {
                throw new ArgumentNullException(nameof(onUnload));
            }

            lock (_lock)
            {
                // If already managing, stop the old subscription first
                if (_subscriptions.ContainsKey(regionName))
                {
                    StopManaging(regionName);
                }

                // Subscribe to Unloaded event
                var subscription = SubscribeToUnloaded(regionName, element, onUnload);

                if (subscription != null)
                {
                    _subscriptions[regionName] = subscription;
                    Debug.WriteLine($"[RegionLifecycleManager] Started managing region '{regionName}'.");
                }
            }
        }

        /// <summary>
        ///     Stops managing the specified region and cancels event subscriptions.
        /// </summary>
        /// <param name="regionName">Region name.</param>
        public void StopManaging(string regionName)
        {
            if (regionName == null)
            {
                throw new ArgumentNullException(nameof(regionName));
            }

            lock (_lock)
            {
                if (_subscriptions.TryGetValue(regionName, out var subscription))
                {
                    subscription?.Dispose();
                    _subscriptions.Remove(regionName);
                    Debug.WriteLine($"[RegionLifecycleManager] Stopped managing region '{regionName}'.");
                }
            }
        }

        /// <summary>
        ///     Subscribes to the element's Unloaded event.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         <strong>Memory Management Notes:</strong>
        ///     </para>
        ///     <para>
        ///         The local function Handler captures the <paramref name="element"/> variable (closure),
        ///         creating a strong reference from Handler → element. Meanwhile, element.SubscribeUnloaded(Handler)
        ///         subscribes to Handler internally via WeakEventManager, creating a weak reference from element → Handler.
        ///     </para>
        ///     <para>
        ///         Although this appears to be a circular reference, because one side is a weak reference (WeakEventManager),
        ///         when external strong references to Handler are removed (e.g., via Dispose),
        ///         the GC can still properly collect these objects without causing memory leaks.
        ///     </para>
        ///     <para>
        ///         Reference relationships:
        ///         <list type="bullet">
        ///             <item>Local function Handler → element (strong reference, closure capture)</item>
        ///             <item>element → Handler (weak reference, via WeakEventManager)</item>
        ///             <item>Dispose() explicitly removes subscriptions, ensuring resource release</item>
        ///         </list>
        ///     </para>
        /// </remarks>
        private IDisposable SubscribeToUnloaded(string regionName, IRegionElement element, Action<string> onUnload)
        {
            // Local function: Handles element unload event
            void Handler(object sender, EventArgs e)
            {
                // Confirm the element has truly left the visual tree (to avoid false positives in cases like TabControl switching)
                // Note: Although element is captured, this does not cause memory leaks (see remarks above)
                if (!element.IsInVisualTree())
                {
                    Debug.WriteLine($"[RegionLifecycleManager] Region '{regionName}' element unloaded, triggering cleanup.");
                    onUnload(regionName);
                }
            }

            return element.SubscribeUnloaded(Handler);
        }
    }
}