using System;
using System.Diagnostics;
using System.Windows;
using NavigationLib.UseCases;

namespace NavigationLib.FrameworksAndDrivers
{
    /// <summary>
    ///     Provides Region attached property for marking Regions in XAML.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting Region.Name on a FrameworkElement will automatically register it to RegionStore when the element is Loaded,
    ///         and unregister it when Unloaded (after confirming it has left the visual tree).
    ///     </para>
    ///     <para>
    ///         Uses WeakEventManager to manage event subscriptions and avoid memory leaks.
    ///         RegionStore manages the lifecycle of RegionElementAdapter through strong references,
    ///         and RegionLifecycleManager subscribes to the Unloaded event to automatically clean up resources.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// &lt;TabControl nav:Region.Name="Shell"&gt;
    ///   &lt;TabItem Header="Home"&gt;
    ///     &lt;local:HomeView nav:Region.Name="Home" /&gt;
    ///   &lt;/TabItem&gt;
    /// &lt;/TabControl&gt;
    /// </code>
    /// </example>
    public static class Region
    {
        /// <summary>
        ///     Region.Name attached property.
        /// </summary>
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.RegisterAttached("Name",
                typeof(string),
                typeof(Region),
                new PropertyMetadata(null, OnNameChanged));

        /// <summary>
        ///     Gets the Region name of an element.
        /// </summary>
        /// <param name="element">The target element.</param>
        /// <returns>The Region name, or null if not set.</returns>
        public static string GetName(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return (string)element.GetValue(NameProperty);
        }

        /// <summary>
        ///     Sets the Region name of an element.
        /// </summary>
        /// <param name="element">The target element.</param>
        /// <param name="value">The Region name.</param>
        public static void SetName(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(NameProperty, value);
        }

        /// <summary>
        ///     Callback when Region.Name property changes.
        /// </summary>
        private static void OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement element))
            {
                Debug.WriteLine("[Region] Region.Name can only be set on FrameworkElement.");
                return;
            }

            // Remove old event handlers
            if (e.OldValue is string oldName && !string.IsNullOrEmpty(oldName))
            {
                LoadedEventManager.RemoveHandler(element, OnElementLoaded);
                UnloadedEventManager.RemoveHandler(element, OnElementUnloaded);
            }

            // If new name is valid, register event handlers
            if (e.NewValue is string newName && !string.IsNullOrEmpty(newName))
            {
                LoadedEventManager.AddHandler(element, OnElementLoaded);
                UnloadedEventManager.AddHandler(element, OnElementUnloaded);

                // If element is already loaded, register immediately
                if (element.IsLoaded)
                {
                    RegisterRegion(element, newName);
                }
            }
        }

        /// <summary>
        ///     Handler for when element is Loaded.
        /// </summary>
        private static void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element))
            {
                return;
            }

            var regionName = GetName(element);

            if (!string.IsNullOrEmpty(regionName))
            {
                RegisterRegion(element, regionName);
            }
        }

        /// <summary>
        ///     Handler for when element is Unloaded.
        /// </summary>
        private static void OnElementUnloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element))
            {
                return;
            }

            var regionName = GetName(element);

            if (!string.IsNullOrEmpty(regionName))
            {
                // Use PresentationSource.FromVisual to check if element has truly left the visual tree
                // This avoids false unregistration due to scenarios like TabControl switching
                if (PresentationSource.FromVisual(element) == null)
                {
                    UnregisterRegion(element, regionName);
                }
                else
                {
                    Debug.WriteLine($"[Region] Element '{regionName}' Unloaded but still in visual tree. Skipping unregistration.");
                }
            }
        }

        /// <summary>
        ///     Registers Region to RegionStore.
        /// </summary>
        private static void RegisterRegion(FrameworkElement element, string regionName)
        {
            try
            {
                // Create a new adapter each time (RegionStore will determine duplication via IsSameElement)
                var adapter = new RegionElementAdapter(element);
                RegionStore.Instance.Register(regionName, adapter);
                Debug.WriteLine($"[Region] Registered region '{regionName}'.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Region] Failed to register region '{0}': {1}", regionName, ex.Message);
            }
        }

        /// <summary>
        ///     Unregisters Region from RegionStore.
        /// </summary>
        private static void UnregisterRegion(FrameworkElement element, string regionName)
        {
            try
            {
                // Call Unregister directly (RegionStore will handle cleanup)
                RegionStore.Instance.Unregister(regionName);
                Debug.WriteLine($"[Region] Unregistered region '{regionName}'.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Region] Failed to unregister region '{0}': {1}", regionName, ex.Message);
            }
        }

        /// <summary>
        ///     WeakEventManager for Loaded event.
        /// </summary>
        private class LoadedEventManager : WeakEventManager
        {
            private LoadedEventManager()
            {
            }

            private static LoadedEventManager CurrentManager
            {
                get
                {
                    var managerType = typeof(LoadedEventManager);
                    var manager = (LoadedEventManager)GetCurrentManager(managerType);

                    if (manager == null)
                    {
                        manager = new LoadedEventManager();
                        SetCurrentManager(managerType, manager);
                    }

                    return manager;
                }
            }

            public static void AddHandler(FrameworkElement element, EventHandler<RoutedEventArgs> handler)
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                CurrentManager.ProtectedAddHandler(element, handler);
            }

            public static void RemoveHandler(FrameworkElement element, EventHandler<RoutedEventArgs> handler)
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                CurrentManager.ProtectedRemoveHandler(element, handler);
            }

            protected override void StartListening(object source)
            {
                if (source is FrameworkElement element)
                {
                    element.Loaded += DeliverEvent;
                }
            }

            protected override void StopListening(object source)
            {
                if (source is FrameworkElement element)
                {
                    element.Loaded -= DeliverEvent;
                }
            }

            protected override ListenerList NewListenerList() => new ListenerList<RoutedEventArgs>();
        }

        /// <summary>
        ///     WeakEventManager for Unloaded event.
        /// </summary>
        private class UnloadedEventManager : WeakEventManager
        {
            private UnloadedEventManager()
            {
            }

            private static UnloadedEventManager CurrentManager
            {
                get
                {
                    var managerType = typeof(UnloadedEventManager);
                    var manager = (UnloadedEventManager)GetCurrentManager(managerType);

                    if (manager == null)
                    {
                        manager = new UnloadedEventManager();
                        SetCurrentManager(managerType, manager);
                    }

                    return manager;
                }
            }

            public static void AddHandler(FrameworkElement element, EventHandler<RoutedEventArgs> handler)
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                CurrentManager.ProtectedAddHandler(element, handler);
            }

            public static void RemoveHandler(FrameworkElement element, EventHandler<RoutedEventArgs> handler)
            {
                if (element == null)
                {
                    throw new ArgumentNullException(nameof(element));
                }

                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                CurrentManager.ProtectedRemoveHandler(element, handler);
            }

            protected override void StartListening(object source)
            {
                if (source is FrameworkElement element)
                {
                    element.Unloaded += DeliverEvent;
                }
            }

            protected override void StopListening(object source)
            {
                if (source is FrameworkElement element)
                {
                    element.Unloaded -= DeliverEvent;
                }
            }

            protected override ListenerList NewListenerList() => new ListenerList<RoutedEventArgs>();
        }
    }
}