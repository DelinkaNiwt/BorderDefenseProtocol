using Verse;

namespace NCL;

public class CompProperties_ClearCorpseWhenDeath : CompProperties
{
	public ThingDef ProductDef;

	public IntRange SpawnCountRange = new IntRange(7, 13);

	public CompProperties_ClearCorpseWhenDeath()
	{
		compClass = typeof(Comp_ClearCorpseWhenDeath);
	}
}
