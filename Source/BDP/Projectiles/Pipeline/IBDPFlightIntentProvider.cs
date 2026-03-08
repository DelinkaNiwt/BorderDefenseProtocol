using UnityEngine;
using Verse;

namespace BDP.Projectiles.Pipeline
{
    /// <summary>
    /// 飞行意图——模块产出的"下一步飞向哪"的意图数据。
    /// </summary>
    public struct FlightIntent
    {
        /// <summary>目标位置。</summary>
        public Vector3 TargetPosition;

        /// <summary>是否激活了追踪（用于宿主判断Phase转换）。</summary>
        public bool TrackingActivated;

        /// <summary>
        /// 精确位置模式——宿主跳过远距离策略，直接用TargetPosition作为destination。
        /// 用于贝塞尔追踪等需要逐帧精确控制位置的场景。
        /// </summary>
        public bool ExactPosition;
    }

    /// <summary>
    /// 飞行意图上下文——模块通过写入Intent表达飞行方向意图。
    /// 注入三层目标供模块读取，模块通过NewCurrentTarget/NewLockedTarget请求修改目标。
    /// </summary>
    public struct FlightIntentContext
    {
        /// <summary>子弹当前位置（只读）。</summary>
        public readonly Vector3 CurrentPosition;

        /// <summary>当前弹道目标点（vanilla destination坐标，只读）。</summary>
        public readonly Vector3 CurrentDestination;

        /// <summary>瞄准目标——发射时锁定，不变（只读）。</summary>
        public readonly LocalTargetInfo AimTarget;

        /// <summary>锁定目标——通常=AimTarget，仅"重定向"机制可改（只读）。</summary>
        public readonly LocalTargetInfo LockedTarget;

        /// <summary>当前目标——此刻飞向谁（只读）。引导段=锚点坐标，引导结束/纯追踪=目标实体。</summary>
        public readonly LocalTargetInfo CurrentTarget;

        /// <summary>飞行意图输出（null=无意图，不修改飞行方向）。</summary>
        public FlightIntent? Intent;

        /// <summary>请求修改CurrentTarget（引导模块用：设锚点/回归LockedTarget）。</summary>
        public LocalTargetInfo? NewCurrentTarget;

        /// <summary>请求修改LockedTarget（追踪重定向用：切换锁定目标）。</summary>
        public LocalTargetInfo? NewLockedTarget;

        public FlightIntentContext(
            Vector3 pos, Vector3 dest,
            LocalTargetInfo aimTarget,
            LocalTargetInfo lockedTarget,
            LocalTargetInfo currentTarget)
        {
            CurrentPosition = pos;
            CurrentDestination = dest;
            AimTarget = aimTarget;
            LockedTarget = lockedTarget;
            CurrentTarget = currentTarget;
            Intent = null;
            NewCurrentTarget = null;
            NewLockedTarget = null;
        }
    }

    /// <summary>
    /// 飞行意图提供者管线接口——每tick产出飞行方向意图。
    /// 执行顺序：管线第2阶段（FlightIntent）。
    /// 宿主取第一个非null Intent执行ApplyFlightRedirect。
    /// </summary>
    public interface IBDPFlightIntentProvider
    {
        /// <summary>产出飞行意图。写入ctx.Intent表达方向变更。</summary>
        void ProvideIntent(Bullet_BDP host, ref FlightIntentContext ctx);
    }
}
