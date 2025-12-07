using System;
using System.Linq;
using System.Threading;
using NavigationLib.Adapters;
using NUnit.Framework;
using NavigationLib.UseCases;
using NavigationLib.Tests.TestHelpers;

namespace NavigationLib.Tests.UseCases
{
    [TestFixture]
    public class RegionStoreTests
    {
        [Test]
        public void Instance_ReturnsSameInstance()
        {
            // Act
            var instance1 = RegionStore.Instance;
            var instance2 = RegionStore.Instance;

            // Assert
            Assert.That(instance1, Is.SameAs(instance2));
        }

        [Test]
        public void RegisterRegion_WithValidRegion_Succeeds()
        {
            // Arrange
            var store = RegionStore.Instance;
            var region = new MockRegionElement();
            string regionName = "TestRegion_" + Guid.NewGuid();

            // Act
            store.Register(regionName, region);
            IRegionElement retrievedRegion;
            bool found = store.TryGetRegion(regionName, out retrievedRegion);

            // Assert
            Assert.That(found, Is.True);
            Assert.That(retrievedRegion, Is.SameAs(region));

            // Cleanup
            store.Unregister(regionName, region);
        }

        [Test]
        public void RegisterRegion_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            var store = RegionStore.Instance;
            var region = new MockRegionElement();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                store.Register(null, region));
        }

        [Test]
        public void RegisterRegion_WithNullRegion_ThrowsArgumentNullException()
        {
            // Arrange
            var store = RegionStore.Instance;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                store.Register("TestRegion", null));
        }

        [Test]
        public void RegisterRegion_DuplicateName_AllowsReregistration()
        {
            // Arrange
            var store = RegionStore.Instance;
            var region1 = new MockRegionElement();
            var region2 = new MockRegionElement();
            string regionName = "TestRegion_" + Guid.NewGuid();

            // Act
            store.Register(regionName, region1);
            store.Register(regionName, region2);
            IRegionElement retrievedRegion;
            bool found = store.TryGetRegion(regionName, out retrievedRegion);

            // Assert
            Assert.That(found, Is.True);
            Assert.That(retrievedRegion, Is.SameAs(region2), "Second registration should replace first");

            // Cleanup
            store.Unregister(regionName, region2);
        }

        [Test]
        public void UnregisterRegion_WithExistingRegion_RemovesRegion()
        {
            // Arrange
            var store = RegionStore.Instance;
            var region = new MockRegionElement();
            string regionName = "TestRegion_" + Guid.NewGuid();
            store.Register(regionName, region);

            // Act
            store.Unregister(regionName, region);
            IRegionElement retrievedRegion;
            bool found = store.TryGetRegion(regionName, out retrievedRegion);

            // Assert
            Assert.That(found, Is.False);
        }

        [Test]
        public void UnregisterRegion_WithNonExistentRegion_DoesNotThrow()
        {
            // Arrange
            var store = RegionStore.Instance;
            var region = new MockRegionElement();

            // Act & Assert
            Assert.DoesNotThrow(() => store.Unregister("NonExistentRegion", region));
        }

        [Test]
        public void TryGetRegion_WithNonExistentRegion_ReturnsFalse()
        {
            // Arrange
            var store = RegionStore.Instance;

            // Act
            IRegionElement region;
            bool found = store.TryGetRegion("NonExistentRegion_" + Guid.NewGuid(), out region);

            // Assert
            Assert.That(found, Is.False);
            Assert.That(region, Is.Null);
        }

        [Test]
        public void GetRegisteredRegionNames_ReturnsRegisteredNames()
        {
            // Arrange
            var store = RegionStore.Instance;
            var region1 = new MockRegionElement();
            var region2 = new MockRegionElement();
            string name1 = "TestRegion1_" + Guid.NewGuid();
            string name2 = "TestRegion2_" + Guid.NewGuid();

            // Act
            store.Register(name1, region1);
            store.Register(name2, region2);
            var names = store.GetRegisteredRegionNames();

            // Assert
            Assert.That(names, Does.Contain(name1));
            Assert.That(names, Does.Contain(name2));

            // Cleanup
            store.Unregister(name1, region1);
            store.Unregister(name2, region2);
        }

        [Test]
        public void WeakReference_AllowsGarbageCollection()
        {
            // Arrange
            var store = RegionStore.Instance;
            string regionName = "TestRegion_" + Guid.NewGuid();

            CreateAndRegisterRegion(store, regionName);

            // Act
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Give the cleanup timer a chance to run
            Thread.Sleep(100);

            IRegionElement region;
            bool found = store.TryGetRegion(regionName, out region);

            // Assert
            Assert.That(found, Is.False, "Weak reference should allow GC");
        }

        private void CreateAndRegisterRegion(RegionStore store, string regionName)
        {
            var region = new MockRegionElement();
            store.Register(regionName, region);
        }

        [Test]
        public void RegionRegistered_EventFires_WhenRegionRegistered()
        {
            // Arrange
            var store = RegionStore.Instance;
            var region = new MockRegionElement();
            string regionName = "TestRegion_" + Guid.NewGuid();
            bool eventFired = false;
            string firedName = null;

            EventHandler<RegionEventArgs> handler = (sender, e) =>
            {
                eventFired = true;
                firedName = e.RegionName;
            };

            // Act
            store.RegionRegistered += handler;
            store.Register(regionName, region);

            // Assert
            Assert.That(eventFired, Is.True);
            Assert.That(firedName, Is.EqualTo(regionName));

            // Cleanup
            store.RegionRegistered -= handler;
            store.Unregister(regionName, region);
        }

        [Test]
        public void RegionUnregistered_EventFires_WhenRegionUnregistered()
        {
            // Arrange
            var store = RegionStore.Instance;
            var region = new MockRegionElement();
            string regionName = "TestRegion_" + Guid.NewGuid();
            bool eventFired = false;
            string firedName = null;

            EventHandler<RegionEventArgs> handler = (sender, e) =>
            {
                eventFired = true;
                firedName = e.RegionName;
            };

            store.Register(regionName, region);

            // Act
            store.RegionUnregistered += handler;
            store.Unregister(regionName, region);

            // Assert
            Assert.That(eventFired, Is.True);
            Assert.That(firedName, Is.EqualTo(regionName));

            // Assert
            Assert.That(eventFired, Is.True);
            Assert.That(firedName, Is.EqualTo(regionName));

            // Cleanup
            store.RegionUnregistered -= handler;
        }
    }
}
