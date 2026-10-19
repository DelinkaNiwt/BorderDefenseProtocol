using System.Collections.Generic;
using BDP.Core.Permissions;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Facility
{
    /// <summary>
    /// 聚合本地图玩家核心的运行许可。
    /// 建筑自行结算资源并推送变化；下游只读取聚合结果，不查询或结算建筑资源。
    /// </summary>
    public sealed class TrionFacilityMapComponent : MapComponent
    {
        /// <summary>Trion 中继核心发布的地图许可键。</summary>
        public const string TrionRelayCorePermissionKey = "BDP.TrionRelayCore.Active";

        /// <summary>当前已经发布运行许可的玩家核心；读档后重新建立，不保存进存档。</summary>
        private HashSet<Building_TrionFacility> activeFacilities = new HashSet<Building_TrionFacility>();

        /// <summary>当前地图的 Trion 上游许可提供器。</summary>
        private TrionRelayCorePermissionProvider permissionProvider;

        /// <summary>当前地图对象，供中性许可适配器读取所属地图。</summary>
        public Map Map
        {
            get { return map; }
        }

        /// <summary>用当前地图初始化许可聚合组件。</summary>
        public TrionFacilityMapComponent(Map map) : base(map)
        {
        }

        /// <summary>读取目标地图的许可聚合组件；不创建独立于地图生命周期的对象。</summary>
        public static TrionFacilityMapComponent GetOrCreate(Map map)
        {
            return map?.GetComponent<TrionFacilityMapComponent>();
        }

        /// <summary>建筑生成后登记当前许可，并确保许可提供器已接入地图。</summary>
        public void Register(Building_TrionFacility facility)
        {
            RegisterPermissionProvider();
            NotifyOperationalStateChanged(facility);
        }

        /// <summary>离图前撤销该核心的许可；其它运行核心仍继续提供许可。</summary>
        public void Unregister(Building_TrionFacility facility)
        {
            bool wasActive = HasActivePlayerTrionFacility();
            if (facility != null)
            {
                activeFacilities.Remove(facility);
            }

            NotifyPermissionIfChanged(wasActive);
        }

        /// <summary>
        /// 接收资源状态或派系变化，只更新发生变化的核心。
        /// 必须先更新集合再通知下游，避免控制台刷新光照时读到旧许可。
        /// </summary>
        public void NotifyOperationalStateChanged(Building_TrionFacility facility)
        {
            if (facility == null)
            {
                return;
            }

            bool wasActive = HasActivePlayerTrionFacility();
            if (IsActivePlayerFacility(facility))
            {
                activeFacilities.Add(facility);
            }
            else
            {
                activeFacilities.Remove(facility);
            }

            NotifyPermissionIfChanged(wasActive);
        }

        /// <summary>读取共享许可；不遍历建筑，不结算资源，多个核心不叠加恢复速率。</summary>
        public bool HasActivePlayerTrionFacility()
        {
            return activeFacilities.Count > 0;
        }

        /// <summary>地图初始化完成后按最终建筑状态重建许可，覆盖新地图及读档登记顺序。</summary>
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            RegisterPermissionProvider();
            activeFacilities.Clear();
            if (map?.listerBuildings != null)
            {
                foreach (Building_TrionFacility facility in
                    map.listerBuildings.AllBuildingsColonistOfClass<Building_TrionFacility>())
                {
                    // 人物引用已经解析；核心自身重建业务快照，地图只聚合最终许可。
                    facility.ResourceComp?.InitializeOperation();
                    if (IsActivePlayerFacility(facility))
                    {
                        activeFacilities.Add(facility);
                    }
                }
            }

            // 初始化时统一重发当前结果，让先于本组件完成初始化的控制台同步光照。
            NotifyPermissionStateChanged();
        }

        /// <summary>运行核心集合不存档；最终许可由地图初始化阶段重建。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                activeFacilities.Clear();
            }
        }

        /// <summary>判断单座核心是否能发布本地图玩家许可；只读状态，不推动资源时间。</summary>
        private bool IsActivePlayerFacility(Building_TrionFacility facility)
        {
            return facility != null
                && !facility.Destroyed
                && facility.Spawned
                && facility.Map == map
                && facility.Faction == Faction.OfPlayer
                && facility.ResourceComp != null
                && facility.ResourceComp.IsOperational;
        }

        /// <summary>将共享许可适配器登记到 Core 的中性地图许可索引。</summary>
        private void RegisterPermissionProvider()
        {
            if (permissionProvider == null)
            {
                permissionProvider = new TrionRelayCorePermissionProvider(this);
            }

            MapPermissionMapComponent permissionMap = map?.GetComponent<MapPermissionMapComponent>();
            permissionMap?.Register(permissionProvider);
            BdpDiagnostics.Once(
                "permission.trion_relay.registration." + (map != null ? map.uniqueID.ToString() : "null"),
                "Trion 中继核心许可提供器登记 | map="
                + (map != null ? map.uniqueID.ToString() : "null")
                + " | mapPermissionComponent=" + (permissionMap != null));
        }

        /// <summary>只有地图聚合许可翻转才通知下游，避免多核心重复开关消费层。</summary>
        private void NotifyPermissionIfChanged(bool wasActive)
        {
            if (wasActive != HasActivePlayerTrionFacility())
            {
                NotifyPermissionStateChanged();
            }
        }

        /// <summary>通知当前地图玩家派系的许可监听者重新读取共享结果。</summary>
        private void NotifyPermissionStateChanged()
        {
            map?.GetComponent<MapPermissionMapComponent>()?.NotifyPermissionStateChanged(
                TrionRelayCorePermissionKey,
                Faction.OfPlayer);
        }
    }
}
