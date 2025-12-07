using System;
using System.Threading;
using NUnit.Framework;
using NavigationLib.UseCases;
using NavigationLib.Entities;
using NavigationLib.Entities.Exceptions;
using NavigationLib.Tests.TestHelpers;

namespace NavigationLib.Tests.UseCases
{
    [TestFixture]
    public class NavigationServiceTests
    {
        [Test]
        public void RequestNavigate_WithNullPath_InvokesCallbackWithFailure()
        {
            // Arrange
            NavigationResult receivedResult = null;
            var waitHandle = new ManualResetEvent(false);

            // Act
            NavigationService.RequestNavigate(null, callback: result =>
            {
                receivedResult = result;
                waitHandle.Set();
            });

            bool completed = waitHandle.WaitOne(TimeSpan.FromSeconds(1));

            // Assert
            Assert.That(completed, Is.True, "Callback should be invoked");
            Assert.That(receivedResult, Is.Not.Null);
            Assert.That(receivedResult.Success, Is.False);
            Assert.That(receivedResult.ErrorMessage, Does.Contain("path"));
        }

        [Test]
        public void RequestNavigate_WithInvalidPath_InvokesCallbackWithFailure()
        {
            // Arrange
            NavigationResult receivedResult = null;
            var waitHandle = new ManualResetEvent(false);

            // Act - use a path with invalid characters
            NavigationService.RequestNavigate("Invalid@Path", callback: result =>
            {
                receivedResult = result;
                waitHandle.Set();
            });

            bool completed = waitHandle.WaitOne(TimeSpan.FromSeconds(1));

            // Assert
            Assert.That(completed, Is.True);
            Assert.That(receivedResult.Success, Is.False);
        }

        [Test]
        public void RequestNavigate_WithNonExistentRegion_InvokesCallbackWithFailure()
        {
            // Arrange
            NavigationResult receivedResult = null;
            var waitHandle = new ManualResetEvent(false);

            // Act
            NavigationService.RequestNavigate("NonExistentRegion", callback: result =>
            {
                receivedResult = result;
                waitHandle.Set();
            }, timeoutMs: 500);

            bool completed = waitHandle.WaitOne(TimeSpan.FromSeconds(2));

            // Assert
            Assert.That(completed, Is.True);
            Assert.That(receivedResult.Success, Is.False);
            Assert.That(receivedResult.ErrorMessage, Does.Contain("NonExistentRegion").Or.Contain("timeout").Or.Contain("not found"));
        }

        [Test]
        public void RequestNavigate_WithSingleSegment_CallsViewModelOnNavigation()
        {
            // Arrange
            var store = RegionStore.Instance;
            string regionName = "TestRegion_" + Guid.NewGuid();
            var region = new MockRegionElement();
            var viewModel = new TestViewModel();
            region.DataContext = viewModel;

            store.Register(regionName, region);

            NavigationResult receivedResult = null;
            var waitHandle = new ManualResetEvent(false);

            // Act
            NavigationService.RequestNavigate(regionName, callback: result =>
            {
                receivedResult = result;
                waitHandle.Set();
            }, timeoutMs: 2000);

            bool completed = waitHandle.WaitOne(TimeSpan.FromSeconds(3));

            // Assert
            Assert.That(completed, Is.True, "Callback should be invoked");
            Assert.That(receivedResult, Is.Not.Null);
            Assert.That(receivedResult.Success, Is.True);
            Assert.That(viewModel.NavigationCallCount, Is.EqualTo(1));
            Assert.That(viewModel.LastReceivedContext, Is.Not.Null);
            Assert.That(viewModel.LastReceivedContext.SegmentName, Is.EqualTo(regionName));
            Assert.That(viewModel.LastReceivedContext.IsLastSegment, Is.True);

            // Cleanup
            store.Unregister(regionName, region);
        }

        [Test]
        public void RequestNavigate_WithMultipleSegments_CallsOnNavigationForEachSegment()
        {
            // Arrange
            var store = RegionStore.Instance;
            string region1 = "Region1_" + Guid.NewGuid();
            string region2 = "Region2_" + Guid.NewGuid();
            string path = region1 + "/" + region2;

            var element1 = new MockRegionElement();
            var element2 = new MockRegionElement();
            var viewModel1 = new TestViewModel();
            var viewModel2 = new TestViewModel();

            element1.DataContext = viewModel1;
            element2.DataContext = viewModel2;

            store.Register(region1, element1);
            store.Register(region2, element2);

            NavigationResult receivedResult = null;
            var waitHandle = new ManualResetEvent(false);

            // Act
            NavigationService.RequestNavigate(path, callback: result =>
            {
                receivedResult = result;
                waitHandle.Set();
            }, timeoutMs: 3000);

            bool completed = waitHandle.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(completed, Is.True);
            Assert.That(receivedResult.Success, Is.True);
            Assert.That(viewModel1.NavigationCallCount, Is.EqualTo(1));
            Assert.That(viewModel2.NavigationCallCount, Is.EqualTo(1));
            Assert.That(viewModel1.LastReceivedContext.SegmentName, Is.EqualTo(region1));
            Assert.That(viewModel2.LastReceivedContext.SegmentName, Is.EqualTo(region2));
            Assert.That(viewModel1.LastReceivedContext.IsLastSegment, Is.False);
            Assert.That(viewModel2.LastReceivedContext.IsLastSegment, Is.True);

            // Cleanup
            store.Unregister(region1, element1);
            store.Unregister(region2, element2);
        }

        [Test]
        public void RequestNavigate_WithParameter_PassesParameterToViewModel()
        {
            // Arrange
            var store = RegionStore.Instance;
            string regionName = "TestRegion_" + Guid.NewGuid();
            var region = new MockRegionElement();
            var viewModel = new TestViewModel();
            region.DataContext = viewModel;
            object parameter = new { Value = 42 };

            store.Register(regionName, region);

            NavigationResult receivedResult = null;
            var waitHandle = new ManualResetEvent(false);

            // Act
            NavigationService.RequestNavigate(regionName, parameter: parameter, callback: result =>
            {
                receivedResult = result;
                waitHandle.Set();
            }, timeoutMs: 2000);

            bool completed = waitHandle.WaitOne(TimeSpan.FromSeconds(3));

            // Assert
            Assert.That(completed, Is.True);
            Assert.That(receivedResult.Success, Is.True);
            Assert.That(viewModel.LastReceivedContext.Parameter, Is.SameAs(parameter));

            // Cleanup
            store.Unregister(regionName, region);
        }

        [Test]
        public void RequestNavigate_WhenViewModelThrows_InvokesCallbackWithFailure()
        {
            // Arrange
            var store = RegionStore.Instance;
            string regionName = "TestRegion_" + Guid.NewGuid();
            var region = new MockRegionElement();
            var viewModel = new TestViewModel { ShouldThrowException = true };
            region.DataContext = viewModel;

            store.Register(regionName, region);

            NavigationResult receivedResult = null;
            var waitHandle = new ManualResetEvent(false);

            // Act
            NavigationService.RequestNavigate(regionName, callback: result =>
            {
                receivedResult = result;
                waitHandle.Set();
            }, timeoutMs: 2000);

            bool completed = waitHandle.WaitOne(TimeSpan.FromSeconds(3));

            // Assert
            Assert.That(completed, Is.True);
            Assert.That(receivedResult, Is.Not.Null);
            Assert.That(receivedResult.Success, Is.False);
            Assert.That(receivedResult.FailedAtSegment, Is.EqualTo(regionName));
            Assert.That(receivedResult.Exception, Is.Not.Null);

            // Cleanup
            store.Unregister(regionName, region);
        }

        [Test]
        public void RequestNavigate_WithDelayedDataContext_WaitsForDataContext()
        {
            // Arrange
            var store = RegionStore.Instance;
            string regionName = "TestRegion_" + Guid.NewGuid();
            var region = new MockRegionElement();
            var viewModel = new TestViewModel();

            store.Register(regionName, region);

            NavigationResult receivedResult = null;
            var waitHandle = new ManualResetEvent(false);

            // Act - Start navigation without DataContext
            NavigationService.RequestNavigate(regionName, callback: result =>
            {
                receivedResult = result;
                waitHandle.Set();
            }, timeoutMs: 3000);

            // Set DataContext after a delay
            Thread.Sleep(500);
            region.DataContext = viewModel;

            bool completed = waitHandle.WaitOne(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(completed, Is.True);
            Assert.That(receivedResult.Success, Is.True);
            Assert.That(viewModel.NavigationCallCount, Is.EqualTo(1));

            // Cleanup
            store.Unregister(regionName, region);
        }

        [Test]
        public void RequestNavigate_WithNullCallback_DoesNotThrow()
        {
            // Arrange
            var store = RegionStore.Instance;
            string regionName = "TestRegion_" + Guid.NewGuid();
            var region = new MockRegionElement();
            var viewModel = new TestViewModel();
            region.DataContext = viewModel;

            store.Register(regionName, region);

            // Act & Assert
            Assert.DoesNotThrow(() =>
                NavigationService.RequestNavigate(regionName, callback: null, timeoutMs: 1000));

            // Give it time to process
            Thread.Sleep(1500);

            // Verify navigation still happened
            Assert.That(viewModel.NavigationCallCount, Is.EqualTo(1));

            // Cleanup
            store.Unregister(regionName, region);
        }
    }
}
