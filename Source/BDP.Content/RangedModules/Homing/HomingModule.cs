using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using BDP.Core.AttackExecution.RangedProtocol.ProjectileInit;
using BDP.Core.Projectiles;
using BDP.Core.Projectiles.RangedFlightProtocol.Arrival;
using BDP.Core.Projectiles.RangedFlightProtocol.Hit;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Projectiles.RangedFlightProtocol.Projection;
using BDP.Support.Diagnostics;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.Homing
{
    /// <summary>
    /// 制导追踪远程模块。
    /// </summary>
    /// <remarks>
    /// 参与四个阶段：
    ///
    /// 1. ProjectileInit（投射物初始化） — 锁定目标，设置初始追踪参数。
    /// 2. Arrival（到达段） — 根据当前距离与角度决定继续追踪/释放/重锁/脱靶。
    /// 3. Hit（命中判定） — 判定是否命中锁定目标，非命中时强制落空。
    /// 4. 内部驱动飞行路径持续更新。
    ///
    /// 核心状态机：Pursuing → HitCheckPending / Relocking / FlyAway → Finished
    /// </remarks>
    public sealed class HomingModule :
        IRangedAttackModuleRuntime,
        IProjectileInitStageModule,
        IArrivalStageModule,
        IHitStageModule,
        ITargetingStageModule
    {
        private HomingConfig config;
        private string resultId;

        void IRangedAttackModuleRuntime.Initialize(RangedAttackModuleRuntimeContext context)
        {
            resultId = context != null ? context.ResultId : null;
            config = ResolveConfigSnapshot(context);
        }

        void ITargetingStageModule.Contribute(TargetingRecord record)
        {
            if (record != null && config != null && config.AllowGroundTarget)
            {
                BdpRangedTargetAcquisitionUtility.EnsureGroundTargetingAllowed(record.TargetingParameters);
            }
        }

        void IProjectileInitStageModule.Contribute(in ProjectileInitStageContext context, ProjectileInitContribution contribution)
        {
            if (contribution == null)
            {
                return;
            }

            HomingState state = context.GetOrCreatePrivateContext<HomingState>();
            if (state == null)
            {
                return;
            }

            LocalTargetInfo lockedTarget = ResolveLockedTarget(context);
            HomingConfig frozenConfig = config != null ? config.CloneTyped() : new HomingConfig();
            bool groundAcquirePending = lockedTarget.IsValid && !lockedTarget.HasThing
                && frozenConfig.AllowGroundTarget && frozenConfig.AcquireRadius > 0f;

            state.FrozenConfig = frozenConfig;
            state.LockedTarget = lockedTarget;
            state.HasTakenFlightControl = false;
            state.Phase = HomingPhase.Pursuing;
            state.RelocksUsed = 0;
            state.HasLastObservedTargetPos = TryResolveTargetPosition(lockedTarget, out Vector3 targetPos);
            state.LastObservedTargetPos = state.HasLastObservedTargetPos ? targetPos : Vector3.zero;
            state.LastTargetObservationTick = GetSafeCurrentTick();
            state.HasExpectedTargetPosition = state.HasLastObservedTargetPos;
            state.ExpectedTargetPosition = state.HasExpectedTargetPosition ? targetPos : Vector3.zero;
            state.HasLastDistanceSample = TryResolveInitialDistance(context.Pawn, lockedTarget, out float distanceToTarget);
            state.LastDistanceToTarget = state.HasLastDistanceSample ? distanceToTarget : 0f;
            state.Seed = ResolveSeed(context, lockedTarget);
            state.FlyAwayIssued = false;
            state.FlyAwayEnd = Vector3.zero;
            state.GroundAcquireDone = !groundAcquirePending;
            ClearReleaseState(state);

            if (!lockedTarget.IsValid)
            {
                return;
            }

            for (int emitIndex = 0; emitIndex < context.EmitCount; emitIndex++)
            {
                contribution.PlanContributions.Add(new ProjectileInitPlanContribution
                {
                    EmitIndex = emitIndex,
                    HasOverrideCurrentTarget = true,
                    OverrideCurrentTarget = lockedTarget,
                    HasInitialSegmentTriggerRatio = true,
                    InitialSegmentTriggerRatio = groundAcquirePending
                        ? frozenConfig.GroundTargetInitialSegmentTriggerRatio
                        : frozenConfig.InitialSegmentTriggerRatio
                });
            }
        }

        void IArrivalStageModule.Contribute(in ArrivalStageContext context, ArrivalContribution contribution)
        {
            if (contribution == null || context.PreviousExclusiveFlightControl)
            {
                return;
            }

            HomingState state = context.GetPrivateContext<HomingState>();
            if (state == null)
            {
                return;
            }

            HomingPhase phaseBefore = state.Phase;

            if (state.Phase == HomingPhase.Finished)
            {
                contribution.HasOverrideContinueFlight = true;
                contribution.OverrideContinueFlight = false;
                return;
            }

            if (state.Phase == HomingPhase.FlyAway && state.FlyAwayIssued)
            {
                state.Phase = HomingPhase.Finished;
                contribution.HasOverrideContinueFlight = true;
                contribution.OverrideContinueFlight = false;
                return;
            }

            Vector3 projectilePos = context.Projectile != null
                ? context.Projectile.ExactPosition.Yto0()
                : Vector3.zero;
            Vector3 projectileForward = context.Projectile != null
                ? (context.Projectile.ExactRotation * Vector3.forward).Yto0()
                : Vector3.forward;
            HomingConfig frozenConfig = state.FrozenConfig ?? new HomingConfig();

            if (context.InitialExclusiveFlightControl && !state.HasTakenFlightControl)
            {
                state.HasTakenFlightControl = true;
                if (TryResolveTargetPosition(state.LockedTarget, out Vector3 handoffTarget))
                {
                    state.HasLastObservedTargetPos = true;
                    state.LastObservedTargetPos = handoffTarget;
                    state.LastTargetObservationTick = GetSafeCurrentTick();
                    SetExpectedTargetPosition(state, handoffTarget);
                    state.HasLastDistanceSample = true;
                    state.LastDistanceToTarget = HomingPathBuilder.ComputeDistanceToTarget(projectilePos, handoffTarget);
                    ClearReleaseState(state);
                    float ratio = !state.GroundAcquireDone
                        ? frozenConfig.GroundTargetInitialSegmentTriggerRatio : frozenConfig.InitialSegmentTriggerRatio;
                    ContinueTracking(contribution, state.LockedTarget,
                        ProjectileFlightPathUtility.CreateLinear(projectilePos, Vector3.Lerp(projectilePos, handoffTarget, Mathf.Clamp01(ratio))));
                    return;
                }
            }

            if (frozenConfig.AllowGroundTarget && !state.GroundAcquireDone
                && state.LockedTarget.IsValid && !state.LockedTarget.HasThing)
            {
                state.GroundAcquireDone = true;
                LocalTargetInfo acquired = BdpRangedTargetAcquisitionUtility.FindNearestAcquirableTarget(
                    projectilePos,
                    context.Map,
                    context.Launcher != null ? context.Launcher.Faction : null,
                    frozenConfig.AcquireRadius,
                    frozenConfig.AcquireRequireLineOfSight,
                    null);
                if (acquired.IsValid)
                {
                    state.LockedTarget = acquired;
                    if (HomingPathBuilder.TryResolveTargetPosition(acquired, out Vector3 acquiredPosition))
                    {
                        state.HasLastObservedTargetPos = true;
                        state.LastObservedTargetPos = acquiredPosition;
                        state.LastTargetObservationTick = GetSafeCurrentTick();
                        SetExpectedTargetPosition(state, acquiredPosition);
                    }

                    state.HasLastDistanceSample = false;
                    state.LastDistanceToTarget = 0f;
                    ClearReleaseState(state);
                }
            }

            if (!HomingPathBuilder.TryResolveTargetPosition(state.LockedTarget, out Vector3 targetCurrentPos))
            {
                LogArrivalDecision(
                    in context,
                    state,
                    phaseBefore,
                    HomingPhase.FlyAway,
                    "target_unresolved_flyaway",
                    projectilePos,
                    projectileForward,
                    Vector3.zero,
                    Vector3.zero,
                    state.HasLastDistanceSample,
                    state.LastDistanceToTarget,
                    float.NaN,
                    float.NaN,
                    float.NaN,
                    false,
                    float.NaN,
                    float.NaN,
                    ResolveReleaseExpectedImpactPoint(state));
                IssueFlyAway(in context, contribution, state, projectilePos, projectileForward);
                return;
            }

            float distanceToTarget = HomingPathBuilder.ComputeDistanceToTarget(projectilePos, targetCurrentPos);
            float turnAngleToTarget = HomingPathBuilder.ComputeTurnAngleToTarget(projectilePos, projectileForward, targetCurrentPos);
            Vector3 targetStepOffset = state.HasLastObservedTargetPos
                ? targetCurrentPos - state.LastObservedTargetPos
                : Vector3.zero;
            int currentTick = GetSafeCurrentTick();
            float targetObservationTicks = ResolveTargetObservationTicks(state, currentTick);
            float nextSegmentTicks = HomingPathBuilder.ResolvePredictionTicks(
                distanceToTarget,
                context.Projectile is BdpProjectile bdpProjectile
                    ? bdpProjectile.SpeedTilesPerTick
                    : context.Projectile != null && context.Projectile.def != null && context.Projectile.def.projectile != null
                    ? context.Projectile.def.projectile.SpeedTilesPerTick
                    : 1f);
            float targetPredictionResidual = ResolveTargetPredictionResidual(state, targetCurrentPos);
            bool hadLastDistanceSample = state.HasLastDistanceSample;
            float previousDistanceToTarget = state.LastDistanceToTarget;
            HomingPathBuilder.ResolveReleaseDistances(
                frozenConfig,
                state.RelocksUsed,
                out float softReleaseDistance,
                out float hardReleaseDistance);
            Vector3 releaseExpectedImpactPoint = ResolveReleaseExpectedImpactPoint(state);

            state.HasLastObservedTargetPos = true;
            state.LastObservedTargetPos = targetCurrentPos;
            if (currentTick >= 0)
            {
                state.LastTargetObservationTick = currentTick;
            }
            state.HasLastDistanceSample = true;
            state.LastDistanceToTarget = distanceToTarget;

            bool targetLost = IsTargetLost(
                hadLastDistanceSample,
                previousDistanceToTarget,
                distanceToTarget,
                turnAngleToTarget,
                targetPredictionResidual,
                frozenConfig);
            bool closingHitWindow = IsClosingHitWindowCandidate(
                distanceToTarget,
                hadLastDistanceSample,
                previousDistanceToTarget,
                frozenConfig);

            if (phaseBefore == HomingPhase.Released)
            {
                if (closingHitWindow)
                {
                    ClearReleaseState(state);
                    state.Phase = HomingPhase.HitCheckPending;
                    LogArrivalDecision(
                        in context,
                        state,
                        phaseBefore,
                        HomingPhase.HitCheckPending,
                        targetLost ? "released_enter_hit_check_closing" : "released_enter_hit_check",
                        projectilePos,
                        projectileForward,
                        targetCurrentPos,
                        targetStepOffset,
                        hadLastDistanceSample,
                        previousDistanceToTarget,
                        distanceToTarget,
                        turnAngleToTarget,
                        targetPredictionResidual,
                        targetLost,
                        softReleaseDistance,
                        hardReleaseDistance,
                        releaseExpectedImpactPoint);
                    contribution.HasOverrideContinueFlight = true;
                    contribution.OverrideContinueFlight = false;
                    return;
                }

                LogArrivalDecision(
                    in context,
                    state,
                    phaseBefore,
                    state.RelocksUsed < Mathf.Max(0, frozenConfig.MaxRelocks)
                        ? HomingPhase.Relocking
                        : HomingPhase.FlyAway,
                    state.RelocksUsed < Mathf.Max(0, frozenConfig.MaxRelocks)
                        ? "released_relock"
                        : "released_flyaway",
                    projectilePos,
                    projectileForward,
                    targetCurrentPos,
                    targetStepOffset,
                    hadLastDistanceSample,
                    previousDistanceToTarget,
                    distanceToTarget,
                    turnAngleToTarget,
                    targetPredictionResidual,
                    false,
                    softReleaseDistance,
                    hardReleaseDistance,
                    releaseExpectedImpactPoint);
                IssueRelockOrFlyAway(
                    in context,
                    contribution,
                    state,
                    phaseBefore,
                    projectilePos,
                    projectileForward,
                    targetCurrentPos,
                    targetStepOffset,
                    hadLastDistanceSample,
                    previousDistanceToTarget,
                    distanceToTarget,
                    turnAngleToTarget,
                    targetLost,
                    targetObservationTicks,
                    nextSegmentTicks,
                    softReleaseDistance,
                    hardReleaseDistance,
                    releaseExpectedImpactPoint);
                return;
            }

            if (targetLost)
            {
                state.Phase = HomingPhase.Lost;
                LogArrivalDecision(
                    in context,
                    state,
                    phaseBefore,
                    state.RelocksUsed < Mathf.Max(0, frozenConfig.MaxRelocks)
                        ? HomingPhase.Relocking
                        : HomingPhase.FlyAway,
                    state.RelocksUsed < Mathf.Max(0, frozenConfig.MaxRelocks)
                        ? "relock"
                        : "lost_flyaway",
                    projectilePos,
                    projectileForward,
                    targetCurrentPos,
                    targetStepOffset,
                    hadLastDistanceSample,
                    previousDistanceToTarget,
                    distanceToTarget,
                    turnAngleToTarget,
                    targetPredictionResidual,
                    true,
                    softReleaseDistance,
                    hardReleaseDistance,
                    releaseExpectedImpactPoint);
                IssueRelockOrFlyAway(
                    in context,
                    contribution,
                    state,
                    phaseBefore,
                    projectilePos,
                    projectileForward,
                    targetCurrentPos,
                    targetStepOffset,
                    hadLastDistanceSample,
                    previousDistanceToTarget,
                    distanceToTarget,
                    turnAngleToTarget,
                    true,
                    targetObservationTicks,
                    nextSegmentTicks,
                    softReleaseDistance,
                    hardReleaseDistance,
                    releaseExpectedImpactPoint);
                return;
            }

            if (closingHitWindow)
            {
                ClearReleaseState(state);
                state.Phase = HomingPhase.HitCheckPending;
                LogArrivalDecision(
                    in context,
                    state,
                    phaseBefore,
                    HomingPhase.HitCheckPending,
                    targetLost ? "enter_hit_check_closing" : "enter_hit_check",
                    projectilePos,
                    projectileForward,
                    targetCurrentPos,
                    targetStepOffset,
                    hadLastDistanceSample,
                    previousDistanceToTarget,
                    distanceToTarget,
                    turnAngleToTarget,
                    targetPredictionResidual,
                    targetLost,
                    softReleaseDistance,
                    hardReleaseDistance,
                    InvalidVector());
                contribution.HasOverrideContinueFlight = true;
                contribution.OverrideContinueFlight = false;
                return;
            }

            if (distanceToTarget <= hardReleaseDistance)
            {
                int terminalPathSeed = Gen.HashCombineInt(
                    state.Seed + (context.EmitIndex * 193) + state.RelocksUsed,
                    131);
                ProjectileFlightPathSnapshot terminalFlightPathSnapshot = HomingPathBuilder.BuildTerminalApproachPath(
                    projectilePos,
                    projectileForward,
                    targetCurrentPos,
                    frozenConfig);

                ClearReleaseState(state);
                state.Phase = HomingPhase.Pursuing;
                LogPathBuild(
                    in context,
                    state,
                    "terminal_approach",
                    projectilePos,
                    projectileForward,
                    targetCurrentPos,
                    targetStepOffset,
                    0f,
                    frozenConfig.PursuitMinTurnAngle,
                    frozenConfig.MaxTurnAngleWhenEvading,
                    frozenConfig.PursuitTurnResponsiveness,
                    1f,
                    terminalPathSeed,
                    terminalFlightPathSnapshot,
                    targetObservationTicks,
                    nextSegmentTicks);
                LogArrivalDecision(
                    in context,
                    state,
                    phaseBefore,
                    HomingPhase.Pursuing,
                    "terminal_approach",
                    projectilePos,
                    projectileForward,
                    targetCurrentPos,
                    targetStepOffset,
                    hadLastDistanceSample,
                    previousDistanceToTarget,
                    distanceToTarget,
                    turnAngleToTarget,
                    targetPredictionResidual,
                    false,
                    softReleaseDistance,
                    hardReleaseDistance,
                    InvalidVector());
                SetExpectedTargetPosition(state, targetCurrentPos);
                ContinueTracking(contribution, state.LockedTarget, terminalFlightPathSnapshot);
                return;
            }

            float turnScale = HomingPathBuilder.ComputeReleaseTurnScale(
                distanceToTarget,
                softReleaseDistance,
                hardReleaseDistance);
            int pursuitPathSeed = state.Seed + (context.EmitIndex * 193) + state.RelocksUsed;
            ProjectileFlightPathSnapshot nextFlightPathSnapshot = HomingPathBuilder.BuildTrackingPath(
                projectilePos,
                projectileForward,
                targetCurrentPos,
                targetStepOffset,
                frozenConfig,
                pursuitPathSeed,
                turnScale,
                targetObservationTicks,
                nextSegmentTicks,
                out float effectiveMaxTurnAngle,
                out float effectiveTurnResponsiveness,
                out Vector3 predictedTargetPos);

            ClearReleaseState(state);
            state.Phase = HomingPhase.Pursuing;
            LogPathBuild(
                in context,
                state,
                "pursuit",
                projectilePos,
                projectileForward,
                targetCurrentPos,
                targetStepOffset,
                frozenConfig.PredictionLeadRatio,
                frozenConfig.PursuitMinTurnAngle,
                effectiveMaxTurnAngle,
                effectiveTurnResponsiveness,
                turnScale,
                pursuitPathSeed,
                nextFlightPathSnapshot,
                targetObservationTicks,
                nextSegmentTicks);
            LogArrivalDecision(
                in context,
                state,
                phaseBefore,
                HomingPhase.Pursuing,
                "pursue",
                projectilePos,
                projectileForward,
                targetCurrentPos,
                targetStepOffset,
                hadLastDistanceSample,
                previousDistanceToTarget,
                distanceToTarget,
                turnAngleToTarget,
                targetPredictionResidual,
                false,
                softReleaseDistance,
                hardReleaseDistance,
                InvalidVector());
            SetExpectedTargetPosition(state, predictedTargetPos);
            ContinueTracking(contribution, state.LockedTarget, nextFlightPathSnapshot);
        }

        void IHitStageModule.Contribute(in HitStageContext context, HitContribution contribution)
        {
            if (contribution == null)
            {
                return;
            }

            HomingState state = context.GetPrivateContext<HomingState>();
            if (state == null || state.Phase != HomingPhase.HitCheckPending)
            {
                return;
            }

            HomingConfig frozenConfig = state.FrozenConfig ?? new HomingConfig();
            bool acceptHit = false;
            bool recoveredTargetHit = false;
            float distanceToTarget = float.NaN;
            bool targetResolved = false;
            if (HomingPathBuilder.TryResolveTargetPosition(state.LockedTarget, out Vector3 targetCurrentPos))
            {
                distanceToTarget = HomingPathBuilder.ComputeDistanceToTarget(
                    context.Projectile != null ? context.Projectile.ExactPosition.Yto0() : context.HitCell.ToVector3Shifted(),
                    targetCurrentPos);
                targetResolved = true;

                if (state.LockedTarget.HasThing)
                {
                    bool targetInsideHitWindow = distanceToTarget <= frozenConfig.HitWindow;
                    bool directHitLockedTarget = context.HitThing == state.LockedTarget.Thing;
                    // 原版发射层可能把本体命中资格过滤掉，但制导模块只关心"此刻这发弹是否已经追回到目标命中窗"。
                    bool filteredTargetHitRecovered = context.HitThing == null
                        && targetInsideHitWindow;
                    acceptHit = targetInsideHitWindow
                        && (directHitLockedTarget || filteredTargetHitRecovered);
                    recoveredTargetHit = filteredTargetHitRecovered;
                }
                else
                {
                    acceptHit = distanceToTarget <= frozenConfig.HitWindow;
                }
            }

            LogHitReview(in context, state, distanceToTarget, acceptHit, targetResolved);
            state.Phase = HomingPhase.Finished;
            if (acceptHit)
            {
                if (recoveredTargetHit)
                {
                    contribution.HasOverrideHitThing = true;
                    contribution.OverrideHitThing = state.LockedTarget.Thing;
                    contribution.HasOverrideHitCell = true;
                    contribution.OverrideHitCell = state.LockedTarget.Cell.IsValid
                        ? state.LockedTarget.Cell
                        : context.Projectile != null
                            ? context.Projectile.Position
                            : context.HitCell;
                }

                return;
            }

            contribution.HasOverrideHitThing = true;
            contribution.OverrideHitThing = null;
            contribution.HasOverrideHitCell = true;
            contribution.OverrideHitCell = context.Projectile != null
                ? context.Projectile.Position
                : context.HitCell;
            contribution.ForceGround = true;
        }

        private static LocalTargetInfo ResolveLockedTarget(in ProjectileInitStageContext context)
        {
            if (context.FinalTarget.IsValid)
            {
                return context.FinalTarget;
            }

            return context.RequestedTarget;
        }

        private static bool TryResolveTargetPosition(LocalTargetInfo target, out Vector3 targetPos)
        {
            return HomingPathBuilder.TryResolveTargetPosition(target, out targetPos);
        }

        private static bool TryResolveInitialDistance(Pawn pawn, LocalTargetInfo target, out float distanceToTarget)
        {
            distanceToTarget = 0f;
            if (pawn == null || !TryResolveTargetPosition(target, out Vector3 targetPos))
            {
                return false;
            }

            distanceToTarget = Vector3.Distance(pawn.DrawPos, targetPos);
            return true;
        }

        private int ResolveSeed(in ProjectileInitStageContext context, LocalTargetInfo lockedTarget)
        {
            int seed = 17;
            seed = unchecked(seed * 31 + SafeHash(context.AttackInstanceId));
            seed = unchecked(seed * 31 + SafeHash(resultId));
            seed = unchecked(seed * 31 + context.EmitCount);
            seed = unchecked(seed * 31 + (context.Pawn != null ? context.Pawn.thingIDNumber : 0));
            seed = unchecked(seed * 31 + (lockedTarget.IsValid ? lockedTarget.Cell.GetHashCode() : 0));
            return seed;
        }

        private static HomingConfig ResolveConfigSnapshot(RangedAttackModuleRuntimeContext context)
        {
            if (context != null && context.Config is HomingConfig typedConfig)
            {
                return typedConfig.CloneTyped();
            }

            return new HomingConfig();
        }

        private static int SafeHash(string value)
        {
            return string.IsNullOrEmpty(value) ? 0 : GenText.StableStringHash(value);
        }

        private static void LogArrivalDecision(
            in ArrivalStageContext context,
            HomingState state,
            HomingPhase phaseBefore,
            HomingPhase phaseAfter,
            string branch,
            Vector3 projectilePos,
            Vector3 projectileForward,
            Vector3 targetCurrentPos,
            Vector3 targetStepOffset,
            bool hadLastDistanceSample,
            float previousDistanceToTarget,
            float distanceToTarget,
            float turnAngleToTarget,
            float targetPredictionResidual,
            bool targetLost,
            float softReleaseDistance,
            float hardReleaseDistance,
            Vector3 releaseExpectedImpactPoint)
        {
            float distanceGrowth = hadLastDistanceSample && !float.IsNaN(distanceToTarget)
                ? distanceToTarget - previousDistanceToTarget
                : float.NaN;
            BdpDiagnostics.AttackExecution(
                "event=tracking_arrival_decision"
                + ", attackId=" + SafeDiagnosticText(context.AttackInstanceId)
                + ", resultId=" + SafeDiagnosticText(context.ResultId)
                + ", emitIndex=" + context.EmitIndex
                + ", projectile=" + DescribeProjectile(context.Projectile)
                + ", phaseBefore=" + phaseBefore
                + ", phaseAfter=" + phaseAfter
                + ", branch=" + SafeDiagnosticText(branch)
                + ", relocksUsed=" + (state != null ? state.RelocksUsed.ToString() : "0")
                + ", lockedTarget=" + DescribeTarget(state != null ? state.LockedTarget : LocalTargetInfo.Invalid)
                + ", projectilePos=" + projectilePos
                + ", projectileForward=" + projectileForward
                + ", targetCurrentPos=" + DescribeVectorOrInvalid(targetCurrentPos)
                + ", targetStepOffset=" + targetStepOffset
                + ", previousDistanceToTarget=" + DescribeFloat(previousDistanceToTarget)
                + ", distanceToTarget=" + DescribeFloat(distanceToTarget)
                + ", distanceGrowth=" + DescribeFloat(distanceGrowth)
                + ", turnAngleToTarget=" + DescribeFloat(turnAngleToTarget)
                + ", targetPredictionResidual=" + DescribeFloat(targetPredictionResidual)
                + ", expectedTargetPosition=" + DescribeVectorOrInvalid(
                    state != null && state.HasExpectedTargetPosition
                        ? state.ExpectedTargetPosition
                        : InvalidVector())
                + ", targetLost=" + targetLost
                + ", softReleaseDistance=" + DescribeFloat(softReleaseDistance)
                + ", hardReleaseDistance=" + DescribeFloat(hardReleaseDistance)
                + ", releaseExpectedImpactPoint=" + DescribeVectorOrInvalid(releaseExpectedImpactPoint));
        }

        private static void LogPathBuild(
            in ArrivalStageContext context,
            HomingState state,
            string pathType,
            Vector3 projectilePos,
            Vector3 projectileForward,
            Vector3 targetCurrentPos,
            Vector3 targetStepOffset,
            float leadRatio,
            float minTurnAngle,
            float maxTurnAngle,
            float turnResponsiveness,
            float turnScale,
            int pathSeed,
            ProjectileFlightPathSnapshot snapshot,
            float observationTicks = 1f,
            float nextSegmentTicks = 1f)
        {
            Vector3 predictedTargetPos = HomingPathBuilder.ComputePredictedTargetPositionWithLead(
                targetCurrentPos,
                targetStepOffset,
                leadRatio,
                observationTicks,
                nextSegmentTicks,
                state != null ? state.FrozenConfig : null,
                pathSeed);
            Vector3 desiredForward = (predictedTargetPos - projectilePos.Yto0()).Yto0();
            Vector3 normalizedProjectileForward = projectileForward.Yto0().normalized;
            Vector3 normalizedDesiredForward = desiredForward.sqrMagnitude > 0.0001f
                ? desiredForward.normalized
                : Vector3.zero;
            float angleError = normalizedProjectileForward.sqrMagnitude > 0.0001f && normalizedDesiredForward.sqrMagnitude > 0.0001f
                ? Vector3.Angle(normalizedProjectileForward, normalizedDesiredForward)
                : 0f;
            float allowedTurnAngle = HomingPathBuilder.ComputeProgressiveTurnAngle(
                angleError,
                minTurnAngle,
                maxTurnAngle,
                turnResponsiveness);
            Vector3 limitedForward = HomingPathBuilder.ComputeProgressiveTurnDirection(
                projectileForward,
                desiredForward,
                minTurnAngle,
                maxTurnAngle,
                turnResponsiveness);

            BdpDiagnostics.AttackExecution(
                "event=tracking_path_build"
                + ", attackId=" + SafeDiagnosticText(context.AttackInstanceId)
                + ", resultId=" + SafeDiagnosticText(context.ResultId)
                + ", emitIndex=" + context.EmitIndex
                + ", projectile=" + DescribeProjectile(context.Projectile)
                + ", pathType=" + SafeDiagnosticText(pathType)
                + ", relocksUsed=" + (state != null ? state.RelocksUsed.ToString() : "0")
                + ", projectilePos=" + projectilePos
                + ", projectileForward=" + projectileForward
                + ", targetCurrentPos=" + targetCurrentPos
                + ", targetStepOffset=" + targetStepOffset
                + ", predictedTargetPos=" + predictedTargetPos
                + ", angleError=" + DescribeFloat(angleError)
                + ", allowedTurnAngle=" + DescribeFloat(allowedTurnAngle)
                + ", minTurnAngle=" + DescribeFloat(minTurnAngle)
                + ", maxTurnAngle=" + DescribeFloat(maxTurnAngle)
                + ", turnResponsiveness=" + DescribeFloat(turnResponsiveness)
                + ", turnScale=" + DescribeFloat(turnScale)
                + ", observationTicks=" + DescribeFloat(observationTicks)
                + ", nextSegmentTicks=" + DescribeFloat(nextSegmentTicks)
                + ", limitedForward=" + limitedForward
                + ", segmentStart=" + DescribeFlightPoint(snapshot != null ? snapshot.Start : Vector3.zero)
                + ", controlA=" + DescribeFlightPoint(snapshot != null ? snapshot.ControlA : Vector3.zero)
                + ", controlB=" + DescribeFlightPoint(snapshot != null ? snapshot.ControlB : Vector3.zero)
                + ", segmentEnd=" + DescribeFlightPoint(snapshot != null ? snapshot.End : Vector3.zero)
                + ", pathLength=" + DescribeFloat(snapshot != null ? snapshot.ApproximateLength : float.NaN));
        }

        private static void LogHitReview(
            in HitStageContext context,
            HomingState state,
            float distanceToTarget,
            bool acceptHit,
            bool targetResolved)
        {
            BdpDiagnostics.AttackExecution(
                "event=tracking_hit_review"
                + ", attackId=" + SafeDiagnosticText(context.AttackInstanceId)
                + ", resultId=" + SafeDiagnosticText(context.ResultId)
                + ", emitIndex=" + context.EmitIndex
                + ", projectile=" + DescribeProjectile(context.Projectile)
                + ", phaseBefore=" + (state != null ? state.Phase.ToString() : "<null>")
                + ", lockedTarget=" + DescribeTarget(state != null ? state.LockedTarget : LocalTargetInfo.Invalid)
                + ", hitThing=" + SafeDiagnosticText(context.HitThing != null ? context.HitThing.ThingID : null)
                + ", hitCell=" + context.HitCell
                + ", targetResolved=" + targetResolved
                + ", distanceToTarget=" + DescribeFloat(distanceToTarget)
                + ", acceptHit=" + acceptHit);
        }

        private static string DescribeProjectile(Projectile projectile)
        {
            return projectile != null ? projectile.ThingID : "<null>";
        }

        private static string DescribeTarget(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return "<invalid>";
            }

            if (target.HasThing && target.Thing != null)
            {
                return target.Thing.ThingID + "@" + target.Thing.Position;
            }

            return target.Cell.ToString();
        }

        private static string SafeDiagnosticText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value;
        }

        private static string DescribeFloat(float value)
        {
            return float.IsNaN(value) ? "<nan>" : value.ToString("F3");
        }

        private static string DescribeVectorOrInvalid(Vector3 value)
        {
            return float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z)
                ? "<invalid>"
                : value.ToString();
        }

        private static string DescribeFlightPoint(Vector3 value)
        {
            return value.ToString();
        }

        private static Vector3 InvalidVector()
        {
            return new Vector3(float.NaN, float.NaN, float.NaN);
        }

        /// <summary>安全读取当前游戏 tick（游戏刻），未就绪时返回 -1。</summary>
        private static int GetSafeCurrentTick()
        {
            try
            {
                return Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>计算两次目标位置观测之间的游戏 tick（游戏刻）间隔。</summary>
        private static float ResolveTargetObservationTicks(HomingState state, int currentTick)
        {
            if (state == null || currentTick < 0 || state.LastTargetObservationTick < 0)
            {
                return 1f;
            }

            return Mathf.Max(1f, currentTick - state.LastTargetObservationTick);
        }

        /// <summary>判断目标是否在命中窗口内且距离仍未持续增大。</summary>
        private static bool IsClosingHitWindowCandidate(
            float distanceToTarget,
            bool hadLastDistanceSample,
            float previousDistanceToTarget,
            HomingConfig config)
        {
            HomingConfig safeConfig = config ?? new HomingConfig();
            if (float.IsNaN(distanceToTarget) || distanceToTarget > safeConfig.HitWindow)
            {
                return false;
            }

            if (!hadLastDistanceSample)
            {
                return true;
            }

            float allowedGrowth = Mathf.Max(0f, safeConfig.LossDistanceGrowthTolerance);
            return distanceToTarget <= previousDistanceToTarget + allowedGrowth;
        }

        private static bool IsTargetLost(
            bool hadLastDistanceSample,
            float previousDistanceToTarget,
            float currentDistanceToTarget,
            float turnAngleToTarget,
            float targetPredictionResidual,
            HomingConfig config)
        {
            HomingConfig safeConfig = config ?? new HomingConfig();
            float residualTolerance = Mathf.Max(0.05f, safeConfig.LossDistanceGrowthTolerance);
            if (float.IsNaN(targetPredictionResidual) || targetPredictionResidual <= residualTolerance)
            {
                return false;
            }

            float safeLossBehindAngle = Mathf.Max(90f, safeConfig.LossBehindAngle);
            if (turnAngleToTarget >= safeLossBehindAngle)
            {
                return true;
            }

            if (!hadLastDistanceSample)
            {
                return false;
            }

            float distanceGrowth = currentDistanceToTarget - previousDistanceToTarget;
            return distanceGrowth > Mathf.Max(0f, safeConfig.LossDistanceGrowthTolerance)
                && turnAngleToTarget >= 90f;
        }

        private static void ContinueTracking(
            ArrivalContribution contribution,
            LocalTargetInfo lockedTarget,
            ProjectileFlightPathSnapshot nextFlightPathSnapshot)
        {
            contribution.HasOverrideContinueFlight = true;
            contribution.OverrideContinueFlight = true;
            contribution.HasNextTarget = true;
            contribution.NextTarget = lockedTarget;
            contribution.HasNextDestination = true;
            contribution.NextDestination = nextFlightPathSnapshot.End;
            contribution.HasNextFlightPathSnapshot = true;
            contribution.NextFlightPathSnapshot = nextFlightPathSnapshot;
        }

        private static void IssueFlyAway(
            in ArrivalStageContext context,
            ArrivalContribution contribution,
            HomingState state,
            Vector3 projectilePos,
            Vector3 projectileForward)
        {
            if (state == null || contribution == null)
            {
                return;
            }

            if (!state.FlyAwayIssued)
            {
                HomingConfig frozenConfig = state.FrozenConfig ?? new HomingConfig();
                ClearReleaseState(state);
                ClearExpectedTargetPosition(state);
                ProjectileFlightPathSnapshot flyAwayPath = HomingPathBuilder.BuildFlyAwayPath(
                    projectilePos,
                    projectileForward,
                    frozenConfig,
                    state.Seed + (context.EmitIndex * 193) + 97);

                state.Phase = HomingPhase.FlyAway;
                state.FlyAwayIssued = true;
                state.FlyAwayEnd = flyAwayPath.End;
                contribution.HasOverrideContinueFlight = true;
                contribution.OverrideContinueFlight = true;
                contribution.HasNextTarget = true;
                contribution.NextTarget = state.LockedTarget.IsValid
                    ? state.LockedTarget
                    : new LocalTargetInfo(flyAwayPath.End.ToIntVec3());
                contribution.HasNextBindingTarget = true;
                contribution.NextBindingTarget = new LocalTargetInfo(flyAwayPath.End.ToIntVec3());
                contribution.HasNextDestination = true;
                contribution.NextDestination = flyAwayPath.End;
                contribution.HasNextFlightPathSnapshot = true;
                contribution.NextFlightPathSnapshot = flyAwayPath;
                return;
            }

            state.Phase = HomingPhase.Finished;
            contribution.HasOverrideContinueFlight = true;
            contribution.OverrideContinueFlight = false;
        }

        private static void IssueRelockOrFlyAway(
            in ArrivalStageContext context,
            ArrivalContribution contribution,
            HomingState state,
            HomingPhase phaseBefore,
            Vector3 projectilePos,
            Vector3 projectileForward,
            Vector3 targetCurrentPos,
            Vector3 targetStepOffset,
            bool hadLastDistanceSample,
            float previousDistanceToTarget,
            float distanceToTarget,
            float turnAngleToTarget,
            bool targetLost,
            float targetObservationTicks,
            float nextSegmentTicks,
            float softReleaseDistance,
            float hardReleaseDistance,
            Vector3 releaseExpectedImpactPoint)
        {
            if (state == null || contribution == null)
            {
                return;
            }

            HomingConfig frozenConfig = state.FrozenConfig ?? new HomingConfig();
            if (state.RelocksUsed < Mathf.Max(0, frozenConfig.MaxRelocks))
            {
                int pathSeed = state.Seed + (context.EmitIndex * 193) + state.RelocksUsed + 1;
                ProjectileFlightPathSnapshot relockFlightPathSnapshot = HomingPathBuilder.BuildRelockPath(
                    projectilePos,
                    projectileForward,
                    targetCurrentPos,
                    targetStepOffset,
                    frozenConfig,
                    pathSeed,
                    targetObservationTicks,
                    nextSegmentTicks,
                    out Vector3 predictedTargetPos);

                state.RelocksUsed += 1;
                state.Phase = HomingPhase.Relocking;
                LogPathBuild(
                    in context,
                    state,
                    "relock",
                    projectilePos,
                    projectileForward,
                    targetCurrentPos,
                    targetStepOffset,
                    frozenConfig.RelockLeadRatio,
                    frozenConfig.RelockMinTurnAngle,
                    frozenConfig.RelockTurnAngle,
                    frozenConfig.RelockTurnResponsiveness,
                    1f,
                    Gen.HashCombineInt(pathSeed, 71),
                    relockFlightPathSnapshot,
                    targetObservationTicks,
                    nextSegmentTicks);
                ClearReleaseState(state);
                SetExpectedTargetPosition(state, predictedTargetPos);
                ContinueTracking(contribution, state.LockedTarget, relockFlightPathSnapshot);
                return;
            }

            ClearReleaseState(state);
            IssueFlyAway(in context, contribution, state, projectilePos, projectileForward);
        }

        private static Vector3 ResolveReleaseExpectedImpactPoint(HomingState state)
        {
            return state != null && state.HasReleaseExpectedImpactPoint
                ? state.ReleaseExpectedImpactPoint
                : InvalidVector();
        }

        /// <summary>计算目标当前位置相对上一段追踪预期点的运动学残差。</summary>
        private static float ResolveTargetPredictionResidual(HomingState state, Vector3 targetCurrentPos)
        {
            if (state == null || !state.HasExpectedTargetPosition)
            {
                return float.NaN;
            }

            return Vector3.Distance(
                state.ExpectedTargetPosition.Yto0(),
                targetCurrentPos.Yto0());
        }

        /// <summary>记录追踪控制器下一段路径使用的预期目标位置。</summary>
        private static void SetExpectedTargetPosition(HomingState state, Vector3 expectedTargetPosition)
        {
            if (state == null)
            {
                return;
            }

            state.HasExpectedTargetPosition = true;
            state.ExpectedTargetPosition = expectedTargetPosition.Yto0();
        }

        /// <summary>清除追踪控制器的预期目标位置。</summary>
        private static void ClearExpectedTargetPosition(HomingState state)
        {
            if (state == null)
            {
                return;
            }

            state.HasExpectedTargetPosition = false;
            state.ExpectedTargetPosition = Vector3.zero;
        }

        private static void ClearReleaseState(HomingState state)
        {
            if (state == null)
            {
                return;
            }

            state.HasReleaseExpectedImpactPoint = false;
            state.ReleaseExpectedImpactPoint = Vector3.zero;
        }
    }
}
