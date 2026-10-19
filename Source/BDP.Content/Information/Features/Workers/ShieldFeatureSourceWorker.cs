using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Information;
using BDP.Content.Information.Features.Sources;
using BDP.Content.Shield;
using RimWorld;
using Verse;

namespace BDP.Content.Information.Features.Workers
{
    /// <summary>从 BDP 正式护盾组件投影护盾功能属性，不展示 Hediff 技术身份。</summary>
    public sealed class ShieldFeatureSourceWorker : IChipFeatureSourceWorker
    {
        /// <summary>读取指定 HediffDef 中的 BDP 能量护盾抵挡属性。</summary>
        public IReadOnlyList<ChipFeatureValueContribution> Resolve(
            ChipFeatureSourceConfig source,
            ChipFeatureReadContext context)
        {
            List<ChipFeatureValueContribution> result =
                new List<ChipFeatureValueContribution>();
            ShieldFeatureSourceConfig shieldSource =
                source as ShieldFeatureSourceConfig;
            ChipExpressionEntryConfig entry =
                ChipFeatureSourceWorkerSupport.ResolveEntry(shieldSource, context);
            if (shieldSource == null || string.IsNullOrWhiteSpace(entry?.HediffDefName))
            {
                return result.AsReadOnly();
            }

            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(entry.HediffDefName);
            if (hediff?.comps == null)
            {
                return result.AsReadOnly();
            }

            for (int index = 0; index < hediff.comps.Count; index++)
            {
                HediffCompProperties_EnergyShield props =
                    hediff.comps[index] as HediffCompProperties_EnergyShield;
                if (props == null)
                {
                    continue;
                }

                bool stacked = ResolveStackedValues(shieldSource, entry, context);
                bool angleCheck = stacked
                    ? props.stackedEnableAngleCheck
                    : props.enableAngleCheck;
                float angle = angleCheck
                    ? stacked ? props.stackedBlockAngleRange : props.blockAngleRange
                    : 360f;
                float chance = stacked ? props.stackedBlockChance : props.blockChance;
                ChipFeatureSourceWorkerSupport.AddSemanticList(
                    result,
                    shieldSource,
                    "BDP_Property_BlockAttackTypes",
                    ResolveBlockTypes(props),
                    ChipInformationSourceKind.Extension);
                ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                    result, shieldSource, "BDP_Property_BlockArc", angle,
                    ChipInformationSourceKind.Extension);
                ChipFeatureSourceWorkerSupport.AddSemanticPercentage(
                    result, shieldSource, "BDP_Property_BlockChance", chance,
                    ChipInformationSourceKind.Extension);
                ChipFeatureSourceWorkerSupport.AddSemanticPercentage(
                    result, shieldSource, "BDP_Property_DamageAbsorptionRatio", 1f,
                    ChipInformationSourceKind.Extension);
                ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                    result, shieldSource, "BDP_Property_BlockCost",
                    props.trionCostMultiplier, ChipInformationSourceKind.Extension);
                break;
            }

            return result.AsReadOnly();
        }

        /// <summary>按当前护盾真实 CanAbsorb 判定组织稳定攻击类别键。</summary>
        private static IReadOnlyList<string> ResolveBlockTypes(
            HediffCompProperties_EnergyShield props)
        {
            List<string> result = new List<string>
            {
                "BDP_BlockAttackType_Ranged",
                "BDP_BlockAttackType_Explosive"
            };
            if (props != null && props.allowMeleeDamage)
            {
                result.Add("BDP_BlockAttackType_Melee");
            }

            return result.AsReadOnly();
        }

        /// <summary>读取当前有效配置中聚合到同一 Hediff 的活动条目数量。</summary>
        private static bool ResolveStackedValues(
            ShieldFeatureSourceConfig source,
            ChipExpressionEntryConfig entry,
            ChipFeatureReadContext context)
        {
            if (source != null && source.UseStackedValues)
            {
                return true;
            }

            if (entry == null
                || !string.Equals(
                    entry.HediffApplyModeKey,
                    "countToSeverity",
                    System.StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(entry.HediffDefName))
            {
                return false;
            }

            ChipExpressionConfig expression = context?.EffectiveConfig?.Expression
                ?? context?.DeclaredConfig?.Expression;
            if (expression?.Entries == null)
            {
                return false;
            }

            int activeMatchCount = 0;
            for (int index = 0; index < expression.Entries.Count; index++)
            {
                ChipExpressionEntryConfig candidate = expression.Entries[index];
                if (candidate == null
                    || !string.Equals(
                        candidate.HediffDefName,
                        entry.HediffDefName,
                        System.StringComparison.Ordinal)
                    || !string.Equals(
                        candidate.HediffApplyModeKey,
                        "countToSeverity",
                        System.StringComparison.OrdinalIgnoreCase)
                    || !IsEntryActive(candidate.Id, context?.ActiveEntryIds))
                {
                    continue;
                }

                activeMatchCount++;
            }

            return activeMatchCount > 1;
        }

        /// <summary>空上下文表示完整配置；否则只统计当前活动形态/姿态条目。</summary>
        private static bool IsEntryActive(
            string entryId,
            IReadOnlyList<string> activeEntryIds)
        {
            if (activeEntryIds == null)
            {
                return true;
            }

            for (int index = 0; index < activeEntryIds.Count; index++)
            {
                if (string.Equals(
                    activeEntryIds[index],
                    entryId,
                    System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
