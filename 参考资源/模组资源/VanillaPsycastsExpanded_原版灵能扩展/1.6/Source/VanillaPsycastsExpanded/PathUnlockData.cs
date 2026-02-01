using System.Xml;
using Verse;

namespace VanillaPsycastsExpanded;

public class PathUnlockData
{
	public PsycasterPathDef path;

	public IntRange unlockedAbilityLevelRange = IntRange.One;

	public IntRange unlockedAbilityCount = IntRange.Zero;

	public void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		if (xmlRoot.ChildNodes.Count != 1)
		{
			Log.Error("Misconfigured UnlockedPath: " + xmlRoot.OuterXml);
			return;
		}
		DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "path", xmlRoot.Name);
		string[] array = xmlRoot.FirstChild.Value.Split('|');
		unlockedAbilityLevelRange = ParseHelper.FromString<IntRange>(array[0]);
		unlockedAbilityCount = ParseHelper.FromString<IntRange>(array[1]);
	}
}
