using System;
using NavigationLib.Adapters;

namespace NavigationLib.Tests.TestHelpers
{
    /// <summary>
    /// Mock implementation of IRegionElement for testing.
    /// </summary>
    public class MockRegionElement : IRegionElement
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
    }
}
