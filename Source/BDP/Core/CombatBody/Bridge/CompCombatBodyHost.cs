using System.Collections.Generic;
using BDP.Core.CombatBody.Wounds;
using BDP.Core.CombatBody.Wounds.Presentation;
using BDP.Core.CombatBodySession;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体主链宿主 Comp。
    /// 它是附着在 Pawn 上的薄宿主位，只负责承接战斗体主链对象，不新增第四个真值 owner。
    /// </summary>
    public sealed class CompCombatBodyHost : ThingComp, IThingHolder, ICombatBodyMaterialHost
    {
        /// <summary>
        /// 战斗体相位真值。
        /// </summary>
        private CombatBodyState state;

        /// <summary>
        /// 战斗体宿主事务状态。
        /// </summary>
        private HostState hostState;

        /// <summary>
        /// Pawn 宿主桥。
        /// </summary>
        private PawnCombatBodyBridge host;

        /// <summary>
        /// 战斗体快照策略。
        /// </summary>
        private CombatBodySnapshotPolicy snapshotPolicy;

        /// <summary>
        /// 战斗体快照服务。
        /// </summary>
        private CombatBodySnapshotService snapshotService;

        /// <summary>
        /// CombatBody 正式服务。
        /// </summary>
        private CombatBodyService rawCombatBodyService;

        /// <summary>
        /// 战斗会话判断策略。
        /// </summary>
        private CombatBodySessionPolicy combatBodySessionPolicy;

        /// <summary>
        /// CombatBody 对外统一接线服务。
        /// </summary>
        private CombatBodySessionService combatBodySessionService;

        /// <summary>
        /// CombatBody Active 期间的伤口运行时。
        /// </summary>
        private CombatBodyWoundRuntime woundRuntime;

        /// <summary>
        /// 对外统一返回 CombatBody 正式服务。
        /// </summary>
        internal CombatBodySessionService Service
        {
            get
            {
                EnsureInternalState();
                return combatBodySessionService;
            }
        }

        /// <summary>现有人类材料身份仍从原会话相位派生。</summary>
        public bool IsCombatBodyActive => Service.Phase == CombatBodyPhase.Active;
        /// <summary>人类崩解表现期也继续维护伤口。</summary>
        public bool HasCombatBodyWoundRuntime => IsCombatBodyActive || Service.Phase == CombatBodyPhase.Collapsing;
        /// <summary>人类仍使用原版可流血部位准入。</summary>
        public bool RequiresBiologicalWoundPart => true;
        /// <summary>共用接点返回原有实例，不迁移存档与持有位置。</summary>
        public CombatBodyWoundRuntime CombatBodyWounds => WoundRuntime;

        /// <summary>对内暴露战斗体原位伤口运行时。</summary>
        internal CombatBodyWoundRuntime WoundRuntime
        {
            get
            {
                EnsureInternalState();
                return woundRuntime;
            }
        }

        /// <summary>
        /// 当前宿主事务状态。
        /// </summary>
        internal HostState HostState
        {
            get
            {
                EnsureInternalState();
                return hostState;
            }
        }

        /// <summary>
        /// 对内暴露原始 CombatBody 相位服务。
        /// </summary>
        internal CombatBodyService RawService
        {
            get
            {
                EnsureInternalState();
                return rawCombatBodyService;
            }
        }

        /// <summary>
        /// 当前宿主的战斗体配置。
        /// </summary>
        private CompProperties_CombatBodyHost Props
        {
            get { return (CompProperties_CombatBodyHost)props; }
        }

        /// <summary>
        /// 当前战斗体维持消耗，从全局配置 Def 解析。
        /// </summary>
        internal float MaintenanceDrainPerSecond
        {
            get { return CombatBodyHostConfigResolver.Resolve().maintenanceDrainPerSecond; }
        }

        /// <summary>
        /// 存读档战斗体相位真值与宿主事务状态，并在读档后重建运行时链路。
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Deep.Look(ref state, "combatBodyState");
            Scribe_Deep.Look(ref hostState, "hostState");
            Scribe_Deep.Look(ref woundRuntime, "woundRuntime");
            CombatBodyWoundPresentationRegistry.ExposeData(parent as Pawn);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureInternalState();
                host?.ReconcileAfterLoad(state.Phase);
                combatBodySessionService?.RestoreAfterLoad();
                woundRuntime?.RestoreAfterLoad(parent as Pawn);
            }
        }

        /// <summary>
        /// 推进战斗体宿主级运行时收尾。
        /// 当前只负责崩解倒计时结束后的正式关闭，不把宿主 comp 扩成新的长期 owner。
        /// </summary>
        public override void CompTick()
        {
            base.CompTick();
            CombatBodySessionService service = Service;
            service.ReconcileResourceRules();
            woundRuntime?.Tick(parent as Pawn);
            if (service.Phase == CombatBodyPhase.Collapsing && service.GetCollapseRemaining() <= 0)
            {
                service.FinalizeCollapse();
            }
        }

        /// <summary>
        /// 初始化主链运行时对象。
        /// 主链对象都寄存在 Pawn 宿主 comp 上，Hediff 只留给战斗体激活后的表现层使用。
        /// </summary>
        private void EnsureInternalState()
        {
            bool shouldTraceInitialization = BdpDiagnostics.Enabled
                && (state == null
                    || hostState == null
                    || snapshotPolicy == null
                    || snapshotService == null
                    || host == null
                    || rawCombatBodyService == null
                    || combatBodySessionPolicy == null
                    || combatBodySessionService == null
                    || woundRuntime == null);
            long initializationStart = shouldTraceInitialization
                ? BdpDiagnostics.BeginTiming()
                : 0L;
            List<string> createdParts = shouldTraceInitialization ? new List<string>() : null;

            if (state == null)
            {
                state = new CombatBodyState();
                createdParts?.Add("state");
            }

            if (hostState == null)
            {
                hostState = new HostState();
                createdParts?.Add("hostState");
            }

            hostState.EnsureSnapshotState(this);
            hostState.EnsureFrontState(this);

            if (snapshotPolicy == null)
            {
                snapshotPolicy = new CombatBodySnapshotPolicy();
                createdParts?.Add("snapshotPolicy");
            }

            if (snapshotService == null)
            {
                snapshotService = new CombatBodySnapshotService(snapshotPolicy);
                createdParts?.Add("snapshotService");
            }

            Pawn pawn = parent as Pawn;
            if (host == null || host.Pawn != pawn)
            {
                host = new PawnCombatBodyBridge(pawn, hostState, snapshotService, snapshotPolicy);
                createdParts?.Add("pawnBridge");
            }

            if (rawCombatBodyService == null)
            {
                rawCombatBodyService = new CombatBodyService(
                    state,
                    host,
                    Props.collapseCooldownTicks);
                createdParts?.Add("combatBodyService");
            }

            if (combatBodySessionPolicy == null)
            {
                combatBodySessionPolicy = new CombatBodySessionPolicy();
                createdParts?.Add("sessionPolicy");
            }

            if (combatBodySessionService == null)
            {
                combatBodySessionService = new CombatBodySessionService(
                    this,
                    rawCombatBodyService,
                    combatBodySessionPolicy);
                createdParts?.Add("sessionService");
            }

            if (woundRuntime == null)
            {
                woundRuntime = new CombatBodyWoundRuntime();
                createdParts?.Add("woundRuntime");
            }

            if (shouldTraceInitialization)
            {
                BdpDiagnostics.TraceTiming(
                    "战斗体宿主初始化"
                    + " | pawn=" + (pawn != null ? pawn.ThingID : "null")
                    + " | created=" + (createdParts != null && createdParts.Count > 0
                        ? string.Join(",", createdParts)
                        : "none"),
                    initializationStart);
            }
        }

        /// <summary>
        /// 返回 CombatBody 宿主直接持有的东西容器。
        /// </summary>
        public ThingOwner GetDirectlyHeldThings()
        {
            EnsureInternalState();
            return hostState.SnapshotState != null
                ? hostState.SnapshotState.GetDirectlyHeldThings()
                : null;
        }

        /// <summary>
        /// 把 CombatBody 宿主持有的子容器追加给 RimWorld。
        /// </summary>
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            EnsureInternalState();
            hostState.SnapshotState?.GetChildHolders(outChildren);
            hostState.FrontState?.GetChildHolders(outChildren);
        }
    }
}
