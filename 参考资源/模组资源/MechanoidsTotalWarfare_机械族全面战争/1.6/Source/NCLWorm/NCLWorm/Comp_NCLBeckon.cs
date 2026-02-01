using RimWorld;
using Verse;

namespace NCLWorm;

public class Comp_NCLBeckon : CompAbilityEffect
{
	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		if (target.Thing is Pawn { Map: var map } pawn)
		{
			FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastSkipFlashEntry);
			pawn.DeSpawn(DestroyMode.WillReplace);
			GenSpawn.Spawn(pawn, parent.pawn.Position, map);
			FleckMaker.Static(pawn.Position, map, FleckDefOf.PsycastSkipInnerExit);
		}
	}
}
