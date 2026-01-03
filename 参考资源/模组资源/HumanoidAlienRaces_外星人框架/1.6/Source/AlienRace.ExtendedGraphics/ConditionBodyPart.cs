using System.Collections.Generic;
using System.Xml;
using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionBodyPart : Condition
{
	public new const string XmlNameParseKey = "BodyPart";

	public BodyPartDef bodyPart;

	public string bodyPartLabel;

	public bool drawWithoutPart;

	public override bool Static => true;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		data.bodyPart = bodyPart;
		data.bodyPartLabel = bodyPartLabel;
		if (!drawWithoutPart)
		{
			return pawn.HasNamedBodyPart(bodyPart, bodyPartLabel);
		}
		return true;
	}

	public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		Utilities.SetInstanceVariablesFromChildNodesOf(xmlRoot, this, new HashSet<string>());
	}
}
