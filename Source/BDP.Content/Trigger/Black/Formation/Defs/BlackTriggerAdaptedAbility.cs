using System;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>黑触发器声明的一项隐藏适应能力。</summary>
    public sealed class BlackTriggerAdaptedAbility
    {
        /// <summary>适应能力的 DefName；使用字符串以支持可选 DLC 或模组。</summary>
        public string abilityDefName;

        /// <summary>可选来源包标识；为空时只按 DefName 匹配。</summary>
        public string sourcePackageId;

        /// <summary>该声明是否具备最小有效信息。</summary>
        public bool IsValid
        {
            get { return !string.IsNullOrWhiteSpace(abilityDefName); }
        }

        /// <summary>按当前运行时数据库解析能力；缺少可选内容时返回空。</summary>
        public AbilityDef ResolveLoadedDef()
        {
            return IsValid ? DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName) : null;
        }

        /// <summary>判断一项冻结来源能力是否命中本声明。</summary>
        public bool Matches(BlackTriggerAbilitySnapshot source)
        {
            if (!IsValid || source == null || !string.Equals(source.AbilityDefName, abilityDefName, StringComparison.Ordinal)) return false;
            return sourcePackageId.NullOrEmpty() || string.Equals(source.SourcePackageId, sourcePackageId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>用于重复声明校验和诊断的稳定键。</summary>
        public string StableKey
        {
            get { return (abilityDefName ?? string.Empty) + "@" + (sourcePackageId ?? string.Empty); }
        }
    }
}
