using UnityEngine;

namespace BDP.Trigger
{
    /// <summary>
    /// v5到达决策上下文——模块通过写入字段表达"到达后怎么办"的意图。
    /// 替代v4的ArrivalContext，增加Phase转换和下一目标点。
    /// </summary>
    public struct ArrivalContextV5
    {
        /// <summary>当前飞行阶段（只读）。</summary>
        public readonly FlightPhase CurrentPhase;

        /// <summary>是否继续飞行（true=不执行Impact）。</summary>
        public bool Continue;

        /// <summary>继续飞行时的下一目标点。</summary>
        public Vector3 NextDestination;

        /// <summary>请求Phase转换（null=不请求）。</summary>
        public FlightPhase? RequestPhaseChange;

        public ArrivalContextV5(FlightPhase phase)
        {
            CurrentPhase = phase;
            Continue = false;
            NextDestination = Vector3.zero;
            RequestPhaseChange = null;
        }
    }

    /// <summary>
    /// v5到达决策管线接口——ticksToImpact≤0时决定继续飞还是命中。
    /// 替代v4的IBDPArrivalHandler。
    /// 执行顺序：ImpactSomething第6阶段（ArrivalPolicy）。
    /// </summary>
    public interface IBDPArrivalPolicy
    {
        /// <summary>决定到达后行为。写入ctx.Continue=true表示继续飞行。</summary>
        void DecideArrival(Bullet_BDP host, ref ArrivalContextV5 ctx);
    }
}
