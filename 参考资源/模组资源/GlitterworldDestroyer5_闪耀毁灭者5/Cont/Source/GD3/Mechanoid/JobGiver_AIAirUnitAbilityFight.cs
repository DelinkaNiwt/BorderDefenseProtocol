using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace GD3
{
	public class JobGiver_AIAirUnitAbilityFight : JobGiver_AIFightEnemy
	{
		private AbilityDef ability;

		private bool skipIfCantTargetNow = true;

        private float chance = 1f;

        private bool waitIfCooldown = false;

		protected override bool OnlyUseAbilityVerbs => true;

		protected override bool OnlyUseRangedSearch => true;

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_AIAirUnitAbilityFight obj = (JobGiver_AIAirUnitAbilityFight)base.DeepCopy(resolve);
			obj.ability = ability;
			obj.skipIfCantTargetNow = skipIfCantTargetNow;
            obj.chance = chance;
            obj.waitIfCooldown = waitIfCooldown;
            return obj;
		}

		protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb verbToUse = null)
		{
			dest = IntVec3.Invalid;
			Thing enemyTarget = pawn.mindState.enemyTarget;
			Ability ability = pawn.abilities.GetAbility(this.ability);
			CastPositionRequest newReq = default(CastPositionRequest);
			newReq.caster = pawn;
			newReq.target = enemyTarget;
			newReq.verb = ability.verb;
			newReq.maxRangeFromTarget = ability.verb.EffectiveRange * 0.65f;
			newReq.wantCoverFromTarget = false;
			newReq.preferredCastPosition = pawn.Position;
			return CastPositionFinder.TryFindCastPosition(newReq, out dest);
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn.abilities.GetAbility(ability).OnCooldown)
			{
				if (waitIfCooldown)
                {
                    return WaitJob(pawn);
                }
                if (skipIfCantTargetNow)
                {
                    return null;
                }
			}
            if (!Rand.Chance(chance))
            {
                return null;
            }
			if (pawn.mindState.duty != null && pawn.mindState.duty.radius > 0f)
			{
				targetAcquireRadius = pawn.mindState.duty.radius;
				targetKeepRadius = pawn.mindState.duty.radius * 1.5f;
			}
			return BaseTryGiveJob(pawn);
		}

        private Job WaitJob(Pawn pawn)
        {
            if ((pawn.IsColonist || pawn.IsColonySubhuman) && pawn.playerSettings.hostilityResponse != HostilityResponseMode.Attack)
            {
                LordJob_Ritual_Duel lordJob_Ritual_Duel = pawn.GetLord()?.LordJob as LordJob_Ritual_Duel;
                if (lordJob_Ritual_Duel == null || !lordJob_Ritual_Duel.duelists.Contains(pawn))
                {
                    return null;
                }
            }

            UpdateEnemyTarget(pawn);
            Thing enemyTarget = pawn.mindState.enemyTarget;
            if (enemyTarget == null)
            {
                return null;
            }

            Pawn pawn2 = enemyTarget as Pawn;
            if (pawn2 != null && pawn2.IsPsychologicallyInvisible())
            {
                return null;
            }

            pawn.pather?.StopDead();
            Job jobWait = JobMaker.MakeJob(JobDefOf.Wait_Combat, ExpiryInterval_ShooterSucceeded.RandomInRange, checkOverrideOnExpiry: true);
            jobWait.expiryInterval = 20;
            jobWait.overrideFacing = pawn.GetRot(enemyTarget.DrawPos);
            jobWait.forceMaintainFacing = true;
            return jobWait;
        }

        private Job BaseTryGiveJob(Pawn pawn)
        {
            if ((pawn.IsColonist || pawn.IsColonySubhuman) && pawn.playerSettings.hostilityResponse != HostilityResponseMode.Attack)
            {
                LordJob_Ritual_Duel lordJob_Ritual_Duel = pawn.GetLord()?.LordJob as LordJob_Ritual_Duel;
                if (lordJob_Ritual_Duel == null || !lordJob_Ritual_Duel.duelists.Contains(pawn))
                {
                    return null;
                }
            }

            UpdateEnemyTarget(pawn);
            Thing enemyTarget = pawn.mindState.enemyTarget;
            if (enemyTarget == null)
            {
                return null;
            }

            Pawn pawn2 = enemyTarget as Pawn;
            if (pawn2 != null && pawn2.IsPsychologicallyInvisible())
            {
                return null;
            }

            bool flag = !pawn.IsColonist && !pawn.IsColonySubhuman && !DisableAbilityVerbs;
            if (flag)
            {
                Job abilityJob = GetAbilityJob(pawn, enemyTarget);
                if (abilityJob != null)
                {
                    return abilityJob;
                }
            }

            if (OnlyUseAbilityVerbs)
            {
                if (!TryFindShootingPosition(pawn, out IntVec3 dest))
                {
                    return null;
                }

                if (dest == pawn.Position)
                {
                    pawn.pather?.StopDead();
                    Job jobWait = JobMaker.MakeJob(JobDefOf.Wait_Combat, ExpiryInterval_ShooterSucceeded.RandomInRange, checkOverrideOnExpiry: true);
                    jobWait.expiryInterval = 20;
                    jobWait.overrideFacing = pawn.GetRot(enemyTarget.DrawPos);
                    jobWait.forceMaintainFacing = true;
                    return jobWait;
                }

                Job job = JobMaker.MakeJob(JobDefOf.Goto, dest);
                job.expiryInterval = ExpiryInterval_Ability.RandomInRange;
                job.checkOverrideOnExpire = true;
                return job;
            }

            Verb verb = pawn.TryGetAttackVerb(enemyTarget, flag, allowTurrets);
            if (verb == null)
            {
                return null;
            }

            if (verb.verbProps.IsMeleeAttack)
            {
                return MeleeAttackJob(pawn, enemyTarget);
            }

            bool num = CoverUtility.CalculateOverallBlockChance(pawn, enemyTarget.Position, pawn.Map) > 0.01f;
            bool flag2 = pawn.Position.WalkableBy(pawn.Map, pawn) && pawn.Map.pawnDestinationReservationManager.CanReserve(pawn.Position, pawn, pawn.Drafted);
            bool flag3 = verb.CanHitTarget(enemyTarget);
            bool flag4 = (pawn.Position - enemyTarget.Position).LengthHorizontalSquared < 25;
            if ((num && flag2 && flag3) || (flag4 && flag3))
            {
                pawn.pather?.StopDead();
                Job jobWait = JobMaker.MakeJob(JobDefOf.Wait_Combat, ExpiryInterval_ShooterSucceeded.RandomInRange, checkOverrideOnExpiry: true);
                jobWait.expiryInterval = 20;
                jobWait.overrideFacing = pawn.GetRot(enemyTarget.DrawPos);
                jobWait.forceMaintainFacing = true;
                return jobWait;
            }

            if (!TryFindShootingPosition(pawn, out IntVec3 dest2))
            {
                return null;
            }

            if (dest2 == pawn.Position)
            {
                pawn.pather?.StopDead();
                Job jobWait = JobMaker.MakeJob(JobDefOf.Wait_Combat, ExpiryInterval_ShooterSucceeded.RandomInRange, checkOverrideOnExpiry: true);
                jobWait.expiryInterval = 20;
                jobWait.overrideFacing = pawn.GetRot(enemyTarget.DrawPos);
                jobWait.forceMaintainFacing = true;
                return jobWait;
            }

            Job job2 = JobMaker.MakeJob(JobDefOf.Goto, dest2);
            job2.expiryInterval = ExpiryInterval_ShooterSucceeded.RandomInRange;
            job2.checkOverrideOnExpire = true;
            return job2;
        }

		protected override bool ShouldLoseTarget(Pawn pawn)
		{
			if (base.ShouldLoseTarget(pawn))
			{
				return true;
			}
			return !CanTarget(pawn, pawn.mindState.enemyTarget);
		}

		protected override bool ExtraTargetValidator(Pawn pawn, Thing target)
		{
			if (base.ExtraTargetValidator(pawn, target))
			{
				return CanTarget(pawn, target);
			}
			return false;
		}

		private bool CanTarget(Pawn pawn, Thing target)
		{
			if (!this.ability.verbProperties.targetParams.CanTarget(target))
			{
				return false;
			}
			Ability ability = pawn.abilities.GetAbility(this.ability);
			if (!ability.CanApplyOn((LocalTargetInfo)target))
			{
				return false;
			}
			if (skipIfCantTargetNow)
			{
				return ability.AICanTargetNow(target);
			}
			return true;
		}
	}

}
