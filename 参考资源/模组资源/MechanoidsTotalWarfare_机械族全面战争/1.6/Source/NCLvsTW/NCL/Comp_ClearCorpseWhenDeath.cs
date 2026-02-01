using Verse;

namespace NCL;

public class Comp_ClearCorpseWhenDeath : ThingComp
{
	public CompProperties_ClearCorpseWhenDeath Props => (CompProperties_ClearCorpseWhenDeath)props;

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		if (mode != DestroyMode.KillFinalize)
		{
			return;
		}
		if (Props.ProductDef != null)
		{
			int spawnCount = Props.SpawnCountRange.RandomInRange;
			for (int i = 0; i < spawnCount; i++)
			{
				Thing thing = ThingMaker.MakeThing(Props.ProductDef);
				GenPlace.TryPlaceThing(thing, parent.Position, previousMap, ThingPlaceMode.Near);
			}
		}
		if (parent is Pawn { Corpse: not null } pawn)
		{
			pawn.Corpse.Destroy();
		}
	}
}
