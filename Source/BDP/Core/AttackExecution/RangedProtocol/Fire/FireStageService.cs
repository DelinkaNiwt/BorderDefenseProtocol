using System.Collections.Generic;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Chips;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Fire
{
    /// <summary>
    /// Fire 阶段服务。
    /// 它负责把准备完毕的一枪展开成正式 FireRecord。
    /// </summary>
    internal sealed class FireStageService
    {
        private readonly List<IFireStageModule> modules;
        private readonly List<IRangedStageAddonModule> addons;

        public FireStageService(IEnumerable<IFireStageModule> modules, IEnumerable<IRangedStageAddonModule> addons)
        {
            this.modules = modules != null ? new List<IFireStageModule>(modules) : new List<IFireStageModule>();
            this.addons = addons != null ? new List<IRangedStageAddonModule>(addons) : new List<IRangedStageAddonModule>();
        }

        public FireRecord Execute(RangedAttackEntry entry, AimRecord aim, PrepareRecord prepare)
        {
            ThingDef baselineProjectile = ResolveBaselineProjectile(entry);
            int baselineFireCount = ResolveBaselineFireCount(entry);
            FireRecord record = new FireRecord
            {
                ProjectileDef = baselineProjectile,
                FireCount = baselineFireCount > 0 ? baselineFireCount : 1
            };
            List<FireContribution> contributions = new List<FireContribution>();
            for (int i = 0; i < modules.Count; i++)
            {
                FireStageContext context = new FireStageContext(entry, aim, prepare, modules[i] as IRangedAttackModuleRuntime);
                FireContribution contribution = new FireContribution();
                modules[i].Contribute(context, contribution);
                contributions.Add(contribution);
                if (contribution.Stop.IsRequested)
                {
                    record.IsAborted = true;
                    record.AbortReason = contribution.Stop.Reason;
                    RangedModuleStageDiagnostics.LogStageStop(
                        RangedStageKind.Fire,
                        modules[i],
                        entry != null ? entry.AttackInstanceId : null,
                        entry != null ? entry.SourceResultId : null,
                        -1,
                        contribution.Stop.Reason);
                }

                if (contribution.OverrideProjectileDef != null)
                {
                    record.ProjectileDef = contribution.OverrideProjectileDef;
                }

                if (contribution.HasOverrideFireCount)
                {
                    record.FireCount = contribution.OverrideFireCount > 0 ? contribution.OverrideFireCount : 1;
                }
                AppendTags(record.Tags, contribution.TagsToAppend);
            }

            for (int emitIndex = 0; emitIndex < record.FireCount; emitIndex++)
            {
                AttackExecutionEmit baselineEmit = ResolveBaselineEmit(entry, emitIndex);
                // 读取正式规格中的中性投射物倍率覆盖，作为 emit 初始因子。
                ProjectileOverrides projectileOv = entry?.SourceResult?.ResolvedVerbSpec?.ProjectileOverrides;
                float baseSpeedFactor = projectileOv?.speedMultiplier ?? 1f;
                float baseDamageFactor = projectileOv?.damageMultiplier ?? 1f;
                FireEmitRecord emit = new FireEmitRecord
                {
                    EmitIndex = emitIndex,
                    Target = baselineEmit != null && baselineEmit.Target.IsValid
                        ? baselineEmit.Target
                        : aim != null ? aim.FinalTarget : LocalTargetInfo.Invalid,
                    SemanticTarget = baselineEmit != null && baselineEmit.SemanticTarget.IsValid
                        ? baselineEmit.SemanticTarget
                        : entry != null && entry.SemanticTarget.IsValid
                            ? entry.SemanticTarget
                            : aim != null ? aim.FinalTarget : LocalTargetInfo.Invalid,
                    OriginOffsetWorld = baselineEmit != null ? baselineEmit.OriginOffset : default,
                    HasOriginSpreadRange = baselineEmit != null && baselineEmit.HasOriginSpreadRange,
                    OriginSpreadLateralMin = baselineEmit != null ? baselineEmit.OriginSpreadLateralMin : 0f,
                    OriginSpreadLateralMax = baselineEmit != null ? baselineEmit.OriginSpreadLateralMax : 0f,
                    OriginSpreadForwardMin = baselineEmit != null ? baselineEmit.OriginSpreadForwardMin : 0f,
                    OriginSpreadForwardMax = baselineEmit != null ? baselineEmit.OriginSpreadForwardMax : 0f,
                    SpeedFactor = baseSpeedFactor,
                    DamageFactor = baseDamageFactor,
                    ProjectileOverride = baselineEmit != null ? baselineEmit.ProjectileDef : null,
                    ResultId = baselineEmit != null ? baselineEmit.ResultId : entry != null ? entry.SourceResultId : null,
                    SourceResultId = baselineEmit != null ? baselineEmit.SourceResultId : entry != null ? entry.SourceResultId : null,
                    SourceResult = baselineEmit != null ? baselineEmit.Result : entry != null ? entry.SourceResult : null,
                    SemanticContext = baselineEmit != null ? baselineEmit.SemanticContext : entry != null ? entry.SemanticContext : null,
                    OriginSide = baselineEmit != null ? baselineEmit.OriginSide : null
                };

                for (int i = 0; i < contributions.Count; i++)
                {
                    FireContribution contribution = contributions[i];
                    for (int j = 0; j < contribution.EmitContributions.Count; j++)
                    {
                        FireEmitContribution emitContribution = contribution.EmitContributions[j];
                        if (emitContribution == null || emitContribution.EmitIndex != emitIndex)
                        {
                            continue;
                        }

                        emit.OriginOffsetWorld += emitContribution.AddedOriginOffsetWorld;
                        emit.SpreadOffsetWorld += emitContribution.AddedSpreadOffsetWorld;
                        emit.SpeedFactor *= emitContribution.SpeedFactorMultiplier;
                        emit.DamageFactor *= emitContribution.DamageFactorMultiplier;
                        if (emitContribution.OverrideProjectileDef != null)
                        {
                            emit.ProjectileOverride = emitContribution.OverrideProjectileDef;
                        }

                        AppendTags(emit.Tags, emitContribution.TagsToAppend);
                    }
                }

                record.Emits.Add(emit);
            }

            LocalTargetInfo finalTarget = aim != null ? aim.FinalTarget : LocalTargetInfo.Invalid;
            RangedStageAddonDispatcher.Execute(
                addons,
                new RangedStageAddonContext(
                    RangedStageKind.Fire,
                    entry != null ? entry.Pawn : null,
                    entry?.Pawn?.Map,
                    entry != null ? entry.AttackInstanceId : null,
                    entry != null ? entry.SourceResultId : null,
                    -1,
                    null,
                    entry != null ? entry.Pawn : null,
                    entry?.Pawn?.equipment?.Primary ?? (Thing)entry?.Pawn,
                    finalTarget,
                    finalTarget,
                    finalTarget.IsValid ? finalTarget.CenterVector3 : default,
                    null,
                    default,
                    entry != null ? entry.SemanticContext : null,
                    entry?.AttackContext?.ToSnapshot()));

            return record;
        }

        private static ThingDef ResolveBaselineProjectile(RangedAttackEntry entry)
        {
            if (entry?.StepEmits != null)
            {
                for (int i = 0; i < entry.StepEmits.Count; i++)
                {
                    if (entry.StepEmits[i]?.ProjectileDef != null)
                    {
                        return entry.StepEmits[i].ProjectileDef;
                    }
                }
            }

            return entry?.SourceResult?.ResolvedVerbSpec != null
                ? entry.SourceResult.ResolvedVerbSpec.ProjectileDef
                : entry?.SessionResult?.ResolvedVerbSpec != null
                    ? entry.SessionResult.ResolvedVerbSpec.ProjectileDef
                    : null;
        }

        private static int ResolveBaselineFireCount(RangedAttackEntry entry)
        {
            if (entry?.StepEmits != null && entry.StepEmits.Count > 0)
            {
                return entry.StepEmits.Count;
            }

            return 1;
        }

        private static AttackExecutionEmit ResolveBaselineEmit(RangedAttackEntry entry, int emitIndex)
        {
            if (entry?.StepEmits == null || emitIndex < 0 || emitIndex >= entry.StepEmits.Count)
            {
                return null;
            }

            return entry.StepEmits[emitIndex];
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
