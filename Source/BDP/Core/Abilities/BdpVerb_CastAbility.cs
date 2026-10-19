using HarmonyLib;
using BDP.Core.AttackExecution;
using RimWorld;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// BDP 的正式 Ability Verb。
    /// 它只补一个能力侧当前确实需要的能力：
    /// - 在真正触发原版 Ability 效果前，先提交 BDP 的 Trion 施法成本
    /// - 触发施放瞬间的身体抖动动效
    /// </summary>
    public class BdpVerb_CastAbility : Verb_CastAbility, IBdpExpressionAbilityVerb
    {
        /// <summary>能力可用性原生查询中性限制，具体分类由业务规则决定。</summary>
        public override bool Available()
        {
            return AttackAdmissionAccess.Evaluate(CasterPawn, this).Accepted && base.Available();
        }

        /// <summary>预热和效果执行前检查准入。</summary>
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack = false, bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            if (!AttackAdmissionAccess.Evaluate(CasterPawn, this).Accepted) return false;
            return TryStartAdmittedCast(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns,
                preventFriendlyFire, nonInterruptingSelfCast);
        }

        /// <summary>已经在自有准备前通过准入的派生能力继续原版起手，避免再次查询同一规则。</summary>
        protected bool TryStartAdmittedCast(LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack, bool canHitNonTargetPawns, bool preventFriendlyFire, bool nonInterruptingSelfCast)
        {
            return base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns,
                preventFriendlyFire, nonInterruptingSelfCast);
        }

        /// <summary>
        /// 先正式提交施法成本，成功后触发施放动效，再继续原版 Ability 激活流程。
        /// </summary>
        protected override bool TryCastShot()
        {
            if (!TryCommitTrionCosts())
            {
                return false;
            }

            TriggerCastJitter();
            return base.TryCastShot();
        }

        /// <summary>
        /// 触发施放瞬间的身体抖动动效。
        /// 力度 0.5（与原版近战相同），方向朝施法目标，享原版 JitterHandler 自动衰减。
        /// </summary>
        protected virtual void TriggerCastJitter()
        {
            Pawn casterPawn = CasterPawn;
            if (casterPawn == null)
            {
                return;
            }

            float castDirection =
                (CurrentTarget.Cell - casterPawn.Position).AngleFlat;
            JitterHandler jitterer = Traverse.Create(casterPawn.Drawer)
                .Field("jitterer")
                .GetValue<JitterHandler>();
            jitterer?.AddOffset(0.5f, castDirection);
        }

        /// <summary>
        /// 遍历当前 Ability 的效果组件，把所有 BDP Trion 成本先扣掉。
        /// 现在只会命中很薄的 Trion 成本组件，不引入额外平台。
        /// </summary>
        protected virtual bool TryCommitTrionCosts()
        {
            return BdpAbilityTrionCostCommitter.TryCommit(ability);
        }
    }
}
