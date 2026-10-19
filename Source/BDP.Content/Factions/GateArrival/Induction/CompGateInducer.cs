using System;
using System.Collections.Generic;
using BDP.Core.Permissions;
using BDP.Content.Trion.Facility;
using RimWorld;
using Verse;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>仅改变诱导对象；停用功能统一使用原版建筑开关。</summary>
    public enum GateInductionMode
    {
        /// <summary>仅诱导明确标记为敌方袭击的门。</summary>
        HostileRaids,
        /// <summary>诱导所有由BDP门系统管理的门，包括任务和友方出场。</summary>
        AllGates
    }

    /// <summary>建筑定义上的诱导参数，不向Core加入门业务。</summary>
    public sealed class CompProperties_GateInducer : CompProperties
    {
        /// <summary>门中心与装置中心之间允许的最大距离。</summary>
        public float radius = 10f;
        /// <summary>每扇门在初始规划时独立诱导的成功率；失败不重试、不转交其它装置。</summary>
        public float successChance = 0.9f;
        /// <summary>与其他外部落点提案共存时的诱导优先级。</summary>
        public int redirectPriority = 100;

        /// <summary>绑定正式业务组件。</summary>
        public CompProperties_GateInducer() { compClass = typeof(CompGateInducer); }
    }

    /// <summary>门诱导装置状态；初始门位规划时读取运行条件，不需要每刻扫描地图。</summary>
    public sealed class CompGateInducer : ThingComp
    {
        /// <summary>默认仅敌方袭击；读档保留玩家选择。</summary>
        private GateInductionMode mode;
        /// <summary>可由Development调试入口设置的运行覆盖；不伪造电力或上游许可。</summary>
        private bool debugForcedOperation;
        /// <summary>可选的本建筑按钮扩展；Development移除后正式建筑仍可独立工作。</summary>
        public static event Action<CompGateInducer, List<Gizmo>> GatheringGizmos;
        /// <summary>当前诱导对象模式。</summary>
        public GateInductionMode Mode { get { return mode; } }
        /// <summary>当前是否启用调试覆盖。</summary>
        public bool DebugForcedOperation { get { return debugForcedOperation; } }
        /// <summary>从定义读取半径，防止非数值和负数造成无限或反向范围。</summary>
        public float Radius
        {
            get
            {
                float radius = ((CompProperties_GateInducer)props).radius;
                return float.IsNaN(radius) || float.IsInfinity(radius) ? 10f : Math.Max(0f, radius);
            }
        }
        /// <summary>本组件的外部提案优先级。</summary>
        public int RedirectPriority { get { return ((CompProperties_GateInducer)props).redirectPriority; } }
        /// <summary>诱导概率与强制运作覆盖独立，非法配置回到90%，合法数值限定0～1。</summary>
        public float SuccessChance
        {
            get
            {
                float chance = ((CompProperties_GateInducer)props).successChance;
                return float.IsNaN(chance) || float.IsInfinity(chance) ? 0.9f : Math.Max(0f, Math.Min(1f, chance));
            }
        }
        /// <summary>正式运行条件；调试覆盖仍遵守建筑手动开关，确保停运即可停止诱导。</summary>
        public bool IsOperational
        {
            get
            {
                if (parent == null || !parent.Spawned || parent.Destroyed
                    || parent.TryGetComp<CompFlickable>()?.SwitchIsOn == false) return false;
                if (debugForcedOperation) return true;
                return parent.TryGetComp<CompBreakdownable>()?.BrokenDown != true
                    && parent.TryGetComp<CompPowerTrader>()?.PowerOn == true
                    && MapPermissionAccess.HasPermission(parent.Map, parent.Faction,
                        TrionRelayCorePermissionProvider.ActivePermissionKey);
            }
        }

        /// <summary>只读取发起方保存的来源标记，不从暂存人物的状态猜测。</summary>
        public bool Accepts(Thing_GateArrival wave)
        { return wave != null && (mode == GateInductionMode.AllGates || wave.IsHostileRaid); }

        /// <summary>调试入口只改变本建筑覆盖状态，模式与手动开关保持独立。</summary>
        public void SetDebugForcedOperation(bool enabled) { debugForcedOperation = enabled; }

        /// <summary>两个模式直接切换；没有额外关闭模式。</summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
            if (parent.Faction == Faction.OfPlayer)
                yield return new Command_Action
                {
                    defaultLabel = "BDP_GateInducer_ModeCommand".Translate(ModeLabel()),
                    defaultDesc = "BDP_GateInducer_ModeDescription".Translate(),
                    action = () => mode = mode == GateInductionMode.HostileRaids
                        ? GateInductionMode.AllGates : GateInductionMode.HostileRaids
                };
            if (GatheringGizmos == null) yield break;
            var extra = new List<Gizmo>();
            GatheringGizmos(this, extra);
            foreach (Gizmo gizmo in extra) yield return gizmo;
        }

        /// <summary>显示运行状态及诱导对象，所有玩家文案均来自语言包。</summary>
        public override string CompInspectStringExtra()
        {
            string status = IsOperational
                ? (debugForcedOperation ? "BDP_GateInducer_ForcedRunning" : "BDP_GateInducer_Running")
                : "BDP_GateInducer_Stopped";
            return status.Translate() + "\n" + "BDP_GateInducer_ModeCommand".Translate(ModeLabel())
                + "\n" + "BDP_GateInducer_SuccessChance".Translate(SuccessChance.ToString("P0"));
        }

        /// <summary>模式名称保持在本业务语言包中。</summary>
        private string ModeLabel()
        { return (mode == GateInductionMode.AllGates ? "BDP_GateInducer_AllGates" : "BDP_GateInducer_HostileRaids").Translate(); }

        /// <summary>保存选择和调试覆盖，不依赖Development承担正式存档字段。</summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref mode, "gateInductionMode", GateInductionMode.HostileRaids);
            Scribe_Values.Look(ref debugForcedOperation, "gateInducerDebugForcedOperation");
        }
    }
}
