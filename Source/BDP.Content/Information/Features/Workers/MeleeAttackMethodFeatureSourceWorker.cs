using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Expressions;
using BDP.Core.Information;
using BDP.Core.Information.Vanilla;
using BDP.Content.Information.Features.Sources;
using Verse;

namespace BDP.Content.Information.Features.Workers
{
    /// <summary>逐 Tool 读取近战方式的原版有效属性。</summary>
    public sealed class MeleeAttackMethodFeatureSourceWorker : IChipFeatureSourceWorker
    {
        /// <summary>读取来源指定的单个近战 Tool 表面。</summary>
        public IReadOnlyList<ChipFeatureValueContribution> Resolve(
            ChipFeatureSourceConfig source,
            ChipFeatureReadContext context)
        {
            List<ChipFeatureValueContribution> result =
                new List<ChipFeatureValueContribution>();
            MeleeAttackMethodFeatureSourceConfig meleeSource =
                source as MeleeAttackMethodFeatureSourceConfig;
            ChipExpressionEntryConfig entry =
                ChipFeatureSourceWorkerSupport.ResolveEntry(meleeSource, context);
            if (meleeSource == null || entry == null)
            {
                return result.AsReadOnly();
            }

            IReadOnlyList<MeleeToolSurface> surfaces =
                ChipFeatureSourceWorkerSupport.BuildMeleeToolSurfaces(entry);
            int toolIndex = meleeSource.ToolIndex;
            if (surfaces == null || toolIndex < 0 || toolIndex >= surfaces.Count)
            {
                return result.AsReadOnly();
            }

            MeleeToolSurface surface = surfaces[toolIndex];
            if (entry.Trion != null && entry.Trion.UseCost > 0f)
            {
                ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                    result, meleeSource, "BDP_Property_UseCost",
                    entry.Trion.UseCost, ChipInformationSourceKind.MeleeTool);
            }
            ChipFeatureSourceWorkerSupport.AddSemanticSustainCostBySourceCount(
                result, meleeSource, entry, ChipInformationSourceKind.MeleeTool);

            VanillaMeleeStatProjection projection;
            if (!VanillaMeleeStatProjection.TryCreate(surface, out projection))
            {
                return result.AsReadOnly();
            }

            ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                result, meleeSource, "BDP_Property_CastRange",
                surface.VerbProps.range, ChipInformationSourceKind.MeleeTool);
            ChipFeatureSourceWorkerSupport.AddSemanticDuration(
                result, meleeSource, "BDP_Property_WarmupDuration",
                projection.WarmupSeconds, ChipInformationSourceKind.MeleeTool);
            ChipFeatureSourceWorkerSupport.AddSemanticDuration(
                result, meleeSource, "BDP_Property_ActionCooldownDuration",
                projection.CooldownSeconds, ChipInformationSourceKind.MeleeTool);
            ChipFeatureSourceWorkerSupport.AddSemanticNumber(
                result, meleeSource, "BDP_Property_DamageAmount",
                projection.Damage, ChipInformationSourceKind.MeleeTool);
            if (surface.DamageDef != null)
            {
                ChipFeatureSourceWorkerSupport.AddSemanticValue(
                    result, meleeSource, "BDP_Property_DamageType",
                    ChipInformationValue.FromDefReference(typeof(DamageDef), surface.DamageDef.defName),
                    ChipInformationSourceKind.MeleeTool);
            }

            ChipFeatureSourceWorkerSupport.AddSemanticPercentage(
                result, meleeSource, "BDP_Property_ArmorPenetration",
                projection.ArmorPenetration, ChipInformationSourceKind.MeleeTool);
            ChipFeatureSourceWorkerSupport.AddSemanticInteger(
                result, meleeSource, "BDP_Property_AttackBurstCount",
                projection.HitCount, ChipInformationSourceKind.MeleeTool);
            if (projection.HitCount > 1)
            {
                ChipFeatureSourceWorkerSupport.AddSemanticDuration(
                    result, meleeSource, "BDP_Property_AttackBurstInterval",
                    projection.HitIntervalTicks / 60f,
                    ChipInformationSourceKind.MeleeTool);
            }

            return result.AsReadOnly();
        }
    }
}
