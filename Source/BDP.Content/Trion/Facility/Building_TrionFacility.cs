using RimWorld;
using BDP.Content.Trion.Facility.Sacrifice;
using Verse;

namespace BDP.Content.Trion.Facility
{
    /// <summary>
    /// Trion 地标设施建筑。
    /// 建筑类只暴露组件入口，不直接持有献祭、心情或 Pawn 恢复规则。
    /// </summary>
    public sealed class Building_TrionFacility : Building, IThingGlower
    {
        /// <summary>
        /// 当前建筑的独立资源容器。
        /// </summary>
        public CompTrionFacilityResource ResourceComp
        {
            get { return this.TryGetComp<CompTrionFacilityResource>(); }
        }

        /// <summary>本核心的皇室归属与正式操作权限。</summary>
        public CompTrionFacilityControl ControlComp
        {
            get { return this.TryGetComp<CompTrionFacilityControl>(); }
        }

        /// <summary>临时供物容器与协作搬运的准备状态。</summary>
        public CompTrionFacilitySacrifice SacrificeComp => this.TryGetComp<CompTrionFacilitySacrifice>();

        /// <summary>
        /// 当前建筑的升级状态组件。
        /// </summary>
        public CompTrionFacilityUpgrade UpgradeComp
        {
            get { return this.TryGetComp<CompTrionFacilityUpgrade>(); }
        }

        /// <summary>
        /// 返回 Trion 中继核心当前是否应由原版光照组件点亮。
        /// 原版 CompGlower 会在自身刷新时调用该接口，不新增自定义绘制。
        /// </summary>
        public bool ShouldBeLitNow()
        {
            return Spawned && ResourceComp != null && ResourceComp.IsOperational;
        }

        /// <summary>
        /// 通知原版光照组件重新读取当前运作状态。
        /// </summary>
        internal void RefreshOperationalGlow()
        {
            if (!Spawned || Map == null)
            {
                return;
            }

            this.TryGetComp<CompGlower>()?.UpdateLit(Map);
        }

        /// <summary>向地图层推送本核心的运作状态；由地图层决定聚合许可是否翻转。</summary>
        internal void NotifyOperationalStateChanged()
        {
            TrionFacilityMapComponent.GetOrCreate(Map)?.NotifyOperationalStateChanged(this);
        }

        /// <summary>
        /// 建筑生成后登记到所属地图的 Trion 设施索引。
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            // 读档须等人物引用解析完成，在地图 FinalizeInit 中重建；新建则立即初始化。
            if (!respawningAfterLoad)
                ResourceComp?.InitializeOperation();
            TrionFacilityMapComponent.GetOrCreate(map)?.Register(this);
        }

        /// <summary>
        /// 建筑离图前移除地图索引，避免停用设施继续提供恢复许可。
        /// </summary>
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map previousMap = Map;
            TrionFacilityMapComponent.GetOrCreate(previousMap)?.Unregister(this);
            base.DeSpawn(mode);
        }

        /// <summary>派系变更完成后刷新许可，覆盖运行核心被夺取或重新归属玩家的情况。</summary>
        public override void SetFaction(Faction newFaction, Pawn recruiter = null)
        {
            base.SetFaction(newFaction, recruiter);
            if (Spawned)
            {
                NotifyOperationalStateChanged();
            }
        }
    }
}
