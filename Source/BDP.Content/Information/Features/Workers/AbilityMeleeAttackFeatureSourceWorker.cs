using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Information;
using BDP.Content.Information.Features.Sources;
using RimWorld;
using Verse;

namespace BDP.Content.Information.Features.Workers
{
    /// <summary>从 Ability 效果组件的权威配置读取近战方式属性。</summary>
    public sealed class AbilityMeleeAttackFeatureSourceWorker : IChipFeatureSourceWorker
    {
        /// <summary>读取指定 Ability 表达条目的第 N 个近战攻击组件。</summary>
        public IReadOnlyList<ChipFeatureValueContribution> Resolve(
            ChipFeatureSourceConfig source,
            ChipFeatureReadContext context)
        {
            List<ChipFeatureValueContribution> result =
                new List<ChipFeatureValueContribution>();
            AbilityMeleeAttackFeatureSourceConfig attackSource =
                source as AbilityMeleeAttackFeatureSourceConfig;
            ChipExpressionEntryConfig entry =
                ChipFeatureSourceWorkerSupport.ResolveEntry(attackSource, context);
            if (attackSource == null
                || entry == null
                || string.IsNullOrWhiteSpace(entry.AbilityDefName))
            {
                return result.AsReadOnly();
            }

            AbilityDef ability = DefDatabase<AbilityDef>.GetNamedSilentFail(entry.AbilityDefName);
            if (ability?.comps == null)
            {
                return result.AsReadOnly();
            }

            int effectIndex = 0;
            for (int index = 0; index < ability.comps.Count; index++)
            {
                IAbilityMeleeAttackInformation attackInformation =
                    ability.comps[index] as IAbilityMeleeAttackInformation;
                if (attackInformation == null)
                {
                    continue;
                }

                if (effectIndex++ != attackSource.EffectIndex)
                {
                    continue;
                }

                ChipInformationSourceKind sourceKind = ChipInformationSourceKind.Ability;
                ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                    result,
                    attackSource,
                    "BDP_Property_DamageAmount",
                    attackInformation.DamageAmount,
                    sourceKind);
                if (attackInformation.DamageDef != null)
                {
                    ChipFeatureSourceWorkerSupport.AddSemanticValue(
                        result,
                        attackSource,
                        "BDP_Property_DamageType",
                        ChipInformationValue.FromDefReference(
                            typeof(DamageDef), attackInformation.DamageDef.defName),
                        sourceKind);
                }

                ChipFeatureSourceWorkerSupport.AddSemanticPercentage(
                    result,
                    attackSource,
                    "BDP_Property_ArmorPenetration",
                    attackInformation.ArmorPenetration,
                    sourceKind);
                break;
            }

            return result.AsReadOnly();
        }
    }
}
