using System;
using NUnit.Framework;
using NavigationLib.Entities;

namespace NavigationLib.Tests.Entities
{
    [TestFixture]
    public class NavigationContextTests
    {
        [Test]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            string fullPath = "Shell/Level1/Level2";
            string[] segments = new[] { "Shell", "Level1", "Level2" };
            object parameter = new object();

            // Act
            var context = new NavigationContext(fullPath, 1, "Level1", segments, false, parameter);

            // Assert
            Assert.That(context.FullPath, Is.EqualTo(fullPath));
            Assert.That(context.SegmentIndex, Is.EqualTo(1));
            Assert.That(context.SegmentName, Is.EqualTo("Level1"));
            Assert.That(context.AllSegments, Is.EqualTo(segments));
            Assert.That(context.IsLastSegment, Is.False);
            Assert.That(context.Parameter, Is.SameAs(parameter));
        }

        [Test]
        public void Constructor_WithNullFullPath_ThrowsArgumentNullException()
        {
            // Arrange
            string[] segments = new[] { "Shell" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new NavigationContext(null, 0, "Shell", segments, true, null));
        }

        [Test]
        public void Constructor_WithNullSegmentName_ThrowsArgumentNullException()
        {
            // Arrange
            string[] segments = new[] { "Shell" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new NavigationContext("Shell", 0, null, segments, true, null));
        }

        [Test]
        public void Constructor_WithNullSegments_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new NavigationContext("Shell", 0, "Shell", null, true, null));
        }

        [Test]
        public void Constructor_WithNegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            string[] segments = new[] { "Shell" };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new NavigationContext("Shell", -1, "Shell", segments, true, null));
        }

        [Test]
        public void Constructor_CopiesSegmentsArray()
        {
            // Arrange
            string[] segments = new[] { "Shell", "Level1" };

            // Act
            var context = new NavigationContext("Shell/Level1", 0, "Shell", segments, false, null);
            segments[0] = "Modified";

            // Assert
            Assert.That(context.AllSegments[0], Is.EqualTo("Shell"), "Segments should be copied, not referenced");
        }

        [Test]
        public void IsLastSegment_WhenTrue_ReturnsTrue()
        {
            // Arrange
            string[] segments = new[] { "Shell", "Level1" };

            // Act
            var context = new NavigationContext("Shell/Level1", 1, "Level1", segments, true, null);

            // Assert
            Assert.That(context.IsLastSegment, Is.True);
        }

        [Test]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            string[] segments = new[] { "Shell", "Level1", "Level2" };
            var context = new NavigationContext("Shell/Level1/Level2", 1, "Level1", segments, false, null);

            // Act
            string result = context.ToString();

            // Assert
            Assert.That(result, Does.Contain("Shell/Level1/Level2"));
            Assert.That(result, Does.Contain("Level1"));
            Assert.That(result, Does.Contain("1"));
        }
    }
}
