using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Prepare
{
    /// <summary>
    /// Prepare 阶段服务。
    /// 它负责把模块贡献合并成正式 PrepareRecord。
    /// </summary>
    internal sealed class PrepareStageService
    {
        private readonly List<IPrepareStageModule> modules;
        private readonly List<IRangedStageAddonModule> addons;

        public PrepareStageService(IEnumerable<IPrepareStageModule> modules, IEnumerable<IRangedStageAddonModule> addons)
        {
            this.modules = modules != null ? new List<IPrepareStageModule>(modules) : new List<IPrepareStageModule>();
            this.addons = addons != null ? new List<IRangedStageAddonModule>(addons) : new List<IRangedStageAddonModule>();
        }

        public PrepareRecord Execute(RangedAttackEntry entry, AimRecord aim)
        {
            PrepareRecord record = new PrepareRecord
            {
                ResourceCost = 0f,
                MinimumRequired = 0f,
                LockSatisfied = true
            };

            for (int i = 0; i < modules.Count; i++)
            {
                IPrepareStageModule module = modules[i];
                PrepareStageContext context = new PrepareStageContext(entry, aim, module as IRangedAttackModuleRuntime);
                PrepareContribution contribution = new PrepareContribution();
                module.Contribute(context, contribution);

                if (contribution.Stop.IsRequested)
                {
                    record.IsAborted = true;
                    record.AbortReason = contribution.Stop.Reason;
                    RangedModuleStageDiagnostics.LogStageStop(
                        RangedStageKind.Prepare,
                        module,
                        entry != null ? entry.AttackInstanceId : null,
                        entry != null ? entry.SourceResultId : null,
                        -1,
                        contribution.Stop.Reason);
                }

                record.ResourceCost += contribution.AddedResourceCost;
                if (contribution.HasMinimumRequiredCandidate)
                {
                    record.MinimumRequired = System.Math.Max(record.MinimumRequired, contribution.MinimumRequiredCandidate);
                }

                record.SkipResourceConsumption |= contribution.SkipResourceConsumption;
                if (contribution.HasWarmupTicksCandidate)
                {
                    record.WarmupTicks = System.Math.Max(record.WarmupTicks, contribution.WarmupTicksCandidate);
                }

                if (contribution.HasChargeTicksCandidate)
                {
                    record.ChargeTicks = System.Math.Max(record.ChargeTicks, contribution.ChargeTicksCandidate);
                }

                record.RequiresWarmup = record.WarmupTicks > 0;
                record.RequiresCharge = record.ChargeTicks > 0;
                record.RequiresLock |= contribution.RequiresLock;
                record.LockSatisfied &= contribution.LockSatisfied;
                AppendTags(record.Tags, contribution.TagsToAppend);
            }

            RangedStageAddonDispatcher.Execute(
                addons,
                new RangedStageAddonContext(
                    RangedStageKind.Prepare,
                    entry != null ? entry.Pawn : null,
                    entry?.Pawn?.Map,
                    entry != null ? entry.AttackInstanceId : null,
                    entry != null ? entry.SourceResultId : null,
                    -1,
                    null,
                    entry != null ? entry.Pawn : null,
                    entry?.Pawn?.equipment?.Primary ?? (Thing)entry?.Pawn,
                    aim != null ? aim.FinalTarget : LocalTargetInfo.Invalid,
                    aim != null ? aim.FinalTarget : LocalTargetInfo.Invalid,
                    aim != null && aim.FinalTarget.IsValid ? aim.FinalTarget.CenterVector3 : default,
                    null,
                    default,
                    entry != null ? entry.SemanticContext : null,
                    entry?.AttackContext?.ToSnapshot()));

            return record;
        }

        private static void AppendTags(List<string> target, List<string> source)
        {
            if (target == null || source == null)
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
