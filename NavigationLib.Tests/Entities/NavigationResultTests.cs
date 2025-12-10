using System;
using NUnit.Framework;
using NavigationLib.UseCases;

namespace NavigationLib.Tests.Entities
{
    [TestFixture]
    public class NavigationResultTests
    {
        [Test]
        public void CreateSuccess_ReturnsSuccessResult()
        {
            // Act
            var result = NavigationResult.CreateSuccess();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.FailedAtSegment, Is.Null);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.Exception, Is.Null);
        }

        [Test]
        public void CreateFailure_WithMessage_ReturnsFailureResult()
        {
            // Arrange
            string segment = "Level1";
            string message = "Test error message";

            // Act
            var result = NavigationResult.CreateFailure(segment, message);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailedAtSegment, Is.EqualTo(segment));
            Assert.That(result.ErrorMessage, Is.EqualTo(message));
            Assert.That(result.Exception, Is.Null);
        }

        [Test]
        public void CreateFailure_WithException_ReturnsFailureResult()
        {
            // Arrange
            string segment = "Level1";
            string message = "Test error message";
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = NavigationResult.CreateFailure(segment, message, exception);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailedAtSegment, Is.EqualTo(segment));
            Assert.That(result.ErrorMessage, Is.EqualTo(message));
            Assert.That(result.Exception, Is.SameAs(exception));
        }

        [Test]
        public void CreateFailure_WithNullMessage_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                NavigationResult.CreateFailure("Level1", null));
        }

        [Test]
        public void ToString_ForSuccess_ReturnsSuccessMessage()
        {
            // Arrange
            var result = NavigationResult.CreateSuccess();

            // Act
            string str = result.ToString();

            // Assert
            Assert.That(str, Does.Contain("Success"));
        }

        [Test]
        public void ToString_ForFailure_ReturnsFailureDetails()
        {
            // Arrange
            var result = NavigationResult.CreateFailure("Level1", "Test error");

            // Act
            string str = result.ToString();

            // Assert
            Assert.That(str, Does.Contain("Failed"));
            Assert.That(str, Does.Contain("Level1"));
            Assert.That(str, Does.Contain("Test error"));
        }
    }
}
