using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace AlienRace.ExtendedGraphics;

[StaticConstructorOnStartup]
public abstract class Condition
{
	public static Dictionary<string, string> XmlNameParseKeys;

	public const string XmlNameParseKey = "";

	public virtual bool Static => false;

	static Condition()
	{
		XmlNameParseKeys = new Dictionary<string, string>();
		foreach (Type type in typeof(Condition).AllSubclassesNonAbstract())
		{
			XmlNameParseKeys.Add(Traverse.Create(type).Field("XmlNameParseKey").GetValue<string>(), type.FullName);
		}
	}

	public abstract bool Satisfied(ExtendedGraphicsPawnWrapper pawn, ref ResolveData data);

	[UsedImplicitly]
	public virtual void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		if (xmlRoot.ChildNodes.Count == 1)
		{
			FieldInfo field = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public).First();
			Utilities.SetFieldFromXmlNodeRaw(Traverse.Create(this).Field(field.Name), xmlRoot, this, field.Name, field.FieldType);
		}
		else
		{
			Utilities.SetInstanceVariablesFromChildNodesOf(xmlRoot, this, new HashSet<string>());
		}
	}

	public static XmlNode CustomListLoader(XmlNode xmlNode)
	{
		foreach (XmlNode node in xmlNode.ChildNodes)
		{
			if (XmlNameParseKeys.TryGetValue(node.Name, out var classTag))
			{
				XmlAttribute attribute = xmlNode.OwnerDocument.CreateAttribute("Class");
				attribute.Value = classTag;
				node.Attributes.SetNamedItem(attribute);
			}
		}
		return xmlNode;
	}
}
