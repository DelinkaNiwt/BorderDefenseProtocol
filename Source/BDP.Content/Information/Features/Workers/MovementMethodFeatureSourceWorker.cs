using System.Collections.Generic;
using BDP.Core.Expressions;
using BDP.Core.Information;
using BDP.Content.Information.Features.Sources;
using RimWorld;
using Verse;

namespace BDP.Content.Information.Features.Workers
{
    /// <summary>读取机动入口声明的目标与原版有效动作参数。</summary>
    public sealed class MovementMethodFeatureSourceWorker : IChipFeatureSourceWorker
    {
        /// <summary>读取一项 Verb 或 Ability 机动方式。</summary>
        public IReadOnlyList<ChipFeatureValueContribution> Resolve(
            ChipFeatureSourceConfig source,
            ChipFeatureReadContext context)
        {
            List<ChipFeatureValueContribution> result =
                new List<ChipFeatureValueContribution>();
            MovementMethodFeatureSourceConfig movementSource =
                source as MovementMethodFeatureSourceConfig;
            ChipExpressionEntryConfig entry =
                ChipFeatureSourceWorkerSupport.ResolveEntry(movementSource, context);
            if (movementSource == null || entry == null)
            {
                return result.AsReadOnly();
            }

            VerbProperties verbProperties =
                ChipFeatureSourceWorkerSupport.ResolveVerbProperties(entry);
            TargetingParameters targetParams = verbProperties?.targetParams;
            ChipInformationSourceKind sourceKind =
                entry.EntryType == ChipExpressionEntryTypeConfig.Ability
                    ? ChipInformationSourceKind.Ability
                    : ChipInformationSourceKind.Expression;
            if (targetParams != null)
            {
                ChipFeatureSourceWorkerSupport.AddTargetTypes(
                    result, movementSource, targetParams, sourceKind);
                bool hasExternalTarget = HasExternalTarget(targetParams);
                if (hasExternalTarget)
                {
                    ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                        result, movementSource, "BDP_Property_CastRange",
                        verbProperties.range, sourceKind);
                    if (verbProperties.minRange > 0f)
                    {
                        ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                            result, movementSource, "BDP_Property_MinimumCastRange",
                            verbProperties.minRange, sourceKind);
                    }

                    ChipFeatureSourceWorkerSupport.AddSemanticBoolean(
                        result, movementSource, "BDP_Property_RequiresLineOfSight",
                        verbProperties.requireLineOfSight, sourceKind);
                }
            }

            if (verbProperties != null)
            {
                ChipFeatureSourceWorkerSupport.AddSemanticDuration(
                    result, movementSource, "BDP_Property_WarmupDuration",
                    verbProperties.warmupTime, sourceKind);
                if (verbProperties.defaultCooldownTime > 0f)
                {
                    ChipFeatureSourceWorkerSupport.AddSemanticDuration(
                        result, movementSource, "BDP_Property_ActionCooldownDuration",
                        verbProperties.defaultCooldownTime, sourceKind);
                }
            }

            return result.AsReadOnly();
        }

        /// <summary>判断目标参数是否含有自身以外的有效目标。</summary>
        private static bool HasExternalTarget(TargetingParameters targetParams)
        {
            return targetParams != null
                && (targetParams.canTargetLocations
                    || targetParams.canTargetPawns
                    || targetParams.canTargetBuildings
                    || targetParams.canTargetItems
                    || targetParams.canTargetFires
                    || targetParams.canTargetPlants
                    || targetParams.canTargetAnimals
                    || targetParams.canTargetHumans
                    || targetParams.canTargetMechs
                    || targetParams.canTargetEntities
                    || targetParams.canTargetCorpses);
        }
    }
}
