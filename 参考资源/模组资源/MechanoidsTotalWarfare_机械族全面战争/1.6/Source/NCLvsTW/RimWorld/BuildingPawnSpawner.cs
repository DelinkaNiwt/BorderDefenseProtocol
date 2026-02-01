using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace RimWorld;

public class BuildingPawnSpawner : BuildingGroundSpawner
{
	protected override ThingDef ThingDefToSpawn => (def.building as BuildingProperties_PawnSpawner)?.pawnKindToSpawn?.race;

	private Faction DefaultFaction
	{
		get
		{
			BuildingProperties_PawnSpawner props = def.building as BuildingProperties_PawnSpawner;
			return (props?.defaultFaction != null) ? Find.FactionManager.FirstFactionOfDef(props.defaultFaction) : Faction.OfAncientsHostile;
		}
	}

	public override void PostMake()
	{
		BuildingProperties_PawnSpawner props = def.building as BuildingProperties_PawnSpawner;
		if (props?.pawnKindToSpawn != null)
		{
			def.building.groundSpawnerThingToSpawn = props.pawnKindToSpawn.race;
		}
		base.PostMake();
	}

	protected override void PostMakeInt()
	{
		BuildingProperties_PawnSpawner props = def.building as BuildingProperties_PawnSpawner;
		if (props?.pawnKindToSpawn != null)
		{
			thingToSpawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(props.pawnKindToSpawn, DefaultFaction, PawnGenerationContext.NonPlayer, null, forceGenerateNewPawn: true));
		}
		else
		{
			Log.Error("BuildingPawnSpawner " + def.defName + " missing valid pawnKindToSpawn");
		}
	}

	protected override void Spawn(Map map, IntVec3 pos)
	{
		if (thingToSpawn == null)
		{
			Log.Error("Trying to spawn null thing from " + def.defName);
			return;
		}
		base.Spawn(map, pos);
		if (thingToSpawn is Pawn { Faction: not null } pawn && pawn.Faction.HostileTo(Faction.OfPlayer))
		{
			LordMaker.MakeNewLord(pawn.Faction, new LordJob_AssaultColony(pawn.Faction), map, new List<Pawn> { pawn });
		}
	}
}
