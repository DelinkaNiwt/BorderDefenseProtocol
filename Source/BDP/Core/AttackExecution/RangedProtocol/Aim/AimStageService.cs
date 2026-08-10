using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Aim
{
    /// <summary>
    /// Aim 阶段服务。
    /// 它负责收集模块贡献并按协议规则合并成正式 AimRecord。
    /// </summary>
    internal sealed class AimStageService
    {
        private readonly List<IAimStageModule> modules;
        private readonly List<IRangedStageAddonModule> addons;

        public AimStageService(IEnumerable<IAimStageModule> modules, IEnumerable<IRangedStageAddonModule> addons)
        {
            this.modules = modules != null ? new List<IAimStageModule>(modules) : new List<IAimStageModule>();
            this.addons = addons != null ? new List<IRangedStageAddonModule>(addons) : new List<IRangedStageAddonModule>();
        }

        public AimRecord Execute(RangedAttackEntry entry)
        {
            AimRecord record = new AimRecord
            {
                OriginalTarget = entry != null ? entry.Target : Verse.LocalTargetInfo.Invalid,
                FinalTarget = entry != null && entry.SemanticTarget.IsValid
                    ? entry.SemanticTarget
                    : entry != null ? entry.Target : Verse.LocalTargetInfo.Invalid,
                AccuracyFactor = 1f,
                ForcedMissRadius = 0f
            };

            for (int i = 0; i < modules.Count; i++)
            {
                IAimStageModule module = modules[i];
                AimStageContext context = new AimStageContext(entry, module as IRangedAttackModuleRuntime);
                AimContribution contribution = new AimContribution();
                module.Contribute(context, contribution);

                if (contribution.Stop.IsRequested)
                {
                    record.IsAborted = true;
                    record.AbortReason = contribution.Stop.Reason;
                    RangedModuleStageDiagnostics.LogStageStop(
                        RangedStageKind.Aim,
                        module,
                        entry != null ? entry.AttackInstanceId : null,
                        entry != null ? entry.SourceResultId : null,
                        -1,
                        contribution.Stop.Reason);
                }

                if (contribution.HasOverrideFinalTarget)
                {
                    record.FinalTarget = contribution.OverrideFinalTarget;
                }
                record.AccuracyFactor *= contribution.AccuracyFactorMultiplier;
                if (contribution.HasForcedMissRadiusCandidate)
                {
                    record.ForcedMissRadius = Mathf.Max(record.ForcedMissRadius, contribution.ForcedMissRadiusCandidate);
                }
                AppendTags(record.Tags, contribution.TagsToAppend);
            }
            RangedStageAddonDispatcher.Execute(
                addons,
                new RangedStageAddonContext(
                    RangedStageKind.Aim,
                    entry != null ? entry.Pawn : null,
                    entry?.Pawn?.Map,
                    entry != null ? entry.AttackInstanceId : null,
                    entry != null ? entry.SourceResultId : null,
                    -1,
                    null,
                    entry != null ? entry.Pawn : null,
                    entry?.Pawn?.equipment?.Primary ?? (Thing)entry?.Pawn,
                    record.FinalTarget,
                    record.FinalTarget,
                    record.FinalTarget.IsValid ? record.FinalTarget.CenterVector3 : default,
                    null,
                    default,
                    entry != null ? entry.SemanticContext : null,
                    entry?.AttackContext?.ToSnapshot()));

            return record;
        }

        private static void AppendTags(List<string> target, List<string> source)
        {
            if (target == null || source == null || source.Count == 0)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(source[i]))
                {
                    target.Add(source[i]);
                }
            }
        }
    }
}
