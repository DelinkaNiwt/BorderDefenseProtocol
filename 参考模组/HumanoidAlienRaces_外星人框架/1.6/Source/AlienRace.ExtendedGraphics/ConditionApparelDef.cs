using System.Linq;
using RimWorld;
using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionApparelDef : Condition
{
	public new const string XmlNameParseKey = "ApparelDef";

	public ThingDef apparel;

	public ThingDef stuff;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.GetWornApparel.Any((Apparel ap) => ap.def == apparel && (stuff == null || ap.Stuff == stuff));
	}
}
