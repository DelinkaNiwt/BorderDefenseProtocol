using System.Collections.Generic;
using System.Linq;
using System.Xml;
using RimWorld;
using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionApparel : Condition
{
	public new const string XmlNameParseKey = "Apparel";

	public List<BodyPartGroupDef> hiddenUnderApparelFor = new List<BodyPartGroupDef>();

	public List<string> hiddenUnderApparelTag = new List<string>();

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		if ((data.head || AlienRenderTreePatches.IsPortrait(pawn.WrappedPawn) || pawn.VisibleInBed()) && (!data.head || !AlienRenderTreePatches.IsPortrait(pawn.WrappedPawn) || !Prefs.HatsOnlyOnMap) && (!hiddenUnderApparelTag.NullOrEmpty() || !hiddenUnderApparelFor.NullOrEmpty()))
		{
			return !pawn.GetWornApparelProps().Any((ApparelProperties ap) => ap.bodyPartGroups.Any((BodyPartGroupDef bpgd) => hiddenUnderApparelFor.Contains(bpgd)) || ap.tags.Any((string s) => hiddenUnderApparelTag.Contains(s)));
		}
		return true;
	}

	public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		Utilities.SetInstanceVariablesFromChildNodesOf(xmlRoot, this, new HashSet<string>());
	}
}
