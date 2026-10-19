using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Expressions;
using BDP.Core.Permissions;
using BDP.Core.Projectiles;
using BDP.Core.Projectiles.Interaction;
using BDP.Core.Verbs;
using BDP.Core.Trigger.Runtime;
using BDP.Support.Diagnostics;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// BDP 的 Ability 远程射击宿主。
    /// 它保留原版 Ability 施放与预热入口，只把一次成功施放接入现有远程攻击协议。
    /// </summary>
    public class BdpVerb_AbilityShoot : BdpVerb_CastAbility, IBdpExpressionAbilityVerb
    {
        /// <summary>
        /// 目标探针读取当前正式结果的动态许可。
        /// 许可有效时只临时放宽原版直达目标视线检查，射程和目标类型仍由原版判定保留。
        /// </summary>
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            bool resolved = TryResolveBoundAbilityResult(out FormalExpressionResult result);
            bool relaxDirectTargetLineOfSight = resolved
                && DirectTargetLineOfSightPermissionAccess.ShouldRelaxDirectTargetLineOfSight(
                    CasterPawn,
                    result,
                    targ);
            BdpDiagnostics.AttackExecutionThrottled(
                "ability.shoot.target_legality." + (CasterPawn != null ? CasterPawn.ThingID : "null"),
                "Ability 目标直视判定 | ability=" + (Ability != null && Ability.def != null ? Ability.def.defName : "null")
                + " | result=" + (result != null ? result.Id : "null")
                + " | resolved=" + resolved
                + " | relaxDirectTargetLineOfSight=" + relaxDirectTargetLineOfSight,
                30);
            if (!relaxDirectTargetLineOfSight)
            {
                return base.CanHitTargetFrom(root, targ);
            }

            if (verbProps == null || !verbProps.requireLineOfSight)
            {
                return base.CanHitTargetFrom(root, targ);
            }

            bool previousRequirement = verbProps.requireLineOfSight;
            verbProps.requireLineOfSight = false;
            try
            {
                return base.CanHitTargetFrom(root, targ);
            }
            finally
            {
                verbProps.requireLineOfSight = previousRequirement;
            }
        }

        /// <summary>
        /// 先把原版 Ability 的 burst 表面收敛为一次 TryCastShot（尝试射击）。
        /// 真正的齐射数量由正式 Ability 结果的 RangedProtocol（远程协议）展开，避免重复扣费和重复冷却。
        /// </summary>
        public override bool TryStartCastOn(
            LocalTargetInfo castTarg,
            LocalTargetInfo destTarg,
            bool surpriseAttack = false,
            bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false,
            bool nonInterruptingSelfCast = false)
        {
            if (!AttackAdmissionAccess.Evaluate(CasterPawn, this).Accepted) return false;
            LogCastDiagnostic(
                "start",
                "target=" + DescribeTarget(castTarg)
                + ", destination=" + DescribeTarget(destTarg));

            if (!TryPrepareSingleShotSurface())
            {
                LogCastDiagnostic("surface_prepare_failed", "target=" + DescribeTarget(castTarg));
                return false;
            }

            bool started = TryStartAdmittedCast(
                castTarg,
                destTarg,
                surpriseAttack,
                canHitNonTargetPawns,
                preventFriendlyFire,
                nonInterruptingSelfCast);
            LogCastDiagnostic(
                started ? "start_accepted" : "start_base_rejected",
                "target=" + DescribeTarget(castTarg));
            return started;
        }

        /// <summary>
        /// 构建远程协议计划、提交一次 Ability 成本并发射计划中的全部投射物。
        /// </summary>
        protected override bool TryCastShot()
        {
            LogCastDiagnostic("cast_enter", "target=" + DescribeTarget(CurrentTarget));

            if (!TryBuildRangedProtocol(
                    out FormalExpressionResult result,
                    out AttackExecutionPreparedContext preparedContext,
                    out RangedAttackProtocolResult protocolResult))
            {
                return false;
            }

            if (!BdpAbilityTrionCostCommitter.TryCommit(Ability))
            {
                LogCastDiagnostic("trion_cost_commit_failed", "target=" + DescribeTarget(CurrentTarget));
                return false;
            }

            TriggerCastJitter();
            int emittedCount = LaunchPlans(
                preparedContext,
                result,
                protocolResult,
                preparedContext.AttackInstanceId);
            if (emittedCount <= 0)
            {
                LogCastDiagnostic(
                    "projectile_emit_zero",
                    "planned=" + protocolResult.ProjectilePlans.Count
                    + ", attackInstanceId=" + preparedContext.AttackInstanceId);
                return false;
            }

            Ability.StartCooldown(Ability.def.cooldownTicksRange.RandomInRange);
            LogCastDiagnostic(
                "cast_emitted",
                "planned=" + protocolResult.ProjectilePlans.Count
                + ", emitted=" + emittedCount
                + ", attackInstanceId=" + preparedContext.AttackInstanceId);
            return true;
        }

        /// <summary>
        /// 把 AbilityDef（能力定义）的原版表面复制成单发壳，保留其射程、预热、音效和目标参数。
        /// </summary>
        private bool TryPrepareSingleShotSurface()
        {
            if (!TryResolveBoundAbilityResult(out FormalExpressionResult result))
            {
                LogCastDiagnostic("surface.bound_result_missing", "abilityDef=" + (Ability?.def?.defName ?? "null"));
                return false;
            }

            VerbProperties source = result.ResolvedVerbSpec != null
                ? ResolvedVerbSpecFactory.CreateSurfaceVerbProps(result.ResolvedVerbSpec)
                : verbProps;
            if (source == null)
            {
                LogCastDiagnostic("surface.verb_props_missing", "resultId=" + (result?.Id ?? "null"));
                return false;
            }

            // 原版 Ability 射击宿主只执行一次，齐射由正式结果 BurstShotCount 展开。
            source.burstShotCount = 1;
            if (DirectTargetLineOfSightPermissionAccess.ShouldRelaxDirectTargetLineOfSight(
                    CasterPawn,
                    result,
                    CurrentTarget))
            {
                // 原版 Verb.TryStartCastOn 的暖机检查读取当前 surface；
                // 只在本轮动态许可有效时关闭这一层直达视线要求。
                source.requireLineOfSight = false;
            }
            verbProps = source;
            return true;
        }

        /// <summary>
        /// 通过发布投影和正式 AttackExecution（攻击执行）入口构建本次 Ability 的远程协议结果。
        /// </summary>
        private bool TryBuildRangedProtocol(
            out FormalExpressionResult result,
            out AttackExecutionPreparedContext preparedContext,
            out RangedAttackProtocolResult protocolResult)
        {
            result = null;
            preparedContext = null;
            protocolResult = null;
            Pawn pawn = CasterPawn;
            if (pawn == null)
            {
                LogCastDiagnostic("build.pawn_missing", "target=" + DescribeTarget(CurrentTarget));
                return false;
            }

            if (Ability?.def == null)
            {
                LogCastDiagnostic("build.ability_def_missing", "target=" + DescribeTarget(CurrentTarget));
                return false;
            }

            if (!CurrentTarget.IsValid)
            {
                LogCastDiagnostic("build.target_invalid", "target=" + DescribeTarget(CurrentTarget));
                return false;
            }

            if (!TryResolveBoundAbilityResult(out FormalExpressionResult boundResult))
            {
                LogCastDiagnostic("build.bound_result_missing", "abilityDef=" + Ability.def.defName);
                return false;
            }

            TriggerCombatProjectionState projection;
            if (!AttackExecutionSurfaceAccess.TryGetPublishedResult(
                    pawn,
                    boundResult.Id,
                    out projection,
                    out result))
            {
                LogCastDiagnostic(
                    "build.published_result_missing",
                    "boundResultId=" + (boundResult.Id ?? "null"));
                return false;
            }

            if (result == null)
            {
                LogCastDiagnostic(
                    "build.published_result_null",
                    "boundResultId=" + (boundResult.Id ?? "null"));
                return false;
            }

            if (result.OutputChannel != ExpressionOutputChannel.Ability)
            {
                LogCastDiagnostic(
                    "build.output_channel_invalid",
                    "resultId=" + result.Id + ", outputChannel=" + result.OutputChannel);
                return false;
            }

            if (result.WeaponMode != WeaponExpressionMode.Ranged)
            {
                LogCastDiagnostic(
                    "build.weapon_mode_invalid",
                    "resultId=" + result.Id + ", weaponMode=" + result.WeaponMode);
                return false;
            }

            if (result.ResolvedVerbSpec == null)
            {
                LogCastDiagnostic("build.resolved_verb_missing", "resultId=" + result.Id);
                return false;
            }

            AttackExecutionService entry = AttackExecutionSurfaceAccess.ResolveEntry(pawn);
            if (entry == null)
            {
                LogCastDiagnostic("build.attack_entry_missing", "resultId=" + result.Id);
                return false;
            }

            AttackExecutionRequest request = new AttackExecutionRequest
            {
                Pawn = pawn,
                SessionToken = AttackSessionToken.Create(pawn, result.Id, projection.ProjectionVersion),
                Target = CurrentTarget,
                Reason = AttackExecutionReason.Manual,
                DispatchIntent = AttackDispatchIntent.ImmediateCast
            };

            if (!entry.TryPreparePlan(request, out preparedContext))
            {
                LogCastDiagnostic("build.attack_plan_prepare_failed", "resultId=" + result.Id);
                return false;
            }

            if (preparedContext == null)
            {
                LogCastDiagnostic("build.attack_context_missing", "resultId=" + result.Id);
                return false;
            }

            if (preparedContext.RuntimeSteps == null)
            {
                LogCastDiagnostic("build.runtime_steps_missing", "resultId=" + result.Id);
                return false;
            }

            AttackRuntimeStep step = ResolveRangedStep(preparedContext.RuntimeSteps);
            if (step == null)
            {
                LogCastDiagnostic("build.ranged_step_missing", "resultId=" + result.Id);
                return false;
            }

            RangedAttackProtocolService protocol = RangedAttackProtocolSurfaceAccess.Resolve(pawn);
            if (protocol == null)
            {
                LogCastDiagnostic("build.protocol_missing", "resultId=" + result.Id);
                return false;
            }

            if (!protocol.TryBuild(preparedContext, step, result, out protocolResult))
            {
                LogCastDiagnostic("build.protocol_build_failed", "resultId=" + result.Id);
                return false;
            }

            if (protocolResult == null)
            {
                LogCastDiagnostic("build.protocol_result_null", "resultId=" + result.Id);
                return false;
            }

            if (protocolResult.ProjectilePlans == null)
            {
                LogCastDiagnostic("build.projectile_plans_null", "resultId=" + result.Id);
                return false;
            }

            if (protocolResult.ProjectilePlans.Count <= 0)
            {
                LogCastDiagnostic("build.projectile_plans_empty", "resultId=" + result.Id);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 在当前 Ability 绑定结果中读取正式表达结果。
        /// </summary>
        private bool TryResolveBoundAbilityResult(out FormalExpressionResult result)
        {
            return DefaultExpressionAbilityHostSynchronizer.TryResolveBoundAbilityResult(
                CasterPawn,
                Ability?.def,
                out result);
        }

        /// <summary>
        /// 从准备好的运行时动作步中取得当前远程动作步。
        /// </summary>
        private static AttackRuntimeStep ResolveRangedStep(
            System.Collections.Generic.IReadOnlyList<AttackRuntimeStep> runtimeSteps)
        {
            for (int index = 0; index < runtimeSteps.Count; index++)
            {
                AttackRuntimeStep step = runtimeSteps[index];
                if (step != null
                    && step.WeaponMode == WeaponExpressionMode.Ranged
                    && step.Emits != null
                    && step.Emits.Count > 0)
                {
                    return step;
                }
            }

            return null;
        }

        /// <summary>
        /// 记录 Ability 远程施放链路的阶段状态。
        /// 所有阶段都按 Pawn、Ability 和阶段限流，避免原版重复探测造成刷屏。
        /// </summary>
        private void LogCastDiagnostic(string stage, string details)
        {
            string pawnId = CasterPawn != null ? CasterPawn.ThingID : "null";
            string abilityDefName = Ability?.def?.defName ?? "null";
            string key = "ability.cast." + pawnId + "." + abilityDefName + "." + stage;
            BdpDiagnostics.AttackExecutionThrottled(
                key,
                "Ability远程施放诊断 | stage=" + stage
                + " | pawn=" + pawnId
                + " | ability=" + abilityDefName
                + " | " + (details ?? "none"),
                60);
        }

        /// <summary>
        /// 将目标压缩成适合诊断日志的一行文本，不展开 Thing 全部状态。
        /// </summary>
        private static string DescribeTarget(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return "invalid";
            }

            if (target.Cell.IsValid)
            {
                return target.Cell.ToString();
            }

            return target.Thing != null ? target.Thing.ThingID : "valid";
        }

        /// <summary>
        /// 按协议生成并发射所有投射物；每枚投射物仍使用原版 IntendedTarget（意图目标）命中标志。
        /// </summary>
        private int LaunchPlans(
            AttackExecutionPreparedContext preparedContext,
            FormalExpressionResult result,
            RangedAttackProtocolResult protocolResult,
            string attackInstanceId)
        {
            int emittedCount = 0;
            if (preparedContext?.Pawn == null || protocolResult?.ProjectilePlans == null)
            {
                return emittedCount;
            }

            bool hasOriginSpreadRange = false;
            for (int index = 0; index < protocolResult.ProjectilePlans.Count; index++)
            {
                ProjectileInitPlan candidate = protocolResult.ProjectilePlans[index];
                if (candidate != null
                    && candidate.HasOriginSpreadRange
                    && !candidate.HasOriginSpreadOffsetOverride)
                {
                    hasOriginSpreadRange = true;
                    break;
                }
            }

            // 同一轮齐射只随机一次首发左右侧别，具体偏移仍由每发独立取样。
            bool originSpreadStartsRight = hasOriginSpreadRange && Rand.Bool;
            int expectedEmitCount = protocolResult.ProjectilePlans.Count;
            for (int index = 0; index < protocolResult.ProjectilePlans.Count; index++)
            {
                ProjectileInitPlan plan = protocolResult.ProjectilePlans[index];
                if (plan == null
                    || !TryLaunchPlan(
                        preparedContext.Pawn,
                        result,
                        plan,
                        attackInstanceId,
                        expectedEmitCount,
                        originSpreadStartsRight))
                {
                    continue;
                }

                emittedCount++;
            }

            return emittedCount;
        }

        /// <summary>
        /// 把一枚初始化计划落成 BDP 投射物并调用原版 Launch（发射）入口。
        /// </summary>
        private bool TryLaunchPlan(
            Pawn pawn,
            FormalExpressionResult result,
            ProjectileInitPlan plan,
            string attackInstanceId,
            int expectedEmitCount,
            bool originSpreadStartsRight)
        {
            LocalTargetInfo semanticTarget = plan.CurrentTarget.IsValid
                ? plan.CurrentTarget
                : plan.AimTarget;
            LocalTargetInfo launchTarget = plan.LaunchTarget.IsValid
                ? plan.LaunchTarget
                : semanticTarget;
            if (!launchTarget.IsValid)
            {
                return false;
            }

            Vector3 origin = pawn.DrawPos + plan.OriginOffsetWorld;
            if (plan.HasAbsoluteOriginWorld)
            {
                origin = plan.AbsoluteOriginWorld + plan.OriginOffsetWorld;
            }
            origin += plan.HasOriginSpreadOffsetOverride
                ? plan.OriginSpreadOffsetOverrideWorld
                : ResolveRandomOriginSpreadOffset(
                    origin,
                    launchTarget,
                    plan,
                    expectedEmitCount,
                    originSpreadStartsRight);

            IntVec3 spawnCell = origin.ToIntVec3();
            if (!spawnCell.IsValid || !spawnCell.InBounds(pawn.Map))
            {
                spawnCell = pawn.Position;
            }

            Projectile projectile = BdpProjectileLaunchService.CreateProjectileForLaunch(
                pawn,
                plan.ProjectileDef ?? result?.ResolvedVerbSpec?.ProjectileDef,
                spawnCell,
                EquipmentSource,
                plan,
                plan.SemanticContext,
                plan.AttackInstanceId ?? attackInstanceId,
                plan.ResultId);
            if (!(projectile is BdpProjectile))
            {
                projectile?.Destroy();
                return false;
            }

            Vector3 destination = ResolveTargetPoint(launchTarget);
            BdpProjectileLaunchService.FreezeIntendedLaunchResolution(plan, launchTarget, destination);
            ProjectileHitFlags hitFlags = ProjectileInterceptionDecision.ResolveIntendedLaunchHitFlags(
                ProjectileHitFlags.IntendedTarget,
                semanticTarget.IsValid ? semanticTarget : launchTarget,
                plan.InteractionPolicy);
            projectile.Launch(
                pawn,
                origin,
                launchTarget,
                semanticTarget.IsValid ? semanticTarget : launchTarget,
                hitFlags,
                false,
                EquipmentSource);
            return true;
        }

        /// <summary>
        /// 按现有远程宿主规则，在 Ability 的真实发射点生成局部随机偏移。
        /// 多发时按发射序号交替左右，偏移幅度和前后偏移仍逐发随机。
        /// </summary>
        private static Vector3 ResolveRandomOriginSpreadOffset(
            Vector3 source,
            LocalTargetInfo target,
            ProjectileInitPlan plan,
            int expectedEmitCount,
            bool startsRight)
        {
            if (plan == null
                || !plan.HasOriginSpreadRange
                || expectedEmitCount <= 1
                || !target.IsValid)
            {
                return Vector3.zero;
            }

            Vector3 shootDir = (target.CenterVector3 - source).normalized;
            if (shootDir == Vector3.zero)
            {
                return Vector3.zero;
            }

            Vector3 rightDir = Vector3.Cross(Vector3.up, shootDir).normalized;
            if (rightDir == Vector3.zero)
            {
                rightDir = Vector3.right;
            }

            float leftMax = Mathf.Max(0f, -plan.OriginSpreadLateralMin);
            float rightMax = Mathf.Max(0f, plan.OriginSpreadLateralMax);
            int sequenceParity = plan.EmitSequence % 2;
            bool rightSide = startsRight ? sequenceParity == 0 : sequenceParity != 0;
            float lateralOffset;
            if (rightSide)
            {
                lateralOffset = rightMax > 0f
                    ? Rand.Range(0f, rightMax)
                    : -Rand.Range(0f, leftMax);
            }
            else
            {
                lateralOffset = leftMax > 0f
                    ? -Rand.Range(0f, leftMax)
                    : Rand.Range(0f, rightMax);
            }

            float forwardOffset = Rand.Range(
                plan.OriginSpreadForwardMin,
                plan.OriginSpreadForwardMax);
            return rightDir * lateralOffset + shootDir * forwardOffset;
        }

        /// <summary>
        /// 解析选中目标格的几何中心；多格目标不退回 Thing 中心。
        /// </summary>
        private static Vector3 ResolveTargetPoint(LocalTargetInfo target)
        {
            return target.Cell.IsValid
                ? target.Cell.ToVector3Shifted()
                : target.CenterVector3;
        }

        /// <summary>
        /// 触发与其它 BDP Ability 一致的施放抖动表现。
        /// </summary>
        protected override void TriggerCastJitter()
        {
            Pawn casterPawn = CasterPawn;
            if (casterPawn == null)
            {
                return;
            }

            float castDirection = (CurrentTarget.Cell - casterPawn.Position).AngleFlat;
            JitterHandler jitterer = Traverse.Create(casterPawn.Drawer)
                .Field("jitterer")
                .GetValue<JitterHandler>();
            jitterer?.AddOffset(0.5f, castDirection);
        }
    }
}
