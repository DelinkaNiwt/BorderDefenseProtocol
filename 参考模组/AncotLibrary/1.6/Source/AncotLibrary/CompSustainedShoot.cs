using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class CompSustainedShoot : ThingComp
{
	public bool isActive;

	public int cachedBurstShotsLeft;

	public int activeTickLeft;

	public int idelTicks = 0;

	public CompProperties_SustainedShoot Props => (CompProperties_SustainedShoot)props;

	public CompEquippable CompEp => parent.TryGetComp<CompEquippable>();

	public Verb_ShootSustained Verb => CompEp.PrimaryVerb as Verb_ShootSustained;

	private Pawn Pawn => Verb.CasterPawn;

	public override void Notify_Equipped(Pawn pawn)
	{
		VerbReset();
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		VerbReset();
	}

	public override void Notify_UsedWeapon(Pawn pawn)
	{
		base.Notify_UsedWeapon(pawn);
		isActive = true;
	}

	public override void CompTick()
	{
		base.CompTick();
		if (!isActive || Verb == null)
		{
			return;
		}
		Job curJob = Pawn.CurJob;
		if (curJob != null && curJob.def != JobDefOf.AttackStatic && curJob.def != JobDefOf.Wait_Combat && curJob.def != JobDefOf.Wait_MaintainPosture)
		{
			ForceStopBurst();
			return;
		}
		if (Verb.state == VerbState.Bursting)
		{
			idelTicks = 0;
			cachedBurstShotsLeft = Verb.BurstShotsLeft;
			Pawn pawn = Verb.CurrentTarget.Pawn;
			if (pawn != null && pawn.Downed && pawn != Verb.forceTargetedDownedPawn)
			{
				Verb.Reset();
			}
		}
		if (Verb.state == VerbState.Idle)
		{
			idelTicks++;
			if (idelTicks > 6)
			{
				ForceStopBurst();
			}
			if (cachedBurstShotsLeft > 1)
			{
				Stance curStance = Verb.CasterPawn.stances.curStance;
				if (curStance is Stance_Cooldown || curStance is Stance_Warmup)
				{
					Stance_Busy stance_Busy = curStance as Stance_Busy;
					if (stance_Busy.verb is Verb_ShootSustained)
					{
						stance_Busy.ticksLeft = 0;
					}
				}
			}
		}
		if (cachedBurstShotsLeft <= 1)
		{
			isActive = false;
		}
	}

	public void VerbReset()
	{
		Verb.Reset();
		Verb.CasterPawn.stances.CancelBusyStanceSoft();
		cachedBurstShotsLeft = 0;
		isActive = false;
		idelTicks = 0;
	}

	public void ForceStopBurst()
	{
		VerbReset();
		Pawn?.stances?.SetStance(new Stance_Cooldown(Verb.verbProps.AdjustedCooldownTicks(Verb, Pawn), Verb.CurrentTarget, Verb));
	}

	public void ResetCached()
	{
		cachedBurstShotsLeft = 0;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref idelTicks, "idelTicks", 0);
		Scribe_Values.Look(ref isActive, "isActive", defaultValue: false);
		Scribe_Values.Look(ref cachedBurstShotsLeft, "cachedBurstShotsLeft", 0);
		Scribe_Values.Look(ref activeTickLeft, "activeTickLeft", 0);
	}
}
