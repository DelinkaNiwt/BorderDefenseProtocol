using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class Building_DroneCharger : Building
{
	private Pawn currentlyChargingMech;

	private CompPowerTrader powerTrader;

	private Sustainer sustainerCharging;

	private Mote moteCharging;

	public CompPowerTrader PowerTrader => powerTrader ?? (powerTrader = this.TryGetComp<CompPowerTrader>());

	public bool IsPowered => PowerTrader.PowerOn;

	public GenDraw.FillableBarRequest BarDrawData => def.building.BarDrawDataFor(base.Rotation);

	public Pawn CurrentlyChargingMech => currentlyChargingMech;

	public bool CanPawnChargeCurrently(Pawn pawn)
	{
		if (!IsCompatibleWithCharger(pawn.kindDef))
		{
			return false;
		}
		if (IsPowered)
		{
			if (currentlyChargingMech == null)
			{
				return true;
			}
			if (currentlyChargingMech == pawn)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsCompatibleWithCharger(PawnKindDef kindDef)
	{
		return IsCompatibleWithCharger(def, kindDef);
	}

	public static bool IsCompatibleWithCharger(ThingDef chargerDef, PawnKindDef kindDef)
	{
		return IsCompatibleWithCharger(chargerDef, kindDef.race);
	}

	public static bool IsCompatibleWithCharger(ThingDef chargerDef, ThingDef mechRace)
	{
		if (mechRace.race.IsMechanoid && mechRace.GetCompProperties<CompProperties_Drone>() != null && mechRace.race.mechWeightClass != null)
		{
			return chargerDef.building.requiredMechWeightClasses.NotNullAndContains(mechRace.race.mechWeightClass);
		}
		return false;
	}

	protected override void Tick()
	{
		base.Tick();
		if (currentlyChargingMech != null && (currentlyChargingMech.CurJobDef != AncotJobDefOf.Ancot_DroneCharge || currentlyChargingMech.CurJob.targetA.Thing != this))
		{
			Log.Warning("Mech did not clean up his charging job properly");
			StopCharging();
		}
		if (currentlyChargingMech != null && PowerTrader.PowerOn)
		{
			currentlyChargingMech.TryGetComp<CompDrone>()?.ChargeTick();
			ChargeEffecter();
		}
	}

	public virtual void ChargeEffecter()
	{
		if (sustainerCharging == null)
		{
			sustainerCharging = SoundDefOf.MechChargerCharging.TrySpawnSustainer(SoundInfo.InMap(this));
		}
		sustainerCharging.Maintain();
		if (moteCharging == null || moteCharging.Destroyed)
		{
			moteCharging = MoteMaker.MakeAttachedOverlay(currentlyChargingMech, ThingDefOf.Mote_MechCharging, Vector3.zero);
		}
		moteCharging?.Maintain();
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (DebugSettings.ShowDevGizmos && currentlyChargingMech != null)
		{
			yield return new Command_Action
			{
				action = delegate
				{
					currentlyChargingMech.needs.TryGetNeed(out Need_MechEnergy need);
					need.CurLevelPercentage = 1f;
				},
				defaultLabel = "DEV: Charge 100%"
			};
		}
	}

	public void StartCharging(Pawn mech)
	{
		if (ModLister.CheckBiotech("Mech charging"))
		{
			if (currentlyChargingMech != null)
			{
				Log.Error("Tried charging on already charging mech charger!");
				return;
			}
			if (!mech.IsColonyMech)
			{
				mech.jobs.EndCurrentJob(JobCondition.Incompletable);
				return;
			}
			currentlyChargingMech = mech;
			mech.TryGetComp<CompDrone>().currentCharger = this;
			SoundDefOf.MechChargerStart.PlayOneShot(this);
		}
	}

	public void StopCharging()
	{
		if (currentlyChargingMech == null)
		{
			Log.Error("Tried stopping charging on currently not charging mech charger!");
			return;
		}
		if (currentlyChargingMech.TryGetComp<CompDrone>() != null)
		{
			currentlyChargingMech.TryGetComp<CompDrone>().currentCharger = null;
		}
		currentlyChargingMech = null;
	}

	public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
	{
		if (base.BeingTransportedOnGravship)
		{
			base.DeSpawn(mode);
		}
		else
		{
			base.DeSpawn(mode);
		}
	}

	public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
	{
		IEnumerable<PawnKindDef> source = DefDatabase<PawnKindDef>.AllDefs.Where(IsCompatibleWithCharger);
		string text = source.Select((PawnKindDef pk) => pk.LabelCap.Resolve()).ToLineList("  - ");
		yield return new StatDrawEntry(StatCategoryDefOf.Basics, "StatsReport_RechargerWeightClass".Translate(), def.building.requiredMechWeightClasses.Select((MechWeightClassDef w) => w.label).ToCommaList().CapitalizeFirst(), "StatsReport_RechargerWeightClass_Desc".Translate() + ": " + "\n\n" + text, 99999, null, source.Select((PawnKindDef pk) => new Dialog_InfoCard.Hyperlink(pk.race)));
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref currentlyChargingMech, "currentlyChargingMech");
	}
}
