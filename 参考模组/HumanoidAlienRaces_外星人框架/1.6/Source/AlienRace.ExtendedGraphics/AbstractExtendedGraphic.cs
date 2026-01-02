using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using Verse;

namespace AlienRace.ExtendedGraphics;

public abstract class AbstractExtendedGraphic : IExtendedGraphic
{
	public string path;

	public List<string> paths = new List<string>();

	public List<string> pathsFallback = new List<string>();

	public bool usingFallback;

	public int variantCount;

	public List<int> variantCounts = new List<int>();

	[LoadAlias("hediffGraphics")]
	[LoadAlias("backstoryGraphics")]
	[LoadAlias("ageGraphics")]
	[LoadAlias("damageGraphics")]
	[LoadAlias("genderGraphics")]
	[LoadAlias("traitGraphics")]
	[LoadAlias("bodytypeGraphics")]
	[LoadAlias("headtypeGraphics")]
	[LoadAlias("geneGraphics")]
	[LoadAlias("raceGraphics")]
	[XmlInheritanceAllowDuplicateNodes]
	public List<AbstractExtendedGraphic> extendedGraphics = new List<AbstractExtendedGraphic>();

	private static readonly Dictionary<string, string> XML_CLASS_DICTIONARY = new Dictionary<string, string>
	{
		{ "hediffGraphics", "Hediff" },
		{ "backstoryGraphics", "Backstory" },
		{ "ageGraphics", "Age" },
		{ "damageGraphics", "Damage" },
		{ "genderGraphics", "Gender" },
		{ "traitGraphics", "Trait" },
		{ "bodytypeGraphics", "BodyType" },
		{ "headtypeGraphics", "HeadType" },
		{ "geneGraphics", "Gene" },
		{ "raceGraphics", "Race" }
	};

	public void Init()
	{
		if (paths.NullOrEmpty() && !path.NullOrEmpty())
		{
			paths.Add(path);
		}
		if (!paths.NullOrEmpty() && path.NullOrEmpty())
		{
			path = paths[0];
		}
		for (int i = 0; i < paths.Count; i++)
		{
			variantCounts.Add(0);
		}
	}

	public string GetPath()
	{
		if (GetPathCount() <= 0)
		{
			return path;
		}
		return GetPath(0);
	}

	public string GetPath(int index)
	{
		if (usingFallback)
		{
			return pathsFallback[index];
		}
		return paths[index];
	}

	public int GetPathCount()
	{
		if (usingFallback)
		{
			return pathsFallback.Count;
		}
		return paths.Count;
	}

	public bool UseFallback()
	{
		usingFallback = true;
		variantCounts.Clear();
		for (int i = 0; i < pathsFallback.Count; i++)
		{
			variantCounts.Add(0);
		}
		return pathsFallback.Any();
	}

	public string GetPathFromVariant(ref int variantIndex, out bool zero)
	{
		zero = true;
		for (int index = 0; index < variantCounts.Count; index++)
		{
			int count = variantCounts[index];
			if (variantIndex < count)
			{
				zero = variantIndex == 0;
				return GetPath(index);
			}
			variantIndex -= count;
		}
		return path;
	}

	public int GetVariantCount()
	{
		return variantCount;
	}

	public int GetVariantCount(int index)
	{
		return variantCounts[index];
	}

	public int IncrementVariantCount()
	{
		return IncrementVariantCount(0);
	}

	public int IncrementVariantCount(int index)
	{
		variantCount++;
		return variantCounts[index]++;
	}

	public abstract bool IsApplicable(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data);

	public virtual IEnumerable<IExtendedGraphic> GetSubGraphics(ExtendedGraphicsPawnWrapper pawn, ResolveData data)
	{
		return GetSubGraphics();
	}

	public virtual IEnumerable<IExtendedGraphic> GetSubGraphics()
	{
		return extendedGraphics;
	}

	[UsedImplicitly]
	public virtual void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		foreach (XmlNode childNode in xmlRoot.ChildNodes)
		{
			if (!XML_CLASS_DICTIONARY.TryGetValue(childNode.Name, out var classTag))
			{
				continue;
			}
			foreach (XmlNode graphicNode in childNode.ChildNodes)
			{
				XmlAttribute attribute2 = xmlRoot.OwnerDocument.CreateAttribute("For");
				attribute2.Value = graphicNode.Name;
				graphicNode.Attributes.SetNamedItem(attribute2);
				CachedData.xmlElementName(graphicNode as XmlElement) = CachedData.xmlDocumentAddName(childNode.OwnerDocument, string.Empty, classTag, string.Empty, null);
			}
			CachedData.xmlElementName(childNode as XmlElement) = CachedData.xmlDocumentAddName(childNode.OwnerDocument, string.Empty, "extendedGraphics", string.Empty, null);
		}
		SetInstanceVariablesFromChildNodesOf(xmlRoot);
	}

	public static XmlNode CustomListLoader(XmlNode xmlNode)
	{
		foreach (XmlNode graphicNode in xmlNode.ChildNodes)
		{
			if (graphicNode.Attributes["Class"] == null)
			{
				XmlAttribute attribute = xmlNode.OwnerDocument.CreateAttribute("Class");
				attribute.Value = typeof(AlienPartGenerator.ExtendedConditionGraphic).FullName;
				graphicNode.Attributes.SetNamedItem(attribute);
			}
		}
		return xmlNode;
	}

	protected virtual void SetInstanceVariablesFromChildNodesOf(XmlNode xmlRootNode)
	{
		Utilities.SetInstanceVariablesFromChildNodesOf(xmlRootNode, this, new HashSet<string>());
		if (path.NullOrEmpty())
		{
			path = xmlRootNode.FirstChild.Value?.Trim();
		}
	}
}
