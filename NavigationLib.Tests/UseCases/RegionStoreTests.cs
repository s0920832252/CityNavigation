using System;
using NavigationLib.Adapters;
using NavigationLib.Tests.TestHelpers;
using NavigationLib.UseCases;
using NUnit.Framework;

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
            var regionName = "TestRegion_" + Guid.NewGuid();

            // Act
            store.Register(regionName, region);
            IRegionElement retrievedRegion;
            var found = store.TryGetRegion(regionName, out retrievedRegion);

            // Assert
            Assert.That(found, Is.True);
            Assert.That(retrievedRegion, Is.SameAs(region));

            // Cleanup
            store.Unregister(regionName);
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
            var regionName = "TestRegion_" + Guid.NewGuid();

            // Act
            store.Register(regionName, region1);
            store.Register(regionName, region2);
            IRegionElement retrievedRegion;
            var found = store.TryGetRegion(regionName, out retrievedRegion);

            // Assert
            Assert.That(found, Is.True);
            Assert.That(retrievedRegion, Is.SameAs(region2), "Second registration should replace first");

            // Cleanup
            store.Unregister(regionName);
        }

        [Test]
        public void UnregisterRegion_WithExistingRegion_RemovesRegion()
        {
            // Arrange
            var store = RegionStore.Instance;
            var region = new MockRegionElement();
            var regionName = "TestRegion_" + Guid.NewGuid();
            store.Register(regionName, region);

            // Act
            store.Unregister(regionName);
            IRegionElement retrievedRegion;
            var found = store.TryGetRegion(regionName, out retrievedRegion);

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
            Assert.DoesNotThrow(() => store.Unregister("NonExistentRegion"));
        }

        [Test]
        public void TryGetRegion_WithNonExistentRegion_ReturnsFalse()
        {
            // Arrange
            var store = RegionStore.Instance;

            // Act
            IRegionElement region;
            var found = store.TryGetRegion("NonExistentRegion_" + Guid.NewGuid(), out region);

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
            var name1 = "TestRegion1_" + Guid.NewGuid();
            var name2 = "TestRegion2_" + Guid.NewGuid();

            // Act
            store.Register(name1, region1);
            store.Register(name2, region2);
            var names = store.GetRegisteredRegionNames();

            // Assert
            Assert.That(names, Does.Contain(name1));
            Assert.That(names, Does.Contain(name2));

            // Cleanup
            store.Unregister(name1);
            store.Unregister(name2);
        }


        [Test]
        public void RegionRegistered_EventFires_WhenRegionRegistered()
        {
            // Arrange
            var store = RegionStore.Instance;
            var region = new MockRegionElement();
            var regionName = "TestRegion_" + Guid.NewGuid();
            var eventFired = false;
            string firedName = null;

            EventHandler<RegionEventArgs> handler = (sender, e) =>
            {
                eventFired = true;
                firedName  = e.RegionName;
            };

            // Act
            store.RegionRegistered += handler;
            store.Register(regionName, region);

            // Assert
            Assert.That(eventFired, Is.True);
            Assert.That(firedName, Is.EqualTo(regionName));

            // Cleanup
            store.RegionRegistered -= handler;
            store.Unregister(regionName);
        }

        [Test]
        public void RegionUnregistered_EventFires_WhenRegionUnregistered()
        {
            // Arrange
            var store = RegionStore.Instance;
            var region = new MockRegionElement();
            var regionName = "TestRegion_" + Guid.NewGuid();
            var eventFired = false;
            string firedName = null;

            EventHandler<RegionEventArgs> handler = (sender, e) =>
            {
                eventFired = true;
                firedName  = e.RegionName;
            };
            store.Register(regionName, region);

            // Act
            store.RegionUnregistered += handler;
            store.Unregister(regionName);

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