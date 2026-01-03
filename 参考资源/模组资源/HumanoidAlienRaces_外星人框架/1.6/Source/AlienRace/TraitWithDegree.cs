using System.Xml;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace AlienRace;

public class TraitWithDegree
{
	public TraitDef def;

	public int degree;

	[UsedImplicitly]
	public void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "def", xmlRoot?.FirstChild?.Value ?? xmlRoot?.Value ?? xmlRoot?.InnerText);
		int.TryParse(xmlRoot.Attributes?["Degree"]?.Value, out degree);
	}

	public override string ToString()
	{
		return string.Format("{0}: {1} | {2}", "TraitWithDegree", def?.defName, degree);
	}
}
