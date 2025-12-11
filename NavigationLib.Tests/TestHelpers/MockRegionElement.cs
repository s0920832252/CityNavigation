using System;
using NavigationLib.Adapters;
using NavigationLib.UseCases;

namespace NavigationLib.Tests.TestHelpers
{
    /// <summary>
    /// Mock implementation of IRegionElement for testing.
    /// </summary>
    internal class MockRegionElement : IRegionElement
    {
        private object _dataContext;
        private event EventHandler _dataContextChanged;

        public MockRegionElement(object dataContext = null)
        {
            _dataContext = dataContext;
        }

        /// <summary>
        /// Helper property for convenient access to DataContext in tests.
        /// </summary>
        public object DataContext
        {
            get { return _dataContext; }
            set { SetDataContext(value); }
        }

        public object GetDataContext()
        {
            return _dataContext;
        }

        public void SetDataContext(object value)
        {
            if (_dataContext != value)
            {
                _dataContext = value;
                _dataContextChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void AddDataContextChangedHandler(EventHandler handler)
        {
            _dataContextChanged += handler;
        }

        public void RemoveDataContextChangedHandler(EventHandler handler)
        {
            _dataContextChanged -= handler;
        }

        public IDispatcher GetDispatcher()
        {
            return new MockDispatcher();
        }

        public bool IsInVisualTree()
        {
            return true;
        }

        public bool IsSameElement(IRegionElement other)
        {
            return ReferenceEquals(this, other);
        }

        public IDisposable SubscribeUnloaded(EventHandler handler)
        {
            // Mock implementation: return empty disposable
            return new MockUnloadedSubscription();
        }

        private class MockUnloadedSubscription : IDisposable
        {
            public void Dispose()
            {
                // No-op for mock
            }
        }
    }
}
