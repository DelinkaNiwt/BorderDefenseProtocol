using Verse;

namespace VanillaPsycastsExpanded;

public class HediffComp_DisappearsOnDespawn : HediffComp
{
	public override bool CompShouldRemove => base.Pawn.MapHeld == null;
}
