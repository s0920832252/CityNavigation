using System;
using System.Windows.Threading;
using NavigationLib.Adapters;

namespace NavigationLib.FrameworksAndDrivers
{
    /// <summary>
    ///     WPF implementation of IDispatcher, wrapping System.Windows.Threading.Dispatcher.
    /// </summary>
    internal class DispatcherAdapter : IDispatcher
    {
        private readonly Dispatcher _dispatcher;

        /// <summary>
        ///     Initializes a new instance of the DispatcherAdapter class.
        /// </summary>
        /// <param name="dispatcher">The WPF Dispatcher to wrap.</param>
        public DispatcherAdapter(Dispatcher dispatcher) => _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        /// <summary>
        ///     Checks whether the current thread is the UI thread.
        /// </summary>
        /// <returns>true if the current thread has access to the UI; otherwise, false.</returns>
        public bool CheckAccess() => _dispatcher.CheckAccess();

        /// <summary>
        ///     Synchronously executes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void Invoke(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_dispatcher.CheckAccess())
            {
                // Already on UI thread, execute directly
                action();
            }
            else
            {
                // Dispatch to UI thread
                _dispatcher.Invoke(action);
            }
        }

        /// <summary>
        ///     Asynchronously executes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void BeginInvoke(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _dispatcher.BeginInvoke(action);
        }
    }
}