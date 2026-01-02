using Verse;

namespace AncotLibrary;

public class HediffCompProperties_RemoveIfShieldDropped : HediffCompProperties
{
	public HediffCompProperties_RemoveIfShieldDropped()
	{
		compClass = typeof(HediffComp_RemoveIfApparelDropped);
	}
}
