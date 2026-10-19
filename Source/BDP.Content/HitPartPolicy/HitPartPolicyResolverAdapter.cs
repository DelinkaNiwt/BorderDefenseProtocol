using BDP.Core.AttackExecution;
using RimWorld;
using Verse;

namespace BDP.Content.HitPartPolicy
{
    /// <summary>
    /// 把 Content 命中部位解析器接入 Core 中性注册桥。
    /// </summary>
    public sealed class HitPartPolicyResolverAdapter : IHitPartPolicyResolver
    {
        /// <summary>
        /// 委托给统一 Content 解析器。
        /// </summary>
        public bool TryResolve(
            Pawn attacker,
            Thing target,
            DamageDef damageDef,
            BodyPartGroupDef bodyPartGroup,
            HitPartPolicyConfig policy,
            out BodyPartRecord hitPart)
        {
            return HitPartPolicyResolver.TryResolve(
                attacker,
                target,
                damageDef,
                bodyPartGroup,
                policy,
                out hitPart);
        }
    }
}
