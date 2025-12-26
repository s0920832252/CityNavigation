using System;
using System.Linq;

namespace NavigationLib.Entities
{
    /// <summary>
    ///     Represents the navigation context, containing all relevant information for a navigation request.
    ///     This class is immutable; all properties can only be initialized through the constructor.
    /// </summary>
    /// <remarks>
    ///     NavigationContext is passed to each ViewModel's OnNavigation method,
    ///     providing information about the segment's position in the navigation path and related parameters.
    /// </remarks>
    public class NavigationContext
    {
        /// <summary>
        ///     Initializes a new instance of NavigationContext.
        /// </summary>
        /// <param name="fullPath">The full navigation path.</param>
        /// <param name="segmentIndex">The index of the current segment.</param>
        /// <param name="segmentName">The name of the current segment.</param>
        /// <param name="allSegments">Array of all segments.</param>
        /// <param name="isLastSegment">Whether this is the last segment.</param>
        /// <param name="parameter">Navigation parameter (can be null).</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="fullPath" />, <paramref name="segmentName" />, or <paramref name="allSegments" /> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="segmentIndex" /> is less than 0.
        /// </exception>
        public NavigationContext(
            string fullPath,
            int segmentIndex,
            string segmentName,
            string[] allSegments,
            bool isLastSegment,
            object parameter)
        {
            if (segmentIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(segmentIndex), "Segment index must be non-negative.");
            }

            FullPath      = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
            SegmentIndex  = segmentIndex;
            SegmentName   = segmentName ?? throw new ArgumentNullException(nameof(segmentName));
            AllSegments   = allSegments?.ToArray() ?? throw new ArgumentNullException(nameof(allSegments)); // Copy array to ensure immutability
            IsLastSegment = isLastSegment;
            Parameter     = parameter;
        }

        /// <summary>
        ///     Gets the full navigation path (e.g., "Shell/Level1/Level2").
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        ///     Gets the index of the current segment in the path (zero-based).
        /// </summary>
        /// <remarks>
        ///     For example, the path "Shell/Level1/Level2" has segment indices 0, 1, and 2 respectively.
        /// </remarks>
        public int SegmentIndex { get; }

        /// <summary>
        ///     Gets the name of the current segment (i.e., the region name).
        /// </summary>
        /// <remarks>
        ///     For example, the path "Shell/Level1/Level2" has segment names "Shell", "Level1", and "Level2" respectively.
        /// </remarks>
        public string SegmentName { get; }

        /// <summary>
        ///     Gets the array of all segments in the path.
        /// </summary>
        /// <remarks>
        ///     For example, the path "Shell/Level1/Level2" has AllSegments = ["Shell", "Level1", "Level2"].
        /// </remarks>
        public string[] AllSegments { get; }

        /// <summary>
        ///     Gets a value indicating whether the current segment is the last segment in the path.
        /// </summary>
        /// <remarks>
        ///     The last segment is typically the actual target view and may require special handling (e.g., parameter processing).
        /// </remarks>
        public bool IsLastSegment { get; }

        /// <summary>
        ///     Gets the navigation parameter (optional).
        /// </summary>
        /// <remarks>
        ///     This parameter is passed when calling NavigationService.RequestNavigate and can be used to pass additional information to the ViewModel.
        /// </remarks>
        public object Parameter { get; }

        /// <summary>
        ///     Returns a string representing the current object.
        /// </summary>
        /// <returns>A string containing the path and segment index.</returns>
        public override string ToString() =>
            string.Format("NavigationContext: Path={0}, Segment={1} (Index {2}/{3})",
                FullPath,
                SegmentName,
                SegmentIndex,
                AllSegments.Length - 1);
    }
}