using System;
using System.Xml;
using VanillaPsycastsExpanded.Technomancer;
using Verse;

namespace VanillaPsycastsExpanded;

public class BackCompatibilityConverter_Constructs : BackCompatibilityConverter
{
	public override bool AppliesToVersion(int majorVer, int minorVer)
	{
		return true;
	}

	public override string BackCompatibleDefName(Type defType, string defName, bool forDefInjections = false, XmlNode node = null)
	{
		return null;
	}

	public override Type GetBackCompatibleType(Type baseType, string providedClassName, XmlNode node)
	{
		bool flag = baseType == typeof(Thing) && providedClassName == GenTypes.GetTypeNameWithoutIgnoredNamespaces(typeof(Pawn));
		if (flag)
		{
			string innerText = node["def"].InnerText;
			bool flag2 = ((innerText == "VPE_SteelConstruct" || innerText == "VPE_RockConstruct") ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return typeof(Pawn_Construct);
		}
		return null;
	}

	public override void PostExposeData(object obj)
	{
	}
}
