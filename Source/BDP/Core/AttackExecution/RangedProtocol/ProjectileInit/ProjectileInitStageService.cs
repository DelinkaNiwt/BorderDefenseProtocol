using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Projectiles.Interaction;
using BDP.Core.Expressions;
using BDP.Core.Projectiles.Visual;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.ProjectileInit
{
    /// <summary>
    /// ProjectileInit 阶段服务。
    /// 它负责把 FireRecord 翻译成 projectile 初始化计划。
    /// </summary>
    internal sealed class ProjectileInitStageService
    {
        /// <summary>
        /// 当前阶段绑定的 ProjectileInit 主模块集合。
        /// 顺序完全以组合根提供的装配顺序为准。
        /// </summary>
        private readonly List<IProjectileInitStageModule> modules;

        /// <summary>
        /// 当前阶段绑定的附加 addon 模块集合。
        /// </summary>
        private readonly List<IRangedStageAddonModule> addons;

        /// <summary>
        /// 用指定模块集合构造 ProjectileInit 阶段服务。
        /// </summary>
        public ProjectileInitStageService(IEnumerable<IProjectileInitStageModule> modules, IEnumerable<IRangedStageAddonModule> addons)
        {
            this.modules = modules != null ? new List<IProjectileInitStageModule>(modules) : new List<IProjectileInitStageModule>();
            this.addons = addons != null ? new List<IRangedStageAddonModule>(addons) : new List<IRangedStageAddonModule>();
        }

        /// <summary>
        /// 执行 ProjectileInit 阶段并生成投射物初始化计划。
        /// </summary>
        public IReadOnlyList<ProjectileInitPlan> Execute(
            RangedAttackEntry entry,
            AimRecord aim,
            PrepareRecord prepare,
            FireRecord fire)
        {
            List<ProjectileInitContribution> contributions = new List<ProjectileInitContribution>();
            for (int i = 0; i < modules.Count; i++)
            {
                ProjectileInitStageContext context = new ProjectileInitStageContext(entry, aim, prepare, fire, modules[i] as IRangedAttackModuleRuntime);
                ProjectileInitContribution contribution = new ProjectileInitContribution();
                modules[i].Contribute(context, contribution);
                contributions.Add(contribution);
                if (contribution.Stop.IsRequested)
                {
                    return new List<ProjectileInitPlan>();
                }
            }
            List<ProjectileInitPlan> plans = new List<ProjectileInitPlan>();
            if (fire?.Emits == null)
            {
                return plans;
            }

            Thing sourceThing = entry != null && entry.Pawn != null
                ? (Thing)entry.Pawn.equipment?.Primary ?? entry.Pawn
                : null;
            // 本窗口在轮次内的起始发射序号,与 ProjectileInitStageContext.EmitSequenceBase 保持同一公式。
            int emitSequenceBase = entry != null && entry.RuntimeStep != null
                ? entry.RuntimeStep.StepIndex * fire.Emits.Count
                : 0;
            for (int emitIndex = 0; emitIndex < fire.Emits.Count; emitIndex++)
            {
                FireEmitRecord emit = fire.Emits[emitIndex];
                ProjectileInitPlan plan = new ProjectileInitPlan
                {
                    AttackInstanceId = entry != null ? entry.AttackInstanceId : null,
                    ResultId = emit != null && !string.IsNullOrWhiteSpace(emit.SourceResultId)
                        ? emit.SourceResultId
                        : entry != null ? entry.SourceResultId : null,
                    EmitIndex = emitIndex,
                    EmitSequence = emitSequenceBase + emitIndex,
                    ProjectileDef = emit?.ProjectileOverride ?? fire.ProjectileDef,
                    Launcher = entry != null ? entry.Pawn : null,
                    SourceThing = sourceThing,
                    OriginOffsetWorld = emit != null ? emit.OriginOffsetWorld : Vector3.zero,
                    HasOriginSpreadRange = emit != null && emit.HasOriginSpreadRange,
                    OriginSpreadLateralMin = emit != null ? emit.OriginSpreadLateralMin : 0f,
                    OriginSpreadLateralMax = emit != null ? emit.OriginSpreadLateralMax : 0f,
                    OriginSpreadForwardMin = emit != null ? emit.OriginSpreadForwardMin : 0f,
                    OriginSpreadForwardMax = emit != null ? emit.OriginSpreadForwardMax : 0f,
                    LaunchTarget = emit != null ? emit.Target : LocalTargetInfo.Invalid,
                    AimTarget = entry != null && entry.SemanticTarget.IsValid
                        ? entry.SemanticTarget
                        : aim != null ? aim.FinalTarget : LocalTargetInfo.Invalid,
                    CurrentTarget = emit != null && emit.SemanticTarget.IsValid
                        ? emit.SemanticTarget
                        : entry != null && entry.SemanticTarget.IsValid
                            ? entry.SemanticTarget
                            : aim != null ? aim.FinalTarget : LocalTargetInfo.Invalid,
                    InitialSpeedFactor = emit != null ? emit.SpeedFactor : 1f,
                    InitialDamageFactor = emit != null ? emit.DamageFactor : 1f,
                    InitialStoppingPowerFactor = emit != null ? emit.StoppingPowerFactor : 1f,
                    MuzzleFlashScale = ResolveMuzzleFlashScale(emit, entry),
                    AccuracyFactor = aim != null ? aim.AccuracyFactor : 1f,
                    ForcedMissRadius = aim != null ? aim.ForcedMissRadius : 0f,
                    HasAccuracy = (emit != null ? emit.SourceResult : entry != null ? entry.SourceResult : null)?.VerbProps != null,
                    AccuracyTouch = (emit != null ? emit.SourceResult : entry != null ? entry.SourceResult : null)?.VerbProps?.accuracyTouch ?? 0f,
                    AccuracyShort = (emit != null ? emit.SourceResult : entry != null ? entry.SourceResult : null)?.VerbProps?.accuracyShort ?? 0f,
                    AccuracyMedium = (emit != null ? emit.SourceResult : entry != null ? entry.SourceResult : null)?.VerbProps?.accuracyMedium ?? 0f,
                    AccuracyLong = (emit != null ? emit.SourceResult : entry != null ? entry.SourceResult : null)?.VerbProps?.accuracyLong ?? 0f,
                    SemanticContext = emit != null ? emit.SemanticContext : entry != null ? entry.SemanticContext : null,
                    AttackContextSnapshot = entry != null && entry.AttackContext != null
                        ? entry.AttackContext.ToSnapshot()
                        : null,
                };
                ThingDef visualProviderDef = ResolveVisualAttachmentProviderDef(emit, entry);
                if (visualProviderDef != null)
                {
                    plan.VisualAttachmentProviderDefs.Add(visualProviderDef);
                }

                AppendTags(plan.Tags, fire != null ? fire.Tags : null);
                AppendTags(plan.Tags, emit != null ? emit.Tags : null);

                for (int i = 0; i < contributions.Count; i++)
                {
                    ProjectileInitContribution contribution = contributions[i];
                    for (int j = 0; j < contribution.PlanContributions.Count; j++)
                    {
                        ProjectileInitPlanContribution planContribution = contribution.PlanContributions[j];
                        if (planContribution == null || planContribution.EmitIndex != emitIndex)
                        {
                            continue;
                        }

                        if (planContribution.HasOverrideOriginWorld)
                        {
                            plan.HasAbsoluteOriginWorld = true;
                            plan.AbsoluteOriginWorld = planContribution.OverrideOriginWorld;
                        }

                        if (planContribution.HasOverrideLaunchTarget)
                        {
                            plan.LaunchTarget = planContribution.OverrideLaunchTarget;
                        }

                        if (planContribution.HasOverrideAimTarget)
                        {
                            plan.AimTarget = planContribution.OverrideAimTarget;
                        }

                        if (planContribution.HasOverrideCurrentTarget)
                        {
                            plan.CurrentTarget = planContribution.OverrideCurrentTarget;
                        }

                        if (planContribution.HasInitialSegmentTriggerRatio)
                        {
                            plan.HasInitialSegmentTriggerRatio = true;
                            plan.InitialSegmentTriggerRatio = planContribution.InitialSegmentTriggerRatio;
                        }

                        if (planContribution.HasInitialFlightPathSnapshot)
                        {
                            plan.InitialFlightPathSnapshot = planContribution.InitialFlightPathSnapshot;
                        }

                        if (planContribution.HasInteractionPolicy
                            && planContribution.InteractionPolicy != null)
                        {
                            plan.InteractionPolicy = MergeInteractionPolicy(
                                plan.InteractionPolicy,
                                planContribution.InteractionPolicy);
                        }

                        if (planContribution.HasTrailColorOverride)
                        {
                            plan.HasTrailColorOverride = true;
                            plan.TrailColorOverride = planContribution.TrailColorOverride;
                        }

                        if (planContribution.HasTrailCoreOverride)
                        {
                            plan.HasTrailCoreOverride = true;
                            plan.TrailCoreColorOverride = planContribution.TrailCoreColorOverride;
                            plan.TrailCoreWidthRatioOverride = planContribution.TrailCoreWidthRatioOverride;
                            plan.TrailCoreOpacityOverride = planContribution.TrailCoreOpacityOverride;
                        }

                        plan.InitialSpeedFactor *= planContribution.InitialSpeedFactorMultiplier;
                        plan.InitialDamageFactor *= planContribution.InitialDamageFactorMultiplier;
                        AppendTags(plan.Tags, planContribution.TagsToAppend);
                    }

                    AppendTags(plan.Tags, contribution.TagsToAppend);
                }

                plan.SyncTargetSemanticsFromLegacyTargets();
                plan.TargetSemantics = plan.TargetSemantics != null ? plan.TargetSemantics.Clone() : null;
                plans.Add(plan);
            }

            for (int i = 0; i < plans.Count; i++)
            {
                ProjectileInitPlan plan = plans[i];
                RangedStageAddonDispatcher.Execute(
                    addons,
                    new RangedStageAddonContext(
                        RangedStageKind.ProjectileInit,
                        entry != null ? entry.Pawn : null,
                        entry?.Pawn?.Map,
                        plan != null ? plan.AttackInstanceId : entry != null ? entry.AttackInstanceId : null,
                        plan != null ? plan.ResultId : entry != null ? entry.SourceResultId : null,
                        plan != null ? plan.EmitIndex : -1,
                        null,
                        plan != null ? plan.Launcher : entry != null ? entry.Pawn : null,
                        plan != null ? plan.SourceThing : entry?.Pawn?.equipment?.Primary ?? (Thing)entry?.Pawn,
                        plan != null ? plan.AimTarget : LocalTargetInfo.Invalid,
                        plan != null ? plan.CurrentTarget : LocalTargetInfo.Invalid,
                        plan != null && plan.LaunchTarget.IsValid ? plan.LaunchTarget.CenterVector3 : default,
                        null,
                        default,
                        plan != null ? plan.SemanticContext : entry != null ? entry.SemanticContext : null,
                        plan?.AttackContextSnapshot));
            }

            return plans;
        }

        /// <summary>
        /// 追加阶段标签集合。
        /// </summary>
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

        /// <summary>
        /// 合并多个模块的护盾/拦截器绕过声明。
        /// 任一模块声明绕过就保留绕过，避免结果依赖模块列表顺序。
        /// </summary>
        private static ProjectileInteractionPolicy MergeInteractionPolicy(
            ProjectileInteractionPolicy current,
            ProjectileInteractionPolicy incoming)
        {
            if (current == null)
            {
                return incoming != null ? incoming.Clone() : null;
            }

            if (incoming == null)
            {
                return current.Clone();
            }

            return new ProjectileInteractionPolicy
            {
                BypassProjectileInterceptors = current.BypassProjectileInterceptors
                    || incoming.BypassProjectileInterceptors,
                BypassRegisteredDamageShields = current.BypassRegisteredDamageShields
                    || incoming.BypassRegisteredDamageShields
            };
        }

        /// <summary>
        /// 解析当前发射来源自己的原版枪口闪光尺寸。
        /// 优先读取正式运行时规格；旧或不完整结果才回退到声明表面。
        /// </summary>
        private static float ResolveMuzzleFlashScale(FireEmitRecord emit, RangedAttackEntry entry)
        {
            FormalExpressionResult sourceResult = emit != null && emit.SourceResult != null
                ? emit.SourceResult
                : entry != null ? entry.SourceResult : null;
            if (sourceResult?.ResolvedVerbSpec != null)
            {
                return sourceResult.ResolvedVerbSpec.MuzzleFlashScale;
            }

            return sourceResult?.VerbProps?.muzzleFlashScale ?? 0f;
        }

        /// <summary>
        /// 解析当前 emit 对应的来源芯片定义。
        /// 只冻结实际声明视觉附加 provider 的来源 Def，避免让计划保存无意义引用。
        /// </summary>
        /// <param name="emit">当前发射记录。</param>
        /// <param name="entry">当前远程协议入口。</param>
        /// <returns>可提供视觉附加件的来源定义；没有时返回空。</returns>
        private static ThingDef ResolveVisualAttachmentProviderDef(FireEmitRecord emit, RangedAttackEntry entry)
        {
            FormalExpressionResult sourceResult = emit != null && emit.SourceResult != null
                ? emit.SourceResult
                : entry != null ? entry.SourceResult : null;
            string chipDefName = sourceResult != null && sourceResult.SourceReference != null
                ? sourceResult.SourceReference.ChipDefName
                : null;
            if (string.IsNullOrWhiteSpace(chipDefName))
            {
                return null;
            }

            ThingDef sourceDef = DefDatabase<ThingDef>.GetNamedSilentFail(chipDefName);
            return HasVisualAttachmentProvider(sourceDef) ? sourceDef : null;
        }

        /// <summary>
        /// 判断指定 Def 是否声明了投射物视觉附加提供器。
        /// </summary>
        /// <param name="def">待检查的定义。</param>
        /// <returns>为 true 时该定义可参与投射物视觉附加创建。</returns>
        private static bool HasVisualAttachmentProvider(ThingDef def)
        {
            if (def?.modExtensions == null)
            {
                return false;
            }

            for (int i = 0; i < def.modExtensions.Count; i++)
            {
                if (def.modExtensions[i] is IProjectileVisualAttachmentProvider)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
