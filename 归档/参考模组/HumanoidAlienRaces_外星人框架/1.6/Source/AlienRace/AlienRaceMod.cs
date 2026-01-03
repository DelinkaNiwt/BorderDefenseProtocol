using System;
using System.Collections.Generic;
using System.Xml;
using AlienRace.ExtendedGraphics;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AlienRace;

public class AlienRaceMod : Mod
{
	public static AlienRaceMod instance;

	public static AlienRaceSettings settings;

	public override string SettingsCategory()
	{
		return "Alien Race";
	}

	public AlienRaceMod(ModContentPack content)
		: base(content)
	{
		instance = this;
		settings = GetSettings<AlienRaceSettings>();
		XmlInheritance.allowDuplicateNodesFieldNames.Add("extendedGraphics");
		XmlInheritance.allowDuplicateNodesFieldNames.Add("conditions");
		foreach (Type type in typeof(ConditionLogicCollection).AllSubclassesNonAbstract())
		{
			XmlInheritance.allowDuplicateNodesFieldNames.Add(Traverse.Create(type).Field("XmlNameParseKey").GetValue<string>());
		}
		XmlDocument xmlDoc = new XmlDocument();
		xmlDoc.LoadXml("<extendedGraphics><li Class=\"" + typeof(AlienPartGenerator.ExtendedGraphicTop).FullName + "\"><path>here</path></li></extendedGraphics>");
		DirectXmlToObject.ObjectFromXml<List<AbstractExtendedGraphic>>(xmlDoc.DocumentElement, doPostLoad: false);
		Func<XmlNode, object> originalFunc = CachedData.listFromXmlMethods()[typeof(List<AbstractExtendedGraphic>)];
		CachedData.listFromXmlMethods()[typeof(List<AbstractExtendedGraphic>)] = (XmlNode node) => originalFunc(AbstractExtendedGraphic.CustomListLoader(node));
		xmlDoc = new XmlDocument();
		xmlDoc.LoadXml("<conditions></conditions>");
		DirectXmlToObject.ObjectFromXml<List<Condition>>(xmlDoc.DocumentElement, doPostLoad: false);
		Func<XmlNode, object> originalFunc2 = CachedData.listFromXmlMethods()[typeof(List<Condition>)];
		CachedData.listFromXmlMethods()[typeof(List<Condition>)] = (XmlNode node) => originalFunc2(Condition.CustomListLoader(node));
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		base.DoSettingsWindowContents(inRect);
		Listing_Standard listingStandard = new Listing_Standard();
		listingStandard.Begin(inRect);
		listingStandard.CheckboxLabeled("HAR.Options.TextureLoadingLogs_Label".Translate(), ref settings.textureLogs, "HAR.Options.TextureLoadingLogs_Tooltip".Translate());
		listingStandard.CheckboxLabeled("HAR.Options.RandomizeStartingPawns_Label".Translate(), ref settings.randomizeStartingPawnsOnReroll, "HAR.Options.RandomizeStartingPawns_Tooltip".Translate());
		listingStandard.End();
	}

	public override void WriteSettings()
	{
		base.WriteSettings();
		settings.UpdateSettings();
	}
}
