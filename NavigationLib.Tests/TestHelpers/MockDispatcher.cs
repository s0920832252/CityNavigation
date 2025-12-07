using System;
using NavigationLib.Adapters;

namespace NavigationLib.Tests.TestHelpers
{
    /// <summary>
    /// Mock implementation of IDispatcher for testing.
    /// Executes actions synchronously on the current thread.
    /// </summary>
    public class MockDispatcher : IDispatcher
    {
        public bool CheckAccess()
        {
            return true;
        }

        public void Invoke(Action action)
        {
            action?.Invoke();
        }

        public void BeginInvoke(Action action)
        {
            action?.Invoke();
        }
    }
}
