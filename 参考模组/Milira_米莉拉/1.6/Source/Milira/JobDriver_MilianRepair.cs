using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Milira;

public class JobDriver_MilianRepair : JobDriver
{
	private const TargetIndex MechInd = TargetIndex.A;

	private const int DefaultTicksPerHeal = 120;

	protected float ticksToNextRepair;

	private static readonly SimpleCurve BonusRepairSpeedCurve = new SimpleCurve
	{
		new CurvePoint(0f, 0.8f),
		new CurvePoint(1f, 0.8f),
		new CurvePoint(15f, 0f)
	};

	protected float TicksPerRepair = 20f;

	protected int RepairTicks
	{
		get
		{
			if (pawn.Drafted)
			{
				return 999999;
			}
			return 600;
		}
	}

	protected Thing RepairObject => job.GetTarget(TargetIndex.A).Thing;

	protected virtual bool Remote => true;

	private float Range => pawn.GetStatValue(StatDefOf.MechRemoteRepairDistance);

	private float RepairTickFactor => pawn.GetStatValue(AncotDefOf.Ancot_MechRepairSpeed);

	private float energyCostFactor_Target
	{
		get
		{
			float num = 1f;
			if (ModsConfig.IsActive("Ancot.MilianModification"))
			{
				if (pawn.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_AnalyseRepairChip) != null)
				{
					num *= 0.6f;
				}
				if (pawn.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_WasteEnergyRepair) != null)
				{
					num = 0f;
				}
			}
			return num;
		}
	}

	private float energyCostFactor_Caster
	{
		get
		{
			float result = 1f;
			if (ModsConfig.IsActive("Ancot.MilianModification") && pawn.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_WasteEnergyRepair) != null)
			{
				result = 0f;
			}
			return result;
		}
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		if (!ModLister.CheckBiotech("Mech repair"))
		{
			yield break;
		}
		this.FailOnDestroyedOrNull(TargetIndex.A);
		this.FailOn(() => RepairObject.Position.DistanceTo(base.pawn.Position) > Range);
		if (!Remote)
		{
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
		}
		Toil toil = Toils_General.Wait(RepairTicks);
		toil.WithEffect(EffecterDefOf.MechRepairing, TargetIndex.A);
		toil.PlaySustainerOrSound(SoundDefOf.RepairMech_Touch);
		toil.AddPreInitAction(delegate
		{
			ticksToNextRepair = TicksPerRepair;
		});
		toil.handlingFacing = true;
		toil.tickIntervalAction = delegate(int delta)
		{
			ticksToNextRepair -= RepairSpeedFactor() * (float)delta;
			if (ticksToNextRepair <= 0f)
			{
				if (base.pawn.needs.energy != null)
				{
					base.pawn.needs.energy.CurLevel -= energyCostFactor_Caster * base.pawn.GetStatValue(StatDefOf.MechEnergyLossPerHP);
				}
				if (RepairObject is Pawn)
				{
					Pawn pawn2 = (Pawn)RepairObject;
					if (pawn2.needs.energy != null)
					{
						pawn2.needs.energy.CurLevel -= energyCostFactor_Target * pawn2.GetStatValue(StatDefOf.MechEnergyLossPerHP);
					}
					MechRepairUtility.RepairTick(pawn2, 1);
				}
				else
				{
					base.TargetThingA.HitPoints += 2;
					base.TargetThingA.HitPoints = Mathf.Min(base.TargetThingA.HitPoints, base.TargetThingA.MaxHitPoints);
				}
				ticksToNextRepair = TicksPerRepair;
			}
			base.pawn.rotationTracker.FaceTarget(RepairObject);
		};
		if (RepairObject is Pawn)
		{
			Pawn pawn = (Pawn)RepairObject;
			toil.AddEndCondition(() => MechRepairUtility.CanRepair(pawn) ? JobCondition.Ongoing : JobCondition.Succeeded);
		}
		else
		{
			toil.AddEndCondition(() => (base.TargetThingA.HitPoints < base.TargetThingA.MaxHitPoints) ? JobCondition.Ongoing : JobCondition.Succeeded);
		}
		yield return toil;
	}

	public float RepairSpeedFactor()
	{
		float num = 1f * RepairTickFactor;
		if (ModsConfig.IsActive("Ancot.MilianModification") && pawn.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_DeepRepair) != null)
		{
			num += num * BonusRepairSpeedCurve.Evaluate(pawn.Position.DistanceTo(base.TargetThingA.PositionHeld));
		}
		return num;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref ticksToNextRepair, "ticksToNextRepair", 0f);
	}
}
