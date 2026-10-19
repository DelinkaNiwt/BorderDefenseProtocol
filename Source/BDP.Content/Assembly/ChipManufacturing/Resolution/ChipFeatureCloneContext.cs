using System;
using System.Collections.Generic;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>
    /// 制造配置复制期间统一维护表达条目、功能实例和来源 Id 映射。
    /// </summary>
    public sealed class ChipFeatureCloneContext
    {
        private readonly Dictionary<string, string> entryIds =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> featureIds =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> sourceIds =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<string> missingEntryIds = new List<string>();

        /// <summary>未能在表达复制阶段登记的来源条目引用。</summary>
        public IReadOnlyList<string> MissingEntryIds => missingEntryIds.AsReadOnly();

        /// <summary>登记一条表达条目 Id 映射。</summary>
        public void RegisterEntry(string sourceId, string cloneId)
        {
            Register(entryIds, sourceId, cloneId);
        }

        /// <summary>登记一个功能实例 Id 映射。</summary>
        public void RegisterFeature(string sourceId, string cloneId)
        {
            Register(featureIds, sourceId, cloneId);
        }

        /// <summary>登记一个功能来源 Id 映射。</summary>
        public void RegisterSource(string sourceId, string cloneId)
        {
            Register(sourceIds, sourceId, cloneId);
        }

        /// <summary>按复制上下文解析来源引用的表达条目 Id。</summary>
        public string ResolveEntry(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return sourceId;
            }

            if (entryIds.TryGetValue(sourceId, out string cloneId))
            {
                return cloneId;
            }

            if (!missingEntryIds.Contains(sourceId))
            {
                missingEntryIds.Add(sourceId);
            }

            return sourceId;
        }

        /// <summary>按复制上下文解析功能实例 Id。</summary>
        public string ResolveFeature(string sourceId)
        {
            return Resolve(featureIds, sourceId);
        }

        /// <summary>按复制上下文解析来源 Id。</summary>
        public string ResolveSource(string sourceId)
        {
            return Resolve(sourceIds, sourceId);
        }

        /// <summary>写入一个非空 Id 映射。</summary>
        private static void Register(
            Dictionary<string, string> mappings,
            string sourceId,
            string cloneId)
        {
            if (!string.IsNullOrWhiteSpace(sourceId)
                && !string.IsNullOrWhiteSpace(cloneId))
            {
                mappings[sourceId] = cloneId;
            }
        }

        /// <summary>读取映射，缺失时保留原 Id 以避免静默改写执行数据。</summary>
        private static string Resolve(
            Dictionary<string, string> mappings,
            string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return sourceId;
            }

            return mappings.TryGetValue(sourceId, out string cloneId)
                ? cloneId
                : sourceId;
        }
    }
}
