using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal static class ApparelTool
{
	internal const string CO_WORNAPPAREL = "wornApparel";

	internal const string CO_COMPS = "comps";

	internal static bool IsApparelForNeck(this ThingDef a)
	{
		return a != null && a.apparel.layers != null && a.apparel.layers.Contains(ApparelLayerDefOf.Overhead) && a.HasBodyPartGroupDefName("Neck");
	}

	internal static bool IsForNeck(this Apparel a)
	{
		return a != null && a.def.apparel.layers != null && a.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead) && a.def.HasBodyPartGroupDefName("Neck");
	}

	internal static bool IsForLegs(this Apparel a)
	{
		return a != null && !a.def.apparel.bodyPartGroups.NullOrEmpty() && a.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs);
	}

	internal static bool IsForEyes(this Apparel a)
	{
		return a != null && a.def.apparel.layers != null && a.def.apparel.layers.Contains(ApparelLayerDefOf.EyeCover);
	}

	internal static bool HasAnyApparel(this Pawn pawn)
	{
		return pawn.HasApparelTracker() && !pawn.apparel.WornApparel.NullOrEmpty();
	}

	internal static bool IsWornByPawn(this Pawn pawn, Apparel a)
	{
		return pawn.HasAnyApparel() && pawn.apparel.WornApparel.Contains(a);
	}

	internal static bool HasThisApparelDef(this Pawn pawn, ThingDef t)
	{
		if (pawn.HasAnyApparel())
		{
			foreach (Apparel item in pawn.apparel.WornApparel)
			{
				if (item.def == t)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static Apparel RandomWornApparel(this Pawn pawn)
	{
		return pawn.HasAnyApparel() ? pawn.apparel.WornApparel.RandomElement() : null;
	}

	internal static Apparel ThisOrFirstWornApparel(this Pawn pawn, Apparel apparel)
	{
		if (pawn == null || pawn.apparel == null || pawn.apparel.WornApparel.NullOrEmpty())
		{
			return null;
		}
		if (apparel == null || !pawn.apparel.WornApparel.Contains(apparel))
		{
			return pawn.apparel.WornApparel.FirstOrFallback();
		}
		return apparel;
	}

	internal static string GetAllApparelAsSeparatedString(this Pawn p)
	{
		if (!p.HasApparelTracker() || p.apparel.WornApparel.NullOrEmpty())
		{
			return "";
		}
		string text = "";
		foreach (Apparel item in p.apparel.WornApparel)
		{
			text += item.GetAsSeparatedString();
			text += ":";
		}
		return text.SubstringRemoveLast();
	}

	internal static void SetApparelFromSeparatedString(this Pawn p, string s)
	{
		if (!p.HasApparelTracker())
		{
			return;
		}
		try
		{
			p.apparel.DestroyAll();
			if (s.NullOrEmpty())
			{
				return;
			}
			string[] array = s.SplitNo(":");
			string[] array2 = array;
			foreach (string s2 in array2)
			{
				string[] array3 = s2.SplitNo("|");
				if (array3.Length >= 6)
				{
					string styledefname = ((array3.Length >= 7) ? array3[6] : "");
					Apparel apparel = GenerateApparel(Selected.ByName(array3[0], array3[1], styledefname, array3[2].HexStringToColor(), array3[3].AsInt32(), array3[4].AsInt32()));
					if (apparel != null)
					{
						apparel.HitPoints = array3[5].AsInt32();
						p.WearThatApparel(apparel);
					}
				}
			}
		}
		catch (Exception e)
		{
			MessageTool.DebugException(e);
		}
	}

	internal static void TestAllApparel(this Pawn pawn)
	{
		if (!pawn.HasApparelTracker() || !pawn.HasAnyApparel())
		{
			return;
		}
		List<Apparel> list = pawn.apparel.WornApparel.ToList();
		for (int num = list.Count - 1; num >= 0; num--)
		{
			Apparel apparel = pawn.apparel.WornApparel[num];
			if (!pawn.ApparelGraphicTest(apparel, showError: false))
			{
				pawn.TransferToInventory(apparel);
			}
		}
	}

	internal static void DestroyAllApparel(this Pawn pawn)
	{
		if (pawn.HasApparelTracker())
		{
			pawn.apparel.DestroyAll();
		}
	}

	internal static void DestroyApparel(this Pawn pawn, Apparel apparel)
	{
		if (pawn.HasApparelTracker())
		{
			pawn.outfits.forcedHandler.SetForced(apparel, forced: false);
			pawn.apparel.WornApparel.Remove(apparel);
			apparel.Destroy();
		}
	}

	internal static List<Apparel> ListOfCopyOutfits(this Pawn pawn)
	{
		return pawn.HasAnyApparel() ? pawn.apparel.WornApparel.ListFullCopy() : null;
	}

	internal static void PasteCopyOutfits(this Pawn pawn, List<Apparel> l)
	{
		if (pawn.HasApparelTracker())
		{
			pawn.apparel.WornApparel.Clear();
			if (!l.NullOrEmpty())
			{
				foreach (Apparel item in l)
				{
					pawn.CreateAndWearApparel(Selected.ByThing(item));
				}
			}
		}
		CEditor.API.UpdateGraphics();
	}

	internal static List<BodyPartGroupDef> GetCoveredBodyPartGroupDefs(this Pawn pawn)
	{
		List<BodyPartGroupDef> list = new List<BodyPartGroupDef>();
		foreach (Apparel item in pawn.apparel.WornApparel)
		{
			list.AddFromList(item.def.apparel.bodyPartGroups);
		}
		return list;
	}

	internal static List<Apparel> GetApparelWithThisLayer(this Pawn pawn, ApparelLayerDef ald)
	{
		return pawn.apparel.WornApparel.Where((Apparel td) => td.def.apparel.layers.Contains(ald)).ToList();
	}

	internal static int CountApparelThatWillBeReplacedByThisApparel(this Pawn pawn, ThingDef apparelDef)
	{
		int num = 0;
		try
		{
			for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
			{
				if (!ApparelUtility.CanWearTogether(apparelDef, pawn.apparel.WornApparel[i].def, pawn.RaceProps.body))
				{
					num++;
				}
			}
		}
		catch
		{
		}
		return num;
	}

	internal static bool CreateAndWearApparel(this Pawn pawn, Selected s)
	{
		Apparel a;
		return pawn.CreateAndWearApparel(s, out a, showError: false);
	}

	internal static bool CreateAndWearApparel(this Pawn pawn, Selected s, out Apparel a, bool showError)
	{
		a = null;
		if (pawn.HasApparelTracker())
		{
			a = GenerateApparel(s);
			if (pawn.CanWearApparel(a, showError))
			{
				pawn.WearThatApparel(a);
				return true;
			}
		}
		return false;
	}

	internal static void ReplaceAndWearRandomApparel(this Pawn pawn, Apparel a, ApparelLayerDef ald = null, bool pawnSpecific = false)
	{
		if (!pawn.HasApparelTracker())
		{
			return;
		}
		ApparelLayerDef apparelLayerDef = ((a != null) ? a.def.apparel.layers.FirstOrDefault() : ald);
		BodyPartGroupDef bodyPartGroupDef = a?.def.apparel.bodyPartGroups.FirstOrDefault();
		if (pawn.GetApparelWithThisLayer(apparelLayerDef).CountAllowNull() == 1)
		{
			string text = ((bodyPartGroupDef != null && bodyPartGroupDef.defName != null) ? bodyPartGroupDef.defName.ToLower() : "");
			if (!text.StartsWith("left") && !text.StartsWith("right") && !text.EndsWith("left") && !text.EndsWith("right"))
			{
				bodyPartGroupDef = null;
			}
		}
		HashSet<ThingDef> hashSet = ListOfApparel(null, apparelLayerDef, bodyPartGroupDef);
		bool hasApparelTags = pawn.kindDef.apparelTags != null;
		if (pawnSpecific)
		{
			hashSet = hashSet.Where((ThingDef td) => td.IsFromMod(pawn.kindDef.GetModName()) || (hasApparelTags && td.apparel.tags != null && td.apparel.tags.Select((string t) => pawn.kindDef.apparelTags.Contains(t)) != null)).ToHashSet();
		}
		Selected selected = null;
		int num = 10;
		if (hashSet.Count <= 1 && ald == ApparelLayerDefOf.EyeCover)
		{
			return;
		}
		do
		{
			for (int num2 = 0; num2 < 5; num2++)
			{
				selected = Selected.Random(hashSet, Event.current.alt);
				if (pawn.CountApparelThatWillBeReplacedByThisApparel(selected.thingDef) <= 1)
				{
					break;
				}
			}
			num--;
		}
		while (!pawn.CreateAndWearApparel(selected) && num > 0);
	}

	internal static bool CanWearApparel(this Pawn pawn, Apparel a, bool showError)
	{
		try
		{
			if (pawn.HasApparelTracker() && a != null && ApparelUtility.HasPartsToWear(pawn, a.def))
			{
				if (pawn.IsAnimal())
				{
					return true;
				}
				return pawn.ApparelGraphicTest(a, showError);
			}
			return false;
		}
		catch
		{
			return pawn.apparel.CanWearWithoutDroppingAnything(a.def);
		}
	}

	internal static void ReplaceThatApparel(this Pawn pawn, Apparel old, Apparel neu)
	{
		if (pawn.HasApparelTracker() && pawn.story != null && neu != null && ApparelUtility.HasPartsToWear(pawn, neu.def))
		{
			pawn.AllowAllApparel(neu);
			pawn.apparel.Remove(old);
			pawn.apparel.GetMemberValue<ThingOwner<Apparel>>("wornApparel", null).TryAdd(neu);
		}
	}

	internal static void WearThatApparel(this Pawn pawn, Apparel a)
	{
		if (pawn.HasApparelTracker() && a != null && ApparelUtility.HasPartsToWear(pawn, a.def))
		{
			pawn.AllowAllApparel(a);
			if (pawn.HasStoryTracker() && (pawn.story.bodyType == BodyTypeDefOf.Child || pawn.story.bodyType == BodyTypeDefOf.Baby))
			{
				pawn.apparel.Wear(a, dropReplacedApparel: false);
			}
			else
			{
				pawn.ForceWearThatApparel(a);
			}
		}
	}

	internal static void ForceWearThatApparel(this Pawn pawn, Apparel a)
	{
		a.DeSpawnOrDeselect();
		if (CompBiocodable.IsBiocoded(a) && !CompBiocodable.IsBiocodedFor(a, pawn))
		{
			CompBiocodable compBiocodable = a.TryGetComp<CompBiocodable>();
			Log.Warning(pawn.ToString() + " tried to wear " + a?.ToString() + " but it is biocoded for " + compBiocodable.CodedPawnLabel + " .");
		}
		else
		{
			for (int num = pawn.apparel.WornApparelCount - 1; num >= 0; num--)
			{
				Apparel apparel = pawn.apparel.WornApparel[num];
				if (!ApparelUtility.CanWearTogether(a.def, apparel.def, pawn.RaceProps.body))
				{
					pawn.apparel.Remove(apparel);
				}
			}
		}
		if (a.Wearer != null)
		{
			Log.Warning(pawn.ToString() + " is trying to wear " + a?.ToString() + " but this apparel already has a wearer (" + a.Wearer?.ToString() + "). This may or may not cause bugs.");
		}
		pawn.apparel.GetMemberValue<ThingOwner<Apparel>>("wornApparel", null).TryAdd(a, canMergeWithExistingStacks: false);
	}

	internal static void AskToWearIncompatibleApparel(this Pawn pawn, Apparel a)
	{
		if (pawn.HasStoryTracker() && (a.def.IsFromCoreMod() || a.def.IsFromMod("Royalty")) && !pawn.story.bodyType.IsFromCoreMod() && !pawn.story.bodyType.IsFromMod("Royalty"))
		{
			WindowTool.Open(Dialog_MessageBox.CreateConfirmation("core or royalty apparel that is not compatible to the current body type found - try to wear it anyway?\n\nif you do, you will get texture error messages on savegame load. but they should be harmless.", delegate
			{
				ApparelGraphicRecordGetter.TryGetGraphicApparel(a, pawn.story.bodyType, forStatue: false, out var _);
				pawn.AllowAllApparel(a);
				pawn.apparel.Wear(a, dropReplacedApparel: false);
				CEditor.API.UpdateGraphics();
			}));
		}
	}

	internal static Apparel GenerateApparel(Selected s)
	{
		if (s == null || s.thingDef == null)
		{
			return null;
		}
		ThingDef thingDef = DefTool.ThingDef(s.thingDef.defName);
		if (thingDef == null || !thingDef.IsApparel)
		{
			return null;
		}
		s.stuff = s.thingDef.ThisOrDefaultStuff(s.stuff);
		if (!s.thingDef.MadeFromStuff)
		{
			s.stuff = null;
		}
		Apparel apparel = (Apparel)ThingMaker.MakeThing(s.thingDef, s.stuff);
		apparel.HitPoints = apparel.MaxHitPoints;
		apparel.SetQuality(s.quality);
		apparel.SetDrawColor(s.DrawColor);
		apparel.stackCount = s.stackVal;
		if (s.style != null)
		{
			apparel.StyleDef = s.style;
			apparel.StyleDef.color = s.style.color;
		}
		return apparel;
	}

	internal static List<Apparel> GetConflictedApparelList(this Pawn pawn, ThingDef apparelToCheck)
	{
		List<Apparel> list = new List<Apparel>();
		try
		{
			foreach (BodyPartGroupDef bodyPartGroup in apparelToCheck.apparel.bodyPartGroups)
			{
				foreach (Apparel item in CEditor.API.Pawn.apparel.WornApparel)
				{
					if (!ApparelUtility.CanWearTogether(item.def, apparelToCheck, CEditor.API.Pawn.RaceProps.body) && !list.Contains(item))
					{
						list.Add(item);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	internal static bool RenderAsPack(ThingDef a)
	{
		if (!a.apparel.LastLayer.IsUtilityLayer)
		{
			return false;
		}
		return a.apparel.wornGraphicData == null || a.apparel.wornGraphicData.renderUtilityAsPack;
	}

	internal static string GetApparelPath(ThingDef a, Pawn p)
	{
		string text = "";
		if (a != null && a.apparel != null)
		{
			bool flag = false;
			if (a.apparel.layers != null && a.apparel.layers.Contains(ApparelLayerDefOf.Overhead))
			{
				text = a.apparel.wornGraphicPath;
				flag = true;
			}
			if (string.IsNullOrEmpty(text) && a.apparel.wornGraphicPath.NullOrEmpty())
			{
				text = a.apparel.wornGraphicPath;
				flag = true;
			}
			if (string.IsNullOrEmpty(text) && RenderAsPack(a))
			{
				text = a.apparel.wornGraphicPath;
				flag = true;
			}
			if (string.IsNullOrEmpty(text) && a.apparel.wornGraphicPath == BaseContent.PlaceholderImagePath)
			{
				text = a.apparel.wornGraphicPath;
				flag = true;
			}
			if (string.IsNullOrEmpty(text) && !flag && p.story != null && p.story.bodyType != null && p.story.bodyType.defName != null)
			{
				text = a.apparel.wornGraphicPath + "_" + p.story.bodyType.defName;
			}
		}
		return text;
	}

	internal static bool ApparalGraphicTest2(this Pawn pawn, ThingDef a, bool showError)
	{
		bool flag = false;
		string text = "";
		if (pawn.HasApparelTracker() && a != null && ApparelUtility.HasPartsToWear(pawn, a))
		{
			if (a.apparel.layers.Contains(ApparelLayerDefOf.Overhead) || a.apparel.layers.Contains(ApparelLayerDefOf.EyeCover) || a.apparel.layers.Contains(ApparelLayerDefOf.Belt))
			{
				return true;
			}
			if ((a.IsFromCoreMod() || a.IsFromMod("Royalty") || a.IsFromMod("Ideology")) && (pawn.story.bodyType.IsFromCoreMod() || pawn.story.bodyType.IsFromMod("Royalty") || pawn.story.bodyType.IsFromMod("Ideology")))
			{
				return true;
			}
			text = GetApparelPath(a, pawn);
			try
			{
				string text2 = text.SubstringBackwardTo("/");
				string text3 = text.SubstringBackwardFrom("/");
				List<Texture2D> list = ContentFinder<Texture2D>.GetAllInFolder(text2).ToList();
				if (Prefs.DevMode)
				{
					Log.Message("searching for compatible apparel in subpath=" + text2 + " .. found=" + list.CountAllowNull() + " .. checking for matching entity=" + text3);
				}
				foreach (Texture2D item in list)
				{
					if (item.name.Contains(text3))
					{
						flag = true;
						break;
					}
				}
				if (!flag && a.apparel.layers.Contains(ApparelLayerDefOf.Overhead) && a.apparel.layers.Count == 1)
				{
					return true;
				}
			}
			catch
			{
			}
		}
		if (!flag && showError)
		{
			MessageTool.Show(Label.NOT_COMPATIBLE_APPAREL + ", missing texture=" + text, MessageTypeDefOf.RejectInput);
		}
		return flag;
	}

	internal static bool ApparelGraphicTest(this Pawn pawn, Apparel a, bool showError, bool force = false)
	{
		if (!force && !CEditor.API.GetO(OptionB.DOAPPARELCHECK))
		{
			return true;
		}
		return pawn.ApparalGraphicTest2(a.def, showError);
	}

	internal static bool HasSameCover(this ThingDef a, ThingDef b)
	{
		if (a == null || b == null)
		{
			return false;
		}
		foreach (BodyPartGroupDef bodyPartGroup in a.apparel.bodyPartGroups)
		{
			foreach (BodyPartGroupDef bodyPartGroup2 in b.apparel.bodyPartGroups)
			{
				if (bodyPartGroup == bodyPartGroup2)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static bool HasSameApparelLayer(this ThingDef a, ThingDef b)
	{
		if (a == null || b == null)
		{
			return false;
		}
		foreach (ApparelLayerDef layer in a.apparel.layers)
		{
			foreach (ApparelLayerDef layer2 in b.apparel.layers)
			{
				if (layer == layer2)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static void PasteApparelLayer(this ThingDef t, List<ApparelLayerDef> l)
	{
		if (t == null || l == null)
		{
			return;
		}
		if (t.apparel.layers == null)
		{
			t.apparel.layers = new List<ApparelLayerDef>();
		}
		foreach (ApparelLayerDef item in l)
		{
			t.AddApparelLayer(item);
		}
	}

	internal static bool HasApparelLayer(this ThingDef t, ApparelLayerDef apparelLayerDef)
	{
		if (t != null && t.apparel.layers != null)
		{
			return t.apparel.layers.Contains(apparelLayerDef);
		}
		return false;
	}

	internal static void AddApparelLayer(this ThingDef t, ApparelLayerDef apparelLayerDef)
	{
		if (t != null && apparelLayerDef != null)
		{
			if (t.apparel.layers == null)
			{
				t.apparel.layers = new List<ApparelLayerDef>();
			}
			if (!t.HasApparelLayer(apparelLayerDef))
			{
				t.apparel.layers.Add(apparelLayerDef);
			}
			t.ResolveReferences();
		}
	}

	internal static void PasteBodyPartGroup(this ThingDef t, List<BodyPartGroupDef> l)
	{
		if (t == null || l == null)
		{
			return;
		}
		if (t.apparel.bodyPartGroups == null)
		{
			t.apparel.bodyPartGroups = new List<BodyPartGroupDef>();
		}
		foreach (BodyPartGroupDef item in l)
		{
			t.AddBodyPart(item);
		}
	}

	internal static bool HasBodyPartGroupDefName(this ThingDef t, string defName)
	{
		if (t != null && t.apparel.bodyPartGroups != null)
		{
			foreach (BodyPartGroupDef bodyPartGroup in t.apparel.bodyPartGroups)
			{
				if (bodyPartGroup.defName == defName)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal static bool HasBodyPartGroup(this ThingDef t, BodyPartGroupDef bodyPartGroupDef)
	{
		if (t != null && t.apparel.bodyPartGroups != null)
		{
			return t.apparel.bodyPartGroups.Contains(bodyPartGroupDef);
		}
		return false;
	}

	internal static void AddBodyPart(this ThingDef t, BodyPartGroupDef bodyPartGroupDef)
	{
		if (t != null && bodyPartGroupDef != null)
		{
			if (t.apparel.bodyPartGroups == null)
			{
				t.apparel.bodyPartGroups = new List<BodyPartGroupDef>();
			}
			if (!t.HasBodyPartGroup(bodyPartGroupDef))
			{
				t.apparel.bodyPartGroups.Add(bodyPartGroupDef);
			}
			t.ResolveReferences();
		}
	}

	internal static void PasteApparelTag(this ThingDef t, List<string> l)
	{
		if (t == null || l == null)
		{
			return;
		}
		if (t.apparel.tags == null)
		{
			t.apparel.tags = new List<string>();
		}
		foreach (string item in l)
		{
			t.AddApparelTag(item);
		}
	}

	internal static bool HasApparelTag(this ThingDef t, string tag)
	{
		if (t != null && t.apparel.tags != null)
		{
			return t.apparel.tags.Contains(tag);
		}
		return false;
	}

	internal static void AddApparelTag(this ThingDef t, string tag)
	{
		if (t != null && tag != null)
		{
			if (t.apparel.tags == null)
			{
				t.apparel.tags = new List<string>();
			}
			if (!t.HasApparelTag(tag))
			{
				t.apparel.tags.Add(tag);
			}
			t.ResolveReferences();
		}
	}

	internal static void RemoveApparelTag(this ThingDef t, string tag)
	{
		if (t == null || t.apparel.tags == null)
		{
			return;
		}
		foreach (string tag2 in t.apparel.tags)
		{
			if (tag2 == tag)
			{
				t.apparel.tags.Remove(tag2);
				break;
			}
		}
		if (t.apparel.tags.Count == 0)
		{
			t.apparel.tags = null;
		}
		t.ResolveReferences();
	}

	internal static void PasteOutfitTag(this ThingDef t, List<string> l)
	{
		if (t == null || l == null)
		{
			return;
		}
		if (t.apparel.defaultOutfitTags == null)
		{
			t.apparel.defaultOutfitTags = new List<string>();
		}
		foreach (string item in l)
		{
			t.AddOutfitTag(item);
		}
	}

	internal static bool HasOutfitTag(this ThingDef t, string tag)
	{
		if (t != null && t.apparel.defaultOutfitTags != null)
		{
			return t.apparel.defaultOutfitTags.Contains(tag);
		}
		return false;
	}

	internal static void AddOutfitTag(this ThingDef t, string tag)
	{
		if (t != null && tag != null)
		{
			if (t.apparel.defaultOutfitTags == null)
			{
				t.apparel.defaultOutfitTags = new List<string>();
			}
			if (!t.HasOutfitTag(tag))
			{
				t.apparel.defaultOutfitTags.Add(tag);
			}
			t.ResolveReferences();
		}
	}

	internal static void RemoveOutfitTag(this ThingDef t, string tag)
	{
		if (t == null || t.apparel.defaultOutfitTags == null)
		{
			return;
		}
		foreach (string defaultOutfitTag in t.apparel.defaultOutfitTags)
		{
			if (defaultOutfitTag == tag)
			{
				t.apparel.defaultOutfitTags.Remove(defaultOutfitTag);
				break;
			}
		}
		if (t.apparel.defaultOutfitTags.Count == 0)
		{
			t.apparel.defaultOutfitTags = null;
		}
		t.ResolveReferences();
	}

	internal static bool RenderAsPack(Apparel apparel)
	{
		if (apparel.def.apparel.LastLayer != ApparelLayerDefOf.Belt)
		{
			return false;
		}
		return apparel.def.apparel.wornGraphicData == null || apparel.def.apparel.wornGraphicData.renderUtilityAsPack;
	}

	internal static void FixForBelts(ThingDef t, StatDef stat)
	{
		if (t != null && t.apparel != null && (stat == StatDefOf.EnergyShieldEnergyMax || stat == StatDefOf.EnergyShieldRechargeRate))
		{
			MessageTypeDef mt = (t.apparel.layers.Contains(ApparelLayerDefOf.Belt) ? MessageTypeDefOf.SilentInput : MessageTypeDefOf.RejectInput);
			MessageTool.Show(Label.ONLYFORSHIELD, mt);
			t.ResolveReferences();
			t.PostLoad();
		}
	}

	internal static List<ApparelLayerDef> ListOfRandomApparelLayerDefs(int numberOfLayers = -1)
	{
		List<ApparelLayerDef> list = ListOfApparelLayerDefs(insertNull: false);
		numberOfLayers = ((numberOfLayers < 0) ? CEditor.zufallswert.Next(2, list.Count) : numberOfLayers);
		numberOfLayers = ((numberOfLayers > 8) ? 8 : numberOfLayers);
		return list.TakeRandom(numberOfLayers).ToList();
	}

	internal static void Redress(this Pawn pawn, Selected selected, bool originalColors, int numberOfLayers = 1, bool pawnSpecific = false)
	{
		if (!pawn.HasApparelTracker())
		{
			return;
		}
		if (selected == null)
		{
			pawn.DestroyAllApparel();
			List<ApparelLayerDef> list = ListOfRandomApparelLayerDefs(numberOfLayers);
			if (pawnSpecific)
			{
				if (!list.Contains(ApparelLayerDefOf.OnSkin))
				{
					list.Add(ApparelLayerDefOf.OnSkin);
				}
				if (!list.Contains(ApparelLayerDefOf.Shell))
				{
					list.Add(ApparelLayerDefOf.Shell);
				}
			}
			Event.current.alt = originalColors;
			foreach (ApparelLayerDef item in list)
			{
				pawn.ReplaceAndWearRandomApparel(null, item, pawnSpecific);
			}
			Event.current.alt = false;
		}
		else
		{
			pawn.CreateAndWearApparel(selected);
		}
	}

	internal static void AllowAllApparel(this Pawn pawn, Apparel apparel = null)
	{
		if (pawn == null)
		{
			return;
		}
		if (pawn.outfits == null)
		{
			pawn.outfits = new Pawn_OutfitTracker(pawn);
		}
		if (pawn.outfits.CurrentApparelPolicy == null)
		{
			pawn.outfits.CurrentApparelPolicy = new ApparelPolicy();
		}
		if (pawn.apparel == null)
		{
			pawn.apparel = new Pawn_ApparelTracker(pawn);
		}
		if (apparel != null)
		{
			pawn.outfits.CurrentApparelPolicy.filter.SetAllow(apparel.def, allow: true);
		}
		else
		{
			if (pawn.apparel.WornApparel.NullOrEmpty())
			{
				return;
			}
			foreach (Apparel item in pawn.apparel.WornApparel)
			{
				pawn.outfits.CurrentApparelPolicy.filter.SetAllow(item.def, allow: true);
			}
		}
	}

	internal static Dictionary<ApparelLayerDef, HashSet<ThingDef>> CreateDicOfApparel()
	{
		Dictionary<ApparelLayerDef, HashSet<ThingDef>> dictionary = new Dictionary<ApparelLayerDef, HashSet<ThingDef>>();
		List<ApparelLayerDef> list = ListOfApparelLayerDefs(insertNull: false);
		foreach (ApparelLayerDef item in list)
		{
			HashSet<ThingDef> value = ListOfApparel(null, item, null);
			dictionary.Add(item, value);
		}
		return dictionary;
	}

	internal static HashSet<string> CreateListOfGraphicPaths()
	{
		Dictionary<GraphicRequest, Graphic> source = (Dictionary<GraphicRequest, Graphic>)typeof(GraphicDatabase).GetMemberValue("allGraphics");
		return source.Select(delegate(KeyValuePair<GraphicRequest, Graphic> td)
		{
			KeyValuePair<GraphicRequest, Graphic> keyValuePair = td;
			return keyValuePair.Value.path;
		}).ToHashSet();
	}

	internal static List<ThingDef> GetAllOnSkn()
	{
		return (from td in DefDatabase<ThingDef>.AllDefs
			where td.IsApparel && td.apparel.LastLayer == ApparelLayerDefOf.OnSkin
			orderby td.label
			select td).ToList();
	}

	internal static List<ThingDef> GetAllBelt()
	{
		return (from td in DefDatabase<ThingDef>.AllDefs
			where td.IsApparel && td.apparel.LastLayer == ApparelLayerDefOf.Belt
			orderby td.label
			select td).ToList();
	}

	internal static List<ThingDef> GetAllMiddle()
	{
		return (from td in DefDatabase<ThingDef>.AllDefs
			where td.IsApparel && td.apparel.LastLayer == ApparelLayerDefOf.Middle
			orderby td.label
			select td).ToList();
	}

	internal static List<ThingDef> GetAllOverhead()
	{
		return (from td in DefDatabase<ThingDef>.AllDefs
			where td.IsApparel && td.apparel.LastLayer == ApparelLayerDefOf.Overhead
			orderby td.label
			select td).ToList();
	}

	internal static List<ThingDef> GetAllShell()
	{
		return (from td in DefDatabase<ThingDef>.AllDefs
			where td.IsApparel && td.apparel.LastLayer == ApparelLayerDefOf.Shell
			orderby td.label
			select td).ToList();
	}

	internal static List<ApparelLayerDef> ListOfApparelLayerDefs(bool insertNull)
	{
		List<ApparelLayerDef> list = (from td in DefDatabase<ApparelLayerDef>.AllDefs
			where td.IsFromCoreMod()
			orderby td.label
			select td).ToList();
		list.AddRange((from td in DefDatabase<ApparelLayerDef>.AllDefs
			where !td.IsFromCoreMod()
			orderby td.label
			select td).ToList());
		List<ApparelLayerDef> list2 = new List<ApparelLayerDef>();
		foreach (ApparelLayerDef item in list)
		{
			if (!ListOfApparel(null, item, null).NullOrEmpty())
			{
				list2.Add(item);
			}
		}
		if (insertNull)
		{
			list2.Insert(0, null);
		}
		return list2;
	}

	internal static HashSet<ThingDef> ListOfApparel(string modname, ApparelLayerDef ld, BodyPartGroupDef bpd)
	{
		bool bAll1 = modname.NullOrEmpty();
		bool bAll2 = ld == null;
		bool bAll3 = bpd == null;
		return (from td in DefDatabase<ThingDef>.AllDefs
			where td.IsApparel && !td.defName.NullOrEmpty() && !td.label.NullOrEmpty() && (bAll1 || td.IsFromMod(modname)) && (bAll2 || (!td.apparel.layers.NullOrEmpty() && td.apparel.layers.Contains(ld))) && (bAll3 || (!td.apparel.bodyPartGroups.NullOrEmpty() && td.apparel.bodyPartGroups.Contains(bpd)))
			orderby td.label
			select td).ToHashSet();
	}

	internal static void MoveDressToInv(this Pawn p, ApparelLayerDef layerOnly)
	{
		if (!CEditor.API.Pawn.HasApparelTracker() || !CEditor.API.Pawn.HasInventoryTracker())
		{
			return;
		}
		int num = (CEditor.API.Pawn.HasApparelTracker() ? CEditor.API.Pawn.apparel.WornApparel.CountAllowNull() : 0);
		if (num <= 0)
		{
			return;
		}
		for (int num2 = num - 1; num2 >= 0; num2--)
		{
			Thing thing = CEditor.API.Pawn.apparel.WornApparel[num2];
			if (thing.def.IsApparel && (layerOnly == null || thing.def.apparel.layers.Contains(layerOnly)))
			{
				CEditor.API.Pawn.TransferToInventory(thing);
			}
		}
	}

	internal static void MoveDressFromInv(this Pawn p, ApparelLayerDef layerOnly)
	{
		if (!CEditor.API.Pawn.HasApparelTracker() || !CEditor.API.Pawn.HasInventoryTracker())
		{
			return;
		}
		p.MoveDressToInv(layerOnly);
		int num = CEditor.API.Pawn.inventory.innerContainer.CountAllowNull();
		if (num <= 0)
		{
			return;
		}
		for (int num2 = num - 1; num2 >= 0; num2--)
		{
			Thing thing = CEditor.API.Pawn.inventory.innerContainer[num2];
			if (thing.def.IsApparel && ((layerOnly == null && CEditor.API.Pawn.apparel.CanWearWithoutDroppingAnything(thing.def)) || (layerOnly != null && thing.def.apparel.layers.Contains(layerOnly))))
			{
				CEditor.API.Pawn.TransferFromInventory(thing);
				if (layerOnly != null)
				{
					break;
				}
			}
		}
	}

	internal static void AllowApparelToBeColorable()
	{
		if (!CEditor.API.GetO(OptionB.ADDCOMPCOLORABLE))
		{
			return;
		}
		try
		{
			Log.Message("allowing apparel to be colorable...");
			List<ThingDef> list = DefTool.ListAll<ThingDef>();
			foreach (ThingDef item in list)
			{
				if (item != null && item.defName != null && item.IsApparel && item.apparel.LastLayer != ApparelLayerDefOf.Belt)
				{
					if (item.colorGenerator == null)
					{
						ColorGenerator_Options colorGenerator_Options = new ColorGenerator_Options();
						ColorOption colorOption = new ColorOption();
						colorOption.weight = 10f;
						colorOption.only = Color.white;
						colorGenerator_Options.options.Add(colorOption);
						item.colorGenerator = colorGenerator_Options;
						item.apparel.useWornGraphicMask = false;
					}
					if (!item.HasComp(typeof(CompColorable)))
					{
						item.AddCompColorable();
					}
					item.ResolveReferences();
					item.PostLoad();
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex.StackTrace);
		}
	}

	internal static void MakeThingColorable(ThingDef t)
	{
		if (t.colorGenerator == null)
		{
			ColorGenerator_Options colorGenerator_Options = new ColorGenerator_Options();
			ColorOption colorOption = new ColorOption();
			colorOption.weight = 10f;
			colorOption.only = Color.white;
			colorGenerator_Options.options.Add(colorOption);
			t.colorGenerator = colorGenerator_Options;
			if (t.apparel != null)
			{
				t.apparel.useWornGraphicMask = false;
			}
		}
		if (!t.HasComp(typeof(CompColorable)))
		{
			t.AddCompColorable();
		}
		t.ResolveReferences();
		t.PostLoad();
	}

	internal static void MakeThingwColorable(ThingWithComps t)
	{
		MakeThingColorable(t.def);
		InitializeCompColorable(t);
	}

	private static void InitializeCompColorable(ThingWithComps t)
	{
		ThingComp thingComp = null;
		try
		{
			thingComp = (ThingComp)Activator.CreateInstance(typeof(CompColorable));
			thingComp.parent = t;
			t.GetMemberValue<List<ThingComp>>("comps", null)?.Add(thingComp);
			CompProperties props = t.def.comps.First((CompProperties x) => x.compClass == typeof(CompColorable));
			thingComp.Initialize(props);
		}
		catch (Exception ex)
		{
			Log.Error("Could not instantiate or initialize a ThingComp: " + ex);
		}
	}
}
