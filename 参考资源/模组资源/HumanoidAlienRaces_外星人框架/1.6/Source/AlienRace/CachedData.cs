using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlienRace;

[StaticConstructorOnStartup]
public static class CachedData
{
	[StaticConstructorOnStartup]
	public static class Textures
	{
		public static readonly Texture2D AlienIconInactive = ContentFinder<Texture2D>.Get("AlienRace/UI/AlienIconInactive");

		public static readonly Texture2D AlienIconActive = ContentFinder<Texture2D>.Get("AlienRace/UI/AlienIconActive");
	}

	public delegate void FromOutfitStandDel(Building_OutfitStand outfitStand);

	public delegate bool BoolFromPawnKindDefDel(PawnKindDef kindDef);

	public delegate void FromPawnAndPawnRequestRefDel(Pawn pawn, ref PawnGenerationRequest request);

	public delegate void FoodUtilityAddThoughtsFromIdeo(HistoryEventDef eventDef, Pawn ingester, ThingDef foodDef, MeatSourceCategory meatSourceCategory);

	public delegate Graphic_Multi GraphicMultiFromGraphicRequest(GraphicRequest req);

	public delegate void FromPawnDel(Pawn pawn);

	public delegate void RenderTreeAddChild(PawnRenderTree tree, PawnRenderNode child, PawnRenderNode parent);

	public delegate IXmlSchemaInfo XmlDocumentAddName(XmlDocument document, string prefix, string localName, string namespaceURI, IXmlSchemaInfo schemaInfo);

	private static readonly Dictionary<RaceProperties, ThingDef> racePropsToRaceDict = new Dictionary<RaceProperties, ThingDef>();

	private static readonly Dictionary<ApparelProperties, ThingDef> apparelPropsToApparelDict = new Dictionary<ApparelProperties, ThingDef>();

	public static readonly FromOutfitStandDel outfitStandRecacheGraphics = AccessTools.MethodDelegate<FromOutfitStandDel>(AccessTools.Method(typeof(Building_OutfitStand), "RecacheGraphics"));

	public static readonly AccessTools.FieldRef<Graphic_Multi> outfitStandBodyGraphic = AccessTools.StaticFieldRefAccess<Graphic_Multi>(AccessTools.Field(typeof(Building_OutfitStand), "bodyGraphic"));

	public static readonly AccessTools.FieldRef<Graphic_Multi> outfitStandHeadGraphic = AccessTools.StaticFieldRefAccess<Graphic_Multi>(AccessTools.Field(typeof(Building_OutfitStand), "headGraphic"));

	public static readonly AccessTools.FieldRef<Vector2> outfitStandDrawSizeBody = AccessTools.StaticFieldRefAccess<Vector2>(AccessTools.Field(typeof(Building_OutfitStand), "bodyDrawSize"));

	public static readonly AccessTools.FieldRef<Vector2> outfitStandDrawSizeHead = AccessTools.StaticFieldRefAccess<Vector2>(AccessTools.Field(typeof(Building_OutfitStand), "headDrawSize"));

	public static readonly BoolFromPawnKindDefDel canBeChild = AccessTools.MethodDelegate<BoolFromPawnKindDefDel>(AccessTools.Method(typeof(ScenarioUtility), "CanBeChild"));

	public static readonly AccessTools.FieldRef<List<ThingStuffPair>> allApparelPairs = AccessTools.StaticFieldRefAccess<List<ThingStuffPair>>(AccessTools.Field(typeof(PawnApparelGenerator), "allApparelPairs"));

	public static readonly AccessTools.FieldRef<List<ThingStuffPair>> allWeaponPairs = AccessTools.StaticFieldRefAccess<List<ThingStuffPair>>(AccessTools.Field(typeof(PawnWeaponGenerator), "allWeaponPairs"));

	public static readonly FromPawnAndPawnRequestRefDel generatePawnsRelations = AccessTools.MethodDelegate<FromPawnAndPawnRequestRefDel>(AccessTools.Method(typeof(PawnGenerator), "GeneratePawnRelations"));

	public static readonly FoodUtilityAddThoughtsFromIdeo foodUtilityAddThoughtsFromIdeo = AccessTools.MethodDelegate<FoodUtilityAddThoughtsFromIdeo>(AccessTools.Method(typeof(FoodUtility), "AddThoughtsFromIdeo"));

	public static readonly AccessTools.FieldRef<PawnTextureAtlas, Dictionary<Pawn, PawnTextureAtlasFrameSet>> pawnTextureAtlasFrameAssignments = AccessTools.FieldRefAccess<PawnTextureAtlas, Dictionary<Pawn, PawnTextureAtlasFrameSet>>("frameAssignments");

	public static readonly AccessTools.FieldRef<List<FoodUtility.ThoughtFromIngesting>> ingestThoughts = AccessTools.StaticFieldRefAccess<List<FoodUtility.ThoughtFromIngesting>>(AccessTools.Field(typeof(FoodUtility), "ingestThoughts"));

	public static readonly AccessTools.FieldRef<Pawn_StoryTracker, Color> hairColor = AccessTools.FieldRefAccess<Pawn_StoryTracker, Color>(AccessTools.Field(typeof(Pawn_StoryTracker), "hairColor"));

	public static readonly AccessTools.FieldRef<Pawn_AgeTracker, Pawn> ageTrackerPawn = AccessTools.FieldRefAccess<Pawn_AgeTracker, Pawn>(AccessTools.Field(typeof(Pawn_AgeTracker), "pawn"));

	private static List<HeadTypeDef> defaultHeadTypeDefs;

	public static readonly GraphicMultiFromGraphicRequest getInnerGraphic = AccessTools.MethodDelegate<GraphicMultiFromGraphicRequest>(AccessTools.Method(typeof(GraphicDatabase), "GetInner", new Type[1] { typeof(GraphicRequest) }, new Type[1] { typeof(Graphic_Multi) }));

	public static readonly FromPawnDel generateStartingPossessions = AccessTools.MethodDelegate<FromPawnDel>(AccessTools.Method(typeof(StartingPawnUtility), "GeneratePossessions"));

	public static readonly AccessTools.FieldRef<Pawn_StoryTracker, Color?> skinColorBase = AccessTools.FieldRefAccess<Pawn_StoryTracker, Color?>(AccessTools.Field(typeof(Pawn_StoryTracker), "skinColorBase"));

	public static readonly Action<Dialog_StylingStation, Rect> drawTabs = AccessTools.MethodDelegate<Action<Dialog_StylingStation, Rect>>(AccessTools.Method(typeof(Dialog_StylingStation), "DrawTabs"));

	public static readonly AccessTools.FieldRef<Dialog_StylingStation, Pawn> stationPawn = AccessTools.FieldRefAccess<Dialog_StylingStation, Pawn>("pawn");

	public static readonly AccessTools.FieldRef<Dialog_StylingStation, Color> stationDesiredHairColor = AccessTools.FieldRefAccess<Dialog_StylingStation, Color>("desiredHairColor");

	public static readonly AccessTools.FieldRef<Dialog_StylingStation, Dialog_StylingStation.StylingTab> stationCurTab = AccessTools.FieldRefAccess<Dialog_StylingStation, Dialog_StylingStation.StylingTab>("curTab");

	public static readonly AccessTools.FieldRef<object, bool> statPartAgeUseBiologicalYearsField = AccessTools.FieldRefAccess<bool>(typeof(StatPart_Age), "useBiologicalYears");

	public static readonly AccessTools.FieldRef<object, SimpleCurve> statPartAgeCurveField = AccessTools.FieldRefAccess<SimpleCurve>(typeof(StatPart_Age), "curve");

	public static readonly RenderTreeAddChild renderTreeAddChild = AccessTools.MethodDelegate<RenderTreeAddChild>(AccessTools.Method(typeof(PawnRenderTree), "AddChild"));

	public static readonly AccessTools.FieldRef<XmlElement, IXmlSchemaInfo> xmlElementName = AccessTools.FieldRefAccess<IXmlSchemaInfo>(typeof(XmlElement), "name");

	public static readonly XmlDocumentAddName xmlDocumentAddName = AccessTools.MethodDelegate<XmlDocumentAddName>(AccessTools.Method(typeof(XmlDocument), "AddXmlName"));

	public static readonly AccessTools.FieldRef<Dictionary<Type, Func<XmlNode, object>>> listFromXmlMethods = AccessTools.StaticFieldRefAccess<Dictionary<Type, Func<XmlNode, object>>>(AccessTools.Field(typeof(DirectXmlToObject), "listFromXmlMethods"));

	public static readonly AccessTools.FieldRef<PawnRenderTree, PawnDrawParms> oldDrawParms = AccessTools.FieldRefAccess<PawnDrawParms>(typeof(PawnRenderTree), "oldParms");

	public static List<HeadTypeDef> DefaultHeadTypeDefs
	{
		get
		{
			if (!defaultHeadTypeDefs.NullOrEmpty())
			{
				return defaultHeadTypeDefs;
			}
			return DefaultHeadTypeDefs = DefDatabase<HeadTypeDef>.AllDefsListForReading.Where((HeadTypeDef hd) => Regex.IsMatch(hd.defName, "(?>Male|Female)_(?>Average|Narrow)(?>Normal|Wide|Pointy)")).ToList();
		}
		set
		{
			defaultHeadTypeDefs = value;
		}
	}

	public static ThingDef GetRaceFromRaceProps(RaceProperties props)
	{
		if (!racePropsToRaceDict.ContainsKey(props))
		{
			racePropsToRaceDict.Add(props, new List<ThingDef>(DefDatabase<ThingDef>.AllDefsListForReading).Concat(new List<ThingDef_AlienRace>(DefDatabase<ThingDef_AlienRace>.AllDefsListForReading)).First((ThingDef td) => td.race == props));
		}
		return racePropsToRaceDict[props];
	}

	public static ThingDef GetApparelFromApparelProps(ApparelProperties props)
	{
		if (!apparelPropsToApparelDict.ContainsKey(props))
		{
			apparelPropsToApparelDict.Add(props, DefDatabase<ThingDef>.AllDefsListForReading.First((ThingDef td) => td.apparel == props));
		}
		return apparelPropsToApparelDict[props];
	}
}
