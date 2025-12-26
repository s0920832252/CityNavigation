using System;

namespace NavigationLib.Adapters
{
    /// <summary>
    /// Interface for isolating WPF Dispatcher dependencies, used to dispatch operations to the UI thread.
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Checks whether the current thread is the UI thread.
        /// </summary>
        /// <returns>True if the current thread can access the UI; otherwise, false.</returns>
        bool CheckAccess();

        /// <summary>
        /// Synchronously executes the specified action on the UI thread.
        /// If already on the UI thread, executes directly; otherwise, dispatches through the Dispatcher.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        void Invoke(Action action);

        /// <summary>
        /// Asynchronously executes the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        void BeginInvoke(Action action);
    }
}
