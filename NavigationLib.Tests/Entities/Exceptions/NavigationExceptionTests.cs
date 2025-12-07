using System;
using NUnit.Framework;
using NavigationLib.Entities.Exceptions;

namespace NavigationLib.Tests.Entities.Exceptions
{
    [TestFixture]
    public class NavigationExceptionTests
    {
        [Test]
        public void Constructor_WithSegmentAndMessage_SetsProperties()
        {
            // Arrange
            string segment = "Level1";
            string message = "Navigation failed";

            // Act
            var exception = new NavigationException(segment, message);

            // Assert
            Assert.That(exception.SegmentName, Is.EqualTo(segment));
            Assert.That(exception.Message, Does.Contain(segment));
            Assert.That(exception.Message, Does.Contain(message));
        }

        [Test]
        public void Constructor_WithSegmentMessageAndInner_SetsProperties()
        {
            // Arrange
            string segment = "Level1";
            string message = "Navigation failed";
            var innerException = new InvalidOperationException("Inner");

            // Act
            var exception = new NavigationException(segment, message, innerException);

            // Assert
            Assert.That(exception.SegmentName, Is.EqualTo(segment));
            Assert.That(exception.InnerException, Is.SameAs(innerException));
        }

        [Test]
        public void Constructor_WithMessage_CreatesException()
        {
            // Arrange
            string message = "Test message";

            // Act
            var exception = new NavigationException(message);

            // Assert
            Assert.That(exception.Message, Is.EqualTo(message));
        }

        [Test]
        public void Constructor_Default_CreatesException()
        {
            // Act
            var exception = new NavigationException();

            // Assert
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Is.Not.Empty);
        }
    }
}
