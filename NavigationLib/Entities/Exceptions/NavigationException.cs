using System;
using System.Runtime.Serialization;

namespace NavigationLib.Entities.Exceptions
{
    /// <summary>
    /// Base exception thrown when a navigation operation fails.
    /// </summary>
    /// <remarks>
    /// This exception serves as the base class for all navigation-related exceptions.
    /// In practice, errors are typically reported via NavigationResult rather than throwing this exception.
    /// </remarks>
    [Serializable]
    public class NavigationException : Exception
    {
        /// <summary>
        /// Gets the segment name (region name) where navigation failed.
        /// </summary>
        public string SegmentName { get; }

        /// <summary>
        /// Initializes a new instance of NavigationException.
        /// </summary>
        public NavigationException()
            : base("Navigation operation failed.")
        {
        }

        /// <summary>
        /// Initializes a new instance of NavigationException with the specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public NavigationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of NavigationException with the specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public NavigationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of NavigationException with the specified segment name and error message.
        /// </summary>
        /// <param name="segmentName">The name of the segment where navigation failed.</param>
        /// <param name="message">The message that describes the error.</param>
        public NavigationException(string segmentName, string message)
            : base(FormatMessage(segmentName, message))
        {
            SegmentName = segmentName;
        }

        /// <summary>
        /// Initializes a new instance of NavigationException with the specified segment name, error message, and inner exception.
        /// </summary>
        /// <param name="segmentName">The name of the segment where navigation failed.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public NavigationException(string segmentName, string message, Exception innerException)
            : base(FormatMessage(segmentName, message), innerException)
        {
            SegmentName = segmentName;
        }

        /// <summary>
        /// Initializes a new instance of NavigationException with serialization data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected NavigationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            SegmentName = info.GetString(nameof(SegmentName));
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(SegmentName), SegmentName);
        }

        private static string FormatMessage(string segmentName, string message)
        {
            if (string.IsNullOrEmpty(segmentName))
                return message;
            return string.Format("Navigation failed at segment '{0}': {1}", segmentName, message);
        }
    }
}
