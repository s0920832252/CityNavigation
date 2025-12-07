using System;
using NUnit.Framework;
using NavigationLib.Entities.Exceptions;

namespace NavigationLib.Tests.Entities.Exceptions
{
    [TestFixture]
    public class InvalidPathExceptionTests
    {
        [Test]
        public void Constructor_WithPathAndReason_SetsProperties()
        {
            // Arrange
            string path = "Invalid//Path";
            string reason = "Contains double slashes";

            // Act
            var exception = new InvalidPathException(path, reason);

            // Assert
            Assert.That(exception.Path, Is.EqualTo(path));
            Assert.That(exception.Reason, Is.EqualTo(reason));
            Assert.That(exception.Message, Does.Contain(path));
            Assert.That(exception.Message, Does.Contain(reason));
        }

        [Test]
        public void Constructor_WithMessage_CreatesException()
        {
            // Arrange
            string message = "Test message";

            // Act
            var exception = new InvalidPathException(message);

            // Assert
            Assert.That(exception.Message, Is.EqualTo(message));
        }

        [Test]
        public void Constructor_WithMessageAndInnerException_CreatesException()
        {
            // Arrange
            string message = "Test message";
            var innerException = new ArgumentException("Inner");

            // Act
            var exception = new InvalidPathException(message, innerException);

            // Assert
            Assert.That(exception.Message, Is.EqualTo(message));
            Assert.That(exception.InnerException, Is.SameAs(innerException));
        }

        [Test]
        public void Constructor_Default_CreatesException()
        {
            // Act
            var exception = new InvalidPathException();

            // Assert
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message, Is.Not.Empty);
        }
    }
}
