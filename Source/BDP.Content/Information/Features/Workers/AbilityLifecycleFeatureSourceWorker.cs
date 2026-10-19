using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Information;
using BDP.Content.Information.Features;
using BDP.Content.Information.Features.Sources;
using RimWorld;
using Verse;

namespace BDP.Content.Information.Features.Workers
{
    /// <summary>只投影能力释放外壳与来源级资源消耗。</summary>
    public sealed class AbilityLifecycleFeatureSourceWorker : IChipFeatureSourceWorker
    {
        /// <summary>读取能力冷却、效果时长与表达来源级资源信息。</summary>
        public IReadOnlyList<ChipFeatureValueContribution> Resolve(
            ChipFeatureSourceConfig source,
            ChipFeatureReadContext context)
        {
            List<ChipFeatureValueContribution> result =
                new List<ChipFeatureValueContribution>();
            AbilityLifecycleFeatureSourceConfig lifecycleSource =
                source as AbilityLifecycleFeatureSourceConfig;
            ChipExpressionEntryConfig entry =
                ChipFeatureSourceWorkerSupport.ResolveEntry(lifecycleSource, context);
            if (lifecycleSource == null || entry == null)
            {
                return result.AsReadOnly();
            }

            AbilityDef ability = string.IsNullOrWhiteSpace(entry.AbilityDefName)
                ? null
                : DefDatabase<AbilityDef>.GetNamedSilentFail(entry.AbilityDefName);
            if (ability != null)
            {
                if (ability.charges > 0)
                {
                    ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                        result, lifecycleSource, "BDP_Property_ChargeCapacity",
                        ability.charges, ChipInformationSourceKind.Ability);
                }
                if (ability.cooldownTicksRange.min == ability.cooldownTicksRange.max)
                {
                    ChipFeatureSourceWorkerSupport.AddSemanticDuration(
                        result,
                        lifecycleSource,
                        ability.cooldownPerCharge
                            ? "BDP_Property_ChargeRecoveryDuration"
                            : "BDP_Property_AbilityCooldownDuration",
                        ability.cooldownTicksRange.min / 60f,
                        ChipInformationSourceKind.Ability);
                }

                float effectDuration = ability.EffectDuration(null);
                if (effectDuration > 0f)
                {
                    ChipFeatureSourceWorkerSupport.AddSemanticDuration(
                        result,
                        lifecycleSource,
                        "BDP_Property_EffectDuration",
                        effectDuration,
                        ChipInformationSourceKind.Ability);
                }
            }

            if (entry.Trion != null)
            {
                ChipInformationSourceKind trionSourceKind =
                    ResolveTrionSourceKind(entry);
                if (entry.Trion.UseCost > 0f)
                {
                    ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                        result,
                        lifecycleSource,
                        "BDP_Property_UseCost",
                        entry.Trion.UseCost,
                        trionSourceKind);
                }

                ChipFeatureSourceWorkerSupport.AddSemanticSustainCostBySourceCount(
                    result,
                    lifecycleSource,
                    entry,
                    trionSourceKind);
            }

            return result.AsReadOnly();
        }

        /// <summary>按表达条目类型保留 Trion（能量）字段的原始来源类别。</summary>
        private static ChipInformationSourceKind ResolveTrionSourceKind(
            ChipExpressionEntryConfig entry)
        {
            if (entry?.EntryType == ChipExpressionEntryTypeConfig.Hediff)
            {
                return ChipInformationSourceKind.Hediff;
            }

            if (entry?.EntryType == ChipExpressionEntryTypeConfig.Passive)
            {
                return ChipInformationSourceKind.Passive;
            }

            return ChipInformationSourceKind.Ability;
        }
    }
}
