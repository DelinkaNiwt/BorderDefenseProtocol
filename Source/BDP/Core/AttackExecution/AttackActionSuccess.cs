using RimWorld;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 一次已经通过原版执行入口的攻击动作通知。
    /// 这不是命中通知；近战未命中或被闪避，只要攻击动作已经完成，IsHit 会为 false。
    /// </summary>
    public sealed class AttackActionSuccess
    {
        /// <summary>
        /// 攻击来源类别。
        /// </summary>
        public AttackActionSourceKind SourceKind { get; private set; }

        /// <summary>
        /// 发起攻击的 Pawn（小人）。
        /// </summary>
        public Pawn Pawn { get; private set; }

        /// <summary>
        /// 武器或能力对应的 Verb（动作动词）。
        /// </summary>
        public Verb Verb { get; private set; }

        /// <summary>
        /// 能力攻击对应的 Ability（能力）。
        /// </summary>
        public Ability Ability { get; private set; }

        /// <summary>
        /// 本次动作是否实际命中；不影响“动作已完成”的通知语义。
        /// </summary>
        public bool IsHit { get; private set; }

        /// <summary>
        /// 构造一次攻击动作完成通知。
        /// </summary>
        private AttackActionSuccess(
            AttackActionSourceKind sourceKind,
            Pawn pawn,
            Verb verb,
            Ability ability,
            bool isHit)
        {
            SourceKind = sourceKind;
            Pawn = pawn;
            Verb = verb;
            Ability = ability;
            IsHit = isHit;
        }

        /// <summary>
        /// 从武器 Verb 构造攻击动作通知。
        /// </summary>
        public static AttackActionSuccess FromWeapon(Verb verb, bool isHit)
        {
            return new AttackActionSuccess(
                AttackActionSourceKind.Weapon,
                verb != null ? verb.CasterPawn : null,
                verb,
                null,
                isHit);
        }

        /// <summary>
        /// 从 Ability.Activate（能力激活）构造能力攻击通知。
        /// </summary>
        public static AttackActionSuccess FromAbility(Ability ability, Verb verb)
        {
            return new AttackActionSuccess(
                AttackActionSourceKind.Ability,
                verb != null ? verb.CasterPawn : null,
                verb,
                ability,
                true);
        }
    }

    /// <summary>
    /// 攻击动作来源类别。
    /// </summary>
    public enum AttackActionSourceKind
    {
        /// <summary>
        /// 原版武器或 BDP 武器 Verb。
        /// </summary>
        Weapon,

        /// <summary>
        /// 原版 Ability 或 BDP Ability。
        /// </summary>
        Ability
    }
}
