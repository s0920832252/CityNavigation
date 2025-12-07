using System;
using System.Linq;
using System.Text.RegularExpressions;
using NavigationLib.Entities.Exceptions;

namespace NavigationLib.UseCases
{
    /// <summary>
    /// 路徑驗證器，用於驗證和解析導航路徑。
    /// </summary>
    internal static class PathValidator
    {
        private static readonly Regex SegmentPattern = new Regex(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        /// <summary>
        /// 驗證並解析導航路徑。
        /// </summary>
        /// <param name="path">要驗證的路徑。</param>
        /// <returns>解析後的段落陣列。</returns>
        /// <exception cref="InvalidPathException">當路徑無效時拋出。</exception>
        public static string[] ValidateAndParse(string path)
        {
            // 檢查 null 或空字串
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidPathException(path, "Path cannot be null or empty.");
            }

            // 分割路徑
            string[] segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // 檢查是否有有效段落
            if (segments.Length == 0)
            {
                throw new InvalidPathException(path, "Path must contain at least one valid segment.");
            }

            // 驗證每個段落
            for (int i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];

                if (string.IsNullOrWhiteSpace(segment))
                {
                    throw new InvalidPathException(path, 
                        string.Format("Segment at index {0} is empty or whitespace.", i));
                }

                if (!SegmentPattern.IsMatch(segment))
                {
                    throw new InvalidPathException(path,
                        string.Format("Segment '{0}' at index {1} contains invalid characters. Only alphanumeric characters, hyphens, and underscores are allowed.", segment, i));
                }
            }

            return segments;
        }
    }
}
