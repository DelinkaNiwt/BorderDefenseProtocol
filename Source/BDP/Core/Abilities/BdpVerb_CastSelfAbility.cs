using RimWorld;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// 通用自施放 Ability Verb。
    ///
    /// 保留原版 Ability 的前摇、Stance、JobDriver、冷却和效果生命周期，
    /// 但不建立远程射击线，也不把自施放表现当成攻击动作处理。
    /// </summary>
    public class BdpVerb_CastSelfAbility : BdpVerb_CastAbility
    {
        /// <summary>
        /// 自施放能力只接受施法者自身作为目标，再复用原版 Ability 效果校验。
        /// </summary>
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!IsSelfTarget(target))
            {
                return false;
            }

            return base.ValidateTarget(target, showMessages);
        }

        /// <summary>
        /// 自施放能力不参与远程射程和视线判定，只允许命中自身。
        /// </summary>
        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            return IsSelfTarget(targ);
        }

        /// <summary>
        /// Stance_Warmup 会持续复核目标；这里保持同一条自施放边界，
        /// 防止前摇期间重新落入原版远程射击线判定。
        /// </summary>
        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            return IsSelfTarget(targ);
        }

        /// <summary>
        /// 启动自身能力的前摇，但跳过原版 Verb.TryStartCastOn 中的
        /// TryFindShootLineFromTo 和 Notify_WarmingCastAlongLine。
        /// </summary>
        public override bool TryStartCastOn(
            LocalTargetInfo castTarg,
            LocalTargetInfo destTarg,
            bool surpriseAttack = false,
            bool canHitNonTargetPawns = true,
            bool preventFriendlyFire = false,
            bool nonInterruptingSelfCast = false)
        {
            if (caster == null)
            {
                Log.Error("Verb " + GetUniqueLoadID() + " needs caster to work (possibly lost during saving/loading).");
                return false;
            }

            if (!caster.Spawned || state == VerbState.Bursting || !CanHitTarget(castTarg))
            {
                return false;
            }

            this.surpriseAttack = surpriseAttack;
            canHitNonTargetPawnsNow = canHitNonTargetPawns;
            this.preventFriendlyFire = preventFriendlyFire;
            this.nonInterruptingSelfCast = nonInterruptingSelfCast;
            currentTarget = castTarg;
            currentDestination = destTarg;

            if (CasterIsPawn && WarmupTime > 0f)
            {
                float statValue = CasterPawn.GetStatValue(StatDefOf.AimingDelayFactor);
                int ticks = (WarmupTime * statValue).SecondsToTicks();
                CasterPawn.stances.SetStance(new BdpStance_SelfCastWarmup(ticks, castTarg, this));

                if (verbProps.stunTargetOnCastStart && castTarg.Pawn != null)
                {
                    castTarg.Pawn.stances.stunner.StunFor(ticks, null, addBattleLog: false);
                }
            }
            else
            {
                if (verbTracker.directOwner is Ability ability)
                {
                    ability.lastCastTick = Find.TickManager.TicksGame;
                }

                WarmupComplete();
            }

            return true;
        }

        /// <summary>
        /// 自施放状态能力不是攻击动作，不添加身体后坐力抖动。
        /// </summary>
        protected override void TriggerCastJitter()
        {
        }

        /// <summary>
        /// 判断目标是否为当前能力的施法者自身。
        /// </summary>
        private bool IsSelfTarget(LocalTargetInfo target)
        {
            return CasterPawn != null
                && target.HasThing
                && target.Thing == CasterPawn
                && targetParams != null
                && targetParams.canTargetSelf;
        }
    }
}
