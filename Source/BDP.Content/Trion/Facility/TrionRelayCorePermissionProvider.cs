using BDP.Core.Permissions;
using BDP.Support.Diagnostics;
using RimWorld;
using Verse;

namespace BDP.Content.Trion.Facility
{
    /// <summary>
    /// 将地图层维护的 Trion 中继核心共享状态适配为地图许可提供器。
    /// 不复制资源余额或消耗规则；读取许可不触发建筑资源结算。
    /// </summary>
    public sealed class TrionRelayCorePermissionProvider : IMapPermissionProvider
    {
        /// <summary>供本模组业务共用的核心运行许可身份。</summary>
        public const string ActivePermissionKey = "BDP.TrionRelayCore.Active";

        /// <summary>当前地图的 Trion 设施索引。</summary>
        private readonly TrionFacilityMapComponent facilityMapComponent;

        /// <summary>使用当前地图设施索引建立上游许可提供器。</summary>
        /// <param name="component">Trion 设施地图组件。</param>
        public TrionRelayCorePermissionProvider(TrionFacilityMapComponent component)
        {
            facilityMapComponent = component;
        }

        /// <summary>提供器所属地图。</summary>
        public Map Map
        {
            get { return facilityMapComponent != null ? facilityMapComponent.Map : null; }
        }

        /// <summary>上游许可归属玩家派系。</summary>
        public Faction Faction
        {
            get { return Faction.OfPlayer; }
        }

        /// <summary>Trion 中继核心活动许可键。</summary>
        public string PermissionKey
        {
            get { return ActivePermissionKey; }
        }

        /// <summary>只读地图聚合结果，作为许可有效性。</summary>
        public bool IsActive
        {
            get
            {
                bool active = facilityMapComponent != null
                    && facilityMapComponent.HasActivePlayerTrionFacility();
                BdpDiagnostics.Throttled(
                    "permission.trion_relay.active." + (Map != null ? Map.uniqueID.ToString() : "null"),
                    "Trion 中继核心许可状态 | map=" + (Map != null ? Map.uniqueID.ToString() : "null")
                    + " | active=" + active,
                    60);
                return active;
            }
        }
    }
}
