using BDP.Core.Permissions;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Content.OperatorConsole
{
    /// <summary>
    /// 需要玩家指派操作员的 BDP 操作员控制台建筑。
    /// </summary>
    public sealed class Building_OperatorConsole : Building, IThingGlower, IMapPermissionStateChangeListener
    {
        /// <summary>当前建筑对应的下游许可提供器。</summary>
        private OperatorConsolePermissionProvider permissionProvider;

        /// <summary>当前控制台运行状态组件。</summary>
        public CompOperatorConsole StateComp
        {
            get { return this.TryGetComp<CompOperatorConsole>(); }
        }

        /// <summary>防止许可通知触发光照更新时重入同一组件。</summary>
        private bool refreshingOperationalGlow;

        /// <summary>当前控制台是否处于对外许可运作状态。</summary>
        public bool IsOperational
        {
            get { return StateComp != null && StateComp.IsOperational; }
        }

        /// <summary>当前是否由上帝模式调试强制运作。</summary>
        public bool IsForcedOperational
        {
            get { return StateComp != null && StateComp.IsForcedOperational; }
        }

        /// <summary>当前 Pawn 是否满足地图、派系和操作能力等人员前提。</summary>
        public bool HasPawnOperationPrerequisites(Pawn pawn)
        {
            return pawn != null
                && Spawned
                && pawn.Map == Map
                && pawn.Faction == Faction
                && pawn.health?.capacities?.CapableOf(PawnCapacityDefOf.Manipulation) == true;
        }

        /// <summary>当前控制台是否满足通电与 Trion 中继核心许可前提；调试强制运作绕过两者。</summary>
        public bool HasOperationalPrerequisites
        {
            get
            {
                return IsForcedOperational
                    || (HasPower && HasTrionRelayCorePermission);
            }
        }

        /// <summary>当前控制台是否接入并开启原版电力。</summary>
        public bool HasPower
        {
            get { return StateComp != null && StateComp.HasPower; }
        }

        /// <summary>当前控制台是否收到所属派系的 Trion 中继核心许可。</summary>
        public bool HasTrionRelayCorePermission
        {
            get { return StateComp != null && StateComp.HasTrionRelayCorePermission; }
        }

        /// <summary>
        /// 返回操作员控制台当前是否应由原版光照组件点亮。
        /// 控制台只有完整满足自身运作条件时才提供局部光照。
        /// </summary>
        public bool ShouldBeLitNow()
        {
            return Spawned && IsOperational;
        }

        /// <summary>
        /// 通知原版光照组件重新读取当前运作状态。
        /// </summary>
        internal void RefreshOperationalGlow()
        {
            if (!Spawned || Map == null || refreshingOperationalGlow)
            {
                return;
            }

            refreshingOperationalGlow = true;
            try
            {
                this.TryGetComp<CompGlower>()?.UpdateLit(Map);
            }
            finally
            {
                refreshingOperationalGlow = false;
            }
        }

        /// <summary>建筑生成后登记下游地图许可提供器。</summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            permissionProvider = new OperatorConsolePermissionProvider(this);
            MapPermissionMapComponent permissionMap = map?.GetComponent<MapPermissionMapComponent>();
            permissionMap?.Register(permissionProvider);
            permissionMap?.RegisterListener(this);
            BdpDiagnostics.Once(
                "permission.operator_console.registration." + ThingID,
                "操作员控制台许可提供器登记 | thing=" + ThingID
                + " | mapPermissionComponent=" + (permissionMap != null));
        }

        /// <summary>建筑离图前移除下游地图许可提供器。</summary>
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map previousMap = Map;
            if (permissionProvider != null)
            {
                previousMap?.GetComponent<MapPermissionMapComponent>()?.Unregister(permissionProvider);
            }

            previousMap?.GetComponent<MapPermissionMapComponent>()?.UnregisterListener(this);

            permissionProvider = null;
            base.DeSpawn(mode);
        }

        /// <summary>判断指定 Pawn 是否可以接受控制台操作工作。</summary>
        /// <param name="pawn">准备操作控制台的 Pawn。</param>
        /// <returns>可以操作时为 true。</returns>
        public bool CanBeOperatedBy(Pawn pawn)
        {
            return HasPawnOperationPrerequisites(pawn)
                && HasOperationalPrerequisites;
        }

        /// <summary>刷新当前操作员的运行时心跳。</summary>
        public void NotifyOperatorHeartbeat(Pawn pawn)
        {
            StateComp?.NotifyOperatorHeartbeat(pawn);
        }

        /// <summary>清除当前操作员心跳。</summary>
        public void ClearOperator(Pawn pawn)
        {
            StateComp?.ClearOperator(pawn);
        }

        /// <summary>
        /// 接收上游地图许可状态变化；控制台只关心 Trion 许可并刷新自身光照。
        /// </summary>
        /// <param name="permissionKey">发生变化的许可键。</param>
        /// <param name="faction">发生变化的许可归属派系。</param>
        public void NotifyPermissionStateChanged(string permissionKey, Faction faction)
        {
            if (permissionKey == OperatorConsolePermissionKeys.TrionRelayCoreActive
                && faction == Faction)
            {
                RefreshOperationalGlow();
            }
        }

    }
}
