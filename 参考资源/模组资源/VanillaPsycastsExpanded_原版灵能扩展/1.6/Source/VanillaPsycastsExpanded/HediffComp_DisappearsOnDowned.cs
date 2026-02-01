using Verse;

namespace VanillaPsycastsExpanded;

public class HediffComp_DisappearsOnDowned : HediffComp
{
	public override bool CompShouldRemove => base.Pawn.Downed;
}
