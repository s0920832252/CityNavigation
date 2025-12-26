using System;
using System.Text.RegularExpressions;
using NavigationLib.Entities.Exceptions;

namespace NavigationLib.UseCases
{
    /// <summary>
    ///     Path validator used for validating and parsing navigation paths.
    /// </summary>
    internal static class PathValidator
    {
        private static readonly Regex SegmentPattern = new Regex(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        /// <summary>
        ///     Validates and parses the navigation path.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>Array of parsed segments.</returns>
        /// <exception cref="InvalidPathException">Thrown when the path is invalid.</exception>
        public static string[] ValidateAndParse(string path)
        {
            // Check for null or empty string
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidPathException(path, "Path cannot be null or empty.");
            }

            // Split the path
            var segments = path.Split(new[] { '/', }, StringSplitOptions.RemoveEmptyEntries);

            // Check if there are valid segments
            if (segments.Length == 0)
            {
                throw new InvalidPathException(path, "Path must contain at least one valid segment.");
            }

            // Validate each segment
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];

                if (string.IsNullOrWhiteSpace(segment))
                {
                    throw new InvalidPathException(path,
                        $"Segment at index {i} is empty or whitespace.");
                }

                if (!SegmentPattern.IsMatch(segment))
                {
                    throw new InvalidPathException(path,
                        $"Segment '{segment}' at index {i} contains invalid characters. Only alphanumeric characters, hyphens, and underscores are allowed.");
                }
            }

            return segments;
        }
    }
}