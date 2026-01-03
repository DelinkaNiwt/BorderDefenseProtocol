using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Milira;

public class JobDriver_EquipMilian : JobDriver
{
	private const TargetIndex MilianInd = TargetIndex.A;

	private const TargetIndex ApparelInd = TargetIndex.B;

	private const int DurationTicks = 600;

	private Mote warmupMote;

	private Pawn Milian => (Pawn)job.GetTarget(TargetIndex.A).Thing;

	private ThingWithComps Weapon => (ThingWithComps)job.GetTarget(TargetIndex.B).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (pawn.Reserve(Milian, job, 1, -1, null, errorOnFailed))
		{
			return pawn.Reserve(Weapon, job, 1, -1, null, errorOnFailed);
		}
		return false;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
		yield return Toils_Haul.StartCarryThing(TargetIndex.B);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
		Toil toil = Toils_General.WaitWith(TargetIndex.A, 60, useProgressBar: true, maintainPosture: true);
		if (pawn == Milian)
		{
			toil = Toils_General.Wait(60);
		}
		toil.WithProgressBarToilDelay(TargetIndex.A);
		toil.FailOnDespawnedOrNull(TargetIndex.A);
		toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
		toil.tickAction = delegate
		{
			CompUsable compUsable = Weapon.TryGetComp<CompUsable>();
			if (compUsable != null && warmupMote == null && compUsable.Props.warmupMote != null)
			{
				warmupMote = MoteMaker.MakeAttachedOverlay(Milian, compUsable.Props.warmupMote, Vector3.zero);
			}
			warmupMote?.Maintain();
		};
		yield return toil;
		yield return Toils_General.Do(EquipMilian);
	}

	private void EquipMilian()
	{
		ThingWithComps thingWithComps = Milian.equipment?.Primary;
		if (thingWithComps != null)
		{
			Milian.equipment.TryDropEquipment(thingWithComps, out var resultingEq, pawn.Position);
			if (ModsConfig.IsActive("Ancot.MilianModification") && Milian.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_TacticalSling) != null)
			{
				HediffComp_AlternateWeapon val = Milian.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_TacticalSling).TryGetComp<HediffComp_AlternateWeapon>();
				if (val.ContainedThing == null)
				{
					val.AddToContainer((Thing)resultingEq);
				}
				Milian.Drawer.renderer.renderTree.SetDirty();
			}
		}
		pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out var _);
		Weapon.DeSpawn();
		Milian.equipment.MakeRoomFor(Weapon);
		Milian.equipment.AddEquipment(Weapon);
	}
}
