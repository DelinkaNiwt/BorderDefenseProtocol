using System.Collections.Generic;
using BDP.Content.Trion.Units;
using BDP.Core.Permissions;
using RimWorld;
using Verse;

namespace BDP.Development.Trion.Units
{
    /// <summary>仅供开发样本的许可与带宽源，不模拟正式核心或机控中枢。</summary>
    public sealed class CompProperties_TrionUnitDebugSource : CompProperties
    {
        /// <summary>将测试定义绑定到可整体删除的开发组件。</summary>
        public CompProperties_TrionUnitDebugSource() { compClass = typeof(CompTrionUnitDebugSource); }
    }

    /// <summary>可移除的真实建筑归属样本，额度选择只用于验证接口。</summary>
    public sealed class CompTrionUnitDebugSource : ThingComp, ITrionUnitBandwidthSource, IMapPermissionProvider
    {
        /// <summary>独立测试许可，不能冒充正式中继核心影响其它业务。</summary>
        public const string DebugPermissionKey = "BDP.TrionUnit.DebugPermit";
        /// <summary>默认关闭，测试入口显式开启。</summary>
        private bool permit;
        /// <summary>测试额度，正式额度管理不在本组件实现。</summary>
        private int bandwidth = 2;
        /// <summary>实际绑定人物引用；资源余额不存入提供者。</summary>
        private List<Pawn> units = new List<Pawn>();
        /// <summary>保存当前登记地图，离图后仍可正确注销。</summary>
        private Map registeredMap;
        /// <summary>地图许可归属。</summary>
        public Map Map => parent.Map;
        /// <summary>只服务本派系归属。</summary>
        public Faction Faction => parent.Faction;
        /// <summary>测试许可身份。</summary>
        public string PermissionKey => DebugPermissionKey;
        /// <summary>测试开关真实控制许可有效性。</summary>
        public bool IsActive => permit && parent.Spawned && !parent.Destroyed;
        /// <summary>便于开发菜单切换测试开关。</summary>
        public bool PermitEnabled => permit;

        /// <summary>只接受同派系的专属样本，测试设施不向正式兵种提供前置资格。</summary>
        public AcceptanceReport CanBind(Pawn unit)
        {
            return unit?.Faction == Faction && TrionUnitDebugActions.IsTestSample(unit) ? AcceptanceReport.WasAccepted
                : new AcceptanceReport("BDP_Dev_TrionUnit_InvalidOwner".Translate());
        }

        /// <summary>依测试绑定顺序提供额度，用于重现缩水后的部分停机。</summary>
        public bool Covers(Pawn unit)
        {
            float used = 0f;
            foreach (Pawn linked in units)
            {
                if (linked == null || linked.Dead || linked.Destroyed
                    || linked.TryGetComp<CompTrionUnit>()?.ControlOwner != parent) continue;
                used += linked.GetStatValue(StatDefOf.BandwidthCost);
                if (linked == unit) return used <= bandwidth;
            }
            return false;
        }

        /// <summary>名单登记幂等，单位读档恢复不会重复占用测试额度。</summary>
        public void NotifyBindingChanged(Pawn unit, bool bound)
        {
            if (bound) { if (!units.Contains(unit)) units.Add(unit); }
            else units.Remove(unit);
        }

        /// <summary>明确修改测试许可并发布现成地图通知。</summary>
        public void SetPermit(bool value)
        {
            permit = value;
            NotifyConditions();
        }
        /// <summary>测试额度归零或恢复后，通知原有单位重读覆盖结果。</summary>
        public void SetBandwidth(int value)
        {
            bandwidth = System.Math.Max(0, value);
            NotifyConditions();
        }
        /// <summary>变化通知不复制人物运行状态。</summary>
        private void NotifyConditions()
        {
            registeredMap?.GetComponent<MapPermissionMapComponent>()?.NotifyPermissionStateChanged(PermissionKey, Faction);
            foreach (Pawn unit in new List<Pawn>(units)) unit?.TryGetComp<CompTrionUnit>()?.RefreshOperatingConditions();
        }
        /// <summary>生成后登记独立测试许可。</summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            registeredMap = Map;
            registeredMap?.GetComponent<MapPermissionMapComponent>()?.Register(this);
            NotifyConditions();
        }
        /// <summary>地图离场时撤销测试许可。</summary>
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            registeredMap?.GetComponent<MapPermissionMapComponent>()?.Unregister(this);
            NotifyConditions();
            registeredMap = null;
        }
        /// <summary>销毁来源使原有单位立即失去归属覆盖。</summary>
        public override void PostDestroy(DestroyMode mode, Map previousMap) { NotifyConditions(); }
        /// <summary>只保存开发开关及原版人物引用。</summary>
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref permit, "permit", false);
            Scribe_Values.Look(ref bandwidth, "bandwidth", 2);
            Scribe_Collections.Look(ref units, "units", LookMode.Reference);
            if (units == null) units = new List<Pawn>();
        }
    }
}
