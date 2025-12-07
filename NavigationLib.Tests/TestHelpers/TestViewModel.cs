using NavigationLib.Entities;

namespace NavigationLib.Tests.TestHelpers
{
    /// <summary>
    /// Test implementation of INavigableViewModel for testing navigation scenarios.
    /// </summary>
    public class TestViewModel : INavigableViewModel
    {
        public NavigationContext LastReceivedContext { get; private set; }
        public int NavigationCallCount { get; private set; }
        public bool ShouldThrowException { get; set; }

        public void OnNavigation(NavigationContext context)
        {
            NavigationCallCount++;
            LastReceivedContext = context;

            if (ShouldThrowException)
            {
                throw new System.InvalidOperationException("Test exception from OnNavigation");
            }
        }

        public void Reset()
        {
            LastReceivedContext = null;
            NavigationCallCount = 0;
            ShouldThrowException = false;
        }
    }
}
