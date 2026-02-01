using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace NCL;

public class JobDriver_AttackMinitor : JobDriver
{
	private const int AttackIntervalTicks = 300;

	private int lastAttackTick = -9999;

	private bool attackCompleted;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(job.targetA, job, -1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		yield return Toils_General.Do(delegate
		{
			pawn.Map.reservationManager.ReleaseAllClaimedBy(pawn);
			InitializeTarget();
		});
		yield return Toils_General.Do(delegate
		{
			InitializeTarget();
		});
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOn(() => attackCompleted);
		yield return new Toil
		{
			initAction = delegate
			{
				if (Find.TickManager.TicksGame - lastAttackTick >= 300)
				{
					if (TryAttackTarget())
					{
						attackCompleted = true;
						ReadyForNextToil();
					}
					else
					{
						InitializeTarget();
					}
					lastAttackTick = Find.TickManager.TicksGame;
				}
			},
			defaultCompleteMode = ToilCompleteMode.Delay,
			defaultDuration = 300
		};
		yield return Toils_General.Do(delegate
		{
			EndJobWith(JobCondition.Succeeded);
		});
	}

	private void InitializeTarget()
	{
		Thing newTarget = FindClosestMinitor();
		if (newTarget != null)
		{
			job.SetTarget(TargetIndex.A, newTarget);
			attackCompleted = false;
		}
	}

	private bool TryAttackTarget()
	{
		if (!(base.TargetA.Thing is Pawn target) || !pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly) || !CanPawnAttack(pawn, target))
		{
			return false;
		}
		pawn.meleeVerbs.TryMeleeAttack(target);
		return true;
	}

	private bool CanPawnAttack(Pawn attacker, Pawn target)
	{
		return attacker != null && target != null && !attacker.Downed && !attacker.Dead && !target.Downed && !target.Dead;
	}

	private Thing FindClosestMinitor()
	{
		return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("TW_University_Minitor")), PathEndMode.Touch, TraverseParms.For(pawn), 50f, (Thing t) => t is Pawn pawn && CanPawnAttack(base.pawn, pawn) && !pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("TW_WasEncouraged")));
	}
}
