using System;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片功能值来源的中性 XML 配置基类。
    /// 具体实现由 Content 或第三方程序集通过 XML Class 扩展。
    /// </summary>
    public abstract class ChipFeatureSourceConfig
    {
        /// <summary>当前功能内稳定且唯一的来源 Id。</summary>
        public string Id;

        /// <summary>读取该来源的 Worker 类型。</summary>
        public Type WorkerClass;

        /// <summary>该来源是否必须引用至少一个表达条目。</summary>
        public virtual bool RequiresExpressionEntryReference => false;

        /// <summary>单个标准表达条目 Id；为空时使用 EntryIds 多条目集合。</summary>
        public string EntryId;

        /// <summary>
        /// 可选的表达条目稳定 Id 集合。
        /// 上下文投影只用它判断来源是否在形态/姿态中生效，不推断功能身份。
        /// </summary>
        public List<string> EntryIds;

        /// <summary>按制造映射复制来源并保留派生配置字段。</summary>
        public ChipFeatureSourceConfig CloneForRemap(
            string clonedId,
            Func<string, string> entryMapper)
        {
            ChipFeatureSourceConfig clone = (ChipFeatureSourceConfig)MemberwiseClone();
            clone.Id = clonedId;
            clone.EntryId = RemapEntryId(EntryId, entryMapper);
            if (EntryIds != null)
            {
                clone.EntryIds = new List<string>(EntryIds.Count);
                for (int index = 0; index < EntryIds.Count; index++)
                {
                    clone.EntryIds.Add(RemapEntryId(EntryIds[index], entryMapper));
                }
            }

            return clone;
        }

        /// <summary>保留空引用，并对已填写的表达条目 Id 应用制造映射。</summary>
        private static string RemapEntryId(
            string entryId,
            Func<string, string> entryMapper)
        {
            return string.IsNullOrWhiteSpace(entryId) || entryMapper == null
                ? entryId
                : entryMapper(entryId);
        }
    }
}
