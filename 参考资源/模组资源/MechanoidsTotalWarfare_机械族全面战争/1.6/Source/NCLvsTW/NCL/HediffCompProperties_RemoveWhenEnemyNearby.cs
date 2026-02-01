using Verse;

namespace NCL;

public class HediffCompProperties_RemoveWhenEnemyNearby : HediffCompProperties
{
	public float? checkRadius;

	public HediffCompProperties_RemoveWhenEnemyNearby()
	{
		compClass = typeof(HediffComp_RemoveWhenEnemyNearby);
	}
}
