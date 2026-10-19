using System;
using BDP.Core.Chips;
using BDP.Core.CombatBody;
using BDP.Core.CombatBody.Wounds;
using BDP.Core.CombatBody.Wounds.Presentation;
using BDP.Core.Expressions;
using BDP.Core.Permissions;
using BDP.Core.Trigger;
using BDP.Core.Trion;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Units
{
    /// <summary>永久战斗体的正式业务宿主；原版健康和既有资源仍各自持有事实。</summary>
    public sealed partial class CompTrionUnit : ThingComp, ICombatBodyMaterialHost,
        ITriggerFunctionAvailability, IChipActivationRequirementExemption, IMapPermissionStateChangeListener
    {
        /// <summary>归属可指向人物或建筑，接口实例本身不存档。</summary>
        private Thing controlOwner;
        /// <summary>保留存档中的运行事实；加载完成后重新读取外部条件。</summary>
        private bool operational;
        /// <summary>资源及地图最终初始化完成前不放行行动。</summary>
        private bool ready;
        /// <summary>该身体唯一的伤口运行时，不建立原身快照。</summary>
        private CombatBodyWoundRuntime wounds = new CombatBodyWoundRuntime();
        /// <summary>可替换的运行条件实例。</summary>
        private ITrionUnitOperationCondition operationCondition;
        /// <summary>当前已登记地图，离图前注销。</summary>
        private Map registeredMap;
        /// <summary>生命周期清理是幂等的，原版死亡与销毁可能先后通知。</summary>
        private bool cleaned;
        /// <summary>内部装备引用由原版装备容器保存本体，此处不重复持有。</summary>
        private ThingWithComps internalEquipment;
        /// <summary>当前失败原因只作派生显示，不保存第二份许可真值。</summary>
        private string operationFailure;

        /// <summary>所属原版角色。</summary>
        public Pawn Pawn => parent as Pawn;
        /// <summary>当前款型接入配置。</summary>
        public CompProperties_TrionUnit Settings => (CompProperties_TrionUnit)props;
        /// <summary>当前归属对象，额度仍由它自己的组件维护。</summary>
        public Thing ControlOwner => controlOwner;
        /// <summary>真正运行资格，不将身体倒地等原版条件覆盖为可行动。</summary>
        public bool IsOperational => ready && operational && Pawn != null && !Pawn.Dead && !Pawn.Destroyed;
        /// <summary>停机身体依然是战斗体，不能接到人类解除或崩解流程。</summary>
        public bool IsCombatBodyActive => Pawn != null && !Pawn.Dead && !Pawn.Destroyed;
        /// <summary>伤口生命周期独立于运行许可，停机期间继续泄露。</summary>
        public bool HasCombatBodyWoundRuntime => IsCombatBodyActive;
        /// <summary>机械部位不以生理流血系数决定 Trion 泄露资格。</summary>
        public bool RequiresBiologicalWoundPart => false;
        /// <summary>共用补丁访问本组件原位持有的实例。</summary>
        public CombatBodyWoundRuntime CombatBodyWounds => wounds;
        /// <summary>停机只限制功能发布，不清除槽位激活。</summary>
        public bool TriggerFunctionsAvailable => IsOperational;
        /// <summary>固定款型不受芯片人物资格限制；实际功能仍由独立运行资格与费用控制。</summary>
        public bool IgnoresChipActivationRequirements => true;
        /// <summary>地图许可监听范围。</summary>
        public Map Map => Pawn?.Map;
        /// <summary>内部装备只供生命周期和保护规则识别。</summary>
        public ThingWithComps InternalEquipment => internalEquipment;
        /// <summary>从有效归属的现成组件读取带宽能力，不认识具体中枢或建筑类。</summary>
        public ITrionUnitBandwidthSource BandwidthSource => ResolveBandwidthSource(controlOwner);

        /// <summary>解析归属能力，死亡人物或销毁建筑不再提供有效归属。</summary>
        private static ITrionUnitBandwidthSource ResolveBandwidthSource(Thing owner)
        {
            if (owner == null || owner.Destroyed || (owner is Pawn pawn && pawn.Dead)) return null;
            ThingWithComps host = owner as ThingWithComps;
            if (host?.AllComps == null) return null;
            foreach (ThingComp comp in host.AllComps)
                if (comp is ITrionUnitBandwidthSource source) return source;
            return null;
        }

        /// <summary>构造定义指定的运行条件，不把场景判断写死进生命周期。</summary>
        public override void Initialize(CompProperties properties)
        {
            base.Initialize(properties);
            operationCondition = (ITrionUnitOperationCondition)Activator.CreateInstance(Settings.operationConditionClass);
        }

        /// <summary>绑定只表达控制归属和带宽，不要求绑定者代为发出玩家命令。</summary>
        public AcceptanceReport TryBindTo(Thing owner)
        {
            ITrionUnitBandwidthSource source = ResolveBandwidthSource(owner);
            if (source == null) return new AcceptanceReport("BDP_TrionUnit_Block_Ownership".Translate());
            AcceptanceReport report = source.CanBind(Pawn);
            if (!report.Accepted) return report;
            if (controlOwner != owner)
            {
                BandwidthSource?.NotifyBindingChanged(Pawn, false);
                controlOwner = owner;
                source.NotifyBindingChanged(Pawn, true);
            }
            RefreshOperatingConditions();
            return true;
        }

        /// <summary>解除归属即撤销覆盖，不改派系、更不会野化。</summary>
        public void Unbind()
        {
            BandwidthSource?.NotifyBindingChanged(Pawn, false);
            controlOwner = null;
            RefreshOperatingConditions();
        }

        /// <summary>保存角色引用及必要状态；伤口本体仍由原版健康系统保存。</summary>
        public override void PostExposeData()
        {
            Scribe_References.Look(ref controlOwner, "trionUnitControlOwner");
            Scribe_References.Look(ref internalEquipment, "trionUnitInternalEquipment");
            Scribe_Values.Look(ref operational, "trionUnitOperational", false);
            Scribe_Values.Look(ref autoCombatEnabled, "trionUnitAutoCombatEnabled", false);
            Scribe_Deep.Look(ref wounds, "trionUnitWounds");
            CombatBodyWoundPresentationRegistry.ExposeData(Pawn);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (wounds == null) wounds = new CombatBodyWoundRuntime();
                ready = false;
                wounds.RestoreAfterLoad(Pawn);
            }
        }

        /// <summary>生成时只登记监听；最终条件推迟到地图初始化后的第一次逐刻处理。</summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            ready = false;
            cleaned = false;
            RegisterMap();
        }

        /// <summary>离图不继承旧地图许可，保留归属供未来跨图接入。</summary>
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            UnregisterMap();
            if (!Pawn.Dead) RefreshOperatingConditions();
        }

        /// <summary>初始化完成后推进生命、既有伤口及低频条件核对。</summary>
        public override void CompTick()
        {
            if (Pawn == null || Pawn.Dead || Pawn.Destroyed) return;
            ITrionReader resources = TrionSurfaceAccess.ResolveReader(Pawn);
            // Max为零表示资源尚未初始化；不得把这时的默认余额当作死亡。
            if (resources == null || resources.Max <= 0f) return;
            // 只在安全逐刻读取当前余额，不把过去的耗尽事件保存成另一个死亡事实。
            if (resources.Cur <= 0f)
            {
                Pawn.Kill(null);
                return;
            }
            if (!ready)
            {
                ready = true;
                // 新的活体初始化开始后，死亡／销毁清理才允许再次执行；不依赖是否重新上地图。
                cleaned = false;
                BandwidthSource?.NotifyBindingChanged(Pawn, true);
                EnsureBodyAndEquipment();
                // 首次判定也清理旧动作并重建投影，不能只等保存的运行值翻转。
                RefreshOperatingConditions(true);
                // 复活复用本组件，但死亡已销毁旧装配；激活恢复属于当前装配，不能终身锁存。
                // 出生、读档和复活统一走此入口；既有流程会跳过已激活槽，普通逐刻不重复执行。
                internalEquipment?.TryGetComp<CompTriggerBody>()?.AutoActivateSpecialSlots();
            }
            wounds.Tick(Pawn);
            if (Pawn.IsHashIntervalTick(Math.Max(1, Settings.conditionCheckInterval))) RefreshOperatingConditions();
            if (Pawn.IsHashIntervalTick(AutoCombatCheckInterval)) RefreshAutoCombat();
        }

        /// <summary>先用原版身体和装备初始化入口建立唯一内部承载。</summary>
        private void EnsureBodyAndEquipment()
        {
            if (Settings.bodyMarker != null && !Pawn.health.hediffSet.HasHediff(Settings.bodyMarker))
                Pawn.health.AddHediff(Settings.bodyMarker);
            if (Settings.internalTrigger == null || Pawn.equipment == null) return;
            if (internalEquipment != null && Pawn.equipment.Contains(internalEquipment)) return;
            foreach (ThingWithComps equipment in Pawn.equipment.AllEquipmentListForReading)
                if (equipment.def == Settings.internalTrigger) { internalEquipment = equipment; return; }
            internalEquipment = (ThingWithComps)ThingMaker.MakeThing(Settings.internalTrigger);
            Pawn.equipment.AddEquipment(internalEquipment);
        }

        /// <summary>条件变化只更新运行状态与既有投影，资源和身体身份不变。</summary>
        public void RefreshOperatingConditions(bool force = false)
        {
            if (!ready || Pawn == null || Pawn.Dead) return;
            RegisterMap();
            AcceptanceReport report = operationCondition.Evaluate(this);
            operationFailure = report.Reason;
            bool next = report.Accepted;
            bool wasOperational = operational;
            if (next == operational && !force) return;
            // 先写运行真值；停止旧任务时原版可能立即求新任务，不能让它读到旧的运行状态。
            operational = next;
            if (!next)
            {
                // 暂空应急同步完成；未来接入应急动作只改此处，不进入人类崩解。
                if (wasOperational) OnOperationRequirementLost();
                if (operational) return; // 应急回调若已恢复条件，旧停机收尾立即失效。
                if (Pawn.drafter != null) Pawn.drafter.Drafted = false;
                Pawn.pather?.StopDead();
                Pawn.jobs?.StopAll();
            }
            ExpressionSurfaceAccess.NotifySourceConditionsChanged(Pawn);
            RefreshAutoCombat();
            BdpDiagnostics.Trace("Trion兵运行状态 | pawn=" + Pawn.ThingID + " | operational=" + next);
        }

        /// <summary>当前应急接点为空；同步回调仅留实际所需替换点。</summary>
        public event Action<CompTrionUnit> OperationRequirementLost;
        /// <summary>调用可插拔应急处理，默认立即完成。</summary>
        private void OnOperationRequirementLost() { OperationRequirementLost?.Invoke(this); }

        /// <summary>许可翻转后重新读取提供者事实，不复制其余额或运行状态。</summary>
        public void NotifyPermissionStateChanged(string permissionKey, Faction faction)
        {
            if (faction == Pawn?.Faction) RefreshOperatingConditions();
        }

        /// <summary>登记当前地图，覆盖换图和读档；不保存全局人物索引。</summary>
        private void RegisterMap()
        {
            if (registeredMap == Map) return;
            UnregisterMap();
            registeredMap = Map;
            registeredMap?.GetComponent<MapPermissionMapComponent>()?.RegisterListener(this);
        }
        /// <summary>离图或死亡时从原地图移除监听。</summary>
        private void UnregisterMap()
        {
            registeredMap?.GetComponent<MapPermissionMapComponent>()?.UnregisterListener(this);
            registeredMap = null;
        }
        /// <summary>原版死亡通知负责释放归属和内部装备，不增加全局死亡补丁。</summary>
        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null) { Cleanup(); }
        /// <summary>销毁同样清理派生状态，重复调用无副作用。</summary>
        public override void PostDestroy(DestroyMode mode, Map previousMap) { Cleanup(); }
        /// <summary>仅清理本兵的订阅、伤口与内部物品，不删除原版尸体。</summary>
        private void Cleanup()
        {
            if (cleaned) return;
            cleaned = true;
            operational = false;
            ready = false;
            UnregisterMap();
            wounds.ClearActiveRuntime(Pawn);
            BandwidthSource?.NotifyBindingChanged(Pawn, false);
            if (internalEquipment != null && Pawn.equipment?.Contains(internalEquipment) == true)
                Pawn.equipment.DestroyEquipment(internalEquipment);
            internalEquipment = null;
        }

        /// <summary>玩家可读的派生运行状态全部来自语言文件。</summary>
        public override string CompInspectStringExtra()
        {
            if (Pawn.Dead) return null;
            if (!ready) return "BDP_TrionUnit_Initializing".Translate();
            return IsOperational ? "BDP_TrionUnit_Running".Translate().ToString()
                : "BDP_TrionUnit_Stopped".Translate(operationFailure ?? string.Empty).ToString();
        }
    }
}
