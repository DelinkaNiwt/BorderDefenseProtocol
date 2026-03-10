using Verse;

namespace BDP.Projectiles.Pipeline
{
    /// <summary>
    /// 命中解析上下文——模块通过写入字段修正命中判定结果。
    /// </summary>
    public struct HitContext
    {
        /// <summary>vanilla命中判定的目标（只读）。</summary>
        public readonly LocalTargetInfo VanillaHitThing;

        /// <summary>锁定目标——供命中修正参考（只读）。</summary>
        public readonly LocalTargetInfo LockedTarget;

        /// <summary>强制打地面（true=忽略usedTarget，Impact(null)）。</summary>
        public bool ForceGround;

        /// <summary>覆盖命中目标（非default时替换usedTarget）。</summary>
        public LocalTargetInfo OverrideTarget;

        public HitContext(LocalTargetInfo vanillaHit, LocalTargetInfo lockedTarget)
        {
            VanillaHitThing = vanillaHit;
            LockedTarget = lockedTarget;
            ForceGround = false;
            OverrideTarget = default;
        }
    }

    /// <summary>
    /// 命中解析管线接口——在vanilla命中判定后、Impact执行前修正结果。
    /// 执行顺序：ImpactSomething第7阶段（HitResolve）。
    /// </summary>
    public interface IBDPHitResolver
    {
        /// <summary>解析命中结果。可通过ctx.ForceGround或ctx.OverrideTarget修正。</summary>
        void ResolveHit(Bullet_BDP host, ref HitContext ctx);
    }
}
