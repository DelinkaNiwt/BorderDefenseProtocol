using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 追踪目标过滤器——决定追踪模块可以锁定哪些类型的目标。
    /// </summary>
    public enum TrackingTargetFilter
    {
        Pawn,
        Projectile,
        Building,
        PawnOrProjectile,
        Any
    }

    /// <summary>
    /// 追踪模块配置——挂在投射物ThingDef的modExtensions上。
    /// 存在即启用追踪行为，弹道每tick转向目标当前位置。
    /// </summary>
    public class BDPTrackingConfig : DefModExtension
    {
        /// <summary>每tick最大转向角度（度）。值越小弹道越平滑，越大越灵敏。</summary>
        public float maxTurnAnglePerTick = 5f;

        /// <summary>发射后延迟多少tick才开始追踪（模拟点火延迟）。</summary>
        public int trackingDelayTicks = 0;

        /// <summary>目标类型过滤器。</summary>
        public TrackingTargetFilter targetFilter = TrackingTargetFilter.Pawn;

        /// <summary>目标丢失时是否尝试搜索新目标。</summary>
        public bool retargetOnLost = false;

        /// <summary>搜索新目标的半径（仅retargetOnLost=true时生效）。</summary>
        public float retargetRadius = 8f;

        /// <summary>最大追踪tick数，0=无限制。超时后停止追踪，直线飞行。</summary>
        public int maxTrackingTicks = 0;
    }
}
