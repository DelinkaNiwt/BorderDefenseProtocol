using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompCombatPlatform : ThingComp
{
	public float floatOffset_xAxis = 0f;

	public float floatOffset_yAxis = 0f;

	public float randTime = Rand.Range(0f, 300f);

	public CompProperties_CombatPlatform Props => (CompProperties_CombatPlatform)props;

	public Pawn PawnOwner
	{
		get
		{
			if (!(parent is Apparel { Wearer: var wearer }))
			{
				if (parent is Pawn result)
				{
					return result;
				}
				return null;
			}
			return wearer;
		}
	}
}
