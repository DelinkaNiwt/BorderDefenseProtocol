using System.Xml;
using HarmonyLib;

namespace AlienRace.ExtendedGraphics;

public abstract class ConditionLogicSingle : Condition
{
	public Condition condition;

	public override bool Static => condition.Static;

	public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		Utilities.SetFieldFromXmlNode(Traverse.Create(this), Condition.CustomListLoader(xmlRoot).FirstChild, this, "condition");
	}
}
