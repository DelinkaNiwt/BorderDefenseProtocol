using System.Xml;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class NeedProperty
{
	public NeedDef needDef;

	public FloatRange interval;

	public void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		int count = xmlRoot.ChildNodes.Count;
		DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "needDef", xmlRoot.Name);
		if (count == 1)
		{
			LoadFromSingleNode(xmlRoot.FirstChild);
		}
	}

	private void LoadFromSingleNode(XmlNode node)
	{
		if (node is XmlText xmlText)
		{
			interval = ParseHelper.FromString<FloatRange>(xmlText.InnerText);
		}
	}
}
