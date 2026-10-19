using RimWorld;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 命中部位策略的业务解析器边界。
    /// Core 只负责调用，不解释 PolicyKey。
    /// </summary>
    public interface IHitPartPolicyResolver
    {
        /// <summary>
        /// 按策略尝试给一次伤害选择明确的命中部位。
        /// 返回 false 或 null 表示继续使用原版随机命中。
        /// </summary>
        bool TryResolve(
            Pawn attacker,
            Thing target,
            DamageDef damageDef,
            BodyPartGroupDef bodyPartGroup,
            HitPartPolicyConfig policy,
            out BodyPartRecord hitPart);
    }
}
