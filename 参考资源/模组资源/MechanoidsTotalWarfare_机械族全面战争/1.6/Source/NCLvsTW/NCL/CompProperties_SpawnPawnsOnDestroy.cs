using Verse;

namespace NCL;

public class CompProperties_SpawnPawnsOnDestroy : CompProperties
{
	public PawnKindDef pawnKind;

	public IntRange spawnCountRange = new IntRange(1, 1);

	public CompProperties_SpawnPawnsOnDestroy()
	{
		compClass = typeof(Comp_SpawnPawnsOnDestroy);
	}
}
