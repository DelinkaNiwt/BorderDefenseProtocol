using System.Collections.Generic;
using System.Xml;
using Verse;

namespace AlienRace.ExtendedGraphics;

public abstract class ConditionLogicCollection : Condition
{
	public List<Condition> conditions = new List<Condition>();

	public override bool Static => conditions.TrueForAll((Condition cd) => cd.Static);

	public override void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		conditions = DirectXmlToObject.ObjectFromXml<List<Condition>>(xmlRoot, doPostLoad: true);
	}
}
