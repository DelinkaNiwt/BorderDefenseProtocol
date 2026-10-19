using RimWorld;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 业务侧访问命中部位策略注册表的窄入口。
    /// </summary>
    public static class HitPartPolicyResolverAccess
    {
        /// <summary>
        /// 尝试解析命中部位；任何未配置情况都保持原版空部位语义。
        /// </summary>
        public static bool TryResolve(
            Pawn attacker,
            Thing target,
            DamageDef damageDef,
            BodyPartGroupDef bodyPartGroup,
            HitPartPolicyConfig policy,
            out BodyPartRecord hitPart)
        {
            return HitPartPolicyResolverRegistry.TryResolve(
                attacker,
                target,
                damageDef,
                bodyPartGroup,
                policy,
                out hitPart);
        }
    }
}
