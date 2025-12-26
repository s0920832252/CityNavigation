using System;
using System.Windows;
using NavigationLib.Adapters;
using NavigationLib.UseCases;

namespace NavigationLib.FrameworksAndDrivers
{
    /// <summary>
    ///     WPF implementation of IRegionElement, wrapping FrameworkElement.
    ///     Subscribes to the element's Unloaded event and proactively unregisters from RegionStore when the element leaves the visual tree.
    /// </summary>
    internal class RegionElementAdapter : IRegionElement, IDisposable
    {
        private readonly DispatcherAdapter _dispatcher;
        private bool _disposed;

        /// <summary>
        ///     Initializes a new instance of the RegionElementAdapter class.
        /// </summary>
        /// <param name="element">The FrameworkElement to wrap.</param>
        public RegionElementAdapter(FrameworkElement element)
        {
            Element     = element ?? throw new ArgumentNullException(nameof(element));
            _dispatcher = new DispatcherAdapter(element.Dispatcher);
        }

        /// <summary>
        ///     Gets the wrapped FrameworkElement.
        /// </summary>
        internal FrameworkElement Element { get; }

        /// <summary>
        ///     Releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // No resources require explicit disposal at this stage:
            // - Unloaded subscription is managed by RegionLifecycleManager, removed via UnloadedSubscription.Dispose()
            // - DataContextChanged subscription is managed via WeakEventManager using weak references
            //
            // The IDisposable interface is retained for:
            // - Integration with RegionStore.CleanupElement lifecycle management
            // - Future centralized handling if the Adapter adds resources requiring disposal
        }

        /// <summary>
        ///     Gets the element's DataContext.
        /// </summary>
        /// <returns>The DataContext object, or null if not set.</returns>
        public object GetDataContext() => Element.DataContext;

        /// <summary>
        ///     Subscribes to DataContext change events.
        /// </summary>
        /// <param name="handler">The handler for when DataContext changes.</param>
        public void AddDataContextChangedHandler(EventHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            DataContextChangedEventManager.AddHandler(Element, handler);
        }

        /// <summary>
        ///     Unsubscribes from DataContext change events.
        /// </summary>
        /// <param name="handler">The handler to remove.</param>
        public void RemoveDataContextChangedHandler(EventHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            DataContextChangedEventManager.RemoveHandler(Element, handler);
        }

        /// <summary>
        ///     Gets the Dispatcher for this element.
        /// </summary>
        /// <returns>The IDispatcher interface instance.</returns>
        public IDispatcher GetDispatcher() => _dispatcher;

        /// <summary>
        ///     Checks whether this element is still in the visual tree.
        /// </summary>
        /// <returns>true if the element is still in the visual tree; otherwise, false.</returns>
        public bool IsInVisualTree() => PresentationSource.FromVisual(Element) != null;

        /// <summary>
        ///     Checks whether this adapter wraps the same FrameworkElement as another adapter.
        /// </summary>
        /// <param name="other">The other Region element to compare.</param>
        /// <returns>true if both wrap the same FrameworkElement instance; otherwise, false.</returns>
        public bool IsSameElement(IRegionElement other)
        {
            if (other is RegionElementAdapter otherAdapter)
            {
                return ReferenceEquals(Element, otherAdapter.Element);
            }

            return false;
        }

        /// <summary>
        ///     Subscribes to the event when the element leaves the visual tree.
        /// </summary>
        /// <param name="handler">The handler for when the element leaves the visual tree.</param>
        /// <returns>An IDisposable instance for unsubscribing.</returns>
        public IDisposable SubscribeUnloaded(EventHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            // Wrap EventHandler as WPF's RoutedEventHandler
            EventHandler<RoutedEventArgs> routedHandler = (sender, e) => handler(sender, EventArgs.Empty);

            UnloadedEventManager.AddHandler(Element, routedHandler);

            return new UnloadedSubscription(Element, routedHandler);
        }

        /// <summary>
        ///     IDisposable implementation encapsulating Unloaded event subscription.
        /// </summary>
        private sealed class UnloadedSubscription : IDisposable
        {
            private readonly FrameworkElement _element;
            private readonly EventHandler<RoutedEventArgs> _handler;
            private bool _disposed;

            public UnloadedSubscription(FrameworkElement element, EventHandler<RoutedEventArgs> handler)
            {
                _element = element ?? throw new ArgumentNullException(nameof(element));
                _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                UnloadedEventManager.RemoveHandler(_element, _handler);
                _disposed = true;
            }
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

        /// <summary>
        ///     WeakEventManager for DataContextChanged event.
        /// </summary>
        private class DataContextChangedEventManager : WeakEventManager
        {
            private DataContextChangedEventManager()
            {
            }

            private static DataContextChangedEventManager CurrentManager
            {
                get
                {
                    var managerType = typeof(DataContextChangedEventManager);
                    var manager = (DataContextChangedEventManager)GetCurrentManager(managerType);

                    if (manager == null)
                    {
                        manager = new DataContextChangedEventManager();
                        SetCurrentManager(managerType, manager);
                    }

                    return manager;
                }
            }

            public static void AddHandler(FrameworkElement element, EventHandler handler)
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

            public static void RemoveHandler(FrameworkElement element, EventHandler handler)
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
                    element.DataContextChanged += OnDataContextChanged;
                }
            }

            protected override void StopListening(object source)
            {
                if (source is FrameworkElement element)
                {
                    element.DataContextChanged -= OnDataContextChanged;
                }
            }

            protected override ListenerList NewListenerList() => new ListenerList();

            private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) =>
                // Forward event to all registered EventHandlers (wrap DependencyPropertyChangedEventArgs as EventArgs)
                DeliverEventToList(sender, EventArgs.Empty, null);
        }
    }
}