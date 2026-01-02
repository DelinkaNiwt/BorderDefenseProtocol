using Verse;

namespace AncotLibrary;

public class CompLeavingItemDestroyed : ThingComp
{
	public CompProperties_LeavingItemDestroyed Props => (CompProperties_LeavingItemDestroyed)props;

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		if (Props.onlyNonPlayerLeaving && parent.Faction.IsPlayer)
		{
			return;
		}
		for (int i = 0; i < Props.thingDefWithCommonalities.Count; i++)
		{
			if (Rand.Chance(Props.thingDefWithCommonalities[i].commonality))
			{
				for (int j = 0; j < Props.thingDefWithCommonalities[i].count; j++)
				{
					GenSpawn.Spawn(Props.thingDefWithCommonalities[i].thingDef, parent.PositionHeld, previousMap);
				}
			}
		}
		base.PostDestroy(mode, previousMap);
	}
}
