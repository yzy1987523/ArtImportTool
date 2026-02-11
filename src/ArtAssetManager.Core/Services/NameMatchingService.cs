using System;
using System.Collections.Generic;
using System.Linq;

namespace ArtAssetManager.Core.Services
{
    public class NameMatchingService
    {
        public class MatchResult
        {
            public string AssetId { get; set; }
            public string AssetName { get; set; }
            public int Distance { get; set; }
            public double Similarity { get; set; }
            public bool IsExactMatch { get; set; }
        }

        /// <summary>
        /// 查找最佳匹配的资源
        /// </summary>
        public MatchResult FindBestMatch(string targetName, List<(string Id, string Name)> candidates, 
                                        int maxDistance = 3)
        {
            if (string.IsNullOrEmpty(targetName) || candidates == null || candidates.Count == 0)
            {
                return null;
            }

            var results = new List<MatchResult>();

            foreach (var candidate in candidates)
            {
                var distance = LevenshteinDistance(targetName, candidate.Name);
                var maxLen = Math.Max(targetName.Length, candidate.Name.Length);
                var similarity = 1.0 - (double)distance / maxLen;

                results.Add(new MatchResult
                {
                    AssetId = candidate.Id,
                    AssetName = candidate.Name,
                    Distance = distance,
                    Similarity = similarity,
                    IsExactMatch = distance == 0
                });
            }

            // 按相似度排序
            var bestMatch = results.OrderBy(r => r.Distance).First();

            // 如果距离太大，返回null
            if (bestMatch.Distance > maxDistance && !bestMatch.IsExactMatch)
            {
                return null;
            }

            return bestMatch;
        }

        /// <summary>
        /// 计算Levenshtein距离（编辑距离）
        /// </summary>
        public int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.IsNullOrEmpty(target) ? 0 : target.Length;
            }

            if (string.IsNullOrEmpty(target))
            {
                return source.Length;
            }

            var sourceLength = source.Length;
            var targetLength = target.Length;
            var distance = new int[sourceLength + 1, targetLength + 1];

            // 初始化第一行和第一列
            for (var i = 0; i <= sourceLength; i++)
            {
                distance[i, 0] = i;
            }

            for (var j = 0; j <= targetLength; j++)
            {
                distance[0, j] = j;
            }

            // 计算编辑距离
            for (var i = 1; i <= sourceLength; i++)
            {
                for (var j = 1; j <= targetLength; j++)
                {
                    var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                    distance[i, j] = Math.Min(
                        Math.Min(
                            distance[i - 1, j] + 1,      // 删除
                            distance[i, j - 1] + 1),     // 插入
                        distance[i - 1, j - 1] + cost);  // 替换
                }
            }

            return distance[sourceLength, targetLength];
        }

        /// <summary>
        /// 规范化文件名（移除扩展名和特殊字符）
        /// </summary>
        public string NormalizeName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            // 移除扩展名
            var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);

            // 转换为小写
            var normalized = nameWithoutExt.ToLowerInvariant();

            // 移除常见的风格后缀
            var suffixes = new[] { "_cartoon", "_realistic", "_pixel", "_org", "_original" };
            foreach (var suffix in suffixes)
            {
                if (normalized.EndsWith(suffix))
                {
                    normalized = normalized.Substring(0, normalized.Length - suffix.Length);
                }
            }

            return normalized;
        }
    }
}
