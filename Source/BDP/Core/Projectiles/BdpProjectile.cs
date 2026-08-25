using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Projectiles.RangedFlightProtocol;
using BDP.Core.Projectiles.RangedFlightProtocol.Collision;
using BDP.Core.Projectiles.RangedFlightProtocol.Effects;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Projectiles.Interaction;
using BDP.Core.Projectiles.RangedFlightProtocol.Projection;
using BDP.Core.Projectiles.Visual;
using BDP.Core.Semantics;
using BDP.Support.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BDP.Core.Projectiles
{
    /// <summary>
    /// BDP 统一投射物宿主桥。
    /// 它始终继承原版 Bullet，只负责承接 launch plan、调用飞行协议，并消费中性 ImpactPlan。
    /// </summary>
    public class BdpProjectile : Bullet, IBdpSemanticCarrier, IAttackEffectTraceCarrier
    {
        /// <summary>
        /// 当前投射物所属攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前投射物所属正式结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前投射物携带的语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前投射物已经冻结的拦截器与伤害护盾交互策略。
        /// </summary>
        public ProjectileInteractionPolicy CurrentInteractionPolicy
        {
            get { return launchPlan != null ? launchPlan.InteractionPolicy : null; }
        }

        /// <summary>
        /// 当前宿主复用的飞行协议服务。
        /// </summary>
        private readonly RangedFlightProtocolService rangedFlightProtocolService = RangedFlightProtocolSurfaceAccess.Resolve();

        /// <summary>
        /// 当前投射物绑定的视觉附加宿主。
        /// 它只负责广播中性视觉事实，不参与战斗主逻辑。
        /// </summary>
        private readonly ProjectileVisualAttachmentHost visualAttachmentHost = new ProjectileVisualAttachmentHost();

        /// <summary>
        /// 当前宿主绑定的初始化计划。
        /// </summary>
        private ProjectileInitPlan launchPlan;

        /// <summary>
        /// 当前宿主维护的飞行阶段记录。
        /// </summary>
        private FlightRecord currentFlightRecord;

        /// <summary>
        /// 当前宿主绑定的飞行路径快照真值。
        /// 宿主的真实位置、朝向与续段时长都以它为准。
        /// </summary>
        private ProjectileFlightPathSnapshot currentFlightPathSnapshot;

        /// <summary>
        /// 当前段飞行路径绑定时的起始时长。
        /// 它服务当前段进度求值与存读档恢复。
        /// </summary>
        private float currentFlightPathStartingTicksToImpact;

        /// <summary>
        /// 当前宿主累计的飞行速度 Tick 校正余量。
        /// 它把协议倍率平滑折算到原版 `ticksToImpact`。
        /// </summary>
        private float speedTickRemainder;

        /// <summary>
        /// 当前段客观碰撞扫描结果。
        /// 它只服务当前宿主管理段的运行态与诊断态复用，不进入存档。
        /// </summary>
        private SegmentCollisionRecord currentSegmentCollisionRecord;

        /// <summary>
        /// 当前一次阻挡命中收束暂存的客观阻挡体。
        /// </summary>
        private Thing pendingObjectiveBlockerImpactThing;

        /// <summary>
        /// 当前一次阻挡命中收束暂存的阻挡格。
        /// </summary>
        private IntVec3 pendingObjectiveBlockerImpactCell = IntVec3.Invalid;

        /// <summary>
        /// 当前一次阻挡命中收束暂存的真实命中位置。
        /// 该位置只在当前一次 Impact 收束期内覆盖视觉终止与落地点采样。
        /// </summary>
        private Vector3? pendingObjectiveBlockerExactPosition;

        /// <summary>
        /// 当前投射物为终止视觉样本冻结的真实终止点。
        /// 它只服务“同 tick 已终止，但视觉样本要在 TickInterval 收尾补发”的桥接，不参与飞行业务，也不进入存档。
        /// </summary>
        private Vector3? terminalVisualExactPosition;

        /// <summary>
        /// 当前投射物的视觉附加件是否已经收到终止通知。
        /// 它用于阻止重复终止广播。
        /// </summary>
        private bool visualAttachmentsTerminated;

        /// <summary>
        /// 当前投射物是否正在执行 `TickInterval（间隔推进）`。
        /// 它用于把“最终样本输出”排在“终止通知”之前。
        /// </summary>
        private bool isProcessingVisualTickInterval;

        /// <summary>
        /// 绑定当前投射物初始化计划。
        /// </summary>
        internal void BindLaunchPlan(ProjectileInitPlan plan)
        {
            launchPlan = plan;
            SemanticContext = plan != null ? plan.SemanticContext : null;
            // projectile 后半段固定消费 plan.ModuleContextSnapshot，不回头重建玩家操作段临时状态。
            currentFlightRecord = null;
            currentFlightPathSnapshot = null;
            currentFlightPathStartingTicksToImpact = 0f;
            speedTickRemainder = 0f;
            currentSegmentCollisionRecord = null;
            ClearPendingObjectiveBlockerImpactAnchor();
            ClearTerminalVisualExactPosition();
            visualAttachmentsTerminated = false;
        }

        /// <summary>
        /// 存读档当前投射物正式计划与飞行状态。
        /// 读档恢复后继续消费冻结计划，不回头重建上游会话。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();

            string attackInstanceId = AttackInstanceId;
            string resultId = ResultId;
            Scribe_Values.Look(ref attackInstanceId, "bdpAttackInstanceId");
            Scribe_Values.Look(ref resultId, "bdpResultId");
            Scribe_Deep.Look(ref launchPlan, "bdpLaunchPlan");
            Scribe_Deep.Look(ref currentFlightRecord, "bdpCurrentFlightRecord");
            Scribe_Deep.Look(ref currentFlightPathSnapshot, "bdpCurrentFlightPathSnapshot");
            Scribe_Values.Look(ref currentFlightPathStartingTicksToImpact, "bdpCurrentFlightStartingTicksToImpact", 0f);
            Scribe_Values.Look(ref speedTickRemainder, "bdpSpeedTickRemainder", 0f);

            AttackInstanceId = attackInstanceId;
            ResultId = resultId;

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                SemanticContext = launchPlan != null ? launchPlan.SemanticContext : null;
                if (string.IsNullOrWhiteSpace(AttackInstanceId))
                {
                    AttackInstanceId = launchPlan != null ? launchPlan.AttackInstanceId : null;
                }

                if (string.IsNullOrWhiteSpace(ResultId))
                {
                    ResultId = launchPlan != null ? launchPlan.ResultId : null;
                }

                if (currentFlightPathSnapshot != null)
                {
                    BindFlightPathSnapshot(currentFlightPathSnapshot);
                    destination = currentFlightPathSnapshot.End;
                }

                InitializeVisualAttachments();
                NotifyVisualRestored();
            }
        }

        /// <summary>
        /// 发射当前投射物，并把当前段的时间计算输入归一到同一高度平面。
        /// 这里不改原版公式，只纠正原版公式收到的段数据。
        /// </summary>
        /// <param name="launcher">发射者。</param>
        /// <param name="origin">原始发射点。</param>
        /// <param name="usedTarget">物理飞行目标。</param>
        /// <param name="intendedTarget">意图目标。</param>
        /// <param name="hitFlags">命中类别。</param>
        /// <param name="preventFriendlyFire">是否阻止友伤。</param>
        /// <param name="equipment">武器实体。</param>
        /// <param name="targetCoverDef">掩体定义。</param>
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            if (launchPlan != null)
            {
                // 原版 Launch 已将 Def 与武器特性合并到实例停止力；构型倍率在此对最终实例值生效。
                stoppingPower *= launchPlan.InitialStoppingPowerFactor;
            }
            Vector3 vanillaDestination = destination;
            NormalizeFlightSegmentToSharedHeight();
            ProjectileFlightPathSnapshot initialFlightPathSnapshot = launchPlan != null
                ? launchPlan.InitialFlightPathSnapshot
                : null;
            initialFlightPathSnapshot = AlignFlightPathSnapshotStart(initialFlightPathSnapshot, this.origin);
            if (initialFlightPathSnapshot == null && launchPlan != null && launchPlan.HasInitialSegmentTriggerRatio)
            {
                initialFlightPathSnapshot = ResolveConfiguredInitialFlightPathSnapshot(
                    BuildLinearFlightPathSnapshot(this.origin, destination));
            }
            BindFlightPathSnapshot(initialFlightPathSnapshot ?? BuildLinearFlightPathSnapshot(this.origin, destination));
            destination = currentFlightPathSnapshot != null
                ? currentFlightPathSnapshot.End
                : destination;
            ResetFlightDurationFromCurrentSegment();
            LogLaunchSnapshotApplied(vanillaDestination, initialFlightPathSnapshot);
            InitializeVisualAttachments();
            NotifyVisualLaunch();
        }

        /// <summary>
        /// 返回当前投射物的真实飞行位置。
        /// 续段后允许把视觉位移真值与 vanilla 拦截起点适配分离。
        /// </summary>
        public override Vector3 ExactPosition
        {
            get
            {
                if (pendingObjectiveBlockerExactPosition.HasValue)
                {
                    return pendingObjectiveBlockerExactPosition.Value;
                }

                if (currentFlightPathSnapshot == null)
                {
                    return base.ExactPosition;
                }

                float progress = ResolveCurrentFlightProgress();
                Vector3 position = ProjectileFlightPathUtility.EvaluatePosition(currentFlightPathSnapshot, progress);
                return position.Yto0() + Vector3.up * def.Altitude;
            }
        }

        /// <summary>
        /// 返回当前投射物的真实飞行朝向。
        /// </summary>
        public override Quaternion ExactRotation
        {
            get
            {
                if (currentFlightPathSnapshot == null)
                {
                    return base.ExactRotation;
                }

                Vector3 tangent = ProjectileFlightPathUtility.EvaluateTangent(currentFlightPathSnapshot, ResolveCurrentFlightProgress());
                return tangent.sqrMagnitude <= 0.0001f
                    ? base.ExactRotation
                    : Quaternion.LookRotation(tangent);
            }
        }

        /// <summary>
        /// 推进当前投射物的飞行阶段。
        /// </summary>
        protected override void Tick()
        {
            if (launchPlan != null)
            {
                currentFlightRecord = rangedFlightProtocolService.ExecuteFlight(this, launchPlan, currentFlightRecord);
                if (currentFlightRecord != null)
                {
                    if (currentFlightRecord.RedirectDestination.HasValue)
                    {
                        destination = currentFlightRecord.RedirectDestination.Value;
                    }

                    if (currentFlightRecord.CurrentTarget.IsValid)
                    {
                        intendedTarget = currentFlightRecord.CurrentTarget;
                    }

                    ApplyLiveTargetSemantics(
                        currentFlightRecord.CurrentTarget,
                        currentFlightRecord.RedirectDestination ?? currentFlightRecord.CurrentDestination,
                        "flight_record");
                    ApplySpeedFactorToFlight(currentFlightRecord.SpeedFactor);
                }
            }

            base.Tick();
        }

        /// <summary>
        /// 在原版真实推进完成后输出一条中性的飞行样本。
        /// 它只发布“推进前 -> 推进后”的真实平面轨迹，不参与任何业务拆段。
        /// </summary>
        /// <param name="delta">本次推进刻数。</param>
        protected override void TickInterval(int delta)
        {
            Vector3 sampleStart = NormalizeFlightPlanePoint(ExactPosition);
            Map sampleMap = base.Map;
            isProcessingVisualTickInterval = true;
            try
            {
                base.TickInterval(delta);
            }
            finally
            {
                isProcessingVisualTickInterval = false;
            }

            Vector3 sampleEnd = ResolveVisualSampleEnd(sampleStart);
            PublishVisualFlightSample(sampleMap, sampleStart, sampleEnd, delta);
            if (Destroyed)
            {
                NotifyVisualTerminate(sampleMap, sampleEnd);
            }
        }

        /// <summary>
        /// 在当前段自然到点时决定是进入下一段，还是回落原版到点命中。
        /// 只有“已经飞到当前规划段终点”这一种情形才允许续段。
        /// </summary>
        protected override void ImpactSomething()
        {
            ArrivalRecord arrival = rangedFlightProtocolService.ExecuteArrival(this, launchPlan, currentFlightRecord);
            ClearPendingObjectiveBlockerImpactAnchor();
            SegmentCollisionRecord segmentCollisionRecord = ResolveCurrentSegmentCollisionRecord();
            if (TryStartObjectiveBlockerImpact(segmentCollisionRecord))
            {
                LogArrivalBoundaryDecision(arrival, false);
                Impact(pendingObjectiveBlockerImpactThing);
                return;
            }

            if (ShouldContinueFlight(arrival))
            {
                LogArrivalBoundaryDecision(arrival, true);
                LogSuspiciousContinueFlight(arrival);
                ContinueFlight(arrival);
                return;
            }

            LogArrivalBoundaryDecision(arrival, false);
            LogSuspiciousVanillaImpactBinding();
            base.ImpactSomething();
        }

        /// <summary>
        /// 在真实碰撞时执行统一飞行后半段与中性 ImpactPlan。
        /// 这里不再重跑续段裁决，避免业务路径模块吞掉原版真实命中。
        /// </summary>
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 position = ResolveImpactPosition();
            ApplyPendingObjectiveBlockerImpactAnchor();
            try
            {
                HitRecord hit = rangedFlightProtocolService.ExecuteHit(this, launchPlan, currentFlightRecord, null, hitThing);
                ImpactPlan impactPlan = rangedFlightProtocolService.ExecuteImpact(this, launchPlan, currentFlightRecord, hit);
                Thing resolvedHitThing = hit != null ? hit.HitThing : hitThing;

                LogImpactResolution(resolvedHitThing, blockedByShield, position);
                CompleteProjectileImpact(blockedByShield);
                BattleLogEntry_RangedImpact battleLogEntry = new BattleLogEntry_RangedImpact(
                    launcher,
                    resolvedHitThing,
                    intendedTarget.Thing,
                    ResolveVanillaBattleLogWeaponDef(equipmentDef),
                    def,
                    targetCoverDef);
                Find.BattleLog.Add(battleLogEntry);
                NotifyImpact(resolvedHitThing, map, position);

                ExecuteImpact(impactPlan, hit, battleLogEntry, map, blockedByShield);
            }
            finally
            {
                ClearPendingObjectiveBlockerImpactAnchor();
            }
        }

        /// <summary>
        /// 只把原版能安全消费的 weaponDef 继续传给原版战斗日志。
        /// </summary>
        private static ThingDef ResolveVanillaBattleLogWeaponDef(ThingDef weaponDef)
        {
            return weaponDef != null && !weaponDef.Verbs.NullOrEmpty() ? weaponDef : null;
        }

        /// <summary>
        /// 在投射物被正式销毁时广播一次终止事件。
        /// 正常命中路径会在 `TickInterval（间隔推进）` 收尾后补发终止，这里只兜底其它销毁路径。
        /// </summary>
        /// <param name="mode">当前销毁模式。</param>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (!isProcessingVisualTickInterval)
            {
                FreezeTerminalVisualExactPosition(ResolveCurrentTerminalVisualExactPosition());
                NotifyVisualTerminate(base.Map, ResolveCurrentTerminalVisualExactPosition());
            }

            base.Destroy(mode);
        }

        /// <summary>
        /// 按飞行协议速度倍率修正原版剩余飞行 Tick。
        /// </summary>
        /// <param name="speedFactor">当前正式飞行倍率。</param>
        private void ApplySpeedFactorToFlight(float speedFactor)
        {
            if (Mathf.Approximately(speedFactor, 1f))
            {
                return;
            }

            speedTickRemainder += speedFactor - 1f;
            while (speedTickRemainder >= 1f)
            {
                ticksToImpact = Mathf.Max(1, ticksToImpact - 1);
                speedTickRemainder -= 1f;
            }

            while (speedTickRemainder <= -1f)
            {
                ticksToImpact += 1;
                lifetime = Mathf.Max(lifetime, ticksToImpact);
                speedTickRemainder += 1f;
            }
        }

        /// <summary>
        /// 判断当前投射物是否应进入下一段飞行，而不是终结命中。
        /// </summary>
        /// <param name="arrival">当前到达阶段快照。</param>
        /// <returns>为 true 时投射物继续飞行。</returns>
        private static bool ShouldContinueFlight(ArrivalRecord arrival)
        {
            return arrival != null
                && arrival.ContinueFlight
                && arrival.NextTarget.IsValid
                && (arrival.NextFlightPathSnapshot != null || arrival.NextDestination != Vector3.zero);
        }

        /// <summary>
        /// 扫描当前宿主管理段的客观碰撞事实。
        /// </summary>
        /// <returns>当前段的客观碰撞扫描结果。</returns>
        private SegmentCollisionRecord ResolveCurrentSegmentCollisionRecord()
        {
            if (currentFlightPathSnapshot != null)
            {
                currentSegmentCollisionRecord = SegmentCollisionService.ScanSegment(this, currentFlightPathSnapshot);
                return currentSegmentCollisionRecord;
            }

            Vector3 segmentStart = NormalizeFlightPlanePoint(origin);
            Vector3 segmentEnd = NormalizeFlightPlanePoint(destination);
            currentSegmentCollisionRecord = SegmentCollisionService.ScanSegment(this, segmentStart, segmentEnd);
            return currentSegmentCollisionRecord;
        }

        /// <summary>
        /// 尝试把当前段首个客观阻挡体转入正式命中收束。
        /// </summary>
        /// <param name="segmentCollisionRecord">当前段客观碰撞扫描结果。</param>
        /// <returns>为 true 时表示当前段应直接收束到阻挡命中。</returns>
        private bool TryStartObjectiveBlockerImpact(SegmentCollisionRecord segmentCollisionRecord)
        {
            // flyOverhead（越顶飞行）弹体应无视路径中途障碍，不在此提前收束。
            if (def?.projectile?.flyOverhead == true)
            {
                return false;
            }

            if (currentFlightPathSnapshot == null
                || segmentCollisionRecord == null
                || !segmentCollisionRecord.CrossedObjectiveBlocker)
            {
                return false;
            }

            Thing objectiveBlocker = segmentCollisionRecord.FirstObjectiveBlockerThing;
            if (objectiveBlocker == null
                || !objectiveBlocker.Spawned
                || objectiveBlocker.Map != base.Map
                || !segmentCollisionRecord.FirstObjectiveBlockerCell.IsValid)
            {
                return false;
            }

            ApplyPendingObjectiveBlockerImpactAnchor(segmentCollisionRecord);
            return pendingObjectiveBlockerImpactThing != null && pendingObjectiveBlockerImpactCell.IsValid;
        }

        /// <summary>
        /// 暂存当前一次阻挡命中的锚点事实。
        /// </summary>
        /// <param name="segmentCollisionRecord">当前段客观碰撞扫描结果。</param>
        private void ApplyPendingObjectiveBlockerImpactAnchor(SegmentCollisionRecord segmentCollisionRecord)
        {
            if (segmentCollisionRecord == null
                || segmentCollisionRecord.FirstObjectiveBlockerThing == null
                || !segmentCollisionRecord.FirstObjectiveBlockerCell.IsValid)
            {
                ClearPendingObjectiveBlockerImpactAnchor();
                return;
            }

            pendingObjectiveBlockerImpactThing = segmentCollisionRecord.FirstObjectiveBlockerThing;
            pendingObjectiveBlockerImpactCell = segmentCollisionRecord.FirstObjectiveBlockerCell;
            pendingObjectiveBlockerExactPosition = pendingObjectiveBlockerImpactCell.ToVector3Shifted() + Vector3.up * def.Altitude;
        }

        /// <summary>
        /// 把当前投射物宿主的真实命中位置锚到阻挡格。
        /// 只在当前一次阻挡命中收束期内生效。
        /// </summary>
        private void ApplyPendingObjectiveBlockerImpactAnchor()
        {
            if (!pendingObjectiveBlockerImpactCell.IsValid || base.Position == pendingObjectiveBlockerImpactCell)
            {
                return;
            }

            base.Position = pendingObjectiveBlockerImpactCell;
        }

        /// <summary>
        /// 清理当前一次阻挡命中收束的临时锚点状态。
        /// </summary>
        private void ClearPendingObjectiveBlockerImpactAnchor()
        {
            pendingObjectiveBlockerImpactThing = null;
            pendingObjectiveBlockerImpactCell = IntVec3.Invalid;
            pendingObjectiveBlockerExactPosition = null;
        }

        /// <summary>
        /// 解析当前一次命中应使用的宿主位置。
        /// 如果存在阻挡锚点，则优先使用阻挡格。
        /// </summary>
        /// <returns>当前一次命中的正式收束格。</returns>
        private IntVec3 ResolveImpactPosition()
        {
            return pendingObjectiveBlockerImpactCell.IsValid
                ? pendingObjectiveBlockerImpactCell
                : base.Position;
        }

        /// <summary>
        /// 把当前 projectile 复位到下一段飞行。
        /// </summary>
        /// <param name="arrival">当前到达阶段快照。</param>
        private void ContinueFlight(ArrivalRecord arrival)
        {
            Vector3 nextOrigin = ExactPosition;
            LocalTargetInfo nextTarget = arrival.NextTarget.IsValid
                ? arrival.NextTarget
                : arrival.CurrentTarget.IsValid
                ? arrival.CurrentTarget
                : new LocalTargetInfo(arrival.NextDestination.ToIntVec3());
            LocalTargetInfo nextBindingTarget = arrival.NextBindingTarget.IsValid
                ? arrival.NextBindingTarget
                : nextTarget.IsValid
                ? nextTarget
                : new LocalTargetInfo(arrival.NextDestination.ToIntVec3());
            ProjectileFlightPathSnapshot nextFlightPathSnapshot = ResolveContinuationBindingFlightPathSnapshot(arrival, nextOrigin);
            BindFlightPathSnapshot(nextFlightPathSnapshot);
            origin = ComputeContinuationOrigin(nextOrigin);
            destination = currentFlightPathSnapshot != null
                ? currentFlightPathSnapshot.End
                : NormalizeFlightPlanePoint(arrival.NextDestination);
            usedTarget = nextBindingTarget;
            intendedTarget = launchPlan != null && launchPlan.CurrentTarget.IsValid
                ? launchPlan.CurrentTarget
                : nextTarget;
            ResetFlightDurationFromCurrentSegment();
            landed = false;
            speedTickRemainder = 0f;
            if (currentFlightRecord != null)
            {
                currentFlightRecord.RedirectDestination = null;
                currentFlightRecord.CurrentDestination = destination;
                currentFlightRecord.CurrentTarget = nextTarget;
            }

            ApplyLiveTargetSemantics(nextTarget, destination, "arrival_continue");
            LogFlightContinuation(arrival, nextTarget, nextBindingTarget, nextFlightPathSnapshot, nextOrigin);
        }

        /// <summary>
        /// 把当前飞行阶段的实时目标写回投射物目标语义 Live 层。
        /// 冻结意图层不在这里改动，确保路径历史和玩家最终选择不会被追踪过程洗掉。
        /// </summary>
        /// <param name="liveNextTarget">当前飞行真正下一目标引用。</param>
        /// <param name="liveNextPoint">当前飞行真正下一真实坐标。</param>
        /// <param name="reason">触发本次同步的生命周期阶段。</param>
        private void ApplyLiveTargetSemantics(
            LocalTargetInfo liveNextTarget,
            Vector3 liveNextPoint,
            string reason)
        {
            RangedProjectileTargetSemantics semantics = launchPlan != null ? launchPlan.TargetSemantics : null;
            if (semantics == null)
            {
                return;
            }

            LocalTargetInfo liveFinalTarget = launchPlan.CurrentTarget.IsValid
                ? launchPlan.CurrentTarget
                : semantics.LiveFinalTarget;
            Vector3 liveFinalPoint = liveFinalTarget.IsValid
                ? liveFinalTarget.CenterVector3
                : semantics.LiveFinalPoint;
            if (TargetsEquivalent(semantics.LiveFinalTarget, liveFinalTarget)
                && TargetsEquivalent(semantics.LiveNextTarget, liveNextTarget)
                && PointsEquivalent(semantics.LiveFinalPoint, liveFinalPoint)
                && PointsEquivalent(semantics.LiveNextPoint, liveNextPoint))
            {
                return;
            }

            semantics.LiveFinalTarget = liveFinalTarget;
            semantics.LiveFinalPoint = liveFinalPoint;
            semantics.LiveNextTarget = liveNextTarget;
            semantics.LiveNextPoint = liveNextPoint;
            AttackExecutionDiagnostics.LogTargetSemanticsLiveUpdate(this, launchPlan, reason);
        }

        /// <summary>
        /// 判断两个真实空间坐标在投射物目标语义层是否可视为相同。
        /// </summary>
        /// <param name="left">左侧坐标。</param>
        /// <param name="right">右侧坐标。</param>
        /// <returns>坐标距离足够小时返回 true。</returns>
        private static bool PointsEquivalent(Vector3 left, Vector3 right)
        {
            return (left - right).sqrMagnitude <= 0.0001f;
        }

        /// <summary>
        /// 判断两个目标引用在投射物目标语义层是否可视为相同。
        /// 实体引用优先按实体比较，非实体目标回退到原版目标格比较。
        /// </summary>
        /// <param name="left">左侧目标引用。</param>
        /// <param name="right">右侧目标引用。</param>
        /// <returns>目标引用等价时返回 true。</returns>
        private static bool TargetsEquivalent(LocalTargetInfo left, LocalTargetInfo right)
        {
            if (!left.IsValid || !right.IsValid)
            {
                return !left.IsValid && !right.IsValid;
            }

            if (left.HasThing && right.HasThing)
            {
                return left.Thing == right.Thing;
            }

            return left.Cell == right.Cell;
        }

        /// <summary>
        /// 解析下一段真正允许绑定到宿主上的路径快照。
        /// 如果宿主层已经确认该段会先撞上客观阻挡，就先把路径裁短到阻挡前缀，避免把墙后的整段错误放行。
        /// </summary>
        private ProjectileFlightPathSnapshot ResolveContinuationBindingFlightPathSnapshot(ArrivalRecord arrival, Vector3 nextOrigin)
        {
            ProjectileFlightPathSnapshot nextFlightPathSnapshot = arrival.NextFlightPathSnapshot
                ?? BuildLinearFlightPathSnapshot(nextOrigin, arrival.NextDestination);
            SegmentCollisionRecord nextSegmentCollisionRecord = SegmentCollisionService.ScanSegment(this, nextFlightPathSnapshot);
            if (nextSegmentCollisionRecord == null
                || !nextSegmentCollisionRecord.CrossedObjectiveBlocker)
            {
                return nextFlightPathSnapshot;
            }

            float blockerProgress = Mathf.Clamp01(nextSegmentCollisionRecord.FirstObjectiveBlockerProgress);
            if (blockerProgress <= 0.0001f)
            {
                return BuildLinearFlightPathSnapshot(
                    nextFlightPathSnapshot.Start,
                    ResolveContinuationObjectiveBlockerTerminalPosition(nextSegmentCollisionRecord, nextFlightPathSnapshot.End));
            }

            return ProjectileFlightPathUtility.CreatePrefix(nextFlightPathSnapshot, blockerProgress);
        }

        /// <summary>
        /// 解析续段首次客观阻挡应该使用的终止位置。
        /// 优先使用扫描阶段已经收束出的阻挡位置，缺失时再退回到阻挡格中心。
        /// </summary>
        private static Vector3 ResolveContinuationObjectiveBlockerTerminalPosition(
            SegmentCollisionRecord nextSegmentCollisionRecord,
            Vector3 fallbackEnd)
        {
            if (nextSegmentCollisionRecord == null)
            {
                return NormalizeFlightPlanePoint(fallbackEnd);
            }

            Vector3 blockerExactPosition = NormalizeFlightPlanePoint(nextSegmentCollisionRecord.FirstObjectiveBlockerExactPosition);
            if (nextSegmentCollisionRecord.FirstObjectiveBlockerCell.IsValid
                && blockerExactPosition.ToIntVec3() == nextSegmentCollisionRecord.FirstObjectiveBlockerCell)
            {
                return blockerExactPosition;
            }

            return nextSegmentCollisionRecord.FirstObjectiveBlockerCell.IsValid
                ? nextSegmentCollisionRecord.FirstObjectiveBlockerCell.ToVector3Shifted()
                : NormalizeFlightPlanePoint(fallbackEnd);
        }

        /// <summary>
        /// 记录可疑的续段飞行。
        /// 重点观察“距离已经极近，但宿主仍然跳过原版命中”的场景。
        /// </summary>
        private void LogSuspiciousContinueFlight(ArrivalRecord arrival)
        {
            ProjectileFlightPathSnapshot nextFlightPathSnapshot = arrival != null
                ? arrival.NextFlightPathSnapshot
                : null;
            Vector3 currentPosition = ExactPosition.Yto0();
            Vector3 nextEnd = nextFlightPathSnapshot != null
                ? nextFlightPathSnapshot.End
                : (arrival != null ? arrival.NextDestination : Vector3.zero);
            float endDistance = (nextEnd - currentPosition).Yto0().magnitude;
            float pathLength = nextFlightPathSnapshot != null
                ? Mathf.Max(0f, nextFlightPathSnapshot.ApproximateLength)
                : endDistance;
            float suspiciousDistanceThreshold = Mathf.Max(0.15f, def != null && def.projectile != null
                ? def.projectile.SpeedTilesPerTick * 1.5f
                : 0.15f);
            if (endDistance > suspiciousDistanceThreshold && pathLength > suspiciousDistanceThreshold)
            {
                return;
            }

            string attackId = !string.IsNullOrWhiteSpace(AttackInstanceId)
                ? AttackInstanceId
                : launchPlan != null ? launchPlan.AttackInstanceId : null;
            string resultId = !string.IsNullOrWhiteSpace(ResultId)
                ? ResultId
                : launchPlan != null ? launchPlan.ResultId : null;
            string diagnosticKey = "projectile_continue_flight_suspicious."
                + SafeDiagnosticId(resultId)
                + "."
                + SafeDiagnosticId(attackId);
            BdpDiagnostics.AttackExecutionThrottled(
                diagnosticKey,
                "event=projectile_continue_flight_suspicious"
                + ", attackId=" + SafeDiagnosticId(attackId)
                + ", resultId=" + SafeDiagnosticId(resultId)
                + ", projectile=" + SafeDiagnosticId(ThingID)
                + ", currentPos=" + currentPosition
                + ", nextEnd=" + nextEnd
                + ", endDistance=" + endDistance.ToString("F3")
                + ", pathLength=" + pathLength.ToString("F3")
                + ", ticksToImpact=" + ticksToImpact
                + ", target=" + DescribeTarget(arrival != null ? arrival.NextTarget : LocalTargetInfo.Invalid),
                30);
        }

        private void LogLaunchSnapshotApplied(Vector3 vanillaDestination, ProjectileFlightPathSnapshot explicitInitialFlightPath)
        {
            string attackId = !string.IsNullOrWhiteSpace(AttackInstanceId)
                ? AttackInstanceId
                : launchPlan != null ? launchPlan.AttackInstanceId : null;
            string resultId = !string.IsNullOrWhiteSpace(ResultId)
                ? ResultId
                : launchPlan != null ? launchPlan.ResultId : null;
            BdpDiagnostics.AttackExecution(
                "event=projectile_launch_bound"
                + ", attackId=" + SafeDiagnosticId(attackId)
                + ", resultId=" + SafeDiagnosticId(resultId)
                + ", projectile=" + SafeDiagnosticId(ThingID)
                + ", usedTarget=" + DescribeTargetDetailed(usedTarget)
                + ", intendedTarget=" + DescribeTargetDetailed(intendedTarget)
                + ", launchPlanLaunchTarget=" + DescribeTargetDetailed(launchPlan != null ? launchPlan.LaunchTarget : LocalTargetInfo.Invalid)
                + ", launchPlanAimTarget=" + DescribeTargetDetailed(launchPlan != null ? launchPlan.AimTarget : LocalTargetInfo.Invalid)
                + ", launchPlanCurrentTarget=" + DescribeTargetDetailed(launchPlan != null ? launchPlan.CurrentTarget : LocalTargetInfo.Invalid)
                + ", vanillaDestination=" + NormalizeFlightPlanePoint(vanillaDestination)
                + ", boundDestination=" + NormalizeFlightPlanePoint(destination)
                + ", currentFlightPath=" + DescribeFlightPathSnapshot(currentFlightPathSnapshot)
                + ", explicitInitialFlightPath=" + DescribeFlightPathSnapshot(explicitInitialFlightPath));
        }

        private void LogFlightContinuation(
            ArrivalRecord arrival,
            LocalTargetInfo nextTarget,
            LocalTargetInfo nextBindingTarget,
            ProjectileFlightPathSnapshot nextFlightPathSnapshot,
            Vector3 nextSegmentStart)
        {
            string attackId = !string.IsNullOrWhiteSpace(AttackInstanceId)
                ? AttackInstanceId
                : launchPlan != null ? launchPlan.AttackInstanceId : null;
            string resultId = !string.IsNullOrWhiteSpace(ResultId)
                ? ResultId
                : launchPlan != null ? launchPlan.ResultId : null;
            Vector3 normalizedNextSegmentStart = NormalizeFlightPlanePoint(nextSegmentStart);
            Vector3 nextSegmentEnd = nextFlightPathSnapshot != null
                ? nextFlightPathSnapshot.End
                : (arrival != null ? arrival.NextDestination : Vector3.zero);
            SegmentTraversalAuditSummary nextSegmentTraversalAudit = BuildSegmentTraversalAudit(
                nextFlightPathSnapshot ?? BuildLinearFlightPathSnapshot(normalizedNextSegmentStart, nextSegmentEnd));
            float nextEndToUsedTargetDistance = TryResolveDiagnosticTargetPosition(usedTarget, out Vector3 usedTargetPos)
                ? Vector3.Distance(nextSegmentEnd, usedTargetPos)
                : -1f;
            BdpDiagnostics.AttackExecution(
                "event=projectile_continue_flight_bound"
                + ", attackId=" + SafeDiagnosticId(attackId)
                + ", resultId=" + SafeDiagnosticId(resultId)
                + ", projectile=" + SafeDiagnosticId(ThingID)
                + ", arrivalCurrentTarget=" + DescribeTargetDetailed(arrival != null ? arrival.CurrentTarget : LocalTargetInfo.Invalid)
                + ", arrivalNextTarget=" + DescribeTargetDetailed(arrival != null ? arrival.NextTarget : LocalTargetInfo.Invalid)
                + ", arrivalNextBindingTarget=" + DescribeTargetDetailed(arrival != null ? arrival.NextBindingTarget : LocalTargetInfo.Invalid)
                + ", resolvedNextTarget=" + DescribeTargetDetailed(nextTarget)
                + ", resolvedNextBindingTarget=" + DescribeTargetDetailed(nextBindingTarget)
                + ", nextSegmentStart=" + normalizedNextSegmentStart
                + ", nextSegmentStartCell=" + normalizedNextSegmentStart.ToIntVec3()
                + ", arrivalNextDestination=" + (arrival != null ? arrival.NextDestination.ToString() : Vector3.zero.ToString())
                + ", segmentEnd=" + nextSegmentEnd
                + ", nextSegmentProjectedEndCell=" + nextSegmentEnd.ToIntVec3()
                + ", nextSegmentPathKind=" + nextSegmentTraversalAudit.PathKind
                + ", nextSegmentSamplePointCount=" + nextSegmentTraversalAudit.SamplePointCount
                + ", nextSegmentTraversedCellCount=" + nextSegmentTraversalAudit.TraversedCellCount
                + ", nextSegmentTraversedCells=" + nextSegmentTraversalAudit.TraversedCellsSummary
                + ", nextSegmentCrossedObjectiveBlocker=" + nextSegmentTraversalAudit.CrossedObjectiveBlocker
                + ", nextSegmentFirstObjectiveBlockerCell=" + nextSegmentTraversalAudit.FirstObjectiveBlockerCell
                + ", nextSegmentFirstObjectiveBlockerAudit=" + nextSegmentTraversalAudit.FirstObjectiveBlockerAudit
                + ", nextSegmentCrossedHitCandidate=" + nextSegmentTraversalAudit.CrossedHitCandidate
                + ", nextSegmentFirstHitCandidateCell=" + nextSegmentTraversalAudit.FirstHitCandidateCell
                + ", nextSegmentFirstHitCandidateAudit=" + nextSegmentTraversalAudit.FirstHitCandidateAudit
                + ", nextSegmentCrossedBlockingThing=" + nextSegmentTraversalAudit.CrossedBlockingThing
                + ", nextSegmentFirstBlockingCell=" + nextSegmentTraversalAudit.FirstBlockingCell
                + ", nextSegmentFirstBlockingAudit=" + nextSegmentTraversalAudit.FirstBlockingAudit
                + ", nextSegmentCrossedClosedDoor=" + nextSegmentTraversalAudit.CrossedClosedDoor
                + ", nextSegmentFirstClosedDoorCell=" + nextSegmentTraversalAudit.FirstClosedDoorCell
                + ", nextSegmentFirstClosedDoorAudit=" + nextSegmentTraversalAudit.FirstClosedDoorAudit
                + ", usedTarget=" + DescribeTargetDetailed(usedTarget)
                + ", intendedTarget=" + DescribeTargetDetailed(intendedTarget)
                + ", launchPlanCurrentTarget=" + DescribeTargetDetailed(launchPlan != null ? launchPlan.CurrentTarget : LocalTargetInfo.Invalid)
                + ", nextEndToUsedTargetDistance=" + nextEndToUsedTargetDistance.ToString("F3"));
        }

        private void LogSuspiciousVanillaImpactBinding()
        {
            if (!usedTarget.HasThing || usedTarget.Thing == null)
            {
                return;
            }

            if (!TryResolveDiagnosticTargetPosition(usedTarget, out Vector3 targetPos))
            {
                return;
            }

            Vector3 exactPos = ExactPosition.Yto0();
            Vector3 segmentEnd = currentFlightPathSnapshot != null
                ? currentFlightPathSnapshot.End
                : NormalizeFlightPlanePoint(destination);
            float segmentEndDistance = Vector3.Distance(segmentEnd, targetPos);
            float exactPosDistance = Vector3.Distance(exactPos, targetPos);
            float suspiciousDistanceThreshold = Mathf.Max(1f, def != null && def.projectile != null
                ? def.projectile.SpeedTilesPerTick * 3f
                : 1f);
            if (segmentEndDistance <= suspiciousDistanceThreshold && exactPosDistance <= suspiciousDistanceThreshold)
            {
                return;
            }

            string attackId = !string.IsNullOrWhiteSpace(AttackInstanceId)
                ? AttackInstanceId
                : launchPlan != null ? launchPlan.AttackInstanceId : null;
            string resultId = !string.IsNullOrWhiteSpace(ResultId)
                ? ResultId
                : launchPlan != null ? launchPlan.ResultId : null;
            BdpDiagnostics.AttackExecution(
                "event=projectile_vanilla_impact_binding_suspicious"
                + ", attackId=" + SafeDiagnosticId(attackId)
                + ", resultId=" + SafeDiagnosticId(resultId)
                + ", projectile=" + SafeDiagnosticId(ThingID)
                + ", exactPos=" + exactPos
                + ", segmentEnd=" + segmentEnd
                + ", destination=" + NormalizeFlightPlanePoint(destination)
                + ", targetPos=" + targetPos
                + ", segmentEndDistance=" + segmentEndDistance.ToString("F3")
                + ", exactPosDistance=" + exactPosDistance.ToString("F3")
                + ", usedTarget=" + DescribeTargetDetailed(usedTarget)
                + ", intendedTarget=" + DescribeTargetDetailed(intendedTarget)
                + ", ticksToImpact=" + ticksToImpact);
        }

        private void ExecuteImpact(
            ImpactPlan impactPlan,
            HitRecord hit,
            BattleLogEntry_RangedImpact battleLogEntry,
            Map map,
            bool blockedByShield)
        {
            if (impactPlan == null)
            {
                return;
            }

            bool executedAnyPlan = false;
            bool hasAttackTargetProducer = impactPlan.ProducesAttackTargetEvents;
            bool damageWasProcessed = false;
            DamageResolution damageResolution = null;

            if (!impactPlan.SuppressBaselineImpact)
            {
                if (!blockedByShield
                    && impactPlan.DamageDisposition != DamageDisposition.SuppressAllProjectileImpact
                    && impactPlan.ApplyBaselineDirectDamage
                    && hit != null
                    && hit.HitThing != null
                    && impactPlan.BaselineDirectDamage != null)
                {
                    DamageResolution baselineResolution;
                    ApplyDirectDamage(
                        impactPlan.BaselineDirectDamage,
                        hit.HitThing,
                        battleLogEntry,
                        ResolveDirectHitFeedbackColor(impactPlan, hasAttackTargetProducer),
                        out baselineResolution);
                    executedAnyPlan = true;
                    damageResolution = baselineResolution;
                    damageWasProcessed |= IsDamageProcessed(baselineResolution);
                }

                if (!blockedByShield
                    && impactPlan.ApplyBaselineAreaEffect
                    && impactPlan.BaselineAreaEffect != null
                    && map != null
                    && (AllowsBaselineDamage(impactPlan)
                        || impactPlan.PreserveTargetResolutionWhenDamageSuppressed))
                {
                    ApplyAreaEffect(impactPlan.BaselineAreaEffect, map, impactPlan, true);
                    executedAnyPlan = true;
                }
            }

            if (!blockedByShield
                && impactPlan.DamageDisposition != DamageDisposition.SuppressAllProjectileImpact
                && impactPlan.DamageDisposition != DamageDisposition.SuppressModuleExtraDamage
                && impactPlan.ApplyDirectDamage
                && hit != null
                && hit.HitThing != null
                && impactPlan.DirectDamage != null)
            {
                DamageResolution moduleResolution;
                ApplyDirectDamage(
                    impactPlan.DirectDamage,
                    hit.HitThing,
                    battleLogEntry,
                    ResolveDirectHitFeedbackColor(impactPlan, hasAttackTargetProducer),
                    out moduleResolution);
                executedAnyPlan = true;
                damageResolution = moduleResolution;
                damageWasProcessed |= IsDamageProcessed(moduleResolution);
            }

            if (blockedByShield && hit != null && hit.HitThing != null)
            {
                damageResolution = DamageResolutionRuntime.CreateProjectileInterception(hit.HitThing);
                executedAnyPlan = true;
            }
            else if (!damageWasProcessed
                && damageResolution == null
                && !hasAttackTargetProducer
                && impactPlan.DamageDisposition == DamageDisposition.SuppressAllProjectileImpact
                && hit != null
                && hit.HitThing != null)
            {
                damageResolution = ResolveSuppressedDirectImpact(impactPlan, hit.HitThing);
                executedAnyPlan = true;
            }

            if (impactPlan.DamageDisposition != DamageDisposition.SuppressAllProjectileImpact
                && impactPlan.DamageDisposition != DamageDisposition.SuppressModuleExtraDamage)
            {
                for (int i = 0; i < impactPlan.ExtraDamages.Count; i++)
                {
                    DamagePlan extra = impactPlan.ExtraDamages[i];
                    if (extra != null && !blockedByShield && hit != null && hit.HitThing != null)
                    {
                        DamageResolution extraResolution;
                        ApplyDirectDamage(extra, hit.HitThing, battleLogEntry, null, out extraResolution);
                        executedAnyPlan = true;
                        damageWasProcessed |= IsDamageProcessed(extraResolution);
                        if (damageResolution == null)
                        {
                            damageResolution = extraResolution;
                        }
                    }
                }
            }

            // 原版 Bullet（子弹）是在 TakeDamage（承受伤害）返回后才通知 Pawn（人形单位）僵直。
            // 只有伤害入口实际通过，或模块明确要求补回完整反馈时，才允许产生 Pawn 僵直。
            if (damageWasProcessed
                && !blockedByShield
                && hit != null
                && hit.HitThing != null)
            {
                (hit.HitThing as Pawn)?.stances?.stagger.Notify_BulletImpact(this);
            }

            if (!blockedByShield
                && impactPlan.ApplyAreaEffect
                && impactPlan.AreaEffect != null
                && map != null
                && (AllowsModuleAreaDamage(impactPlan)
                    || impactPlan.PreserveTargetResolutionWhenDamageSuppressed))
            {
                ApplyAreaEffect(impactPlan.AreaEffect, map, impactPlan, false);
                executedAnyPlan = true;
            }

            // 伤害或模块拦截裁决完成后，才派发独立减益效果；护盾拦截不会进入这里。
            bool canDispatchTargetEffects = damageResolution != null
                && !damageResolution.IsShieldBlocked
                && (damageResolution.IsDamageProcessed
                    || damageResolution.Outcome == DamageResolutionOutcome.ModuleIntercepted);
            if (canDispatchTargetEffects
                && !hasAttackTargetProducer
                && hit != null
                && hit.HitThing != null)
            {
                executedAnyPlan |= ExecuteDirectAttackTargetEvent(impactPlan, hit, map);
            }

            // 完全取消真实伤害的模块必须显式声明是否需要补回完整 Pawn 反馈。
            // 普通攻击不走这里，仍由原版 TakeDamage（目标承伤）链决定反馈。
            if (damageResolution != null
                && damageResolution.Outcome == DamageResolutionOutcome.ModuleIntercepted
                && !hasAttackTargetProducer
                && hit != null
                && hit.HitThing != null
                && impactPlan.InterceptedHitFeedback == ImpactHitFeedbackMode.VanillaPawn)
            {
                ApplySuppressedHitFeedback(
                    hit.HitThing,
                    ResolveDirectHitFeedbackColor(impactPlan, hasAttackTargetProducer),
                    true);
                executedAnyPlan = true;
            }

            if (!executedAnyPlan && !blockedByShield)
            {
                PlayFallbackImpactEffects(map);
            }
        }

        /// <summary>
        /// 执行没有其他目标生产者时的直接攻击目标事件。
        /// </summary>
        private bool ExecuteDirectAttackTargetEvent(ImpactPlan impactPlan, HitRecord hit, Map map)
        {
            return AttackTargetEventDispatcher.Dispatch(
                new AttackTargetEvent
                {
                    Source = AttackTargetEventSource.DirectImpact,
                    TargetThing = hit.HitThing,
                    TargetCell = hit.HitCell,
                    ExtraEffects = impactPlan.ExtraEffects,
                    Map = map,
                    Instigator = launcher,
                    SourceThing = launchPlan != null ? launchPlan.SourceThing : null,
                    Projectile = this,
                    SemanticContext = SemanticContext,
                    AttackInstanceId = AttackInstanceId,
                    ResultId = ResultId
                });
        }

        /// <summary>
        /// 判断基线范围伤害是否可以正常执行。
        /// </summary>
        private static bool AllowsBaselineDamage(ImpactPlan impactPlan)
        {
            return impactPlan != null
                && impactPlan.DamageDisposition != DamageDisposition.SuppressAllProjectileImpact
                && impactPlan.DamageDisposition != DamageDisposition.SuppressBaselineImpact;
        }

        /// <summary>
        /// 判断模块范围伤害是否可以正常执行。
        /// </summary>
        private static bool AllowsModuleAreaDamage(ImpactPlan impactPlan)
        {
            return impactPlan != null
                && impactPlan.DamageDisposition != DamageDisposition.SuppressAllProjectileImpact
                && impactPlan.DamageDisposition != DamageDisposition.SuppressModuleExtraDamage;
        }

        /// <summary>
        /// 判断一次承伤结果是否真正通过了伤害入口。
        /// </summary>
        private static bool IsDamageProcessed(DamageResolution resolution)
        {
            return resolution != null
                && resolution.Outcome == DamageResolutionOutcome.DamageProcessed;
        }

        /// <summary>
        /// 解析“模块取消伤害但仍需尊重伤害前护盾”的直接命中结果。
        /// </summary>
        private DamageResolution ResolveSuppressedDirectImpact(ImpactPlan impactPlan, Thing hitThing)
        {
            if (CurrentInteractionPolicy != null
                && CurrentInteractionPolicy.BypassRegisteredDamageShields)
            {
                return DamageResolutionRuntime.CreateModuleInterception(hitThing);
            }

            DamageInfo probeDamageInfo = BuildDamageInfo(
                ResolveProbeDamagePlan(impactPlan),
                hitThing);
            bool absorbed;
            bool probed;
            using (ProjectileInteractionPolicyScope.Push(CurrentInteractionPolicy))
            using (SemanticRuntimeScope.Push(SemanticContext))
            {
                probed = DamageResolutionRuntime.TryProbeDamageInterception(
                    hitThing,
                    ref probeDamageInfo,
                    out absorbed);
            }

            if (!probed)
            {
                return DamageResolutionRuntime.CreateModuleInterception(hitThing);
            }

            return absorbed
                ? DamageResolutionRuntime.CreateProjectileInterception(hitThing)
                : DamageResolutionRuntime.CreateModuleInterception(hitThing);
        }

        /// <summary>
        /// 为护盾探针选择一份只用于裁决的伤害计划。
        /// </summary>
        private DamagePlan ResolveProbeDamagePlan(ImpactPlan impactPlan)
        {
            if (impactPlan != null && impactPlan.DirectDamage != null)
            {
                return impactPlan.DirectDamage;
            }

            if (impactPlan != null && impactPlan.BaselineDirectDamage != null)
            {
                return impactPlan.BaselineDirectDamage;
            }

            return new DamagePlan
            {
                DamageDef = base.DamageDef,
                Amount = DamageAmount,
                ArmorPenetration = ArmorPenetration,
                Instigator = launcher,
                Weapon = launchPlan != null ? launchPlan.SourceThing : null,
                IntendedTarget = intendedTarget,
                SemanticContext = SemanticContext
            };
        }

        /// <summary>
        /// 把单条直接伤害计划落回原版伤害系统。
        /// </summary>
        private DamageWorker.DamageResult ApplyDirectDamage(
            DamagePlan damagePlan,
            Thing hitThing,
            BattleLogEntry_RangedImpact battleLogEntry,
            Color? hitFeedbackColorOverride,
            out DamageResolution resolution)
        {
            resolution = null;
            if (damagePlan == null || hitThing == null)
            {
                return new DamageWorker.DamageResult();
            }

            DamageInfo damageInfo = BuildDamageInfo(damagePlan, hitThing);
            DamageWorker.DamageResult damageResult;
            using (ProjectileInteractionPolicyScope.Push(CurrentInteractionPolicy))
            using (SemanticRuntimeScope.Push(damagePlan.SemanticContext ?? SemanticContext))
            {
                damageResult = hitThing.TakeDamage(damageInfo);
                resolution = DamageResolutionRuntime.ConsumeLast(hitThing);
                damageResult.AssociateWithLog(battleLogEntry);
            }

            if (resolution == null)
            {
                resolution = new DamageResolution
                {
                    TargetThing = hitThing,
                    Outcome = DamageResolutionOutcome.DamageProcessed,
                    DamageResult = damageResult
                };
            }

            // 只有原版伤害工作器确认实际受伤后，才消费减益模块提交的颜色。
            // 护盾吸收或 0 伤害都会被结果层挡住，不会污染闪烁颜色。
            if (hitFeedbackColorOverride.HasValue
                && damageResult.wounded
                && hitThing is Pawn hitPawn)
            {
                HitFeedbackColorRuntime.Register(hitPawn, hitFeedbackColorOverride.Value);
            }

            return damageResult;
        }

        /// <summary>
        /// 把正式伤害计划转换为原版 DamageInfo（伤害信息）。
        /// </summary>
        private DamageInfo BuildDamageInfo(DamagePlan damagePlan, Thing hitThing)
        {
            bool instigatorGuilty = !(launcher is Pawn pawn) || !pawn.Drafted;
            DamageInfo damageInfo = new DamageInfo(
                damagePlan.DamageDef ?? base.DamageDef,
                damagePlan.Amount,
                damagePlan.ArmorPenetration,
                ExactRotation.eulerAngles.y,
                damagePlan.Instigator ?? launcher,
                null,
                damagePlan.Weapon != null ? damagePlan.Weapon.def : equipmentDef,
                DamageInfo.SourceCategory.ThingOrUnknown,
                damagePlan.IntendedTarget.Thing ?? intendedTarget.Thing,
                instigatorGuilty);
            damageInfo.SetWeaponQuality(equipmentQuality);
            return damageInfo;
        }

        /// <summary>
        /// 仅为完全跳过原版伤害链的无伤害命中补回原版不会自动产生的反馈。
        /// 普通伤害命中由 DamageWorker（伤害工作器）在 TakeDamage（承受伤害）内部自然触发反馈。
        /// </summary>
        internal void ApplySuppressedHitFeedback(
            Thing hitThing,
            Color? hitFeedbackColorOverride,
            bool applyBulletStagger)
        {
            Pawn hitPawn = hitThing as Pawn;
            if (hitPawn == null)
            {
                return;
            }

            if (hitFeedbackColorOverride.HasValue)
            {
                HitFeedbackColorRuntime.Register(hitPawn, hitFeedbackColorOverride.Value);
            }

            // 原版伤害工作器会先把同一份 DamageInfo（伤害信息）交给 Pawn Drawer（角色绘制器），
            // 由它触发 JitterHandler（受击抖动器）和受击闪烁；这里用 0 伤害的反馈副本保留视觉，不回灌伤害。
            bool instigatorGuilty = !(launcher is Pawn pawn) || !pawn.Drafted;
            DamageInfo feedbackDamageInfo = new DamageInfo(
                base.DamageDef,
                0f,
                ArmorPenetration,
                ExactRotation.eulerAngles.y,
                launcher,
                null,
                equipmentDef,
                DamageInfo.SourceCategory.ThingOrUnknown,
                intendedTarget.Thing,
                instigatorGuilty);
            feedbackDamageInfo.SetWeaponQuality(equipmentQuality);
            hitPawn.Drawer?.Notify_DamageApplied(feedbackDamageInfo);

            // 受击僵直与受击抖动是两个独立的原版反馈，均不等同于伤害本体。
            if (applyBulletStagger)
            {
                hitPawn.stances?.stagger.Notify_BulletImpact(this);
            }
        }

        /// <summary>
        /// 解析直接命中入口是否有适用的命中反馈颜色。
        /// 攻击生产者存在时，AttackTargetEvents（攻击目标事件）只由生产者逐目标消费，
        /// 不把生产者的直接撞击点额外当作同一范围目标事件。
        /// </summary>
        private static Color? ResolveDirectHitFeedbackColor(
            ImpactPlan impactPlan,
            bool hasAttackTargetProducer)
        {
            if (impactPlan == null || !impactPlan.HasHitFeedbackColor)
            {
                return null;
            }

            if (impactPlan.HitFeedbackTargetScope == ExtraEffectTargetScope.DirectHitThing
                || (impactPlan.HitFeedbackTargetScope == ExtraEffectTargetScope.AttackTargetEvents
                    && !hasAttackTargetProducer))
            {
                return impactPlan.HitFeedbackColor;
            }

            return null;
        }

        /// <summary>
        /// 把区域效果计划落回原版爆炸系统。
        /// </summary>
        private void ApplyAreaEffect(
            AreaEffectPlan areaEffectPlan,
            Map map,
            ImpactPlan impactPlan,
            bool isBaselineAreaEffect)
        {
            ExplosionPresentationPolicy presentationPolicy = impactPlan != null
                && impactPlan.AreaPresentationPolicyOverride != null
                ? impactPlan.AreaPresentationPolicyOverride
                : areaEffectPlan.PresentationPolicy;
            bool suppressCurrentAreaDamage = impactPlan != null
                && (impactPlan.DamageDisposition == DamageDisposition.SuppressAllProjectileImpact
                    || (isBaselineAreaEffect && impactPlan.DamageDisposition == DamageDisposition.SuppressBaselineImpact)
                    || (!isBaselineAreaEffect && impactPlan.DamageDisposition == DamageDisposition.SuppressModuleExtraDamage));
            ExplosionImpactDispatchContext impactContext = impactPlan != null
                ? new ExplosionImpactDispatchContext
                {
                    ExtraEffects = impactPlan.ExtraEffects,
                    SuppressCurrentAreaDamage = suppressCurrentAreaDamage,
                    Instigator = areaEffectPlan.Instigator ?? launcher,
                    SourceThing = areaEffectPlan.Weapon ?? (launchPlan != null ? launchPlan.SourceThing : null),
                    Projectile = this,
                    Map = map,
                    SemanticContext = areaEffectPlan.SemanticContext ?? SemanticContext,
                    AttackInstanceId = AttackInstanceId,
                    ResultId = ResultId,
                    HasHitFeedbackColor = impactPlan.HasHitFeedbackColor,
                     HitFeedbackColor = impactPlan.HitFeedbackColor,
                     HitFeedbackTargetScope = impactPlan.HitFeedbackTargetScope,
                     InterceptedHitFeedback = impactPlan.InterceptedHitFeedback,
                     PresentationPolicy = presentationPolicy != null ? presentationPolicy.Clone() : null,
                     InteractionPolicy = CurrentInteractionPolicy != null ? CurrentInteractionPolicy.Clone() : null,
                     DamageDef = areaEffectPlan.DamageDef ?? base.DamageDef,
                     DamageAmount = areaEffectPlan.DamageAmount,
                     ArmorPenetration = areaEffectPlan.ArmorPenetration,
                     IntendedTarget = intendedTarget
                 }
                : null;

            using (ProjectileInteractionPolicyScope.Push(CurrentInteractionPolicy))
            using (ExplosionImpactRuntimeScope.Push(impactContext))
            using (SemanticRuntimeScope.Push(areaEffectPlan.SemanticContext ?? SemanticContext))
            {
                bool doVisualEffects = def.projectile.doExplosionVFX
                    && (presentationPolicy == null || !presentationPolicy.SuppressVanillaVisualEffects);
                bool doSoundEffects = presentationPolicy == null || !presentationPolicy.SuppressVanillaSoundEffects;
                float screenShakeFactor = presentationPolicy != null && presentationPolicy.OverrideScreenShakeFactor
                    ? presentationPolicy.ScreenShakeFactor
                    : def.projectile.screenShakeFactor;
                GenExplosion.DoExplosion(
                    areaEffectPlan.Center,
                    map,
                    areaEffectPlan.Radius,
                    areaEffectPlan.DamageDef ?? base.DamageDef,
                    areaEffectPlan.Instigator ?? launcher,
                    (int)areaEffectPlan.DamageAmount,
                    areaEffectPlan.ArmorPenetration,
                    def.projectile.soundExplode,
                    areaEffectPlan.Weapon != null ? areaEffectPlan.Weapon.def : equipmentDef,
                    def,
                    intendedTarget.Thing,
                    def.projectile.postExplosionSpawnThingDef ?? (def.projectile.explosionSpawnsSingleFilth ? null : def.projectile.filth),
                    def.projectile.postExplosionSpawnChance,
                    def.projectile.postExplosionSpawnThingCount,
                    def.projectile.postExplosionGasType,
                    null,
                    255,
                    def.projectile.applyDamageToExplosionCellsNeighbors,
                    def.projectile.preExplosionSpawnThingDef,
                    def.projectile.preExplosionSpawnChance,
                    def.projectile.preExplosionSpawnThingCount,
                    def.projectile.explosionChanceToStartFire,
                    def.projectile.explosionDamageFalloff,
                    origin.AngleToFlat(destination),
                    null,
                    null,
                    doVisualEffects,
                    base.DamageDef.expolosionPropagationSpeed,
                    0f,
                    doSoundEffects,
                    def.projectile.postExplosionSpawnThingDefWater,
                    screenShakeFactor,
                    null,
                    null,
                    def.projectile.postExplosionSpawnSingleThingDef,
                    def.projectile.preExplosionSpawnSingleThingDef);
            }

            if (def.projectile.explosionSpawnsSingleFilth
                && def.projectile.filth != null
                && def.projectile.filthCount.TrueMax > 0
                && Rand.Chance(def.projectile.filthChance)
                && !base.Position.Filled(map))
            {
                FilthMaker.TryMakeFilth(base.Position, map, def.projectile.filth, def.projectile.filthCount.RandomInRange);
            }
        }

        /// <summary>
        /// 在没有显式计划时沿用最小原版命中表现。
        /// </summary>
        private void PlayFallbackImpactEffects(Map map)
        {
            if (map == null)
            {
                return;
            }

            SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(base.Position, map));
            if (base.Position.GetTerrain(map).takeSplashes)
            {
                FleckMaker.WaterSplash(ExactPosition, map, Mathf.Sqrt(DamageAmount) * 1f, 4f);
            }
            else
            {
                FleckMaker.Static(ExactPosition, map, FleckDefOf.ShotHit_Dirt);
            }

            if (Rand.Chance(base.DamageDef.igniteCellChance))
            {
                FireUtility.TryStartFireIn(base.Position, map, Rand.Range(0.55f, 0.85f), launcher);
            }
        }

        /// <summary>
        /// 完成当前投射物命中后的宿主收尾。
        /// </summary>
        private void CompleteProjectileImpact(bool blockedByShield)
        {
            GenClamor.DoClamor(this, 12f, ClamorDefOf.Impact);
            if (!blockedByShield && def.projectile.landedEffecter != null)
            {
                def.projectile.landedEffecter.Spawn(base.Position, base.Map).Cleanup();
            }

            FreezeTerminalVisualExactPosition(ResolveCurrentTerminalVisualExactPosition());
            Destroy();
        }

        /// <summary>
        /// 向周围 Thing 广播命中邻近事件。
        /// </summary>
        private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
        {
            BulletImpactData impactData = new BulletImpactData
            {
                bullet = this,
                hitThing = hitThing,
                impactPosition = position
            };
            hitThing?.Notify_BulletImpactNearby(impactData);
            int num = 9;
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = position + GenRadial.RadialPattern[i];
                if (!c.InBounds(map))
                {
                    continue;
                }

                System.Collections.Generic.List<Thing> thingList = c.GetThingList(map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (thingList[j] != hitThing)
                    {
                        thingList[j].Notify_BulletImpactNearby(impactData);
                    }
                }
            }
        }

        /// <summary>
        /// 把当前飞行段的起终点归一到同一高度平面。
        /// 当前宿主的真实飞行高度由 `ExactPosition（精确位置）` 里的 `def.Altitude` 负责；
        /// 这里的归一只用于让 `StartingTicksToImpact（初始飞行时长）` 回到平面距离语义。
        /// </summary>
        private void NormalizeFlightSegmentToSharedHeight()
        {
            origin = NormalizeFlightPlanePoint(origin);
            destination = NormalizeFlightPlanePoint(destination);
        }

        /// <summary>
        /// 把单个坐标点压回原版地图平面。
        /// 保留 `Vector3（三维向量）` 类型，但让时间计算输入处于同一高度。
        /// </summary>
        /// <param name="point">待归一的坐标点。</param>
        /// <returns>高度被归一后的坐标点。</returns>
        private static Vector3 NormalizeFlightPlanePoint(Vector3 point)
        {
            point.y = 0f;
            return point;
        }

        /// <summary>
        /// 用当前投射物定义和计划中冻结的来源定义重新初始化视觉附加宿主。
        /// 这里不保存运行时附加件实例到存档，读档后直接按 Def 重建。
        /// </summary>
        private void InitializeVisualAttachments()
        {
            ClearTerminalVisualExactPosition();
            visualAttachmentsTerminated = false;
            visualAttachmentHost.Initialize(
                def,
                launchPlan != null ? launchPlan.VisualAttachmentProviderDefs : null,
                new ProjectileVisualAppearanceOverrides(
                    launchPlan != null && launchPlan.HasTrailColorOverride,
                    launchPlan != null ? launchPlan.TrailColorOverride : Color.white,
                    launchPlan != null && launchPlan.HasTrailCoreOverride,
                    launchPlan != null ? launchPlan.TrailCoreColorOverride : Color.black,
                    launchPlan != null ? launchPlan.TrailCoreWidthRatioOverride : 0.45f,
                    launchPlan != null ? launchPlan.TrailCoreOpacityOverride : 1f));
        }

        /// <summary>
        /// 绑定当前段飞行路径快照真值。
        /// </summary>
        private void BindFlightPathSnapshot(ProjectileFlightPathSnapshot snapshot)
        {
            if (snapshot == null)
            {
                currentFlightPathSnapshot = null;
                currentFlightPathStartingTicksToImpact = 0f;
                return;
            }

            currentFlightPathSnapshot = new ProjectileFlightPathSnapshot
            {
                Kind = snapshot.Kind,
                Start = NormalizeFlightPlanePoint(snapshot.Start),
                ControlA = NormalizeFlightPlanePoint(snapshot.ControlA),
                ControlB = NormalizeFlightPlanePoint(snapshot.ControlB),
                End = NormalizeFlightPlanePoint(snapshot.End)
            };
            currentFlightPathSnapshot.ApproximateLength = ProjectileFlightPathUtility.EstimateLength(currentFlightPathSnapshot);
            currentFlightPathStartingTicksToImpact = ComputeFlightPathStartingTicksToImpact(currentFlightPathSnapshot);
            // TODO: 如果后续要做飞行中途实时重建路径，只扩展路径宿主，不允许业务模块直接写 projectile 物理字段。
        }

        /// <summary>
        /// 把路径快照整体平移到真实发射点。
        /// 首段路径在计划阶段只能拿到近似原点，因此真正发射时必须再对齐一次。
        /// </summary>
        private ProjectileFlightPathSnapshot AlignFlightPathSnapshotStart(ProjectileFlightPathSnapshot snapshot, Vector3 actualStart)
        {
            if (snapshot == null)
            {
                return null;
            }

            Vector3 normalizedActualStart = NormalizeFlightPlanePoint(actualStart);
            Vector3 delta = normalizedActualStart - NormalizeFlightPlanePoint(snapshot.Start);
            return new ProjectileFlightPathSnapshot
            {
                Kind = snapshot.Kind,
                Start = normalizedActualStart,
                ControlA = NormalizeFlightPlanePoint(snapshot.ControlA) + delta,
                ControlB = NormalizeFlightPlanePoint(snapshot.ControlB) + delta,
                End = NormalizeFlightPlanePoint(snapshot.End) + delta,
                ApproximateLength = snapshot.ApproximateLength
            };
        }

        /// <summary>
        /// 构造当前段的线性路径快照。
        /// </summary>
        private ProjectileFlightPathSnapshot BuildLinearFlightPathSnapshot(Vector3 start, Vector3 end)
        {
            return ProjectileFlightPathUtility.CreateLinear(
                NormalizeFlightPlanePoint(start),
                NormalizeFlightPlanePoint(end));
        }

        private ProjectileFlightPathSnapshot ResolveConfiguredInitialFlightPathSnapshot(ProjectileFlightPathSnapshot defaultInitialFlightPathSnapshot)
        {
            if (defaultInitialFlightPathSnapshot == null || launchPlan == null || !launchPlan.HasInitialSegmentTriggerRatio)
            {
                return defaultInitialFlightPathSnapshot;
            }

            float initialSegmentTriggerRatio = Mathf.Clamp(launchPlan.InitialSegmentTriggerRatio, 0.1f, 1f);
            if (initialSegmentTriggerRatio >= 0.999f)
            {
                return defaultInitialFlightPathSnapshot;
            }

            Vector3 start = NormalizeFlightPlanePoint(defaultInitialFlightPathSnapshot.Start);
            Vector3 end = NormalizeFlightPlanePoint(defaultInitialFlightPathSnapshot.End);
            Vector3 shortenedEnd = Vector3.Lerp(start, end, initialSegmentTriggerRatio);
            return ProjectileFlightPathUtility.CreateLinear(start, shortenedEnd);
        }

        /// <summary>
        /// 计算续段时供 vanilla 自由拦截使用的兼容起点。
        /// </summary>
        private Vector3 ComputeContinuationOrigin(Vector3 nextOrigin)
        {
            Vector3 normalizedOrigin = NormalizeFlightPlanePoint(nextOrigin);
            Vector3 startDirection = ResolveCurrentFlightStartDirection();
            if (startDirection.sqrMagnitude <= 0.0001f)
            {
                return normalizedOrigin;
            }

            return normalizedOrigin - startDirection * 6f;
        }

        /// <summary>
        /// 计算当前段已飞行进度。
        /// </summary>
        private float ResolveCurrentFlightProgress()
        {
            if (currentFlightPathStartingTicksToImpact <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(1f - (float)ticksToImpact / currentFlightPathStartingTicksToImpact);
        }

        /// <summary>
        /// 解析当前段起点切线方向。
        /// </summary>
        private Vector3 ResolveCurrentFlightStartDirection()
        {
            Vector3 startDirection = currentFlightPathSnapshot != null
                ? ProjectileFlightPathUtility.EvaluateTangent(currentFlightPathSnapshot, 0f)
                : (destination - origin).Yto0();
            if (startDirection.sqrMagnitude <= 0.0001f && currentFlightPathSnapshot != null)
            {
                startDirection = (currentFlightPathSnapshot.End - currentFlightPathSnapshot.Start).Yto0();
            }

            return startDirection.sqrMagnitude <= 0.0001f
                ? Vector3.forward
                : startDirection.normalized;
        }

        /// <summary>
        /// 向视觉附加宿主广播发射事件。
        /// </summary>
        private void NotifyVisualLaunch()
        {
            visualAttachmentHost.NotifyLaunch(
                new ProjectileVisualLaunchContext(
                    base.Map,
                    def,
                    ThingID,
                    AttackInstanceId,
                    ResultId,
                    currentFlightPathSnapshot != null
                        ? currentFlightPathSnapshot.Start
                        : NormalizeFlightPlanePoint(origin),
                    ResolveCurrentFlightStartDirection()));
        }

        /// <summary>
        /// 向视觉附加宿主广播读档恢复事件。
        /// </summary>
        private void NotifyVisualRestored()
        {
            visualAttachmentHost.NotifyRestored(
                new ProjectileVisualRestoreContext(
                    base.Map,
                    def,
                    ThingID,
                    AttackInstanceId,
                    ResultId,
                    NormalizeFlightPlanePoint(ExactPosition)));
        }

        /// <summary>
        /// 向视觉附加宿主广播本次飞行样本。
        /// </summary>
        /// <param name="sampleMap">当前样本使用的地图快照。</param>
        /// <param name="sampleStart">当前样本起点。</param>
        /// <param name="sampleEnd">当前样本终点。</param>
        /// <param name="delta">当前样本推进刻数。</param>
        private void PublishVisualFlightSample(Map sampleMap, Vector3 sampleStart, Vector3 sampleEnd, int delta)
        {
            if (visualAttachmentsTerminated
                || sampleMap == null
                || delta <= 0
                || (sampleEnd - sampleStart).sqrMagnitude <= 0.0001f)
            {
                return;
            }

            visualAttachmentHost.NotifyFlightSample(
                new ProjectileVisualFlightSampleContext(
                    sampleMap,
                    def,
                    ThingID,
                    AttackInstanceId,
                    ResultId,
                    sampleStart,
                    sampleEnd,
                    delta));
        }

        /// <summary>
        /// 向视觉附加宿主广播一次终止事件。
        /// </summary>
        /// <param name="sampleMap">当前终止使用的地图快照。</param>
        /// <param name="currentPosition">当前终止位置。</param>
        private void NotifyVisualTerminate(Map sampleMap, Vector3 currentPosition)
        {
            if (visualAttachmentsTerminated)
            {
                return;
            }

            visualAttachmentsTerminated = true;
            visualAttachmentHost.NotifyTerminate(
                new ProjectileVisualTerminateContext(
                    sampleMap,
                    def,
                    ThingID,
                    AttackInstanceId,
                    ResultId,
                    currentPosition));
            ClearTerminalVisualExactPosition();
        }

        /// <summary>
        /// 解析本次 `TickInterval（间隔推进）` 结束后的视觉样本终点。
        /// 视觉样本只允许消费真实轨迹真值，绝不允许退回格坐标去伪造一个“看起来还有终点”的点。
        /// 如果当前段已经自然走到终点，就取路径终点；如果是未知销毁路径，就停在最后一个已确认的真实点。
        /// </summary>
        /// <param name="sampleStart">当前样本起点。</param>
        /// <returns>可用于视觉样本的终点。</returns>
        private Vector3 ResolveVisualSampleEnd(Vector3 sampleStart)
        {
            if (Destroyed && terminalVisualExactPosition.HasValue)
            {
                return terminalVisualExactPosition.Value;
            }

            return NormalizeFlightPlanePoint(ExactPosition);
        }

        /// <summary>
        /// 冻结当前投射物本次终止应交给视觉层消费的真实终止点。
        /// 这里统一压回共享飞行平面，避免附件层再推断一次“命中后应该停在哪里”。
        /// </summary>
        /// <param name="exactPosition">当前已确认的真实终止点。</param>
        private void FreezeTerminalVisualExactPosition(Vector3 exactPosition)
        {
            terminalVisualExactPosition = NormalizeFlightPlanePoint(exactPosition);
        }

        /// <summary>
        /// 清理当前投射物暂存的终止视觉真值。
        /// 终止事件发出后必须立即清掉，避免后续路径误读到上一轮终止点。
        /// </summary>
        private void ClearTerminalVisualExactPosition()
        {
            terminalVisualExactPosition = null;
        }

        /// <summary>
        /// 解析当前投射物应冻结给终止视觉样本使用的真实终止点。
        /// 如果本轮已经冻结过，就直接复用；否则读取当前 ExactPosition 真值作为保底。
        /// </summary>
        /// <returns>可用于终止视觉样本的真实终止点。</returns>
        private Vector3 ResolveCurrentTerminalVisualExactPosition()
        {
            return terminalVisualExactPosition.HasValue
                ? terminalVisualExactPosition.Value
                : NormalizeFlightPlanePoint(ExactPosition);
        }

        /// <summary>
        /// 计算当前路径快照的起始段时长。
        /// </summary>
        private float ComputeFlightPathStartingTicksToImpact(ProjectileFlightPathSnapshot snapshot)
        {
            float pathLength = snapshot != null
                ? Mathf.Max(0.001f, snapshot.ApproximateLength)
                : (origin - destination).magnitude;
            float pathTicksToImpact = pathLength / def.projectile.SpeedTilesPerTick;
            if (pathTicksToImpact <= 0f)
            {
                pathTicksToImpact = 0.001f;
            }

            return pathTicksToImpact;
        }

        /// <summary>
        /// 基于当前段重新计算原版飞行时长。
        /// 只复用原版 `StartingTicksToImpact（初始飞行时长）` 公式，不引入新速度逻辑。
        /// </summary>
        private void ResetFlightDurationFromCurrentSegment()
        {
            float pathLength = currentFlightPathSnapshot != null
                ? Mathf.Max(0.001f, currentFlightPathSnapshot.ApproximateLength)
                : (origin - destination).magnitude;
            currentFlightPathStartingTicksToImpact = pathLength / def.projectile.SpeedTilesPerTick;
            if (currentFlightPathStartingTicksToImpact <= 0f)
            {
                currentFlightPathStartingTicksToImpact = 0.001f;
            }

            ticksToImpact = Mathf.CeilToInt(currentFlightPathStartingTicksToImpact);
            if (ticksToImpact < 1)
            {
                ticksToImpact = 1;
            }

            lifetime = ticksToImpact;
        }

        /// <summary>
        /// 输出诊断时规避空标识。
        /// </summary>
        private static bool TryResolveDiagnosticTargetPosition(LocalTargetInfo target, out Vector3 targetPos)
        {
            targetPos = Vector3.zero;
            if (!target.IsValid)
            {
                return false;
            }

            if (!target.HasThing || target.Thing == null)
            {
                targetPos = target.Cell.ToVector3Shifted().Yto0();
                return true;
            }

            Thing thing = target.Thing;
            targetPos = thing.Spawned
                ? thing.DrawPos.Yto0()
                : target.Cell.ToVector3Shifted().Yto0();
            return true;
        }

        private static string DescribeTargetDetailed(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return "<invalid>";
            }

            if (!target.HasThing || target.Thing == null)
            {
                return "cell=" + target.Cell;
            }

            Thing thing = target.Thing;
            string drawPos = thing.Spawned
                ? thing.DrawPos.ToString()
                : "<unspawned>";
            return thing.ThingID
                + "|cell=" + target.Cell
                + "|drawPos=" + drawPos;
        }

        private static string DescribeFlightPathSnapshot(ProjectileFlightPathSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "<none>";
            }

            return snapshot.Kind
                + "|start=" + snapshot.Start
                + "|controlA=" + snapshot.ControlA
                + "|controlB=" + snapshot.ControlB
                + "|end=" + snapshot.End
                + "|length=" + snapshot.ApproximateLength.ToString("F3");
        }

        private static string SafeDiagnosticId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value;
        }

        /// <summary>
        /// 段内穿格审计快照。
        /// 用于定位“段终点格没问题，但中间疑似穿过了满格阻挡物”的证据。
        /// </summary>
        private sealed class SegmentTraversalAuditSummary
        {
            public ProjectileFlightPathKind PathKind = ProjectileFlightPathKind.Linear;

            public int SamplePointCount;

            public int TraversedCellCount;

            public string TraversedCellsSummary = "none";

            public bool CrossedObjectiveBlocker;

            public IntVec3 FirstObjectiveBlockerCell = IntVec3.Invalid;

            public string FirstObjectiveBlockerAudit = "none";

            public bool CrossedHitCandidate;

            public IntVec3 FirstHitCandidateCell = IntVec3.Invalid;

            public string FirstHitCandidateAudit = "none";

            public bool CrossedBlockingThing;

            public IntVec3 FirstBlockingCell = IntVec3.Invalid;

            public string FirstBlockingAudit = "none";

            public bool CrossedClosedDoor;

            public IntVec3 FirstClosedDoorCell = IntVec3.Invalid;

            public string FirstClosedDoorAudit = "none";
        }

        private void LogArrivalBoundaryDecision(ArrivalRecord arrival, bool continueFlight)
        {
            if (!BdpDiagnostics.AttackExecutionEnabled)
            {
                return;
            }

            Vector3 exactPos = ExactPosition.Yto0();
            Vector3 segmentStart = currentFlightPathSnapshot != null
                ? currentFlightPathSnapshot.Start
                : NormalizeFlightPlanePoint(origin);
            Vector3 segmentEnd = currentFlightPathSnapshot != null
                ? currentFlightPathSnapshot.End
                : NormalizeFlightPlanePoint(destination);
            SegmentTraversalAuditSummary segmentTraversalAudit = currentSegmentCollisionRecord != null
                ? BuildSegmentTraversalAudit(currentSegmentCollisionRecord)
                : currentFlightPathSnapshot != null
                ? BuildSegmentTraversalAudit(currentFlightPathSnapshot)
                : BuildSegmentTraversalAudit(segmentStart, segmentEnd);
            IntVec3 segmentEndCell = segmentEnd.ToIntVec3();
            IntVec3 physicalDestinationCell = destination.ToIntVec3();
            string endCellAudit = DescribeCellAudit(
                segmentEndCell,
                out bool endCellHasHitCandidate,
                out bool endCellHasBlockingThing,
                out bool endCellHasClosedDoor);
            bool objectiveBlockerTerminated = pendingObjectiveBlockerImpactThing != null && pendingObjectiveBlockerImpactCell.IsValid;
            string attackId = !string.IsNullOrWhiteSpace(AttackInstanceId)
                ? AttackInstanceId
                : launchPlan != null ? launchPlan.AttackInstanceId : null;
            string resultId = !string.IsNullOrWhiteSpace(ResultId)
                ? ResultId
                : launchPlan != null ? launchPlan.ResultId : null;
            BdpDiagnostics.AttackExecution(
                "event=projectile_arrival_boundary"
                + ", attackId=" + SafeDiagnosticId(attackId)
                + ", resultId=" + SafeDiagnosticId(resultId)
                + ", projectile=" + SafeDiagnosticId(ThingID)
                + ", branch=" + (continueFlight ? "continue_flight" : "vanilla_impact")
                + ", exactPos=" + exactPos
                + ", segmentStart=" + segmentStart
                + ", segmentStartCell=" + segmentStart.ToIntVec3()
                + ", positionCell=" + base.Position
                + ", segmentEnd=" + segmentEnd
                + ", segmentEndCell=" + segmentEndCell
                + ", physicalDestination=" + NormalizeFlightPlanePoint(destination)
                + ", physicalDestinationCell=" + physicalDestinationCell
                + ", vanillaFreeInterceptWouldSkipEndCell=" + (segmentEndCell == physicalDestinationCell)
                + ", segmentPathKind=" + segmentTraversalAudit.PathKind
                + ", segmentSamplePointCount=" + segmentTraversalAudit.SamplePointCount
                + ", segmentTraversedCellCount=" + segmentTraversalAudit.TraversedCellCount
                + ", segmentTraversedCells=" + segmentTraversalAudit.TraversedCellsSummary
                + ", segmentCrossedObjectiveBlocker=" + segmentTraversalAudit.CrossedObjectiveBlocker
                + ", segmentFirstObjectiveBlockerCell=" + segmentTraversalAudit.FirstObjectiveBlockerCell
                + ", segmentFirstObjectiveBlockerAudit=" + segmentTraversalAudit.FirstObjectiveBlockerAudit
                + ", segmentCrossedHitCandidate=" + segmentTraversalAudit.CrossedHitCandidate
                + ", segmentFirstHitCandidateCell=" + segmentTraversalAudit.FirstHitCandidateCell
                + ", segmentFirstHitCandidateAudit=" + segmentTraversalAudit.FirstHitCandidateAudit
                + ", segmentCrossedBlockingThing=" + segmentTraversalAudit.CrossedBlockingThing
                + ", segmentFirstBlockingCell=" + segmentTraversalAudit.FirstBlockingCell
                + ", segmentFirstBlockingAudit=" + segmentTraversalAudit.FirstBlockingAudit
                + ", segmentCrossedClosedDoor=" + segmentTraversalAudit.CrossedClosedDoor
                + ", segmentFirstClosedDoorCell=" + segmentTraversalAudit.FirstClosedDoorCell
                + ", segmentFirstClosedDoorAudit=" + segmentTraversalAudit.FirstClosedDoorAudit
                + ", objectiveBlockerTerminated=" + objectiveBlockerTerminated
                + ", objectiveBlockerImpactThing=" + DescribeThingBrief(pendingObjectiveBlockerImpactThing)
                + ", objectiveBlockerImpactCell=" + pendingObjectiveBlockerImpactCell
                + ", endCellHasHitCandidate=" + endCellHasHitCandidate
                + ", endCellHasBlockingThing=" + endCellHasBlockingThing
                + ", endCellHasClosedDoor=" + endCellHasClosedDoor
                + ", usedTarget=" + DescribeTargetDetailed(usedTarget)
                + ", intendedTarget=" + DescribeTargetDetailed(intendedTarget)
                + ", arrivalNextTarget=" + DescribeTargetDetailed(arrival != null ? arrival.NextTarget : LocalTargetInfo.Invalid)
                + ", arrivalNextBindingTarget=" + DescribeTargetDetailed(arrival != null ? arrival.NextBindingTarget : LocalTargetInfo.Invalid)
                + ", arrivalNextDestination=" + (arrival != null ? arrival.NextDestination.ToString() : Vector3.zero.ToString())
                + ", endCellAudit=" + endCellAudit);
        }

        private void LogImpactResolution(Thing hitThing, bool blockedByShield, IntVec3 position)
        {
            if (!BdpDiagnostics.AttackExecutionEnabled)
            {
                return;
            }

            string attackId = !string.IsNullOrWhiteSpace(AttackInstanceId)
                ? AttackInstanceId
                : launchPlan != null ? launchPlan.AttackInstanceId : null;
            string resultId = !string.IsNullOrWhiteSpace(ResultId)
                ? ResultId
                : launchPlan != null ? launchPlan.ResultId : null;
            Vector3 exactPos = ExactPosition.Yto0();
            Vector3 impactSegmentStart = currentFlightPathSnapshot != null
                ? currentFlightPathSnapshot.Start
                : NormalizeFlightPlanePoint(origin);
            Vector3 impactSegmentProjectedEnd = currentFlightPathSnapshot != null
                ? currentFlightPathSnapshot.End
                : NormalizeFlightPlanePoint(destination);
            SegmentTraversalAuditSummary impactSegmentTraversalAudit = currentFlightPathSnapshot != null
                ? BuildSegmentTraversalAudit(currentFlightPathSnapshot, ResolveCurrentFlightProgress())
                : BuildSegmentTraversalAudit(impactSegmentStart, exactPos);
            BdpDiagnostics.AttackExecution(
                "event=projectile_real_impact"
                + ", attackId=" + SafeDiagnosticId(attackId)
                + ", resultId=" + SafeDiagnosticId(resultId)
                + ", projectile=" + SafeDiagnosticId(ThingID)
                + ", impactCell=" + position
                + ", exactPos=" + exactPos
                + ", impactSegmentStart=" + impactSegmentStart
                + ", impactSegmentStartCell=" + impactSegmentStart.ToIntVec3()
                + ", impactSegmentProjectedEnd=" + impactSegmentProjectedEnd
                + ", impactSegmentProjectedEndCell=" + impactSegmentProjectedEnd.ToIntVec3()
                + ", impactSegmentPathKind=" + impactSegmentTraversalAudit.PathKind
                + ", impactSegmentSamplePointCount=" + impactSegmentTraversalAudit.SamplePointCount
                + ", impactSegmentTraversedCellCount=" + impactSegmentTraversalAudit.TraversedCellCount
                + ", impactSegmentTraversedCells=" + impactSegmentTraversalAudit.TraversedCellsSummary
                + ", impactSegmentCrossedObjectiveBlocker=" + impactSegmentTraversalAudit.CrossedObjectiveBlocker
                + ", impactSegmentFirstObjectiveBlockerCell=" + impactSegmentTraversalAudit.FirstObjectiveBlockerCell
                + ", impactSegmentFirstObjectiveBlockerAudit=" + impactSegmentTraversalAudit.FirstObjectiveBlockerAudit
                + ", impactSegmentCrossedHitCandidate=" + impactSegmentTraversalAudit.CrossedHitCandidate
                + ", impactSegmentFirstHitCandidateCell=" + impactSegmentTraversalAudit.FirstHitCandidateCell
                + ", impactSegmentFirstHitCandidateAudit=" + impactSegmentTraversalAudit.FirstHitCandidateAudit
                + ", impactSegmentCrossedBlockingThing=" + impactSegmentTraversalAudit.CrossedBlockingThing
                + ", impactSegmentFirstBlockingCell=" + impactSegmentTraversalAudit.FirstBlockingCell
                + ", impactSegmentFirstBlockingAudit=" + impactSegmentTraversalAudit.FirstBlockingAudit
                + ", impactSegmentCrossedClosedDoor=" + impactSegmentTraversalAudit.CrossedClosedDoor
                + ", impactSegmentFirstClosedDoorCell=" + impactSegmentTraversalAudit.FirstClosedDoorCell
                + ", impactSegmentFirstClosedDoorAudit=" + impactSegmentTraversalAudit.FirstClosedDoorAudit
                + ", blockedByShield=" + blockedByShield
                + ", hitThing=" + DescribeThingBrief(hitThing)
                + ", usedTarget=" + DescribeTargetDetailed(usedTarget)
                + ", intendedTarget=" + DescribeTargetDetailed(intendedTarget));
        }

        /// <summary>
        /// 基于统一段扫描服务生成当前飞行段的宿主级审计摘要。
        /// 它复用运行态客观碰撞事实，只补齐诊断所需的候选命中与格审计文本。
        /// </summary>
        private SegmentTraversalAuditSummary BuildSegmentTraversalAudit(Vector3 segmentStart, Vector3 segmentEnd)
        {
            SegmentCollisionRecord segmentCollisionRecord = SegmentCollisionService.ScanSegment(this, segmentStart, segmentEnd);
            return BuildSegmentTraversalAudit(segmentCollisionRecord);
        }

        /// <summary>
        /// 基于完整 flight path snapshot 生成宿主级审计摘要。
        /// </summary>
        /// <param name="flightPathSnapshot">当前段几何快照。</param>
        /// <returns>当前段的宿主级审计摘要。</returns>
        private SegmentTraversalAuditSummary BuildSegmentTraversalAudit(ProjectileFlightPathSnapshot flightPathSnapshot)
        {
            SegmentCollisionRecord segmentCollisionRecord = SegmentCollisionService.ScanSegment(this, flightPathSnapshot);
            return BuildSegmentTraversalAudit(segmentCollisionRecord);
        }

        /// <summary>
        /// 基于 flight path snapshot 的局部进度区间生成宿主级审计摘要。
        /// </summary>
        /// <param name="flightPathSnapshot">当前段几何快照。</param>
        /// <param name="endProgress">扫描终止进度。</param>
        /// <returns>当前进度区间的宿主级审计摘要。</returns>
        private SegmentTraversalAuditSummary BuildSegmentTraversalAudit(
            ProjectileFlightPathSnapshot flightPathSnapshot,
            float endProgress)
        {
            SegmentCollisionRecord segmentCollisionRecord = SegmentCollisionService.ScanSegment(
                this,
                flightPathSnapshot,
                0f,
                endProgress);
            return BuildSegmentTraversalAudit(segmentCollisionRecord);
        }

        /// <summary>
        /// 基于统一段扫描事实，补齐宿主级命中候选与阻挡审计摘要。
        /// </summary>
        /// <param name="segmentCollisionRecord">当前段客观碰撞扫描结果。</param>
        /// <returns>当前段的宿主级审计摘要。</returns>
        private SegmentTraversalAuditSummary BuildSegmentTraversalAudit(SegmentCollisionRecord segmentCollisionRecord)
        {
            SegmentTraversalAuditSummary summary = new SegmentTraversalAuditSummary();
            if (segmentCollisionRecord == null)
            {
                return summary;
            }

            Map map = base.Map;
            List<IntVec3> traversedCells = segmentCollisionRecord.TraversedCells ?? new List<IntVec3>();
            summary.PathKind = segmentCollisionRecord.PathKind;
            summary.SamplePointCount = segmentCollisionRecord.SamplePointCount;
            summary.TraversedCellCount = traversedCells.Count;
            summary.TraversedCellsSummary = DescribeTraversedCells(traversedCells);
            summary.CrossedObjectiveBlocker = segmentCollisionRecord.CrossedObjectiveBlocker;
            summary.FirstObjectiveBlockerCell = segmentCollisionRecord.FirstObjectiveBlockerCell;
            summary.FirstObjectiveBlockerAudit = segmentCollisionRecord.FirstObjectiveBlockerAudit;

            if (map == null)
            {
                summary.TraversedCellsSummary = "<no_map>";
                return summary;
            }

            for (int i = 0; i < traversedCells.Count; i++)
            {
                IntVec3 cell = traversedCells[i];
                string audit = DescribeCellAudit(
                    cell,
                    out bool hasHitCandidate,
                    out bool hasBlockingThing,
                    out bool hasClosedDoor);

                if (hasHitCandidate && !summary.CrossedHitCandidate)
                {
                    summary.CrossedHitCandidate = true;
                    summary.FirstHitCandidateCell = cell;
                    summary.FirstHitCandidateAudit = audit;
                }

                if (hasBlockingThing && !summary.CrossedBlockingThing)
                {
                    summary.CrossedBlockingThing = true;
                    summary.FirstBlockingCell = cell;
                    summary.FirstBlockingAudit = audit;
                }

                if (hasClosedDoor && !summary.CrossedClosedDoor)
                {
                    summary.CrossedClosedDoor = true;
                    summary.FirstClosedDoorCell = cell;
                    summary.FirstClosedDoorAudit = audit;
                }
            }

            return summary;
        }

        /// <summary>
        /// 输出目标摘要，方便定位续段是否仍在追同一 Thing。
        /// </summary>
        private static string DescribeTarget(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return "<invalid>";
            }

            if (target.HasThing && target.Thing != null)
            {
                return target.Thing.ThingID;
            }

            return target.Cell.ToString();
        }

        /// <summary>
        /// 预测 base.Tick 执行完后的 ExactPosition（精确世界坐标）。
        /// 这里只复刻原版位置公式，不参与任何行为修改。
        /// </summary>
        /// <returns>下一帧理论终点位置。</returns>
        /// <summary>
        /// 统计并记录某个候选格在原版自由拦截逻辑里是否具备“会被检查”的资格。
        /// 它只复刻资格判断，不触发真实命中。
        /// </summary>
        /// <param name="cell">当前正在预演的格子。</param>
        /// <param name="physicalDestinationCell">当前物理终点格。</param>
        /// <param name="eligibleCellCount">累计可进入原版拦截判定的格子数。</param>
        /// <param name="hitCandidateCellCount">累计存在可命中候选物的格子数。</param>
        /// <param name="firstEligibleCell">首个可进入原版拦截判定的格子。</param>
        /// <param name="firstHitCandidateCell">首个存在可命中候选物的格子。</param>
        /// <param name="firstHitCandidateThings">首个候选格的候选物摘要。</param>
        private string DescribeCellAudit(
            IntVec3 cell,
            out bool hasHitCandidate,
            out bool hasBlockingThing,
            out bool hasClosedDoor)
        {
            hasHitCandidate = false;
            hasBlockingThing = false;
            hasClosedDoor = false;

            Map map = base.Map;
            if (map == null)
            {
                return "<no_map>";
            }

            if (!cell.InBounds(map))
            {
                return "<out_of_bounds>";
            }

            List<Thing> things = cell.GetThingList(map);
            if (things == null || things.Count == 0)
            {
                return "none";
            }

            List<string> entries = new List<string>();
            int overflowCount = 0;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing == this)
                {
                    continue;
                }

                bool canHit = CanHit(thing);
                bool closedDoor = thing is Building_Door door && !door.Open;
                bool blockingThing = thing.def != null
                    && thing.def.Fillage == FillCategory.Full
                    && !closedDoor;
                if (canHit)
                {
                    hasHitCandidate = true;
                }

                if (blockingThing)
                {
                    hasBlockingThing = true;
                }

                if (closedDoor)
                {
                    hasClosedDoor = true;
                }

                if (entries.Count < 6)
                {
                    entries.Add(DescribeThingAudit(thing, canHit, blockingThing, closedDoor));
                }
                else
                {
                    overflowCount++;
                }
            }

            if (entries.Count == 0)
            {
                return "none";
            }

            if (overflowCount > 0)
            {
                entries.Add("...+" + overflowCount);
            }

            return string.Join("|", entries);
        }

        private static string DescribeTraversedCells(List<IntVec3> cells)
        {
            if (cells == null || cells.Count == 0)
            {
                return "none";
            }

            List<string> parts = new List<string>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                parts.Add(cells[i].ToString());
            }

            return string.Join(">", parts);
        }

        private string DescribeThingAudit(Thing thing, bool canHit, bool blockingThing, bool closedDoor)
        {
            if (thing == null)
            {
                return "<null>";
            }

            string fillage = thing.def != null
                ? thing.def.Fillage.ToString()
                : "<none>";
            return SafeDiagnosticId(thing.ThingID)
                + ":" + SafeDiagnosticId(thing.def != null ? thing.def.defName : thing.GetType().Name)
                + "{canHit=" + canHit
                + ",reason=" + ResolveCanHitReason(thing)
                + ",fillage=" + fillage
                + ",closedDoor=" + closedDoor
                + ",blocking=" + blockingThing
                + "}";
        }

        private string ResolveCanHitReason(Thing thing)
        {
            if (thing == null)
            {
                return "null";
            }

            if (!thing.Spawned)
            {
                return "not_spawned";
            }

            if (thing == this)
            {
                return "self";
            }

            if (thing == launcher)
            {
                return "launcher";
            }

            ProjectileHitFlags hitFlags = HitFlags;
            if (hitFlags == ProjectileHitFlags.None)
            {
                return "hit_flags_none";
            }

            if (thing.Map != base.Map)
            {
                return "map_mismatch";
            }

            if (CoverUtility.ThingCovered(thing, base.Map))
            {
                return "thing_covered";
            }

            if (thing == intendedTarget && (hitFlags & ProjectileHitFlags.IntendedTarget) != ProjectileHitFlags.None)
            {
                return "intended_target";
            }

            if (thing != intendedTarget)
            {
                if (thing is Pawn && (hitFlags & ProjectileHitFlags.NonTargetPawns) != ProjectileHitFlags.None)
                {
                    return "non_target_pawn";
                }

                if (!(thing is Pawn) && (hitFlags & ProjectileHitFlags.NonTargetWorld) != ProjectileHitFlags.None)
                {
                    return "non_target_world";
                }
            }

            if (thing == intendedTarget && thing.def != null && thing.def.Fillage == FillCategory.Full)
            {
                return "intended_full_fill";
            }

            return "filtered_by_hit_flags";
        }

        private static string DescribeThingBrief(Thing thing)
        {
            if (thing == null)
            {
                return "<null>";
            }

            return SafeDiagnosticId(thing.ThingID)
                + ":" + SafeDiagnosticId(thing.def != null ? thing.def.defName : thing.GetType().Name)
                + "@"
                + thing.Position;
        }
    }
}
