using System.Xml;
using RimWorld;
using Verse;

namespace AlienRace;

public class StatPartAgeOverride
{
	public StatDef stat;

	public StatPart_Age overridePart;

	public void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "stat", xmlRoot.Name);
		overridePart = DirectXmlToObject.ObjectFromXml<StatPart_Age>(xmlRoot, doPostLoad: false);
	}
}
