using System;
using System.Runtime.Serialization;

namespace NavigationLib.Entities.Exceptions
{
    /// <summary>
    /// 當導航路徑無效時拋出的例外。
    /// </summary>
    /// <remarks>
    /// 路徑無效的情況包括：
    /// <list type="bullet">
    /// <item><description>路徑為 null 或空字串</description></item>
    /// <item><description>路徑段落包含無效字元（不符合 [a-zA-Z0-9_-]+ 模式）</description></item>
    /// <item><description>路徑格式錯誤（例如包含連續的斜線）</description></item>
    /// </list>
    /// </remarks>
    [Serializable]
    public class InvalidPathException : Exception
    {
        /// <summary>
        /// 取得導致例外的無效路徑。
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// 取得路徑無效的原因描述。
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// 初始化 InvalidPathException 的新執行個體。
        /// </summary>
        public InvalidPathException()
            : base("The navigation path is invalid.")
        {
        }

        /// <summary>
        /// 使用指定的錯誤訊息初始化 InvalidPathException 的新執行個體。
        /// </summary>
        /// <param name="message">說明錯誤的訊息。</param>
        public InvalidPathException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// 使用指定的錯誤訊息和內部例外初始化 InvalidPathException 的新執行個體。
        /// </summary>
        /// <param name="message">說明錯誤的訊息。</param>
        /// <param name="innerException">造成目前例外的例外。</param>
        public InvalidPathException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// 使用指定的路徑和原因初始化 InvalidPathException 的新執行個體。
        /// </summary>
        /// <param name="path">無效的導航路徑。</param>
        /// <param name="reason">路徑無效的原因。</param>
        public InvalidPathException(string path, string reason)
            : base(FormatMessage(path, reason))
        {
            Path = path;
            Reason = reason;
        }

        /// <summary>
        /// 使用序列化資料初始化 InvalidPathException 的新執行個體。
        /// </summary>
        /// <param name="info">SerializationInfo，包含序列化物件資料。</param>
        /// <param name="context">StreamingContext，包含來源和目的端的內容資訊。</param>
        protected InvalidPathException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Path = info.GetString(nameof(Path));
            Reason = info.GetString(nameof(Reason));
        }

        /// <summary>
        /// 設定 SerializationInfo，包含例外的相關資料。
        /// </summary>
        /// <param name="info">SerializationInfo，用於存放序列化物件資料。</param>
        /// <param name="context">StreamingContext，包含來源和目的端的內容資訊。</param>
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
