using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NavigationLib.UseCases
{
    /// <summary>
    ///     Arguments for Region registration events.
    /// </summary>
    internal class RegionEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of RegionEventArgs.
        /// </summary>
        /// <param name="regionName">The Region name.</param>
        /// <param name="element">The Region element.</param>
        public RegionEventArgs(string regionName, IRegionElement element)
        {
            RegionName = regionName;
            Element    = element;
        }

        /// <summary>
        ///     Gets the name of the Region.
        /// </summary>
        public string RegionName { get; }

        /// <summary>
        ///     Gets the associated Region element.
        /// </summary>
        public IRegionElement Element { get; }
    }

    /// <summary>
    ///     Region registration center responsible for managing all registered Regions.
    ///     Uses strong references to store Region elements, combined with an event-driven cleanup mechanism to prevent memory leaks.
    ///     This class is a thread-safe Singleton.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         RegionStore maintains Region registrations at a global scope, with each name mapping to a single active element.
    ///     </para>
    ///     <para>
    ///         Duplicate registration behavior:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>If the newly registered element is the same instance as the already registered element, it is treated as idempotent (duplicate registration is ignored)</description>
    ///             </item>
    ///             <item>
    ///                 <description>If it is a different element, the registration will be updated to the new element, and a warning will be logged to assist with diagnostics</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Memory management strategy: Uses strong references to guarantee object existence during navigation, but relies on RegionElementAdapter
    ///         to subscribe to Unloaded events and actively notify RegionStore for removal, thereby preventing memory leaks.
    ///     </para>
    /// </remarks>
    internal sealed class RegionStore
    {
        private readonly RegionLifecycleManager _lifecycleManager;
        private readonly object _lock = new object();

        private readonly Dictionary<string, IRegionElement> _regions;

        private static readonly Lazy<RegionStore> _instance =
            new Lazy<RegionStore>(() => new RegionStore());

        private RegionStore()
        {
            _regions          = new Dictionary<string, IRegionElement>(StringComparer.OrdinalIgnoreCase);
            _lifecycleManager = new RegionLifecycleManager();
        }

        /// <summary>
        ///     Gets the Singleton instance of RegionStore.
        /// </summary>
        public static RegionStore Instance => _instance.Value;

        /// <summary>
        ///     Occurs when a Region is registered.
        /// </summary>
        /// <remarks>
        ///     This event is raised on the UI thread or the thread that calls Register.
        ///     Subscribers should keep processing lightweight to avoid blocking.
        /// </remarks>
        public event EventHandler<RegionEventArgs> RegionRegistered;

        /// <summary>
        ///     Occurs when a Region is unregistered.
        /// </summary>
        /// <remarks>
        ///     This event is raised on the UI thread or the thread that calls Unregister.
        /// </remarks>
        public event EventHandler<RegionEventArgs> RegionUnregistered;

        /// <summary>
        ///     Registers a Region element.
        /// </summary>
        /// <param name="regionName">The name of the Region.</param>
        /// <param name="element">The element to register.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="regionName" /> or <paramref name="element" /> is null.
        /// </exception>
        /// <remarks>
        ///     <para>
        ///         If regionName already exists:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>If it is the same instance, ignore (idempotent)</description>
        ///             </item>
        ///             <item>
        ///                 <description>If it is a different instance, update to the new instance and log a warning</description>
        ///             </item>
        ///         </list>
        ///     </para>
        ///     <para>
        ///         This method automatically cleans up invalidated weak references.
        ///     </para>
        /// </remarks>
        public void Register(string regionName, IRegionElement element)
        {
            if (regionName == null)
            {
                throw new ArgumentNullException(nameof(regionName));
            }

            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            lock (_lock)
            {
                // Check if already exists
                if (_regions.TryGetValue(regionName, out var existingElement))
                {
                    // Already exists, check if it's the same underlying element
                    if (existingElement.IsSameElement(element))
                    {
                        // Same underlying element, ignore (idempotent)
                        Debug.WriteLine(
                            $"[RegionStore] Region '{regionName}' already registered with the same element. Ignoring duplicate registration.");
                        return;
                    }

                    // Different underlying element, clean up the old one first
                    Debug.WriteLine(
                        $"[RegionStore] Warning: Region '{regionName}' is being re-registered with a different element. Updating registration.");
                    CleanupElement(regionName, existingElement);
                } // Register the new one

                _regions[regionName] = element;

                // Start managing lifecycle (subscribe to Unloaded event)
                _lifecycleManager.ManageRegion(regionName, element, Unregister);
            }

            var eventArgs = new RegionEventArgs(regionName, element);

            // Raise event outside the lock
            OnRegionRegistered(eventArgs);
        }

        /// <summary>
        ///     Unregisters a Region element.
        /// </summary>
        /// <param name="regionName">The name of the Region.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="regionName" /> is null.
        /// </exception>
        /// <remarks>
        ///     This method stops managing the region's lifecycle and cleans up resources.
        /// </remarks>
        public void Unregister(string regionName)
        {
            if (regionName == null)
            {
                throw new ArgumentNullException(nameof(regionName));
            }

            var shouldRaiseEvent = false;
            RegionEventArgs eventArgs = null;

            lock (_lock)
            {
                if (_regions.TryGetValue(regionName, out var element))
                {
                    CleanupElement(regionName, element);
                    shouldRaiseEvent = true;
                    eventArgs        = new RegionEventArgs(regionName, element);
                }
            }

            // Raise event outside the lock
            if (shouldRaiseEvent)
            {
                OnRegionUnregistered(eventArgs);
            }
        }

        /// <summary>
        ///     Attempts to retrieve a registered Region element.
        /// </summary>
        /// <param name="regionName">The name of the Region.</param>
        /// <param name="element">If found, the corresponding element; otherwise null.</param>
        /// <returns>True if found and the element is still alive; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="regionName" /> is null.
        /// </exception>
        public bool TryGetRegion(string regionName, out IRegionElement element)
        {
            if (regionName == null)
            {
                throw new ArgumentNullException(nameof(regionName));
            }

            element = null;

            lock (_lock)
            {
                if (!_regions.TryGetValue(regionName, out var target))
                {
                    return false;
                }

                element = target;
                return true;
            }
        }

        /// <summary>
        ///     Gets all currently registered Region names (for testing/diagnostics).
        /// </summary>
        /// <returns>A collection of registered Region names.</returns>
        internal IEnumerable<string> GetRegisteredRegionNames()
        {
            lock (_lock)
            {
                return _regions.Keys.ToList();
            }
        }


        /// <summary>
        ///     Cleans up a region element and associated resources.
        /// </summary>
        /// <remarks>
        ///     This method should be called within a lock.
        /// </remarks>
        private void CleanupElement(string regionName, IRegionElement element)
        {
            // Stop managing lifecycle (unsubscribe from Unloaded)
            _lifecycleManager.StopManaging(regionName);

            // Remove from dictionary
            _regions.Remove(regionName);

            // Clean up adapter
            if (element is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Debug.WriteLine($"[RegionStore] Cleaned up region '{regionName}'.");
        }

        /// <summary>
        ///     Raises the RegionRegistered event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnRegionRegistered(RegionEventArgs e) => RegionRegistered?.Invoke(this, e);

        /// <summary>
        ///     Raises the RegionUnregistered event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnRegionUnregistered(RegionEventArgs e) => RegionUnregistered?.Invoke(this, e);
    }
}