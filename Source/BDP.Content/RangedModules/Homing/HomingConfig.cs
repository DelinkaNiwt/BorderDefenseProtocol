using BDP.Core.AttackExecution;
using Verse;

namespace BDP.Content.RangedModules.Homing
{
    /// <summary>
    /// 制导追踪模块配置。
    /// </summary>
    public sealed class HomingConfig : RangedModuleConfigNode, IExposable
    {
        /// <summary>首段触发比例。</summary>
        public float InitialSegmentTriggerRatio = 0.5f;
        /// <summary>初始目标为空地时的首段触发比例。</summary>
        public float GroundTargetInitialSegmentTriggerRatio = 1f;
        /// <summary>最大重锁次数。</summary>
        public int MaxRelocks = 2;
        /// <summary>命中判定窗口半径。</summary>
        public float HitWindow = 0.85f;
        /// <summary>目标落到身后时视为丢失的角度阈值。</summary>
        public float LossBehindAngle = 115f;
        /// <summary>目标距离增长容忍度，超过此值且角度≥90°视为丢失。</summary>
        public float LossDistanceGrowthTolerance = 0.2f;
        /// <summary>重锁飞行距离。</summary>
        public float RelockWindow = 7.5f;
        /// <summary>最小接近距离。</summary>
        public float MinClosingDistance = 0.05f;
        /// <summary>回避时最大转角。</summary>
        public float MaxTurnAngleWhenEvading = 18f;
        /// <summary>追踪时最小转角。</summary>
        public float PursuitMinTurnAngle = 0f;
        /// <summary>追踪转向响应度。</summary>
        public float PursuitTurnResponsiveness = 0.3f;
        /// <summary>静态目标角度旁路距离。</summary>
        public float StaticTargetAngleBypassDistance = 0.18f;
        /// <summary>预测前导比例。</summary>
        public float PredictionLeadRatio = 0.35f;
        /// <summary>重锁前导比例。</summary>
        public float RelockLeadRatio = 0.18f;
        /// <summary>重锁转角。</summary>
        public float RelockTurnAngle = 26f;
        /// <summary>重锁最小转角。</summary>
        public float RelockMinTurnAngle = 2f;
        /// <summary>重锁转向响应度。</summary>
        public float RelockTurnResponsiveness = 0.36f;
        /// <summary>软释放距离。</summary>
        public float SoftReleaseDistance = 2.4f;
        /// <summary>硬释放距离。</summary>
        public float HardReleaseDistance = 1.6f;
        /// <summary>每次重锁后释放距离衰减量。</summary>
        public float ReleaseDistanceDecayPerRelock = 0.3f;
        /// <summary>曲线弧度强度。</summary>
        public float CurveArcStrength = 0.45f;
        /// <summary>惯性权重。</summary>
        public float InertiaWeight = 0.7f;
        /// <summary>捕获权重。</summary>
        public float CaptureWeight = 0.85f;
        /// <summary>终端脱靶飞行距离。</summary>
        public float TerminalFlyAwayDistance = 5.5f;
        /// <summary>每发方差。</summary>
        public float PerEmitVariance = 0.2f;
        /// <summary>是否允许瞄准空地并在落点检索目标。</summary>
        public bool AllowGroundTarget;
        /// <summary>落点检索半径；零表示关闭检索。</summary>
        public float AcquireRadius;
        /// <summary>落点检索是否要求视线。</summary>
        public bool AcquireRequireLineOfSight = true;

        public override RangedModuleConfigNode Clone()
        {
            return CloneTyped();
        }

        public HomingConfig CloneTyped()
        {
            return new HomingConfig
            {
                InitialSegmentTriggerRatio = InitialSegmentTriggerRatio,
                GroundTargetInitialSegmentTriggerRatio = GroundTargetInitialSegmentTriggerRatio,
                MaxRelocks = MaxRelocks,
                HitWindow = HitWindow,
                LossBehindAngle = LossBehindAngle,
                LossDistanceGrowthTolerance = LossDistanceGrowthTolerance,
                RelockWindow = RelockWindow,
                MinClosingDistance = MinClosingDistance,
                MaxTurnAngleWhenEvading = MaxTurnAngleWhenEvading,
                PursuitMinTurnAngle = PursuitMinTurnAngle,
                PursuitTurnResponsiveness = PursuitTurnResponsiveness,
                StaticTargetAngleBypassDistance = StaticTargetAngleBypassDistance,
                PredictionLeadRatio = PredictionLeadRatio,
                RelockLeadRatio = RelockLeadRatio,
                RelockTurnAngle = RelockTurnAngle,
                RelockMinTurnAngle = RelockMinTurnAngle,
                RelockTurnResponsiveness = RelockTurnResponsiveness,
                SoftReleaseDistance = SoftReleaseDistance,
                HardReleaseDistance = HardReleaseDistance,
                ReleaseDistanceDecayPerRelock = ReleaseDistanceDecayPerRelock,
                CurveArcStrength = CurveArcStrength,
                InertiaWeight = InertiaWeight,
                CaptureWeight = CaptureWeight,
                TerminalFlyAwayDistance = TerminalFlyAwayDistance,
                PerEmitVariance = PerEmitVariance,
                AllowGroundTarget = AllowGroundTarget,
                AcquireRadius = AcquireRadius,
                AcquireRequireLineOfSight = AcquireRequireLineOfSight
            };
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref InitialSegmentTriggerRatio, "initialSegmentTriggerRatio", 0.5f);
            Scribe_Values.Look(ref GroundTargetInitialSegmentTriggerRatio, "groundTargetInitialSegmentTriggerRatio", 1f);
            Scribe_Values.Look(ref MaxRelocks, "maxRelocks", 2);
            Scribe_Values.Look(ref HitWindow, "hitWindow", 0.85f);
            Scribe_Values.Look(ref LossBehindAngle, "lossBehindAngle", 115f);
            Scribe_Values.Look(ref LossDistanceGrowthTolerance, "lossDistanceGrowthTolerance", 0.2f);
            Scribe_Values.Look(ref RelockWindow, "relockWindow", 7.5f);
            Scribe_Values.Look(ref MinClosingDistance, "minClosingDistance", 0.05f);
            Scribe_Values.Look(ref MaxTurnAngleWhenEvading, "maxTurnAngleWhenEvading", 18f);
            Scribe_Values.Look(ref PursuitMinTurnAngle, "pursuitMinTurnAngle", 0f);
            Scribe_Values.Look(ref PursuitTurnResponsiveness, "pursuitTurnResponsiveness", 0.3f);
            Scribe_Values.Look(ref StaticTargetAngleBypassDistance, "staticTargetAngleBypassDistance", 0.18f);
            Scribe_Values.Look(ref PredictionLeadRatio, "predictionLeadRatio", 0.35f);
            Scribe_Values.Look(ref RelockLeadRatio, "relockLeadRatio", 0.18f);
            Scribe_Values.Look(ref RelockTurnAngle, "relockTurnAngle", 26f);
            Scribe_Values.Look(ref RelockMinTurnAngle, "relockMinTurnAngle", 2f);
            Scribe_Values.Look(ref RelockTurnResponsiveness, "relockTurnResponsiveness", 0.36f);
            Scribe_Values.Look(ref SoftReleaseDistance, "softReleaseDistance", 2.4f);
            Scribe_Values.Look(ref HardReleaseDistance, "hardReleaseDistance", 1.6f);
            Scribe_Values.Look(ref ReleaseDistanceDecayPerRelock, "releaseDistanceDecayPerRelock", 0.3f);
            Scribe_Values.Look(ref CurveArcStrength, "curveArcStrength", 0.45f);
            Scribe_Values.Look(ref InertiaWeight, "inertiaWeight", 0.7f);
            Scribe_Values.Look(ref CaptureWeight, "captureWeight", 0.85f);
            Scribe_Values.Look(ref TerminalFlyAwayDistance, "terminalFlyAwayDistance", 5.5f);
            Scribe_Values.Look(ref PerEmitVariance, "perEmitVariance", 0.2f);
            Scribe_Values.Look(ref AllowGroundTarget, "allowGroundTarget", false);
            Scribe_Values.Look(ref AcquireRadius, "acquireRadius", 0f);
            Scribe_Values.Look(ref AcquireRequireLineOfSight, "acquireRequireLineOfSight", true);
        }
    }
}
