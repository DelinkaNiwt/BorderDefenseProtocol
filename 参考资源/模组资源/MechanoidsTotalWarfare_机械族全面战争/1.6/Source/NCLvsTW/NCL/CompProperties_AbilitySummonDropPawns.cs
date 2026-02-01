using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

public class CompProperties_AbilitySummonDropPawns : CompProperties_AbilityEffect
{
	public List<PawnKindDef> pawnKinds;

	public int pawnCount = 1;

	public List<PawnKindDef> secondaryPawnKinds;

	public int secondaryPawnCount = 0;

	public int spawnRadius = 5;

	public int minSpawnDistance = 2;

	public bool leaveSlag = false;

	public bool canRoofPunch = true;

	public CompProperties_AbilitySummonDropPawns()
	{
		compClass = typeof(CompAbilityEffect_SummonDropPawns);
	}
}
