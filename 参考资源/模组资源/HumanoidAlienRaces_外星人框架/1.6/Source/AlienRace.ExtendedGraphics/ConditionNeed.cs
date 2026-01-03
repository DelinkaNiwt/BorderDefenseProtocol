using System.Collections.Generic;
using System.Xml;
using RimWorld;

namespace AlienRace.ExtendedGraphics;

public class ConditionNeed : Condition
{
	public new const string XmlNameParseKey = "Need";

	public NeedDef need;

	public float level;

	public bool percentage;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.GetNeed(need, percentage) > level;
	}

	public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		Utilities.SetInstanceVariablesFromChildNodesOf(xmlRoot, this, new HashSet<string>());
	}
}
