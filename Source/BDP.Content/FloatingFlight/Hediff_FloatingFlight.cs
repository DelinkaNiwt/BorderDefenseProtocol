using Verse;
using BDP.Core.Permissions;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 浮空实际状态 Hediff。
    /// 是否实际飞行只由该 Hediff 是否存在决定；起降动画不在这里保存第二份状态。
    /// </summary>
    public sealed class Hediff_FloatingFlight : Hediff, IPawnPermissionProvider
    {
        /// <summary>发布自身许可的 Pawn。</summary>
        public Pawn Pawn
        {
            get { return pawn; }
        }

        /// <summary>浮空自身许可的稳定键。</summary>
        public string PermissionKey
        {
            get { return FloatingFlightPermissionKeys.SelfActive; }
        }

        /// <summary>
        /// 只有当前实例仍是 Pawn 的实际浮空 Hediff 时才发布许可。
        /// </summary>
        public bool IsActive
        {
            get
            {
                return pawn != null
                    && pawn.Spawned
                    && pawn.health?.hediffSet?.GetFirstHediffOfDef(def) == this;
            }
        }

        /// <summary>Hediff 被外部添加后通知浮空状态控制器。</summary>
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            RegisterSelfPermission();
            FloatingFlightStateController.NotifyHediffAdded(pawn);
        }

        /// <summary>Hediff 被外部移除后通知浮空状态控制器。</summary>
        public override void PostRemoved()
        {
            UnregisterSelfPermission();
            base.PostRemoved();
            FloatingFlightStateController.NotifyHediffRemoved(pawn);
        }

        /// <summary>
        /// 读档或跨地图后重新登记自身许可；重复登记由地图索引去重。
        /// </summary>
        internal void EnsureSelfPermissionRegistration()
        {
            RegisterSelfPermission();
        }

        /// <summary>将当前 Hediff 登记到所属地图的 Pawn 自身许可索引。</summary>
        private void RegisterSelfPermission()
        {
            pawn?.Map?.GetComponent<MapPermissionMapComponent>()?.RegisterPawnProvider(this);
        }

        /// <summary>从所属地图的 Pawn 自身许可索引移除当前 Hediff。</summary>
        private void UnregisterSelfPermission()
        {
            pawn?.Map?.GetComponent<MapPermissionMapComponent>()?.UnregisterPawnProvider(this);
        }
    }
}
