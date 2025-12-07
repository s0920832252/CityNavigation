using System;
using System.Linq;

namespace NavigationLib.Entities
{
    /// <summary>
    /// 表示導航上下文，包含導航請求的所有相關資訊。
    /// 此類別為不可變（immutable），所有屬性僅能透過建構子初始化。
    /// </summary>
    /// <remarks>
    /// NavigationContext 會傳遞給每個 ViewModel 的 OnNavigation 方法，
    /// 提供該段落在整個導航路徑中的位置資訊以及相關參數。
    /// </remarks>
    public class NavigationContext
    {
        /// <summary>
        /// 取得完整的導航路徑（例如："Shell/Level1/Level2"）。
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// 取得當前段落在路徑中的索引（從 0 開始）。
        /// </summary>
        /// <remarks>
        /// 例如，路徑 "Shell/Level1/Level2" 的段落索引分別為 0、1、2。
        /// </remarks>
        public int SegmentIndex { get; }

        /// <summary>
        /// 取得當前段落的名稱（即 region 名稱）。
        /// </summary>
        /// <remarks>
        /// 例如，路徑 "Shell/Level1/Level2" 的段落名稱分別為 "Shell"、"Level1"、"Level2"。
        /// </remarks>
        public string SegmentName { get; }

        /// <summary>
        /// 取得路徑中所有段落的陣列。
        /// </summary>
        /// <remarks>
        /// 例如，路徑 "Shell/Level1/Level2" 的 AllSegments 為 ["Shell", "Level1", "Level2"]。
        /// </remarks>
        public string[] AllSegments { get; }

        /// <summary>
        /// 取得指示當前段落是否為路徑中最後一個段落的值。
        /// </summary>
        /// <remarks>
        /// 最後一個段落通常是實際的目標視圖，可能需要特殊處理（例如處理參數）。
        /// </remarks>
        public bool IsLastSegment { get; }

        /// <summary>
        /// 取得導航參數（可選）。
        /// </summary>
        /// <remarks>
        /// 此參數由呼叫 NavigationService.RequestNavigate 時傳入，可用於傳遞額外資訊給 ViewModel。
        /// </remarks>
        public object Parameter { get; }

        /// <summary>
        /// 初始化 NavigationContext 的新執行個體。
        /// </summary>
        /// <param name="fullPath">完整的導航路徑。</param>
        /// <param name="segmentIndex">當前段落的索引。</param>
        /// <param name="segmentName">當前段落的名稱。</param>
        /// <param name="allSegments">所有段落的陣列。</param>
        /// <param name="isLastSegment">是否為最後一個段落。</param>
        /// <param name="parameter">導航參數（可為 null）。</param>
        /// <exception cref="ArgumentNullException">
        /// 當 <paramref name="fullPath"/>、<paramref name="segmentName"/> 或 <paramref name="allSegments"/> 為 null 時拋出。
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 當 <paramref name="segmentIndex"/> 小於 0 時拋出。
        /// </exception>
        public NavigationContext(
            string fullPath,
            int segmentIndex,
            string segmentName,
            string[] allSegments,
            bool isLastSegment,
            object parameter)
        {
            if (fullPath == null)
                throw new ArgumentNullException(nameof(fullPath));
            if (segmentName == null)
                throw new ArgumentNullException(nameof(segmentName));
            if (allSegments == null)
                throw new ArgumentNullException(nameof(allSegments));
            if (segmentIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(segmentIndex), "Segment index must be non-negative.");

            FullPath = fullPath;
            SegmentIndex = segmentIndex;
            SegmentName = segmentName;
            AllSegments = allSegments.ToArray(); // 複製陣列以確保不可變性
            IsLastSegment = isLastSegment;
            Parameter = parameter;
        }

        /// <summary>
        /// 傳回代表目前物件的字串。
        /// </summary>
        /// <returns>包含路徑和段落索引的字串。</returns>
        public override string ToString()
        {
            return string.Format("NavigationContext: Path={0}, Segment={1} (Index {2}/{3})",
                FullPath, SegmentName, SegmentIndex, AllSegments.Length - 1);
        }
    }
}
