using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 延迟伤害量解析上下文。
    /// Core（核心基础设施）只提供已经成立的承伤目标和命中部件事实。
    /// </summary>
    public readonly struct DamageAmountResolutionContext
    {
        /// <summary>
        /// 创建一份延迟伤害量解析上下文。
        /// </summary>
        internal DamageAmountResolutionContext(Thing targetThing, BodyPartRecord hitPart)
        {
            TargetThing = targetThing;
            HitPart = hitPart;
        }

        /// <summary>
        /// 本次伤害最终作用的目标 Thing（承伤对象）。
        /// </summary>
        public Thing TargetThing { get; }

        /// <summary>
        /// 本次伤害最终解析出的身体部件。
        /// </summary>
        public BodyPartRecord HitPart { get; }
    }
}
