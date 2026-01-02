using System.Collections.Generic;
using System.Linq;
using AlienRace;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Milira;

public class JobDriver_HumanizeMilian : JobDriver
{
	private const TargetIndex MilianInd = TargetIndex.A;

	private const TargetIndex ItemInd = TargetIndex.B;

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
		yield return Toils_General.Do(HumanizeMilian);
	}

	private void HumanizeMilian()
	{
		IntVec3 position = Milian.Position;
		Map map = Milian.Map;
		long ageBiologicalTicks = Milian.ageTracker.AgeBiologicalTicks;
		long ageChronologicalTicks = Milian.ageTracker.AgeChronologicalTicks;
		List<Apparel> list = Milian.apparel.WornApparel.ToList();
		Milian.equipment.TryDropEquipment(Milian.equipment.Primary, out var resultingEq, Milian.Position);
		foreach (Apparel item in list)
		{
			Milian.apparel.TryDrop(item);
		}
		Milian.Destroy();
		Pawn pawn = MilianPawnGenerator.GeneratePawn();
		pawn.ageTracker.AgeBiologicalTicks = ageBiologicalTicks;
		pawn.ageTracker.AgeChronologicalTicks = ageChronologicalTicks;
		ThingDef def = pawn.def;
		ThingDef_AlienRace val = (ThingDef_AlienRace)(object)((def is ThingDef_AlienRace) ? def : null);
		pawn.SetFaction(Faction.OfPlayer);
		for (int i = 0; i < list.Count; i++)
		{
			Log.Message("pawn.apparel.Wear");
			Apparel newApparel = list[i];
			pawn.apparel.Wear(newApparel, dropReplacedApparel: true, locked: true);
		}
		resultingEq.DeSpawn();
		Milian.equipment.MakeRoomFor(resultingEq);
		Milian.equipment.AddEquipment(resultingEq);
		GenSpawn.Spawn(pawn, position, map);
	}
}
