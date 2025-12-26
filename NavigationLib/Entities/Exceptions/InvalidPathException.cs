using System;
using System.Runtime.Serialization;

namespace NavigationLib.Entities.Exceptions
{
    /// <summary>
    /// Exception thrown when a navigation path is invalid.
    /// </summary>
    /// <remarks>
    /// Invalid path conditions include:
    /// <list type="bullet">
    /// <item><description>Path is null or empty string</description></item>
    /// <item><description>Path segments contain invalid characters (not matching [a-zA-Z0-9_-]+ pattern)</description></item>
    /// <item><description>Path format is incorrect (e.g., contains consecutive slashes)</description></item>
    /// </list>
    /// </remarks>
    [Serializable]
    public class InvalidPathException : Exception
    {
        /// <summary>
        /// Gets the invalid path that caused the exception.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the description of why the path is invalid.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Initializes a new instance of InvalidPathException.
        /// </summary>
        public InvalidPathException()
            : base("The navigation path is invalid.")
        {
        }

        /// <summary>
        /// Initializes a new instance of InvalidPathException with the specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidPathException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of InvalidPathException with the specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public InvalidPathException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of InvalidPathException with the specified path and reason.
        /// </summary>
        /// <param name="path">The invalid navigation path.</param>
        /// <param name="reason">The reason why the path is invalid.</param>
        public InvalidPathException(string path, string reason)
            : base(FormatMessage(path, reason))
        {
            Path = path;
            Reason = reason;
        }

        /// <summary>
        /// Initializes a new instance of InvalidPathException with serialization data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected InvalidPathException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Path = info.GetString(nameof(Path));
            Reason = info.GetString(nameof(Reason));
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Path), Path);
            info.AddValue(nameof(Reason), Reason);
        }

        private static string FormatMessage(string path, string reason)
        {
            return string.Format("Invalid navigation path '{0}': {1}", path ?? "(null)", reason ?? "(no reason provided)");
        }
    }
}
