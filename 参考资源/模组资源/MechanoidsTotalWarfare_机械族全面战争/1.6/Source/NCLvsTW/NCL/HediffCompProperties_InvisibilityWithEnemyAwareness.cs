using Verse;

namespace NCL;

public class HediffCompProperties_InvisibilityWithEnemyAwareness : HediffCompProperties_Invisibility
{
	public float detectionRadius = 12f;

	public int checkInterval = 30;

	public HediffCompProperties_InvisibilityWithEnemyAwareness()
	{
		compClass = typeof(HediffComp_InvisibilityWithEnemyAwareness);
	}
}
