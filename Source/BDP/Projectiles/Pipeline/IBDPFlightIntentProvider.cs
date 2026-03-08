using UnityEngine;

namespace BDP.Trigger
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
    /// </summary>
    public struct FlightIntentContext
    {
        /// <summary>子弹当前位置（只读）。</summary>
        public readonly Vector3 CurrentPosition;

        /// <summary>当前弹道目标点（只读）。</summary>
        public readonly Vector3 CurrentDestination;

        /// <summary>当前飞行阶段（只读）。</summary>
        public readonly FlightPhase CurrentPhase;

        /// <summary>飞行意图输出（null=无意图，不修改飞行方向）。</summary>
        public FlightIntent? Intent;

        /// <summary>请求Phase转换（null=不请求）。与Intent独立。</summary>
        public FlightPhase? RequestPhaseChange;

        public FlightIntentContext(Vector3 pos, Vector3 dest, FlightPhase phase)
        {
            CurrentPosition = pos;
            CurrentDestination = dest;
            CurrentPhase = phase;
            Intent = null;
            RequestPhaseChange = null;
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
