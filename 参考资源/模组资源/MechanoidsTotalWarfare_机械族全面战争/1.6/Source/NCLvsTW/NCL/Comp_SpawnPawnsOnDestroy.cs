using System;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NCL;

public class Comp_SpawnPawnsOnDestroy : ThingComp
{
	public CompProperties_SpawnPawnsOnDestroy Props => (CompProperties_SpawnPawnsOnDestroy)props;

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		if (previousMap == null)
		{
			return;
		}
		if (Props.pawnKind == null)
		{
			Log.Warning("Comp_SpawnPawnsOnDestroy: No pawnKind defined");
			return;
		}
		int spawnCount = Props.spawnCountRange.RandomInRange;
		for (int i = 0; i < spawnCount; i++)
		{
			TrySpawnPawn(previousMap);
		}
	}

	private void TrySpawnPawn(Map map)
	{
		try
		{
			Faction mechanoidFaction = GetMechanoidFaction();
			PawnKindDef pawnKind = Props.pawnKind;
			PlanetTile? tile = -1;
			float? fixedBiologicalAge = 0f;
			float? fixedChronologicalAge = 0f;
			PawnGenerationRequest request = new PawnGenerationRequest(pawnKind, mechanoidFaction, PawnGenerationContext.NonPlayer, tile, forceGenerateNewPawn: true, allowDead: false, allowDowned: false, canGeneratePawnRelations: false, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, fixedBiologicalAge, fixedChronologicalAge);
			Pawn pawn = PawnGenerator.GeneratePawn(request);
			GenPlace.TryPlaceThing(pawn, parent.Position, map, ThingPlaceMode.Near, null, null, Rot4.Random);
			if (Current.ProgramState == ProgramState.Playing)
			{
				FleckMaker.ThrowDustPuff(parent.Position, map, 1f);
			}
		}
		catch (Exception arg)
		{
			Log.Error($"Failed to spawn pawn: {arg}");
		}
	}

	private Faction GetMechanoidFaction()
	{
		FactionDef mechanoidDef = DefDatabase<FactionDef>.GetNamed("Mechanoid", errorOnFail: false);
		if (mechanoidDef == null)
		{
			Log.Error("找不到defName为'Mechanoids'的派系定义！");
			return null;
		}
		Faction mechanoidFaction = Find.FactionManager.FirstFactionOfDef(mechanoidDef);
		if (mechanoidFaction != null)
		{
			return mechanoidFaction;
		}
		return mechanoidFaction;
	}
}
