using System.Xml;
using Verse;

namespace AlienRace.ExtendedGraphics;

public class ConditionJob : Condition
{
	public new const string XmlNameParseKey = "Job";

	public JobDef job;

	public override bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data)
	{
		return pawn.CurJob?.def == job;
	}

	public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		string value = xmlRoot.FirstChild.Value;
		if (value.Trim() == "None")
		{
			job = null;
		}
		else
		{
			base.LoadDataFromXmlCustom(xmlRoot);
		}
	}
}
