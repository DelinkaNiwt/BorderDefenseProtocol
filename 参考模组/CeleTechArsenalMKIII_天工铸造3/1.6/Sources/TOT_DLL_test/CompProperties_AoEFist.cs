using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class CompProperties_AoEFist : CompProperties_AbilityEffect
{
	public float range;

	public float lineWidthEnd = 13f;

	public FleckDef SpawnFleck;

	public int Fleck_Num = 11;

	public CompProperties_AoEFist()
	{
		compClass = typeof(CompProperties_AoEFist);
	}
}
