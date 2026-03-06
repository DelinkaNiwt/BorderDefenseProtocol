using UnityEngine;

namespace BDP.Trigger
{
    /// <summary>
    /// Arrival decision context for v5 projectile pipeline.
    /// 到达决策上下文：模块通过写入字段表达“到达后如何处理”的意图。
    /// </summary>
    public struct ArrivalContextV5
    {
        /// <summary>Current flight phase (read-only). 当前飞行阶段（只读）。</summary>
        public readonly FlightPhase CurrentPhase;

        /// <summary>Continue flight or resolve impact. 是否继续飞行（true=不执行Impact）。</summary>
        public bool Continue;

        /// <summary>Next destination when continuing flight. 继续飞行时的下一目标点。</summary>
        public Vector3 NextDestination;

        /// <summary>Requested phase transition (null = no request). 请求Phase转换（null=不请求）。</summary>
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
    /// Arrival decision stage in v5 pipeline.
    /// 到达决策接口：ticksToImpact<=1时决定继续飞行还是命中。
    /// 执行顺序：ImpactSomething第1阶段（ArrivalPolicy）。
    /// </summary>
    public interface IBDPArrivalPolicy
    {
        /// <summary>Decide arrival behavior. 写入ctx.Continue=true表示继续飞行。</summary>
        void DecideArrival(Bullet_BDP host, ref ArrivalContextV5 ctx);
    }
}
