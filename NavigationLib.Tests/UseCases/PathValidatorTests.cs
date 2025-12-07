using System;
using NUnit.Framework;
using NavigationLib.UseCases;
using NavigationLib.Entities.Exceptions;

namespace NavigationLib.Tests.UseCases
{
    [TestFixture]
    public class PathValidatorTests
    {
        [Test]
        public void ValidateAndParse_WithValidPath_ReturnsSegments()
        {
            // Arrange
            string path = "Shell/Level1/Level2";

            // Act
            string[] segments = PathValidator.ValidateAndParse(path);

            // Assert
            Assert.That(segments, Is.Not.Null);
            Assert.That(segments.Length, Is.EqualTo(3));
            Assert.That(segments[0], Is.EqualTo("Shell"));
            Assert.That(segments[1], Is.EqualTo("Level1"));
            Assert.That(segments[2], Is.EqualTo("Level2"));
        }

        [Test]
        public void ValidateAndParse_WithSingleSegment_ReturnsOneSegment()
        {
            // Arrange
            string path = "Shell";

            // Act
            string[] segments = PathValidator.ValidateAndParse(path);

            // Assert
            Assert.That(segments.Length, Is.EqualTo(1));
            Assert.That(segments[0], Is.EqualTo("Shell"));
        }

        [Test]
        public void ValidateAndParse_WithNullPath_ThrowsInvalidPathException()
        {
            // Act & Assert
            Assert.Throws<InvalidPathException>(() => PathValidator.ValidateAndParse(null));
        }

        [Test]
        public void ValidateAndParse_WithEmptyPath_ThrowsInvalidPathException()
        {
            // Act & Assert
            Assert.Throws<InvalidPathException>(() => PathValidator.ValidateAndParse(""));
        }

        [Test]
        public void ValidateAndParse_WithWhitespacePath_ThrowsInvalidPathException()
        {
            // Act & Assert
            Assert.Throws<InvalidPathException>(() => PathValidator.ValidateAndParse("   "));
        }

        [Test]
        public void ValidateAndParse_WithDoubleSlash_IgnoresEmptySegments()
        {
            // Act
            var segments = PathValidator.ValidateAndParse("Shell//Level1");
            
            // Assert
            Assert.That(segments.Length, Is.EqualTo(2));
            Assert.That(segments[0], Is.EqualTo("Shell"));
            Assert.That(segments[1], Is.EqualTo("Level1"));
        }

        [Test]
        public void ValidateAndParse_WithLeadingSlash_IgnoresEmptySegment()
        {
            // Act
            var segments = PathValidator.ValidateAndParse("/Shell");
            
            // Assert
            Assert.That(segments.Length, Is.EqualTo(1));
            Assert.That(segments[0], Is.EqualTo("Shell"));
        }

        [Test]
        public void ValidateAndParse_WithTrailingSlash_IgnoresEmptySegment()
        {
            // Act
            var segments = PathValidator.ValidateAndParse("Shell/");
            
            // Assert
            Assert.That(segments.Length, Is.EqualTo(1));
            Assert.That(segments[0], Is.EqualTo("Shell"));
        }

        [Test]
        public void ValidateAndParse_WithInvalidCharacters_ThrowsInvalidPathException()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidPathException>(() => 
                PathValidator.ValidateAndParse("Shell/Level@1"));
            
            Assert.That(exception.Reason, Does.Contain("invalid"));
        }

        [Test]
        public void ValidateAndParse_WithValidPath_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => PathValidator.ValidateAndParse("Shell/Level1"));
        }

        [Test]
        public void ValidateAndParse_WithInvalidPath_ThrowsInvalidPathException()
        {
            // Act & Assert - use invalid characters
            var exception = Assert.Throws<InvalidPathException>(() =>
                PathValidator.ValidateAndParse("Shell/Level@1"));

            Assert.That(exception.Path, Is.EqualTo("Shell/Level@1"));
            Assert.That(exception.Reason, Is.Not.Null);
        }
    }
}
