using System;
using System.Collections.Generic;
using System.Linq;
using FloatSubMenus;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceInventoryTab;

[StaticConstructorOnStartup]
public static class ApparelSlotUtility
{
	public class PotentialSlot
	{
		public ApparelLayerDef layer;

		public HashSet<BodyPartGroupDef> coveredParts = new HashSet<BodyPartGroupDef>();

		public List<ThingDef> possibleApparel = new List<ThingDef>();

		public string displayName;

		public int drawOrder => layer.drawOrder;

		public int availableCount => possibleApparel.Count;
	}

	public static readonly List<PotentialSlot> AllPotentialSlots;

	static ApparelSlotUtility()
	{
		AllPotentialSlots = new List<PotentialSlot>();
		RebuildSlots();
	}

	public static void RebuildSlots()
	{
		AllPotentialSlots.Clear();
		Dictionary<string, PotentialSlot> dictionary = new Dictionary<string, PotentialSlot>();
		foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.IsApparel))
		{
			if (item.apparel == null || item.apparel.layers == null || item.apparel.layers.Count == 0)
			{
				continue;
			}
			List<BodyPartGroupDef> list = item.apparel.bodyPartGroups ?? new List<BodyPartGroupDef>();
			foreach (ApparelLayerDef layer in item.apparel.layers)
			{
				List<BodyPartGroupDef> list2 = new List<BodyPartGroupDef>(list);
				list2.Sort((BodyPartGroupDef a, BodyPartGroupDef b) => string.Compare(a.defName, b.defName, StringComparison.Ordinal));
				string text = string.Join("|", list2.Select((BodyPartGroupDef p) => p.defName));
				string key = layer.defName + "_" + text;
				if (!dictionary.TryGetValue(key, out var value))
				{
					value = new PotentialSlot
					{
						layer = layer,
						displayName = BuildDisplayName(layer, list)
					};
					foreach (BodyPartGroupDef item2 in list)
					{
						value.coveredParts.Add(item2);
					}
					dictionary[key] = value;
					AllPotentialSlots.Add(value);
				}
				value.possibleApparel.Add(item);
			}
		}
		AllPotentialSlots.Sort(delegate(PotentialSlot a, PotentialSlot b)
		{
			int num = a.drawOrder.CompareTo(b.drawOrder);
			return (num != 0) ? num : string.Compare(a.displayName, b.displayName, StringComparison.Ordinal);
		});
	}

	private static string BuildDisplayName(ApparelLayerDef layer, List<BodyPartGroupDef> parts)
	{
		string text = layer.LabelCap;
		if (parts == null || parts.Count == 0)
		{
			return text;
		}
		IEnumerable<string> values = parts.Select((BodyPartGroupDef p) => p.LabelShort ?? p.label ?? p.defName).Distinct();
		return text + " — " + string.Join(", ", values);
	}

	public static List<BodyPartGroupDef> GetAvailableBodyPartGroupsOnLayer(Pawn pawn, ApparelLayerDef layer, List<PotentialSlot> availableSlots)
	{
		HashSet<BodyPartGroupDef> hashSet = new HashSet<BodyPartGroupDef>();
		if (availableSlots == null)
		{
			availableSlots = GetAvailableSlots(pawn);
		}
		foreach (PotentialSlot availableSlot in availableSlots)
		{
			if (availableSlot.layer != layer)
			{
				continue;
			}
			foreach (BodyPartGroupDef coveredPart in availableSlot.coveredParts)
			{
				hashSet.Add(coveredPart);
			}
		}
		return hashSet.ToList();
	}

	public static List<ApparelLayerDef> GetAvailableLayers(Pawn pawn, List<PotentialSlot> availableSlots)
	{
		if (availableSlots == null)
		{
			availableSlots = GetAvailableSlots(pawn);
		}
		HashSet<ApparelLayerDef> hashSet = new HashSet<ApparelLayerDef>();
		foreach (PotentialSlot availableSlot in availableSlots)
		{
			hashSet.Add(availableSlot.layer);
		}
		List<ApparelLayerDef> list = new List<ApparelLayerDef>(hashSet);
		list.Sort((ApparelLayerDef a, ApparelLayerDef b) => a.drawOrder.CompareTo(b.drawOrder));
		return list;
	}

	public static List<ThingDef> GetAvailableThingDefsOnLayer(Pawn pawn, ApparelLayerDef layer)
	{
		HashSet<ThingDef> hashSet = new HashSet<ThingDef>();
		foreach (PotentialSlot availableSlot in GetAvailableSlots(pawn))
		{
			if (availableSlot.layer != layer)
			{
				continue;
			}
			foreach (ThingDef item in availableSlot.possibleApparel)
			{
				hashSet.Add(item);
			}
		}
		return hashSet.ToList();
	}

	public static List<ThingDef> GetAvailableThingDefs(Pawn pawn)
	{
		HashSet<ThingDef> hashSet = new HashSet<ThingDef>();
		foreach (PotentialSlot availableSlot in GetAvailableSlots(pawn))
		{
			foreach (ThingDef item in availableSlot.possibleApparel)
			{
				hashSet.Add(item);
			}
		}
		return hashSet.ToList();
	}

	public static List<PotentialSlot> GetAvailableSlots(Pawn pawn)
	{
		List<PotentialSlot> list = new List<PotentialSlot>();
		if (pawn?.apparel?.WornApparel == null || pawn.apparel.WornApparel.Count == 0)
		{
			return new List<PotentialSlot>(AllPotentialSlots);
		}
		BodyDef body = pawn.def.race.body;
		foreach (PotentialSlot slot in AllPotentialSlots)
		{
			bool flag = false;
			foreach (Apparel item in pawn.apparel.WornApparel)
			{
				ThingDef def = item.def;
				if (def.apparel?.layers == null || !def.apparel.layers.Exists((ApparelLayerDef l) => l == slot.layer))
				{
					continue;
				}
				BodyPartGroupDef[] interferingBodyPartGroups = def.apparel.GetInterferingBodyPartGroups(body);
				foreach (BodyPartGroupDef coveredPart in slot.coveredParts)
				{
					if (interferingBodyPartGroups.Contains(coveredPart))
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				list.Add(slot);
			}
		}
		return list;
	}

	public static void OpenFloatMenu(List<PotentialSlot> slots, ApparelLayerDef apparelLayer, bool checkCanWear = true, List<FloatMenuOption> extraOptions = null)
	{
		if (!ITab_Pawn_Gear_Patch.lastPawn.Spawned || ITab_Pawn_Gear_Patch.lastPawn.Map == null || (ITab_Pawn_Gear_Patch.lastPawn.IsMutant && ITab_Pawn_Gear_Patch.lastPawn.mutant.Def.disableApparel))
		{
			return;
		}
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		if (!extraOptions.NullOrEmpty())
		{
			list.AddRange(extraOptions);
		}
		List<FloatMenuOption> list2 = new List<FloatMenuOption>();
		foreach (Thing app in FindAllApparel(ITab_Pawn_Gear_Patch.lastPawn, ITab_Pawn_Gear_Patch.lastPawn.Map, slots, checkCanWear))
		{
			bool flag = app.IsForbidden(ITab_Pawn_Gear_Patch.lastPawn);
			TaggedString labelCap = app.LabelCap;
			if (flag)
			{
				labelCap = "NIT_Forbidden".Translate() + app.LabelNoParenthesisCap;
			}
			list2.Add(new FloatMenuAdvanced(labelCap, delegate
			{
				CommandUtility.CommandWear(ITab_Pawn_Gear_Patch.lastPawn, app);
			}, app, Color.white, flag));
		}
		if (!list2.NullOrEmpty())
		{
			list2.SortBy((FloatMenuOption u) => (u as FloatMenuAdvanced).Darken);
			list.Add(new FloatSubMenus.FloatSubMenu("NIT_Wear".Translate(), list2));
		}
		else
		{
			list.Add(new FloatMenuOption("NIT_NothingToWear".Translate(), null));
		}
		List<FloatMenuOption> list3 = new List<FloatMenuOption>();
		foreach (CraftableItem item in FindAllApparelCanCreate(ITab_Pawn_Gear_Patch.lastPawn.Map, slots))
		{
			if (SuitableFor(ITab_Pawn_Gear_Patch.lastPawn, item.thingDef) && (!checkCanWear || ITab_Pawn_Gear_Patch.lastPawn.apparel.CanWearWithoutDroppingAnything(item.thingDef)) && item.workTables.Any())
			{
				list3.Add(MakeSubMenusToCraft(item, apparelLayer));
			}
		}
		if (!list3.NullOrEmpty())
		{
			list.Add(new FloatSubMenus.FloatSubMenu("NIT_Make".Translate(), list3));
		}
		else
		{
			list.Add(new FloatMenuOption("NIT_NothingToMake".Translate(), null));
		}
		if (Utils.InDevMode)
		{
			List<PotentialSlot> slots2 = AllPotentialSlots.Where((PotentialSlot x) => x.layer == apparelLayer).ToList();
			list.Add(new FloatSubMenus.FloatSubMenu("DEV create", DevCreate(slots2)));
		}
		if (Settings.EXR_OptMaster)
		{
			list.Add(new FloatMenuOption("OpenOptimizationMaster".Translate(), delegate
			{
				Find.WindowStack.Add(new Dialog_OptimizeEquipment(ITab_Pawn_Gear_Patch.lastPawn));
			}));
		}
		Find.WindowStack.Add(new FloatMenu(list));
	}

	private static string DEVLabel(ThingDef x)
	{
		if (!ITab_Pawn_Gear_Patch.lastPawn.apparel.CanWearWithoutDroppingAnything(x))
		{
			return "NIT_MayDrop".Translate() + x.LabelCap;
		}
		return x.LabelCap;
	}

	private static List<FloatMenuOption> DevCreate(List<PotentialSlot> slots)
	{
		return (from x in slots.SelectMany((PotentialSlot x) => x.possibleApparel).ToList()
			select new FloatMenuAdvanced(DEVLabel(x), delegate
			{
				Thing thing = ThingMaker.MakeThing(x, GenStuff.DefaultStuffFor(x));
				ITab_Pawn_Gear_Patch.lastPawn.apparel.Wear(thing as Apparel);
			}, Widgets.GetIconFor(x, GenStuff.DefaultStuffFor(x)), Color.white, !ITab_Pawn_Gear_Patch.lastPawn.apparel.CanWearWithoutDroppingAnything(x)) into u
			orderby u.Darken
			select u).Cast<FloatMenuOption>().ToList();
	}

	private static FloatMenuOption MakeSubMenusToCraft(CraftableItem item, ApparelLayerDef apparelLayer)
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		foreach (Building_WorkTable item2 in item.workTables.Distinct())
		{
			Building_WorkTable wtl = item2;
			list.Add(new FloatMenuOption("NIT_WorktableTasks".Translate(wtl.LabelCap, wtl.BillStack.Count), delegate
			{
				CommandUtility.CommandCreate(wtl, item.thingDef, item.recipe, apparelLayer);
			}, wtl, Color.white));
		}
		FloatSubMenus.FloatSubMenu floatSubMenu = new FloatSubMenus.FloatSubMenu(item.thingDef.LabelCap, list, item.thingDef);
		Building_WorkTable fst = item.workTables.FirstOrDefault();
		if (fst != null)
		{
			floatSubMenu.action = delegate
			{
				CommandUtility.CommandCreate(fst, item.thingDef, item.recipe, apparelLayer);
			};
		}
		return floatSubMenu;
	}

	private static List<Thing> FindAllApparel(Pawn pawn, Map map, List<PotentialSlot> slots, bool checkCanWear)
	{
		List<ThingDef> defList = slots.SelectMany((PotentialSlot x) => x.possibleApparel).ToList();
		if (checkCanWear)
		{
			return map.listerThings.GetAllThings((Thing x) => x.Spawned && defList.Contains(x.def) && !x.Fogged() && SuitableFor(pawn, x) && pawn.apparel.CanWearWithoutDroppingAnything(x.def)).ToList();
		}
		return map.listerThings.GetAllThings((Thing x) => x.Spawned && defList.Contains(x.def) && !x.Fogged() && SuitableFor(pawn, x)).ToList();
	}

	private static List<CraftableItem> FindAllApparelCanCreate(Map map, List<PotentialSlot> slots)
	{
		return CraftingUtility.GetAllCraftableApparel(slots, map);
	}

	private static bool SuitableFor(Pawn p, ThingDef app)
	{
		if (!ApparelUtility.HasPartsToWear(p, app))
		{
			return false;
		}
		if (WouldReplaceLockedApparel(p, app))
		{
			return false;
		}
		if (app.IsApparel && !app.apparel.developmentalStageFilter.Has(p.DevelopmentalStage))
		{
			return false;
		}
		return true;
	}

	private static bool SuitableFor(Pawn p, Thing app)
	{
		if (!ApparelUtility.HasPartsToWear(p, app.def))
		{
			return false;
		}
		if (p.apparel.WouldReplaceLockedApparel(app as Apparel))
		{
			return false;
		}
		if (!EquipmentUtility.CanEquip(app, p))
		{
			return false;
		}
		return true;
	}

	private static bool WouldReplaceLockedApparel(Pawn p, ThingDef newApparel)
	{
		if (!p.apparel.AnyApparelLocked)
		{
			return false;
		}
		for (int i = 0; i < p.apparel.LockedApparel.Count; i++)
		{
			if (!ApparelUtility.CanWearTogether(newApparel, p.apparel.LockedApparel[i].def, p.RaceProps.body))
			{
				return true;
			}
		}
		return false;
	}

	[DebugAction("General", "Log All Slots", false, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing)]
	private static void LogAll()
	{
		Log.Message($"=== ALL SLOTS: {AllPotentialSlots.Count} ===");
		foreach (PotentialSlot allPotentialSlot in AllPotentialSlots)
		{
			Log.Message($"- {allPotentialSlot.displayName} [{allPotentialSlot.layer.defName}] — {allPotentialSlot.possibleApparel.Count} items");
		}
	}

	[DebugAction("General", "Log Available for Selected", false, false, false, false, false, 0, false, allowedGameStates = AllowedGameStates.Playing)]
	private static void LogAvailable()
	{
		if (!(Find.Selector.SingleSelectedThing is Pawn pawn))
		{
			Log.Warning("Выбери пешку");
			return;
		}
		List<PotentialSlot> availableSlots = GetAvailableSlots(pawn);
		Log.Message($"=== ДОСТУПНО ДЛЯ {pawn.Name}: {availableSlots.Count} слотов ===");
		foreach (PotentialSlot item in availableSlots)
		{
			Log.Message($"- {item.displayName} — {item.availableCount} вариантов");
		}
	}
}
