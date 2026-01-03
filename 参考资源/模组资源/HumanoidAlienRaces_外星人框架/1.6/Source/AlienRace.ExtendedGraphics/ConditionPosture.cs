using System.Collections.Generic;
using System.Xml;
using RimWorld;

namespace AlienRace.ExtendedGraphics;

public class ConditionPosture : Condition
{
	public new const string XmlNameParseKey = "Posture";

	private bool drawnStanding;

	private bool drawnLaying;

	private bool drawnInBed;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		if (pawn.GetPosture() != PawnPosture.Standing || !drawnStanding)
		{
			if (pawn.GetPosture() != PawnPosture.Standing && drawnLaying)
			{
				if (!pawn.VisibleInBed())
				{
					return drawnInBed;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		Utilities.SetInstanceVariablesFromChildNodesOf(xmlRoot, this, new HashSet<string>());
	}
}
