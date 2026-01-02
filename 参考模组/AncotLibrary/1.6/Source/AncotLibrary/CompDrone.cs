using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class CompDrone : CompDraftable
{
	public bool depleted;

	public DroneWorkModeDef workMode;

	private DroneGizmo gizmo;

	private int powerTicksLeft;

	public float PercentRecharge = 0.4f;

	public Building_DroneCharger currentCharger;

	public bool initialized;

	public bool autoRepair;

	public bool disassemble;

	public override bool Draftable => !depleted;

	public Pawn Mech => (Pawn)parent;

	public new CompProperties_Drone Props => (CompProperties_Drone)props;

	public int MaxPowerTicks => (int)(Mech.GetStatValue(AncotDefOf.Ancot_DroneEnduranceTime) * 2500f);

	public int RechargePowerTicks => (int)(Mech.GetStatValue(AncotDefOf.Ancot_DroneRechargeTime) * 2500f);

	public float PercentFull => (float)powerTicksLeft / (float)MaxPowerTicks;

	public int PowerTicksLeft => powerTicksLeft;

	public bool IsSelfShutdown => Mech.CurJobDef == AncotJobDefOf.Ancot_DroneSelfShutdown;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (!initialized)
		{
			powerTicksLeft = MaxPowerTicks;
			workMode = Props.initialWorkMode;
			initialized = true;
		}
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (Mech.Faction != Faction.OfPlayer && !Props.showGizmoOnNonPlayerControlled)
		{
			yield break;
		}
		if (gizmo == null)
		{
			gizmo = new DroneGizmo(this)
			{
				Order = -100f
			};
		}
		yield return gizmo;
		if (DebugSettings.ShowDevGizmos)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Power left 0%",
				action = delegate
				{
					powerTicksLeft = 0;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "DEV: Power left 20%",
				action = delegate
				{
					powerTicksLeft = (int)((float)MaxPowerTicks * 0.2f);
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "DEV: Power left 100%",
				action = delegate
				{
					powerTicksLeft = MaxPowerTicks;
				}
			};
		}
	}

	public override void CompTickInterval(int delta)
	{
		if (currentCharger != null || !Mech.Spawned)
		{
			return;
		}
		if (depleted && PercentFull > 0f)
		{
			depleted = false;
		}
		else if (!IsSelfShutdown)
		{
			powerTicksLeft -= delta;
			if (powerTicksLeft <= 0)
			{
				SelfShutdown();
				powerTicksLeft = 0;
				depleted = true;
			}
		}
	}

	public virtual void ChargeTick()
	{
		powerTicksLeft += (int)((float)MaxPowerTicks / (float)RechargePowerTicks);
		if (powerTicksLeft > MaxPowerTicks)
		{
			powerTicksLeft = MaxPowerTicks;
		}
	}

	public virtual void SelfShutdown()
	{
		IntVec3 result = Mech.Position;
		RCellFinder.TryFindNearbyMechSelfShutdownSpot(Mech.Position, Mech, Mech.Map, out result, allowForbidden: true);
		if (Mech.Drafted)
		{
			Mech.drafter.Drafted = false;
		}
		Mech.jobs.StartJob(JobMaker.MakeJob(AncotJobDefOf.Ancot_DroneSelfShutdown, result), JobCondition.InterruptForced, null, resumeCurJobAfterwards: false, cancelBusyStances: true, null, JobTag.SatisfyingNeeds);
	}

	public bool CanRepair()
	{
		if (GetHediffToHeal(Mech) == null)
		{
			return MechRepairUtility.IsMissingWeapon(Mech);
		}
		return true;
	}

	private static Hediff GetHediffToHeal(Pawn mech)
	{
		Hediff hediff = null;
		float num = float.PositiveInfinity;
		foreach (Hediff hediff2 in mech.health.hediffSet.hediffs)
		{
			if (hediff2 is Hediff_Injury && hediff2.Severity < num)
			{
				num = hediff2.Severity;
				hediff = hediff2;
			}
		}
		if (hediff != null)
		{
			return hediff;
		}
		foreach (Hediff hediff3 in mech.health.hediffSet.hediffs)
		{
			if (hediff3 is Hediff_MissingPart)
			{
				return hediff3;
			}
		}
		return null;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref powerTicksLeft, "powerTicksLeft", 0);
		Scribe_Values.Look(ref PercentRecharge, "PercentRecharge", 0.4f);
		Scribe_Values.Look(ref depleted, "depleted", defaultValue: false);
		Scribe_References.Look(ref currentCharger, "currentCharger");
		Scribe_Defs.Look(ref workMode, "workMode");
		Scribe_Values.Look(ref autoRepair, "autoRepair", defaultValue: false);
		Scribe_Values.Look(ref disassemble, "disassemble", defaultValue: false);
		Scribe_Values.Look(ref initialized, "initialized", defaultValue: false);
	}
}
