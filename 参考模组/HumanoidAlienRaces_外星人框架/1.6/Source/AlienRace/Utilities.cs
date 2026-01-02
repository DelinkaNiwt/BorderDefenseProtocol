using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using AlienRace.ExtendedGraphics;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AlienRace;

public static class Utilities
{
	public class BodyAddonGene : DefModExtension
	{
		public AlienPartGenerator.BodyAddon addon;

		public List<AlienPartGenerator.BodyAddon> addons;
	}

	private static List<AlienPartGenerator.BodyAddon> universalBodyAddons;

	public static List<AlienPartGenerator.BodyAddon> UniversalBodyAddons
	{
		get
		{
			if (universalBodyAddons == null)
			{
				universalBodyAddons = DefDatabase<RaceSettings>.AllDefsListForReading.SelectMany((RaceSettings rs) => rs.universalBodyAddons).ToList();
				universalBodyAddons.GeneBodyAddonPatcher();
				foreach (AlienPartGenerator.BodyAddon bodyAddon in universalBodyAddons)
				{
					AlienPartGenerator.DirectionalOffset offsets = bodyAddon.offsets;
					if (offsets.west == null)
					{
						offsets.west = bodyAddon.offsets.east;
					}
				}
				DefaultGraphicsLoader graphicsLoader = new DefaultGraphicsLoader();
				graphicsLoader.LoadAllGraphics("Universal Addons", universalBodyAddons.Cast<AlienPartGenerator.ExtendedGraphicTop>().ToArray());
			}
			return universalBodyAddons;
		}
	}

	public static bool IsGenderApplicable(this GenderPossibility possibility, Gender gender)
	{
		return possibility switch
		{
			GenderPossibility.Either => true, 
			GenderPossibility.Male => gender == Gender.Male, 
			GenderPossibility.Female => gender == Gender.Female, 
			_ => false, 
		};
	}

	public static bool DifferentRace(ThingDef one, ThingDef two)
	{
		if (one != two && one != null && two != null && one.race.Humanlike && two.race.Humanlike && (!(one is ThingDef_AlienRace oneAr) || !oneAr.alienRace.generalSettings.notXenophobistTowards.Contains(two)))
		{
			if (two is ThingDef_AlienRace twoAr)
			{
				return !twoAr.alienRace.generalSettings.immuneToXenophobia;
			}
			return true;
		}
		return false;
	}

	public static void SetInstanceVariablesFromChildNodesOf(XmlNode xmlRootNode, object wanter, HashSet<string> excludedFieldNames)
	{
		Traverse traverse = Traverse.Create(wanter);
		foreach (XmlNode xmlNode in xmlRootNode.ChildNodes)
		{
			if (!excludedFieldNames.Contains(xmlNode.Name))
			{
				SetFieldFromXmlNode(traverse, xmlNode, wanter, xmlNode.Name);
			}
		}
	}

	public static void SetFieldFromXmlNode(Traverse traverse, XmlNode xmlNode, object wanter, string fieldName)
	{
		Traverse field = traverse.Field(fieldName);
		if (!field.FieldExists())
		{
			if (fieldName != "#text")
			{
				Log.Error($"field {fieldName} for {wanter} doesn't exist\n{xmlNode.OuterXml}\n{xmlNode.ParentNode?.OuterXml}\n\n{xmlNode.ParentNode?.ParentNode?.OuterXml}");
			}
		}
		else
		{
			Type valueType = field.GetValueType();
			XmlAttribute xmlAttribute = xmlNode.Attributes["Class"];
			if (xmlAttribute != null)
			{
				Type typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(xmlAttribute.Value, valueType.Namespace);
				valueType = typeInAnyAssembly ?? valueType;
			}
			SetFieldFromXmlNodeRaw(field, xmlNode, wanter, fieldName, valueType);
		}
	}

	public static void SetFieldFromXmlNodeRaw(Traverse field, XmlNode xmlNode, object wanter, string fieldName, Type valueType)
	{
		if (valueType.IsSubclassOf(typeof(Def)))
		{
			DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(wanter, fieldName, xmlNode.FirstChild.Value ?? xmlNode.Value ?? xmlNode.InnerText);
		}
		else
		{
			field.SetValue((valueType.IsGenericType || !ParseHelper.HandlesType(valueType)) ? DirectXmlToObject.GetObjectFromXmlMethod(valueType)(xmlNode, arg2: false) : ParseHelper.FromString(xmlNode.InnerXml.Trim(), valueType));
		}
	}

	public static void GeneBodyAddonPatcher(this List<AlienPartGenerator.BodyAddon> universal)
	{
		List<AlienPartGenerator.BodyAddon> geneAddons = new List<AlienPartGenerator.BodyAddon>();
		AlienPartGenerator partHandler = new AlienPartGenerator();
		partHandler.GenericOffsets();
		foreach (GeneDef gene in DefDatabase<GeneDef>.AllDefsListForReading)
		{
			if (!gene.HasModExtension<BodyAddonGene>())
			{
				continue;
			}
			BodyAddonGene har = gene.GetModExtension<BodyAddonGene>();
			har.addon.conditions.Add(new ConditionGene
			{
				gene = gene
			});
			har.addon.defaultOffsets = partHandler.offsetDefaults.Find((AlienPartGenerator.OffsetNamed on) => on.name == har.addon.defaultOffset).offsets;
			geneAddons.Add(har.addon);
			if (har.addons.NullOrEmpty())
			{
				continue;
			}
			foreach (AlienPartGenerator.BodyAddon addon in har.addons)
			{
				addon.defaultOffsets = partHandler.offsetDefaults.Find((AlienPartGenerator.OffsetNamed on) => on.name == addon.defaultOffset).offsets;
				addon.conditions.Add(new ConditionGene
				{
					gene = gene
				});
			}
			geneAddons.AddRange(har.addons);
		}
		universal.AddRange(geneAddons);
	}
}
