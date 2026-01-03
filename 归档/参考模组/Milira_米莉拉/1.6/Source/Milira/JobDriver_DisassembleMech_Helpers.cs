using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public static class JobDriver_DisassembleMech_Helpers
{
	public static IEnumerable<Toil> DisassembleMilianToils(Pawn mech, Pawn selPawn)
	{
		if (!ModLister.CheckBiotech("Disassemble mech"))
		{
			yield break;
		}
		JobDriver_DisassembleMech __instance = new JobDriver_DisassembleMech();
		__instance.pawn = selPawn;
		__instance.job = new Job();
		__instance.job.SetTarget(TargetIndex.A, mech);
		__instance.FailOnDestroyedNullOrForbidden(TargetIndex.A);
		__instance.FailOn(() => mech.IsFighting());
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
		yield return Toils_General.WaitWith(TargetIndex.A, 300, useProgressBar: true, maintainPosture: true, maintainSleep: false, TargetIndex.A).WithEffect(EffecterDefOf.ControlMech, TargetIndex.A);
		yield return Toils_General.Do(delegate
		{
			foreach (ThingDefCountClass item in MechanitorUtility.IngredientsFromDisassembly(mech.def))
			{
				Thing thing = ThingMaker.MakeThing(item.thingDef);
				thing.stackCount = item.count;
				GenPlace.TryPlaceThing(thing, mech.Position, mech.Map, ThingPlaceMode.Near);
			}
			List<Apparel> list = new List<Apparel>(mech.apparel.WornApparel);
			foreach (Apparel item2 in list)
			{
				mech.apparel.TryDrop(item2, out var _);
			}
			mech.forceNoDeathNotification = true;
			mech.Kill(null, null);
			mech.forceNoDeathNotification = false;
			mech.Corpse.Destroy();
		}).WithEffect(EffecterDefOf.ButcherMechanoid, TargetIndex.A).PlaySustainerOrSound(SoundDefOf.Recipe_ButcherCorpseMechanoid);
	}
}
