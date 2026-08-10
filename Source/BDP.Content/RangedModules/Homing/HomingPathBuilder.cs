using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Projectiles.RangedFlightProtocol.Projection;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.Homing
{
    /// <summary>
    /// 制导路径构建器。
    /// 负责计算追踪曲线、释放直线、重锁曲线和脱靶飞行路径。
    /// </summary>
    public static class HomingPathBuilder
    {
        /// <summary>尝试解析目标世界坐标。</summary>
        public static bool TryResolveTargetPosition(LocalTargetInfo target, out Vector3 targetPos)
        {
            targetPos = Vector3.zero;
            if (!target.IsValid)
            {
                return false;
            }

            if (target.HasThing)
            {
                Thing thing = target.Thing;
                if (thing == null || !thing.Spawned)
                {
                    return false;
                }

                targetPos = thing.DrawPos.Yto0();
                return true;
            }

            targetPos = target.Cell.ToVector3Shifted().Yto0();
            return true;
        }

        /// <summary>计算弹体到目标的距离。</summary>
        public static float ComputeDistanceToTarget(Vector3 projectilePos, Vector3 targetPos)
        {
            return (targetPos.Yto0() - projectilePos.Yto0()).magnitude;
        }

        /// <summary>计算弹体朝向到目标方向的角度差。</summary>
        public static float ComputeTurnAngleToTarget(Vector3 projectilePos, Vector3 projectileForward, Vector3 targetPos)
        {
            Vector3 flatForward = NormalizeFlat(projectileForward);
            Vector3 toTarget = NormalizeFlat(targetPos - projectilePos);
            if (flatForward.sqrMagnitude <= 0.0001f || toTarget.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            return Vector3.Angle(flatForward, toTarget);
        }

        /// <summary>计算含前导预测的目标位置。</summary>
        public static Vector3 ComputePredictedTargetPosition(
            Vector3 targetCurrentPos,
            Vector3 targetStepOffset,
            HomingConfig config,
            int seed)
        {
            HomingConfig safeConfig = config ?? new HomingConfig();
            return ComputePredictedTargetPosition(
                targetCurrentPos,
                targetStepOffset,
                safeConfig.PredictionLeadRatio,
                safeConfig,
                seed);
        }

        /// <summary>按指定前导比例计算预测位置。</summary>
        public static Vector3 ComputePredictedTargetPositionWithLead(
            Vector3 targetCurrentPos,
            Vector3 targetStepOffset,
            float leadRatio,
            HomingConfig config,
            int seed)
        {
            HomingConfig safeConfig = config ?? new HomingConfig();
            return ComputePredictedTargetPosition(
                targetCurrentPos,
                targetStepOffset,
                leadRatio,
                safeConfig,
                seed);
        }

        /// <summary>构建追踪路径曲线。</summary>
        public static ProjectileFlightPathSnapshot BuildTrackingPath(
            Vector3 projectilePos,
            Vector3 projectileForward,
            Vector3 targetCurrentPos,
            Vector3 targetStepOffset,
            HomingConfig config,
            int seed,
            float turnScale,
            out float effectiveMaxTurnAngle,
            out float effectiveTurnResponsiveness)
        {
            HomingConfig safeConfig = config ?? new HomingConfig();
            float clampedTurnScale = Mathf.Clamp01(turnScale);
            Vector3 predictedTargetPos = ComputePredictedTargetPosition(targetCurrentPos, targetStepOffset, safeConfig, seed);
            effectiveMaxTurnAngle = Mathf.Max(
                safeConfig.PursuitMinTurnAngle,
                safeConfig.MaxTurnAngleWhenEvading * clampedTurnScale);
            effectiveTurnResponsiveness = safeConfig.PursuitTurnResponsiveness * Mathf.Lerp(0.2f, 1f, clampedTurnScale);
            return BuildPursuitCurve(
                projectilePos.Yto0(),
                projectileForward,
                predictedTargetPos,
                safeConfig,
                safeConfig.PursuitMinTurnAngle,
                effectiveMaxTurnAngle,
                effectiveTurnResponsiveness,
                false);
        }

        /// <summary>构建重锁路径。</summary>
        public static ProjectileFlightPathSnapshot BuildRelockPath(
            Vector3 projectilePos,
            Vector3 projectileForward,
            Vector3 targetCurrentPos,
            Vector3 targetStepOffset,
            HomingConfig config,
            int seed)
        {
            HomingConfig safeConfig = config ?? new HomingConfig();
            Vector3 relockTargetPos = ComputePredictedTargetPosition(
                targetCurrentPos,
                targetStepOffset,
                safeConfig.RelockLeadRatio,
                safeConfig,
                Gen.HashCombineInt(seed, 71));
            return BuildPursuitCurve(
                projectilePos.Yto0(),
                projectileForward,
                relockTargetPos,
                safeConfig,
                safeConfig.RelockMinTurnAngle,
                safeConfig.RelockTurnAngle,
                safeConfig.RelockTurnResponsiveness,
                true);
        }

        /// <summary>构建脱靶飞行路径。</summary>
        public static ProjectileFlightPathSnapshot BuildFlyAwayPath(
            Vector3 projectilePos,
            Vector3 projectileForward,
            HomingConfig config,
            int seed)
        {
            HomingConfig safeConfig = config ?? new HomingConfig();
            Vector3 start = projectilePos.Yto0();
            Vector3 forward = NormalizeFlat(projectileForward);
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }

            float flyAwayDistance = Mathf.Max(1.25f, safeConfig.TerminalFlyAwayDistance);
            Vector3 right = NormalizeFlat(Vector3.Cross(Vector3.up, forward));
            if (right.sqrMagnitude <= 0.0001f)
            {
                right = Vector3.right;
            }

            float lateralSign = ResolveSignedVariance(seed, 53) >= 0f ? 1f : -1f;
            float offsetDistance = flyAwayDistance * Mathf.Clamp(Mathf.Max(0f, safeConfig.CurveArcStrength) * 0.2f, 0.08f, 0.32f);
            Vector3 end = start + forward * flyAwayDistance + right * lateralSign * offsetDistance;
            if (flyAwayDistance <= 1.5f)
            {
                return ProjectileFlightPathUtility.CreateLinear(start, end);
            }

            Vector3 controlA = start + forward * flyAwayDistance * 0.28f;
            Vector3 controlB = start + forward * flyAwayDistance * 0.72f + right * lateralSign * offsetDistance;
            return ProjectileFlightPathUtility.CreateCubicBezier(start, controlA, controlB, end);
        }

        /// <summary>解析释放距离（含重锁衰减）。</summary>
        public static void ResolveReleaseDistances(
            HomingConfig config,
            int relocksUsed,
            out float softReleaseDistance,
            out float hardReleaseDistance)
        {
            HomingConfig safeConfig = config ?? new HomingConfig();
            float decay = Mathf.Max(0f, safeConfig.ReleaseDistanceDecayPerRelock) * Mathf.Max(0, relocksUsed);
            float minimumHardReleaseDistance = Mathf.Max(safeConfig.HitWindow + 0.15f, 1f);
            hardReleaseDistance = Mathf.Max(minimumHardReleaseDistance, safeConfig.HardReleaseDistance - decay);
            softReleaseDistance = Mathf.Max(hardReleaseDistance + 0.35f, safeConfig.SoftReleaseDistance - decay);
        }

        /// <summary>计算释放阶段转向缩放。</summary>
        public static float ComputeReleaseTurnScale(
            float distanceToTarget,
            float softReleaseDistance,
            float hardReleaseDistance)
        {
            if (distanceToTarget >= softReleaseDistance)
            {
                return 1f;
            }

            if (distanceToTarget <= hardReleaseDistance)
            {
                return 0f;
            }

            return Mathf.InverseLerp(hardReleaseDistance, softReleaseDistance, distanceToTarget);
        }

        /// <summary>构建释放阶段直线路径。</summary>
        public static ProjectileFlightPathSnapshot BuildReleasePath(
            Vector3 projectilePos,
            Vector3 projectileForward,
            Vector3 targetCurrentPos,
            Vector3 targetStepOffset,
            float leadRatio,
            HomingConfig config,
            int seed,
            out Vector3 expectedImpactPoint)
        {
            HomingConfig safeConfig = config ?? new HomingConfig();
            Vector3 start = projectilePos.Yto0();
            Vector3 predictedTargetPos = ComputePredictedTargetPositionWithLead(
                targetCurrentPos,
                targetStepOffset,
                leadRatio,
                safeConfig,
                seed);
            Vector3 releaseForward = NormalizeFlat(projectileForward);
            if (releaseForward.sqrMagnitude <= 0.0001f)
            {
                releaseForward = NormalizeFlat(predictedTargetPos - start);
            }

            if (releaseForward.sqrMagnitude <= 0.0001f)
            {
                releaseForward = Vector3.forward;
            }

            float projectedDistance = Vector3.Dot(predictedTargetPos - start, releaseForward);
            float minimumReleaseTravel = Mathf.Max(safeConfig.HitWindow + 0.15f, 0.9f);
            float releaseTravel = Mathf.Max(minimumReleaseTravel, projectedDistance);
            expectedImpactPoint = start + releaseForward * releaseTravel;
            return ProjectileFlightPathUtility.CreateLinear(start, expectedImpactPoint);
        }

        /// <summary>计算渐进转向角度。</summary>
        public static float ComputeProgressiveTurnAngle(
            float angleError,
            float minTurnAngle,
            float maxTurnAngle,
            float turnResponsiveness)
        {
            float safeMinTurnAngle = Mathf.Max(0f, minTurnAngle);
            float safeMaxTurnAngle = Mathf.Max(safeMinTurnAngle, maxTurnAngle);
            float safeTurnResponsiveness = Mathf.Max(0f, turnResponsiveness);
            float progressiveTurnAngle = angleError * safeTurnResponsiveness;
            return Mathf.Clamp(progressiveTurnAngle, safeMinTurnAngle, safeMaxTurnAngle);
        }

        /// <summary>计算渐进转向方向。</summary>
        public static Vector3 ComputeProgressiveTurnDirection(
            Vector3 currentForward,
            Vector3 desiredForward,
            float minTurnAngle,
            float maxTurnAngle,
            float turnResponsiveness)
        {
            Vector3 flatCurrent = NormalizeFlat(currentForward);
            Vector3 flatDesired = NormalizeFlat(desiredForward);
            if (flatCurrent.sqrMagnitude <= 0.0001f)
            {
                return flatDesired;
            }

            if (flatDesired.sqrMagnitude <= 0.0001f)
            {
                return flatCurrent;
            }

            float angleError = Vector3.Angle(flatCurrent, flatDesired);
            float progressiveTurnAngle = ComputeProgressiveTurnAngle(
                angleError,
                minTurnAngle,
                maxTurnAngle,
                turnResponsiveness);
            float radians = Mathf.Max(0f, progressiveTurnAngle) * Mathf.Deg2Rad;
            Vector3 rotated = Vector3.RotateTowards(flatCurrent, flatDesired, radians, 0f);
            return NormalizeFlat(rotated);
        }

        private static ProjectileFlightPathSnapshot BuildPursuitCurve(
            Vector3 start,
            Vector3 projectileForward,
            Vector3 targetPos,
            HomingConfig config,
            float minTurnAngle,
            float maxTurnAngle,
            float turnResponsiveness,
            bool shortenForRecovery)
        {
            Vector3 endTarget = targetPos.Yto0();
            Vector3 toTarget = endTarget - start;
            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget <= 0.001f)
            {
                return ProjectileFlightPathUtility.CreateLinear(start, start);
            }

            Vector3 toTargetDir = toTarget / distanceToTarget;
            Vector3 forward = NormalizeFlat(projectileForward);
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = toTargetDir;
            }

            Vector3 limitedForward = ComputeProgressiveTurnDirection(
                forward,
                toTargetDir,
                minTurnAngle,
                maxTurnAngle,
                turnResponsiveness);

            Vector3 end = shortenForRecovery
                ? ResolveRecoveryEnd(start, limitedForward, endTarget, config)
                : endTarget;
            Vector3 chaseVector = end - start;
            float chaseDistance = chaseVector.magnitude;
            if (chaseDistance <= 0.001f)
            {
                return ProjectileFlightPathUtility.CreateLinear(start, end);
            }

            Vector3 chaseDir = chaseVector / chaseDistance;
            float shortDistanceThreshold = Mathf.Max(1.1f, config.HitWindow * 2f);
            if (chaseDistance <= shortDistanceThreshold && Vector3.Angle(limitedForward, chaseDir) <= 15f)
            {
                return ProjectileFlightPathUtility.CreateLinear(start, end);
            }

            float arcScale = Mathf.Lerp(0.72f, 1.08f, Mathf.Clamp01(Mathf.Max(0f, config.CurveArcStrength)));
            float inertiaDistance = Mathf.Max(0.2f, chaseDistance * Mathf.Clamp01(0.18f + Mathf.Max(0f, config.InertiaWeight) * 0.28f)) * arcScale;
            float captureDistance = Mathf.Max(0.2f, chaseDistance * Mathf.Clamp01(0.16f + Mathf.Max(0f, config.CaptureWeight) * 0.26f)) * arcScale;
            // 起手方向直接沿用当前飞行朝向，保证段边界切线连续——重定向不在段与段的交接点产生折角。
            // 渐进转向完全由段内控制点完成，曲线终点方向仍受 maxTurnAngle 限制，多段拼接成一条连续弧线。
            Vector3 launchDir = NormalizeFlat(forward);
            if (launchDir.sqrMagnitude <= 0.0001f)
            {
                launchDir = chaseDir;
            }

            Vector3 arrivalDir = ComputeProgressiveTurnDirection(
                launchDir,
                toTargetDir,
                0f,
                Mathf.Max(4f, maxTurnAngle * (shortenForRecovery ? 0.8f : 0.6f)),
                turnResponsiveness * 0.7f);
            if (arrivalDir.sqrMagnitude <= 0.0001f)
            {
                arrivalDir = chaseDir;
            }

            if (chaseDistance <= shortDistanceThreshold)
            {
                Vector3 shortControlA = start + launchDir * chaseDistance * 0.32f;
                Vector3 shortControlB = end - arrivalDir * chaseDistance * 0.22f;
                return ProjectileFlightPathUtility.CreateCubicBezier(start, shortControlA, shortControlB, end);
            }

            Vector3 controlA = start + launchDir * inertiaDistance;
            Vector3 controlB = end - arrivalDir * captureDistance;
            return ProjectileFlightPathUtility.CreateCubicBezier(start, controlA, controlB, end);
        }

        private static Vector3 ResolveRecoveryEnd(
            Vector3 start,
            Vector3 limitedForward,
            Vector3 targetPos,
            HomingConfig config)
        {
            Vector3 toTarget = targetPos - start;
            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget <= 0.001f)
            {
                return targetPos;
            }

            Vector3 targetDir = toTarget / distanceToTarget;
            Vector3 recoveryDir = NormalizeFlat(Vector3.Lerp(limitedForward, targetDir, 0.28f));
            if (recoveryDir.sqrMagnitude <= 0.0001f)
            {
                recoveryDir = targetDir;
            }

            float minimumRecoveryDistance = Mathf.Max(config.HitWindow * 2f, 1.5f);
            float configuredRecoveryDistance = Mathf.Max(minimumRecoveryDistance, config.RelockWindow);
            if (distanceToTarget <= configuredRecoveryDistance)
            {
                return targetPos;
            }

            return start + recoveryDir * configuredRecoveryDistance;
        }

        private static Vector3 ComputePredictedTargetPosition(
            Vector3 targetCurrentPos,
            Vector3 targetStepOffset,
            float leadRatio,
            HomingConfig config,
            int seed)
        {
            float variance = ResolveSignedVariance(seed, 17) * Mathf.Max(0f, config.PerEmitVariance);
            float effectiveLeadRatio = Mathf.Max(0f, leadRatio * (1f + variance));
            return targetCurrentPos.Yto0() + targetStepOffset.Yto0() * effectiveLeadRatio;
        }

        private static Vector3 NormalizeFlat(Vector3 value)
        {
            Vector3 flatValue = value.Yto0();
            return flatValue.sqrMagnitude <= 0.0001f ? Vector3.zero : flatValue.normalized;
        }

        private static float ResolveSignedVariance(int seed, int salt)
        {
            int combinedSeed = Gen.HashCombineInt(seed, salt);
            Rand.PushState(combinedSeed);
            try
            {
                return Rand.Value * 2f - 1f;
            }
            finally
            {
                Rand.PopState();
            }
        }
    }
}
