using RimWorld;
using Verse;

namespace NCL;

public class HediffCompProperties_ForceBodyType : HediffCompProperties
{
	public BodyTypeDef bodyType;

	public HediffCompProperties_ForceBodyType()
	{
		compClass = typeof(HediffComp_ForceBodyType);
	}
}
