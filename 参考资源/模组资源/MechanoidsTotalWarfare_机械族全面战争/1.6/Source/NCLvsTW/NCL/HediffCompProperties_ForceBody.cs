using RimWorld;
using Verse;

namespace NCL;

public class HediffCompProperties_ForceBody : HediffCompProperties
{
	public BodyTypeDef bodyType;

	public BodyDef bodyDef;

	public HediffCompProperties_ForceBody()
	{
		compClass = typeof(HediffComp_ForceBody);
	}
}
