using System;
using System.Runtime.Serialization;

namespace NavigationLib.Entities.Exceptions
{
    /// <summary>
    /// 當導航操作失敗時拋出的基礎例外。
    /// </summary>
    /// <remarks>
    /// 此例外作為所有導航相關例外的基底類別。
    /// 實際使用中，通常會透過 NavigationResult 回報錯誤，而非拋出此例外。
    /// </remarks>
    [Serializable]
    public class NavigationException : Exception
    {
        /// <summary>
        /// 取得導航失敗時的段落名稱（region 名稱）。
        /// </summary>
        public string SegmentName { get; }

        /// <summary>
        /// 初始化 NavigationException 的新執行個體。
        /// </summary>
        public NavigationException()
            : base("Navigation operation failed.")
        {
        }

        /// <summary>
        /// 使用指定的錯誤訊息初始化 NavigationException 的新執行個體。
        /// </summary>
        /// <param name="message">說明錯誤的訊息。</param>
        public NavigationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// 使用指定的錯誤訊息和內部例外初始化 NavigationException 的新執行個體。
        /// </summary>
        /// <param name="message">說明錯誤的訊息。</param>
        /// <param name="innerException">造成目前例外的例外。</param>
        public NavigationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// 使用指定的段落名稱和錯誤訊息初始化 NavigationException 的新執行個體。
        /// </summary>
        /// <param name="segmentName">導航失敗的段落名稱。</param>
        /// <param name="message">說明錯誤的訊息。</param>
        public NavigationException(string segmentName, string message)
            : base(FormatMessage(segmentName, message))
        {
            SegmentName = segmentName;
        }

        /// <summary>
        /// 使用指定的段落名稱、錯誤訊息和內部例外初始化 NavigationException 的新執行個體。
        /// </summary>
        /// <param name="segmentName">導航失敗的段落名稱。</param>
        /// <param name="message">說明錯誤的訊息。</param>
        /// <param name="innerException">造成目前例外的例外。</param>
        public NavigationException(string segmentName, string message, Exception innerException)
            : base(FormatMessage(segmentName, message), innerException)
        {
            SegmentName = segmentName;
        }

        /// <summary>
        /// 使用序列化資料初始化 NavigationException 的新執行個體。
        /// </summary>
        /// <param name="info">SerializationInfo，包含序列化物件資料。</param>
        /// <param name="context">StreamingContext，包含來源和目的端的內容資訊。</param>
        protected NavigationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            SegmentName = info.GetString(nameof(SegmentName));
        }

        /// <summary>
        /// 設定 SerializationInfo，包含例外的相關資料。
        /// </summary>
        /// <param name="info">SerializationInfo，用於存放序列化物件資料。</param>
        /// <param name="context">StreamingContext，包含來源和目的端的內容資訊。</param>
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
