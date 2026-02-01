using RimWorld;
using Verse;
using Verse.AI.Group;

namespace NCL;

public class CompSpawnThingOnDestroy : ThingComp
{
	private Map map;

	public CompProperties_SpawnThingOnDestroy Props => (CompProperties_SpawnThingOnDestroy)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		map = parent.Map;
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		if (Props.enableThingSpawn && Props.thingDef != null)
		{
			Thing thing = ThingMaker.MakeThing(Props.thingDef);
			if (!thing.def.MadeFromStuff)
			{
				GenPlace.TryPlaceThing(thing, parent.Position, map, ThingPlaceMode.Near, null, null, default(Rot4));
			}
		}
		if (Props.enablePawnSpawn && Props.pawnKindDef != null && Props.faction != null)
		{
			Faction faction = Find.FactionManager.FirstFactionOfDef(Props.faction);
			Pawn pawn = PawnGenerator.GeneratePawn(Props.pawnKindDef, faction);
			GenPlace.TryPlaceThing(pawn, parent.Position, map, ThingPlaceMode.Near, null, null, default(Rot4));
			Lord lord = LordMaker.MakeNewLord(faction, new LordJob_AssaultThings(faction, map.listerThings.AllThings.FindAll((Thing p) => p is Pawn && p.Faction != null && p.Faction.HostileTo(faction))), map);
			lord.AddPawn(pawn);
		}
		base.PostDestroy(mode, previousMap);
	}
}
