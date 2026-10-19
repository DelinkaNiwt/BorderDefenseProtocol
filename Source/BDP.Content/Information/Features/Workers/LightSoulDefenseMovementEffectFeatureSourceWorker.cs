using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Information;
using BDP.Content.Information.Features.Sources;
using RimWorld;
using Verse;

namespace BDP.Content.Information.Features.Workers
{
    /// <summary>从光魂防护形态 Hediff 阶段读取已定义的移动速度倍率。</summary>
    public sealed class LightSoulDefenseMovementEffectFeatureSourceWorker : ExtraEffectFeatureSourceWorker
    {
        /// <summary>返回当前防护 Hediff 的 MoveSpeed 因子，不复制定义中的数值。</summary>
        public override IReadOnlyList<ChipFeatureValueContribution> Resolve(
            ChipFeatureSourceConfig source,
            ChipFeatureReadContext context)
        {
            List<ChipFeatureValueContribution> result =
                new List<ChipFeatureValueContribution>();
            LightSoulDefenseMovementEffectFeatureSourceConfig effectSource =
                source as LightSoulDefenseMovementEffectFeatureSourceConfig;
            ChipExpressionEntryConfig entry =
                ChipFeatureSourceWorkerSupport.ResolveEntry(effectSource, context);
            HediffDef hediff = string.IsNullOrWhiteSpace(entry?.HediffDefName)
                ? null
                : DefDatabase<HediffDef>.GetNamedSilentFail(entry.HediffDefName);
            if (effectSource == null || hediff?.stages == null)
            {
                return result.AsReadOnly();
            }

            for (int stageIndex = 0; stageIndex < hediff.stages.Count; stageIndex++)
            {
                HediffStage stage = hediff.stages[stageIndex];
                if (stage?.statFactors == null)
                {
                    continue;
                }

                for (int factorIndex = 0; factorIndex < stage.statFactors.Count; factorIndex++)
                {
                    StatModifier factor = stage.statFactors[factorIndex];
                    if (!string.Equals(
                        factor?.stat?.defName,
                        "MoveSpeed",
                        System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                        result,
                        effectSource,
                        "BDP_Property_MovementSpeedMultiplier",
                        factor.value,
                        ChipInformationSourceKind.Hediff);
                }
            }

            return result.AsReadOnly();
        }
    }
}
