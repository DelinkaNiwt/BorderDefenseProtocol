using System.Collections.Generic;
using AncotLibrary;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class JobDriver_FortressMode : JobDriver
{
	private Thing thing;

	public virtual ThingDef turretDef => MiliraDefOf.Milian_Fortress;

	public CompThingContainer CompThingContainer => thing.TryGetComp<CompThingContainer>();

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		IntVec3 position = pawn.Position;
		foreach (IntVec3 cell in GenRadial.RadialCellsAround(position, 1.5f, useCenter: false))
		{
			if (!cell.IsValid || !cell.InBounds(pawn.Map) || !cell.Walkable(pawn.Map) || !cell.GetEdifice(pawn.Map).DestroyedOrNull() || cell.Roofed(pawn.Map))
			{
				Ability ability = pawn.abilities.abilities.FirstOrDefault((Ability a) => a.def.defName == "Milira_Fortress");
				ability.StartCooldown(0);
				yield break;
			}
		}
		yield return Toils_General.Do(DeployPod);
		Toil toil = ToilMaker.MakeToil("MakeNewToils");
		toil.initAction = delegate
		{
			CompThingContainer compThingContainer = CompThingContainer;
			bool flag = pawn.DeSpawnOrDeselect();
			compThingContainer.GetDirectlyHeldThings().TryAdd(pawn);
			if (flag)
			{
				Find.Selector.Deselect(pawn);
				Find.Selector.Select(thing, playSound: false, forceDesignatorDeselect: false);
			}
		};
		yield return toil;
	}

	private void DeployPod()
	{
		CompThingCarrier_Custom val = ((Thing)pawn).TryGetComp<CompThingCarrier_Custom>();
		if (val != null)
		{
			FleckMaker.Static(pawn.TrueCenter(), pawn.Map, FleckDefOf.Milian_FortressFormed);
			thing = GenSpawn.Spawn(turretDef, pawn.Position, pawn.Map);
			thing.SetFaction(pawn.Faction);
			if (ModsConfig.IsActive("Ancot.MilianModification") && pawn.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.MilianFitting_RapidDeployment) != null)
			{
				SetHitPointAndRemoveResourceInCarrier(val, 200, 60);
			}
			else
			{
				SetHitPointAndRemoveResourceInCarrier(val, 1200, 600);
			}
			CompThingContainer_Milian compThingContainer_Milian = thing.TryGetComp<CompThingContainer_Milian>();
			compThingContainer_Milian.hitPointMax = thing.HitPoints;
		}
	}

	private void SetHitPointAndRemoveResourceInCarrier(CompThingCarrier_Custom comp, int hitPoint, int initiationDelayTicks)
	{
		if (comp.IngredientCount > hitPoint + 400)
		{
			comp.TryRemoveThingInCarrier(hitPoint);
			thing.HitPoints = hitPoint;
		}
		else
		{
			int num = comp.IngredientCount / 2;
			comp.TryRemoveThingInCarrier(num);
			thing.HitPoints = num;
		}
		CompInitiatable compInitiatable = thing.TryGetComp<CompInitiatable>();
		if (compInitiatable != null)
		{
			compInitiatable.initiationDelayTicksOverride = initiationDelayTicks;
		}
	}
}
