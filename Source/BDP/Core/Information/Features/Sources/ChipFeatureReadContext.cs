using BDP.Core.Chips;
using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 芯片功能来源 Worker 的只读读取上下文。
    /// </summary>
    public sealed class ChipFeatureReadContext
    {
        /// <summary>当前芯片身份。</summary>
        public ChipInformationIdentity Identity { get; private set; }

        /// <summary>作者声明配置。</summary>
        public ChipDefinitionConfig DeclaredConfig { get; private set; }

        /// <summary>制造、武装构型等处理后的有效配置。</summary>
        public ChipDefinitionConfig EffectiveConfig { get; private set; }

        /// <summary>可选运行时状态快照。</summary>
        public ChipInformationRuntimeState Runtime { get; private set; }

        /// <summary>当前上下文中生效的表达条目 Id 集合。</summary>
        public IReadOnlyList<string> ActiveEntryIds { get; private set; }

        /// <summary>创建只读来源读取上下文。</summary>
        public ChipFeatureReadContext(
            ChipInformationIdentity identity,
            ChipDefinitionConfig declaredConfig,
            ChipDefinitionConfig effectiveConfig,
            ChipInformationRuntimeState runtime,
            IEnumerable<string> activeEntryIds = null)
        {
            Identity = identity;
            DeclaredConfig = declaredConfig;
            EffectiveConfig = effectiveConfig;
            Runtime = runtime ?? new ChipInformationRuntimeState();
            if (activeEntryIds == null)
            {
                ActiveEntryIds = null;
                return;
            }

            List<string> result = new List<string>();
            foreach (string entryId in activeEntryIds)
            {
                if (!string.IsNullOrWhiteSpace(entryId)
                    && !result.Contains(entryId))
                {
                    result.Add(entryId);
                }
            }

            ActiveEntryIds = result.AsReadOnly();
        }
    }
}
