using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

public static class AlienRaceTool
{
	private const string CO_PATH = "path";

	private const string CO_VARIANTCOUNTMAX = "variantCountMax";

	private const string CO_DRAWFORFEMALE = "drawForFemale";

	private const string CO_DRAWFORMALE = "drawForMale";

	private const string CO_DRAWSIZE = "drawSize";

	private const string CO_ANGLE = "angle";

	private const string CO_OFFSETS = "offsets";

	private const string CO_SOUTH = "south";

	private const string CO_NORTH = "north";

	private const string CO_EAST = "east";

	private const string CO_WEST = "west";

	private const string CO_LAYEROFFSET = "layerOffset";

	private const string CO_GENERALSETTINGS = "generalSettings";

	private const string CO_TOARRAY = "ToArray";

	private const string CO_ALIENPARTGENERATOR = "alienPartGenerator";

	private const string CO_CUSTOMDRAWSIZE = "customDrawSize";

	private const string CO_HEADTYPES = "headTypes";

	private const string CO_BODYTYPES = "bodyTypes";

	private const string CO_CLEAR = "Clear";

	private const string CO_BODYADDONS = "bodyAddons";

	private const string CO_ADDONGRAPHICS = "addonGraphics";

	private const string CO_ADDONVARIANTS = "addonVariants";

	private const string CO_ALIENRACE = "alienRace";

	private const string CO_FIRST = "first";

	private const string CO_GETCHANNEL = "GetChannel";

	private const string CO_SECOND = "second";

	private const string CO_SKIN = "skin";

	private const string CO_HAIR = "hair";

	private const string CO_TYPEALIENCOMP = "AlienComp";

	private const string CO_HUMANOIDALIENRACE = "AlienRace";

	internal static string BodyAddon_GetPath(object bodyAddon)
	{
		return (bodyAddon != null) ? bodyAddon.GetMemberValue("path", "") : "";
	}

	internal static int BodyAddon_GetVariantCountMax(object bodyAddon)
	{
		return bodyAddon?.GetMemberValue("variantCountMax", 0) ?? 0;
	}

	internal static bool BodyAddon_GetDrawForFemale(object bodyAddon)
	{
		return bodyAddon?.GetMemberValue("drawForFemale", fallback: false) ?? false;
	}

	internal static bool BodyAddon_GetDrawForMale(object bodyAddon)
	{
		return bodyAddon?.GetMemberValue("drawForMale", fallback: false) ?? false;
	}

	internal static Vector2 BodyAddon_GetDrawSize(object bodyAddon)
	{
		return bodyAddon?.GetMemberValue("drawSize", Vector2.one) ?? Vector2.one;
	}

	internal static float BodyAddon_GetRotation(object bodyAddon)
	{
		return bodyAddon?.GetMemberValue("angle", 0f) ?? 0f;
	}

	internal static object BodyAddon_GetOffsets(object bodyAddon)
	{
		return bodyAddon?.GetMemberValue<object>("offsets", null);
	}

	internal static object BodyAddon_GetRoationOffsetSouth(object bodyAddon)
	{
		return BodyAddon_GetOffsets(bodyAddon).GetMemberValue<object>("south", null);
	}

	internal static object BodyAddon_GetRoationOffsetNorth(object bodyAddon)
	{
		return BodyAddon_GetOffsets(bodyAddon).GetMemberValue<object>("north", null);
	}

	internal static object BodyAddon_GetRoationOffsetEast(object bodyAddon)
	{
		return BodyAddon_GetOffsets(bodyAddon).GetMemberValue<object>("east", null);
	}

	internal static object BodyAddon_GetRoationOffsetWest(object bodyAddon)
	{
		return BodyAddon_GetOffsets(bodyAddon).GetMemberValue<object>("west", null);
	}

	internal static float BodyAddon_GetLayerOffset(object bodyAddonRotationOffset)
	{
		return bodyAddonRotationOffset?.GetMemberValue("layerOffset", 0f) ?? 0f;
	}

	internal static void AlienPartGenerator_BodyAddon_Toggle_DrawFor(this Pawn p, int index, bool female)
	{
		object obj = p.AlienPartGenerator_GetBodyAddonAtIndex(index);
		bool memberValue = obj.GetMemberValue(female ? "drawForFemale" : "drawForMale", fallback: false);
		memberValue = !memberValue;
		obj.SetMemberValue(female ? "drawForFemale" : "drawForMale", memberValue);
	}

	internal static void BodyAddon_SetDrawForFemale(object bodyAddon, bool val)
	{
		bodyAddon.SetMemberValue("drawForFemale", val);
	}

	internal static void BodyAddon_SetDrawForMale(object bodyAddon, bool val)
	{
		bodyAddon.SetMemberValue("drawForMale", val);
	}

	internal static void BodyAddon_SetDrawSize(object bodyAddon, float val)
	{
		bodyAddon.SetMemberValue("drawSize", new Vector2(val, val));
	}

	internal static void BodyAddon_SetRotation(object bodyAddon, float val)
	{
		bodyAddon.SetMemberValue("angle", val);
	}

	internal static void BodyAddon_SetLayerOffset(object bodyAddonRotationOffset, float val)
	{
		bodyAddonRotationOffset.SetMemberValue("layerOffset", val);
	}

	internal static bool IsAlienRace(this Pawn pawn)
	{
		return CEditor.IsAlienRaceActive && pawn != null && pawn.def != null && pawn.def.GetType().ToString().StartsWith("AlienRace");
	}

	internal static object AlienRace(this Pawn pawn)
	{
		return pawn.IsAlienRace() ? pawn.def.GetMemberValue<object>("alienRace", null) : null;
	}

	internal static object AlienRace_GetGeneralSettings(this Pawn pawn)
	{
		return pawn.AlienRace().GetMemberValue<object>("generalSettings", null);
	}

	internal static object AlienPartGenerator_GetBodyAddons(this Pawn p)
	{
		return p.AlienPartGenerator().GetMemberValue<object>("bodyAddons", null);
	}

	internal static object[] AlienPartGenerator_GetBodyAddonsAsArray(this Pawn p)
	{
		return (object[])p.AlienPartGenerator_GetBodyAddons().CallMethod("ToArray", null);
	}

	internal static object AlienPartGenerator_GetBodyAddonAtIndex(this Pawn p, int index)
	{
		object[] array = p.AlienPartGenerator_GetBodyAddonsAsArray();
		if (array == null || array.Length < index)
		{
			return null;
		}
		return array[index];
	}

	internal static object AlienPartGenerator(this Pawn pawn)
	{
		return pawn.AlienRace_GetGeneralSettings().GetMemberValue<object>("alienPartGenerator", null);
	}

	internal static Vector2 AlienPartGenerator_GetCustomDrawSize(this Pawn p)
	{
		return p.AlienPartGenerator().GetMemberValue("customDrawSize", Vector2.one);
	}

	internal static List<HeadTypeDef> AlienPartGenerator_GetHeadTypes(this Pawn p)
	{
		return p.AlienPartGenerator().GetMemberValue<List<HeadTypeDef>>("headTypes", null);
	}

	internal static List<BodyTypeDef> AlienPartGenerator_GetBodyTypes(this Pawn p)
	{
		return p.AlienPartGenerator().GetMemberValue<List<BodyTypeDef>>("bodyTypes", null);
	}

	internal static void AlienPartGenerator_SetCustomDrawSize(this Pawn p, Vector2 val)
	{
		p.AlienPartGenerator().SetMemberValue("customDrawSize", val);
	}

	internal static void AlienPartGenerator_DeleteAllAddons(this Pawn p)
	{
		object obj = p.AlienPartGenerator_GetBodyAddons();
		obj.CallMethod("Clear", null);
		p.AlienPartGenerator().SetMemberValue("bodyAddons", obj);
	}

	internal static ThingComp AlienRace_GetAlienRaceComp(this Pawn pawn)
	{
		object obj = pawn.AlienRace();
		if (obj == null)
		{
			return null;
		}
		foreach (ThingComp comp in pawn.GetComps<ThingComp>())
		{
			if (comp.GetType().Namespace.EqualsIgnoreCase("AlienRace") && comp.GetType().Name.EndsWith("AlienComp"))
			{
				return comp;
			}
		}
		return null;
	}

	internal static Vector2 AlienRaceComp_GetCustomDrawSize(this Pawn p)
	{
		return p.AlienRace_GetAlienRaceComp().GetMemberValue("customDrawSize", Vector2.one);
	}

	internal static object AlienRaceComp_GetChannel(this Pawn p, string channelName)
	{
		return p.AlienRace_GetAlienRaceComp().CallMethod("GetChannel", new object[1] { channelName });
	}

	internal static Color AlienRaceComp_GetChannelColor(this Pawn p, string channelName, bool primary)
	{
		return p.AlienRaceComp_GetChannel(channelName).GetMemberValue(primary ? "first" : "second", Color.white);
	}

	internal static Color AlienRaceComp_GetSkinColor(this Pawn p, bool primary)
	{
		return p.AlienRaceComp_GetChannelColor("skin", primary);
	}

	internal static List<Graphic> AlienRaceComp_GetAddonGraphics(this Pawn p)
	{
		return p.AlienRace_GetAlienRaceComp().GetMemberValue<List<Graphic>>("addonGraphics", null);
	}

	internal static List<int> AlienRaceComp_GetAddonVariants(this Pawn p)
	{
		return p.AlienRace_GetAlienRaceComp().GetMemberValue<List<int>>("addonVariants", null);
	}

	internal static void AlienRaceComp_SetCustomDrawSize(this Pawn p, Vector2 val)
	{
		p.AlienRace_GetAlienRaceComp().SetMemberValue("customDrawSize", val);
	}

	internal static void AlienRaceComp_SetChannelColor(this Pawn p, string channelName, bool primary, Color val)
	{
		p.AlienRaceComp_GetChannel(channelName).SetMemberValue(primary ? "first" : "second", val);
	}

	internal static void AlienRaceComp_SetSkinColor(this Pawn p, bool primary, Color val)
	{
		p.AlienRaceComp_SetChannelColor("skin", primary, val);
	}

	internal static void AlienRaceComp_SetHairColor(this Pawn p, bool primary, Color val)
	{
		p.AlienRaceComp_SetChannelColor("hair", primary, val);
	}

	internal static void AlienRaceComp_SetAddonVariants(this Pawn p, List<int> l)
	{
		p.AlienRace_GetAlienRaceComp().SetMemberValue("addonVariants", l);
	}

	internal static void AlienRaceComp_ClearAllAddons(this Pawn p)
	{
		p.AlienRace_GetAlienRaceComp().SetMemberValue("addonVariants", new List<int>());
		p.AlienRace_GetAlienRaceComp().SetMemberValue("addonGraphics", new List<Graphic>());
	}
}
