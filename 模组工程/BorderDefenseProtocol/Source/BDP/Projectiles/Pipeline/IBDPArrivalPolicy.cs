using UnityEngine;
using Verse;

namespace BDP.Projectiles.Pipeline
{
    /// <summary>
    /// 到达决策上下文：模块通过写入字段表达”到达后如何处理”的意图。
    /// 注入三层目标供模块读取，模块通过NewCurrentTarget请求修改目标。
    /// </summary>
    public struct ArrivalContext
    {
        /// <summary>瞄准目标——发射时锁定，不变（只读）。</summary>
        public readonly LocalTargetInfo AimTarget;

        /// <summary>锁定目标——通常=AimTarget，仅”重定向”机制可改（只读）。</summary>
        public readonly LocalTargetInfo LockedTarget;

        /// <summary>当前目标——此刻飞向谁（只读）。</summary>
        public readonly LocalTargetInfo CurrentTarget;

        /// <summary>是否继续飞行（true=不执行Impact）。</summary>
        public bool Continue;

        /// <summary>继续飞行时的下一目标点。</summary>
        public Vector3 NextDestination;

        /// <summary>请求修改CurrentTarget（引导模块最后一段：回归LockedTarget）。</summary>
        public LocalTargetInfo? NewCurrentTarget;

        public ArrivalContext(
            LocalTargetInfo aimTarget,
            LocalTargetInfo lockedTarget,
            LocalTargetInfo currentTarget)
        {
            AimTarget = aimTarget;
            LockedTarget = lockedTarget;
            CurrentTarget = currentTarget;
            Continue = false;
            NextDestination = Vector3.zero;
            NewCurrentTarget = null;
        }
    }

    /// <summary>
    /// 到达决策接口：ticksToImpact<=1时决定继续飞行还是命中。
    /// 执行顺序：ImpactSomething第1阶段（ArrivalPolicy）。
    /// </summary>
    public interface IBDPArrivalPolicy
    {
        /// <summary>决定到达行为。写入ctx.Continue=true表示继续飞行。</summary>
        void DecideArrival(Bullet_BDP host, ref ArrivalContext ctx);
    }
}
