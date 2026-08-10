using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedModules.Runtime;
using UnityEngine;
using Verse;

namespace BDP.Content.RangedModules.Homing
{
    /// <summary>
    /// 制导追踪模块私有状态。
    /// </summary>
    public sealed class HomingState : IRangedModulePrivateContext
    {
        /// <summary>当前冻结的配置快照。</summary>
        public HomingConfig FrozenConfig { get; set; } = new HomingConfig();

        /// <summary>锁定目标。</summary>
        public LocalTargetInfo LockedTarget { get; set; } = LocalTargetInfo.Invalid;

        /// <summary>当前追踪阶段。</summary>
        public HomingPhase Phase { get; set; } = HomingPhase.Pursuing;

        /// <summary>已使用重锁次数。</summary>
        public int RelocksUsed { get; set; }

        /// <summary>是否存在上一次观察到的目标位置。</summary>
        public bool HasLastObservedTargetPos { get; set; }

        /// <summary>上一次观察到的目标位置。</summary>
        public Vector3 LastObservedTargetPos { get; set; } = Vector3.zero;

        /// <summary>是否存在上一次距离采样。</summary>
        public bool HasLastDistanceSample { get; set; }

        /// <summary>上一次到目标的距离。</summary>
        public float LastDistanceToTarget { get; set; }

        /// <summary>稳定随机种子。</summary>
        public int Seed { get; set; }

        /// <summary>是否已发出脱靶飞行指令。</summary>
        public bool FlyAwayIssued { get; set; }

        /// <summary>脱靶飞行终点。</summary>
        public Vector3 FlyAwayEnd { get; set; } = Vector3.zero;

        /// <summary>是否存在释放阶段预期命中点。</summary>
        public bool HasReleaseExpectedImpactPoint { get; set; }

        /// <summary>释放阶段预期命中点。</summary>
        public Vector3 ReleaseExpectedImpactPoint { get; set; } = Vector3.zero;

        public IAttackContextNode Clone()
        {
            return new HomingState
            {
                FrozenConfig = FrozenConfig != null ? FrozenConfig.CloneTyped() : new HomingConfig(),
                LockedTarget = LockedTarget,
                Phase = Phase,
                RelocksUsed = RelocksUsed,
                HasLastObservedTargetPos = HasLastObservedTargetPos,
                LastObservedTargetPos = LastObservedTargetPos,
                HasLastDistanceSample = HasLastDistanceSample,
                LastDistanceToTarget = LastDistanceToTarget,
                Seed = Seed,
                FlyAwayIssued = FlyAwayIssued,
                FlyAwayEnd = FlyAwayEnd,
                HasReleaseExpectedImpactPoint = HasReleaseExpectedImpactPoint,
                ReleaseExpectedImpactPoint = ReleaseExpectedImpactPoint
            };
        }

        public void ExposeData()
        {
            HomingConfig frozenConfig = FrozenConfig;
            LocalTargetInfo lockedTarget = LockedTarget;
            HomingPhase phase = Phase;
            int relocksUsed = RelocksUsed;
            bool hasLastObservedTargetPos = HasLastObservedTargetPos;
            Vector3 lastObservedTargetPos = LastObservedTargetPos;
            bool hasLastDistanceSample = HasLastDistanceSample;
            float lastDistanceToTarget = LastDistanceToTarget;
            int seed = Seed;
            bool flyAwayIssued = FlyAwayIssued;
            Vector3 flyAwayEnd = FlyAwayEnd;
            bool hasReleaseExpectedImpactPoint = HasReleaseExpectedImpactPoint;
            Vector3 releaseExpectedImpactPoint = ReleaseExpectedImpactPoint;

            Scribe_Deep.Look(ref frozenConfig, "frozenConfig");
            Scribe_TargetInfo.Look(ref lockedTarget, "lockedTarget");
            Scribe_Values.Look(ref phase, "phase", HomingPhase.Pursuing);
            Scribe_Values.Look(ref relocksUsed, "relocksUsed", 0);
            Scribe_Values.Look(ref hasLastObservedTargetPos, "hasLastObservedTargetPos", false);
            Scribe_Values.Look(ref lastObservedTargetPos, "lastObservedTargetPos");
            Scribe_Values.Look(ref hasLastDistanceSample, "hasLastDistanceSample", false);
            Scribe_Values.Look(ref lastDistanceToTarget, "lastDistanceToTarget", 0f);
            Scribe_Values.Look(ref seed, "seed", 0);
            Scribe_Values.Look(ref flyAwayIssued, "flyAwayIssued", false);
            Scribe_Values.Look(ref flyAwayEnd, "flyAwayEnd");
            Scribe_Values.Look(ref hasReleaseExpectedImpactPoint, "hasReleaseExpectedImpactPoint", false);
            Scribe_Values.Look(ref releaseExpectedImpactPoint, "releaseExpectedImpactPoint");

            FrozenConfig = frozenConfig ?? new HomingConfig();
            LockedTarget = lockedTarget;
            Phase = phase;
            RelocksUsed = relocksUsed;
            HasLastObservedTargetPos = hasLastObservedTargetPos;
            LastObservedTargetPos = lastObservedTargetPos;
            HasLastDistanceSample = hasLastDistanceSample;
            LastDistanceToTarget = lastDistanceToTarget;
            Seed = seed;
            FlyAwayIssued = flyAwayIssued;
            FlyAwayEnd = flyAwayEnd;
            HasReleaseExpectedImpactPoint = hasReleaseExpectedImpactPoint;
            ReleaseExpectedImpactPoint = releaseExpectedImpactPoint;
        }
    }

    /// <summary>
    /// 制导追踪阶段。
    /// </summary>
    public enum HomingPhase
    {
        /// <summary>追踪中。</summary>
        Pursuing = 0,
        /// <summary>已丢失目标。</summary>
        Lost = 1,
        /// <summary>重锁中。</summary>
        Relocking = 2,
        /// <summary>已释放。</summary>
        Released = 3,
        /// <summary>待命中检查。</summary>
        HitCheckPending = 4,
        /// <summary>脱靶飞行。</summary>
        FlyAway = 5,
        /// <summary>已完成。</summary>
        Finished = 6
    }
}
