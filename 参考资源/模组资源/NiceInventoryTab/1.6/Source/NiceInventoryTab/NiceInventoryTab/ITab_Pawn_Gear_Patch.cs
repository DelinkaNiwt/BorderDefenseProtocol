using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NiceInventoryTab;

[HarmonyPatch(typeof(ITab_Pawn_Gear), "FillTab")]
public static class ITab_Pawn_Gear_Patch
{
	public static bool enabled = true;

	public static bool alternative = false;

	public static Widget root = null;

	private static List<StatDrawer> Var1Stats = new List<StatDrawer>();

	private static List<StatDrawer> Var2Stats = new List<StatDrawer>();

	public static HLayout row_top = null;

	public static GroupBox equipment_zone = null;

	public static GroupBox apparel_zone = null;

	public static GroupBox inventory_zone = null;

	public static GroupBox mass_zone = null;

	public static FloatRef fr_mass = new FloatRef();

	public static FloatRef fr_dgt_mass = new FloatRef();

	public static FloatRef weapFRef = new FloatRef();

	public static Pawn lastPawn = null;

	public static bool shouldRecache = false;

	private static FieldInfo SizeField = AccessTools.Field(typeof(InspectTabBase), "size");

	public static ITab_Pawn_Gear Instance;

	public static bool ShouldUpdateSizes = false;

	private static readonly int CacheInterval = 160;

	private static int lastCacheTick = -9999;

	private static List<Thing> wornApparel = new List<Thing>();

	private static List<Thing> wornEquipment = new List<Thing>();

	private static List<Thing> wornInventory = new List<Thing>();

	private static int lastWornApparelVersion = -1;

	private static int lastWornEquipmentVersion = -1;

	private static int lastInventoryVersion = -1;

	public static void SwitchStats()
	{
		foreach (StatDrawer var1Stat in Var1Stats)
		{
			var1Stat.Visible = !alternative;
		}
		foreach (StatDrawer var2Stat in Var2Stats)
		{
			var2Stat.Visible = alternative;
		}
	}

	public static void UpdateSize()
	{
		if (Instance != null)
		{
			if (enabled)
			{
				SizeField.SetValue(Instance, new Vector2(Settings.TabWidth, Settings.TabHeight));
			}
			else
			{
				SizeField.SetValue(Instance, new Vector2(460f, 450f));
			}
			ShouldUpdateSizes = true;
		}
	}

	public static bool Prefix(ITab_Pawn_Gear __instance, ref Vector2 ___size)
	{
		Instance = __instance;
		Pawn pawn = GetPawn();
		Text.Font = GameFont.Small;
		Rect rect = new Rect(0f, 0f, ___size.x, ___size.y);
		if (lastPawn != pawn)
		{
			FloatRef.ClearValues();
			lastPawn = pawn;
			shouldRecache = true;
		}
		if (root == null)
		{
			FloatRef.ClearValues();
			CreateLayout();
			root.Geometry = rect;
			root.Update();
		}
		Rect rect2 = rect.ContractedBy(4f);
		rect2.height = 20f;
		rect2.xMax -= 24f;
		int num = 0;
		if (!enabled && Utils.InDevMode)
		{
			num = 2;
		}
		if (SettingsCheckBox(rect2, num, Assets.VanilaListTex, ref enabled))
		{
			if (enabled)
			{
				ITab_Pawn_Gear_Constructor_Patch.SetSize(ref ___size);
				ShouldUpdateSizes = true;
				return true;
			}
			ITab_Pawn_Gear_Constructor_Patch.SetDefaultSize(ref ___size);
			ShouldUpdateSizes = true;
			return false;
		}
		if (enabled)
		{
			if (SettingsCheckBox(rect2, num + 1, Assets.MedTex, ref Settings.DrugImpactVisible))
			{
				ShouldUpdateSizes = true;
			}
			if (SettingsCheckBox(rect2, num + 2, Assets.AltTex, ref alternative))
			{
				SwitchStats();
				ShouldUpdateSizes = true;
			}
			if (SettingsCheckBox(rect2, num + 3, Assets.ApparelSlotTex, ref Settings.ApparelSlotsVisible, 0.9f))
			{
				ShouldUpdateSizes = true;
			}
			AddonCheckBoxes(rect2, num + 4);
			SettingsOpenButton(rect2);
			if (ModIntegration.CLActive && CompositableLoadoutsIntegration.IsValidLoadoutHolder(pawn))
			{
				CompositableLoadoutsIntegration.DrawEditButtons(new Rect(rect2.x + 60f, rect2.y, rect2.width - 324f, rect2.height), pawn);
			}
		}
		if (ShouldUpdateSizes)
		{
			root.Geometry = rect;
			FloatRef.ClearValues();
			shouldRecache = true;
			root.Update();
			ShouldUpdateSizes = false;
		}
		if (enabled)
		{
			Recache(pawn);
			root.Draw();
			return false;
		}
		return true;
	}

	public static void AddonCheckBoxes(Rect rectButtons, int indent)
	{
	}

	private static Pawn GetPawn()
	{
		Thing singleSelectedThing = Find.Selector.SingleSelectedThing;
		if (singleSelectedThing is Pawn result)
		{
			return result;
		}
		if (singleSelectedThing is Corpse corpse)
		{
			return corpse.InnerPawn;
		}
		return null;
	}

	public static bool SettingsCheckBox(Rect barRect, int id, Texture icon, ref bool value, float iconSize = 1f)
	{
		Rect rect = new Rect(barRect.xMax - 54f * (float)(id + 1), barRect.y, 46f, barRect.height);
		Rect rect2 = rect.LeftHalf();
		rect2.width = rect2.height;
		Rect rect3 = rect.RightHalf();
		rect3.xMin = rect3.xMax - rect3.height;
		if (Mouse.IsOver(rect))
		{
			GUI.color = Color.white;
		}
		else
		{
			GUI.color = Assets.ColorButtons;
		}
		GUI.DrawTexture(rect2, (Texture)(value ? Assets.Checkbox1Tex : Assets.Checkbox0Tex));
		GUI.DrawTexture(rect3.ContractedBy(-2f).ScaledBy(iconSize), icon);
		if (Widgets.ButtonInvisible(rect))
		{
			GUI.color = Color.white;
			value = !value;
			return true;
		}
		GUI.color = Color.white;
		return false;
	}

	public static void SettingsOpenButton(Rect barRect)
	{
		Rect rect = new Rect(barRect.x, barRect.y, barRect.height, barRect.height);
		if (Mouse.IsOver(rect))
		{
			GUI.color = Color.white;
		}
		else
		{
			GUI.color = Assets.ColorButtons;
		}
		GUI.DrawTexture(rect, (Texture)Assets.SettingsTex);
		if (Widgets.ButtonInvisible(rect))
		{
			Dialog_ModSettings window = new Dialog_ModSettings(NiceInventoryTabMod.instance);
			Find.WindowStack.Add(window);
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
		}
	}

	public static void CreateLayout()
	{
		root = new VLayout
		{
			Geometry = new Rect(0f, 0f, 80f, 60f),
			MarginTop = 26f,
			MarginLeft = 6f,
			MarginRight = 6f,
			MarginBottom = 6f
		};
		row_top = new HLayout
		{
			Stretch = 1.46f
		};
		HLayout hLayout = new HLayout
		{
			Stretch = 4f
		};
		mass_zone = new GroupBox("NIT_MassAndBulk".Translate(), 0.56f);
		root.AddChild(row_top);
		root.AddChild(hLayout);
		VLayout vLayout = new VLayout
		{
			Stretch = 2f
		};
		vLayout.Spacing = 3f;
		apparel_zone = new GroupBox("NIT_Apparel".Translate(), 2f);
		apparel_zone.SetVerticalLayout(Scrollable: true);
		hLayout.AddChild(vLayout);
		hLayout.AddChild(apparel_zone);
		inventory_zone = new GroupBox("NIT_Inventory".Translate(), 1.9f);
		inventory_zone.SetHorizonalLayout(Scrollable: true);
		equipment_zone = new GroupBox("NIT_Equipment".Translate(), 3f);
		equipment_zone.SetVerticalLayout(Scrollable: true);
		vLayout.AddChild(equipment_zone);
		vLayout.AddChild(inventory_zone);
		vLayout.AddChild(mass_zone);
		GroupBox groupBox = new GroupBox("NIT_Combat".Translate(), 1f);
		GroupBox groupBox2 = new GroupBox("NIT_Resistances".Translate(), 1f);
		row_top.AddChild(groupBox);
		row_top.AddChild(groupBox2);
		groupBox.SetVerticalLayout();
		groupBox.MarginTop = GroupBox.TitleHeight + 8f;
		FillCombatZone(groupBox);
		groupBox2.SetVerticalLayout();
		groupBox2.MarginTop = GroupBox.TitleHeight + 8f;
		FillResistZone(groupBox2);
		fr_mass = new FloatRef();
		fr_dgt_mass = new FloatRef();
		mass_zone.AddChild(new StatBar("NIT_Mass".Translate(), "", MassStatUtility.AllMass, MassStatUtility.Capacity, Assets.ICMass, fr_mass, fr_dgt_mass).SetFormat(Assets.Format_KG));
		mass_zone.MarginTop = GroupBox.TitleHeight + 6f;
		SwitchStats();
	}

	private static void FillResistZone(GroupBox resist_zone)
	{
		FloatRef tsep = new FloatRef();
		FloatRef digitSep = new FloatRef();
		StatBar statBar = new StatBar("NIT_Sharp".Translate(), "", ArmorUtility.SharpArmor, ArmorUtility.MaxSharpArmor, Assets.ICArmorSharp, tsep, digitSep);
		StatBar statBar2 = new StatBar("NIT_Blunt".Translate(), "", ArmorUtility.BluntArmor, ArmorUtility.MaxBluntArmor, Assets.ICArmorBlunt, tsep, digitSep);
		StatBar statBar3 = new StatBar("NIT_Heat".Translate(), "", ArmorUtility.HeatArmor, ArmorUtility.MaxHeatArmor, Assets.ICArmorHeat, tsep, digitSep);
		ArmorUtility.SharpFormat.SetFor(statBar);
		ArmorUtility.BluntFormat.SetFor(statBar2);
		ArmorUtility.HeatFormat.SetFor(statBar3);
		StatDrawer statDrawer = new StatRange("ComfyTemperatureRange".Translate() + ": ", "", (Pawn p, StatDrawer s) => (p.GetStatValue(StatDefOf.ComfyTemperatureMin), p.GetStatValue(StatDefOf.ComfyTemperatureMax))).SetFormatMode(StatDrawer.FormatMode.Temperature);
		(statDrawer as StatRange).AltTitle = "NIT_TemperatureRangeShort".Translate() + ": ";
		Var1Stats.Add(statBar);
		Var1Stats.Add(statBar2);
		Var1Stats.Add(statBar3);
		Var1Stats.Add(statDrawer);
		resist_zone.AddChild(statBar);
		resist_zone.AddChild(statBar2);
		resist_zone.AddChild(statBar3);
		resist_zone.AddChild(statDrawer);
		StatDrawer statDrawer2 = new StatBar("NIT_Toxic".Translate(), "", ArmorUtility.ToxicResist, ArmorUtility.MaxPercent, Assets.ICToxic, tsep, digitSep).SetFormatMode(StatDrawer.FormatMode.Percent);
		StatDrawer statDrawer3 = new StatBar("NIT_Vacuum".Translate(), "", ArmorUtility.VacuumResist, ArmorUtility.MaxPercent, Assets.ICVacuum, tsep, digitSep).SetFormatMode(StatDrawer.FormatMode.Percent);
		StatDrawer statDrawer4 = new StatBar("NIT_Flammability".Translate(), "", ArmorUtility.Flamability, ArmorUtility.MaxPercent, Assets.ICFlame, tsep, digitSep).SetFormatMode(StatDrawer.FormatMode.Percent);
		StatDrawer statDrawer5 = new StatBar("NIT_Psychic".Translate(), "", ArmorUtility.PsychicSensitivity, ArmorUtility.MaxPsychicSensitivity, Assets.ICPsy, tsep, digitSep).SetFormatMode(StatDrawer.FormatMode.Percent);
		Var2Stats.Add(statDrawer2);
		Var2Stats.Add(statDrawer3);
		Var2Stats.Add(statDrawer4);
		Var2Stats.Add(statDrawer5);
		resist_zone.AddChild(statDrawer2);
		resist_zone.AddChild(statDrawer3);
		resist_zone.AddChild(statDrawer4);
		resist_zone.AddChild(statDrawer5);
	}

	private static void FillCombatZone(GroupBox combat_zone)
	{
		FloatRef tsep = new FloatRef();
		FloatRef digitSep = new FloatRef();
		StatBar statBar = new StatBar("NIT_Melee".Translate(), "", DamageUtility.MeleePawnDPS, DamageUtility.MaxMeleeDPS, Assets.ICDamageMelee, tsep, digitSep);
		StatBar statBar2 = new StatBar("NIT_Ranged".Translate(), "", DamageUtility.RangedPawnDPS, DamageUtility.MaxRangedDPS, Assets.ICDamageRanged, tsep, digitSep);
		StatDrawer statDrawer = new StatBarOptimalRange("NIT_Range".Translate(), "", DamageUtility.GetRange, DamageUtility.GetMaxRange, Assets.ICRange, tsep, digitSep).SetFormat(Assets.Format_Meters);
		StatDrawer statDrawer2 = new StatBar("NIT_Speed".Translate(), "", MobilityUtility.MoveSpeed, MobilityUtility.MaxMoveSpeed, Assets.ICMoveSpeed, tsep, digitSep).SetFormat(Assets.Format_MoveSpeed);
		Var1Stats.Add(statBar);
		Var1Stats.Add(statBar2);
		Var1Stats.Add(statDrawer);
		Var1Stats.Add(statDrawer2);
		combat_zone.AddChild(statBar);
		combat_zone.AddChild(statBar2);
		combat_zone.AddChild(statDrawer);
		combat_zone.AddChild(statDrawer2);
		StatDrawer statDrawer3 = new StatBar("NIT_Medical".Translate(), "", CommonStatUtility.TendQuality, CommonStatUtility.MaxTendQuality, Assets.ICMedical, tsep, digitSep).SetFormatMode(StatDrawer.FormatMode.Percent);
		StatDrawer statDrawer4 = new StatBar("NIT_Social".Translate(), "", CommonStatUtility.SocialImpact, CommonStatUtility.MaxSocialImpact, Assets.ICSocial, tsep, digitSep).SetFormatMode(StatDrawer.FormatMode.Percent);
		StatDrawer statDrawer5 = new StatBar("NIT_Negotiation".Translate(), "", CommonStatUtility.NegotiationFactor, CommonStatUtility.MaxNegoriation, Assets.ICChat, tsep, digitSep).SetFormatMode(StatDrawer.FormatMode.Percent);
		StatDrawer statDrawer6 = new StatBar("NIT_WorkEfficiency".Translate(), "", WorkStatUtility.WorkEfficiency, WorkStatUtility.MaxWorkEfficiency, Assets.ICWork, tsep, digitSep).SetFormatMode(StatDrawer.FormatMode.Percent);
		Var2Stats.Add(statDrawer3);
		Var2Stats.Add(statDrawer4);
		Var2Stats.Add(statDrawer5);
		Var2Stats.Add(statDrawer6);
		combat_zone.AddChild(statDrawer3);
		combat_zone.AddChild(statDrawer4);
		combat_zone.AddChild(statDrawer5);
		combat_zone.AddChild(statDrawer6);
	}

	public static bool ShouldBeEquipment(Apparel x)
	{
		if (Assets.ApparelLayerDefOf_Shield != null && x.def.apparel.layers.Contains(Assets.ApparelLayerDefOf_Shield))
		{
			return true;
		}
		return x.def.apparel.layers.Contains(ApparelLayerDefOf.Belt);
	}

	public static bool ShouldBeEquipment(ApparelLayerDef x)
	{
		if (Assets.ApparelLayerDefOf_Shield != null && x == Assets.ApparelLayerDefOf_Shield)
		{
			return true;
		}
		return x == ApparelLayerDefOf.Belt;
	}

	public static void InspectWornApparel(Pawn pawn)
	{
		wornApparel.Clear();
		if (pawn.apparel != null)
		{
			wornApparel.AddRange(from x in pawn.apparel.WornApparel
				where !ShouldBeEquipment(x)
				orderby x.def.apparel.bodyPartGroups[0].listOrder descending
				select x);
		}
	}

	public static void InspectWornEquipment(Pawn pawn)
	{
		wornEquipment.Clear();
		if (pawn.equipment == null)
		{
			return;
		}
		if (Settings.WeaponsAreEquipment && pawn.inventory?.innerContainer != null && !SomeKindOfAnimal(pawn))
		{
			Thing weapon = DamageUtility.GetPawnWeapon(pawn);
			if (weapon != null)
			{
				wornEquipment.Add(weapon);
			}
			wornEquipment.AddRange(pawn.inventory.innerContainer.InnerListForReading.Where((Thing x) => x.def.IsWeapon && !BlackListedWeapon(x.def)));
			wornEquipment.AddRange(pawn.equipment.AllEquipmentListForReading.Where((ThingWithComps x) => x != weapon));
		}
		else
		{
			wornEquipment.AddRange(pawn.equipment.AllEquipmentListForReading);
		}
		wornEquipment.AddRange(pawn.apparel.WornApparel.Where((Apparel x) => ShouldBeEquipment(x)));
	}

	private static bool BlackListedWeapon(ThingDef def)
	{
		return def == ThingDefOf.WoodLog;
	}

	private static bool SomeKindOfAnimal(Pawn pawn)
	{
		if (!pawn.IsAnimal)
		{
			return pawn.equipment == null;
		}
		return true;
	}

	public static void InspectWornInventory(Pawn pawn)
	{
		wornInventory.Clear();
		if (pawn.inventory?.innerContainer == null)
		{
			return;
		}
		if (Settings.WeaponsAreEquipment && !SomeKindOfAnimal(pawn))
		{
			wornInventory.AddRange(pawn.inventory.innerContainer.InnerListForReading.Where((Thing x) => !x.def.IsWeapon || BlackListedWeapon(x.def)));
		}
		else
		{
			wornInventory.AddRange(pawn.inventory.innerContainer.InnerListForReading);
		}
	}

	private static int GetListVersion(List<Thing> list)
	{
		if (list == null || list.Count == 0)
		{
			return 0;
		}
		int num = list.Count;
		int count = list.Count;
		for (int i = 0; i < count; i++)
		{
			num = num * 31 + (list[i]?.GetHashCode() ?? 0);
		}
		return num;
	}

	public static void Recache(Pawn pawn)
	{
		if (!shouldRecache && Time.frameCount % CacheInterval != 0)
		{
			return;
		}
		bool flag = false;
		InspectWornApparel(pawn);
		int listVersion = GetListVersion(wornApparel);
		if (listVersion != lastWornApparelVersion)
		{
			if (wornApparel.Count > 0)
			{
				Recache_Items(apparel_zone, pawn, wornApparel);
			}
			else
			{
				Clear_Items(apparel_zone);
			}
			lastWornApparelVersion = listVersion;
			flag = true;
		}
		InspectWornEquipment(pawn);
		int listVersion2 = GetListVersion(wornEquipment);
		if (listVersion2 != lastWornEquipmentVersion)
		{
			if (wornEquipment.Count > 0)
			{
				Recache_Items(equipment_zone, pawn, wornEquipment, weapFRef);
			}
			else
			{
				Clear_Items(equipment_zone);
			}
			lastWornEquipmentVersion = listVersion2;
			flag = true;
		}
		else
		{
			foreach (Widget item in equipment_zone.InLayout.Childs.Where((Widget x) => x is EquippedItem equippedItem && equippedItem.ShouldRecache))
			{
				(item as EquippedItem).UpdateStats();
			}
		}
		InspectWornInventory(pawn);
		int listVersion3 = GetListVersion(wornInventory);
		if (listVersion3 != lastInventoryVersion)
		{
			if (wornInventory.Count > 0)
			{
				Recache_Inventory_Items(inventory_zone, pawn, wornInventory);
			}
			else
			{
				Clear_Items(inventory_zone);
			}
			lastInventoryVersion = listVersion3;
			flag = true;
		}
		if (Settings.ApparelSlotsVisible && PawnCanWearApparel(pawn) && (flag || shouldRecache))
		{
			apparel_zone.InLayout.Childs.RemoveAll((Widget x) => x is EquipmentEmptySlot);
			equipment_zone.InLayout.Childs.RemoveAll((Widget x) => x is EquipmentEmptySlot);
			inventory_zone.InLayout.Childs.RemoveAll((Widget x) => x is EquipmentEmptySlot);
			RecacheEmptySlots(pawn);
			flag = true;
		}
		else if (!Settings.ApparelSlotsVisible && shouldRecache && apparel_zone.InLayout.Childs.RemoveAll((Widget x) => x is EquipmentEmptySlot) + equipment_zone.InLayout.Childs.RemoveAll((Widget x) => x is EquipmentEmptySlot) + inventory_zone.InLayout.Childs.RemoveAll((Widget x) => x is EquipmentEmptySlot) > 0)
		{
			flag = true;
		}
		Recache_Recursive(root, pawn);
		if (flag || shouldRecache)
		{
			root.Update();
		}
		lastCacheTick = Find.TickManager.TicksGame;
		shouldRecache = false;
	}

	private static bool PawnCanWearApparel(Pawn pawn)
	{
		if (!pawn.RaceProps.Humanlike || pawn.IsMutant || pawn.RaceProps.IsMechanoid || pawn.apparel == null)
		{
			return false;
		}
		return true;
	}

	private static void RecacheEmptySlots(Pawn pawn)
	{
		List<ApparelSlotUtility.PotentialSlot> availableSlots = ApparelSlotUtility.GetAvailableSlots(pawn);
		foreach (ApparelLayerDef layer in ApparelSlotUtility.GetAvailableLayers(pawn, availableSlots))
		{
			List<BodyPartGroupDef> availableBodyPartGroupsOnLayer = ApparelSlotUtility.GetAvailableBodyPartGroupsOnLayer(pawn, layer, availableSlots);
			EquipmentEmptySlot equipmentEmptySlot = new EquipmentEmptySlot(layer, (layer == ApparelLayerDefOf.Belt) ? Assets.BeltSlotTex : null, availableBodyPartGroupsOnLayer, availableSlots.Where((ApparelSlotUtility.PotentialSlot x) => x.layer == layer).ToList());
			if (!ShouldBeEquipment(layer))
			{
				apparel_zone.AddChild(equipmentEmptySlot);
			}
			else
			{
				equipment_zone.AddChild(equipmentEmptySlot);
			}
			if (Settings.EXR_EnableProgressBars)
			{
				equipmentEmptySlot.SolveProgressBar(pawn);
			}
		}
	}

	public static void Recache_Recursive(Widget w, Pawn pawn)
	{
		if (w is StatDrawer { Visible: not false } statDrawer)
		{
			statDrawer.UpdateValues(pawn);
		}
		foreach (Widget child in w.Childs)
		{
			Recache_Recursive(child, pawn);
		}
	}

	public static void Clear_Items(GroupBox gb)
	{
		gb.InLayout.Childs.Clear();
	}

	public static void Recache_Items(GroupBox gb, Pawn pawn, List<Thing> items, FloatRef fref = null)
	{
		gb.InLayout.Childs.Clear();
		foreach (Thing item in items)
		{
			EquippedItem equippedItem = MakeItemSlot(item, pawn, fref);
			equippedItem.UpdateStats();
			gb.AddChild(equippedItem);
		}
	}

	public static EquippedItem MakeItemSlot(Thing item, Pawn pawn, FloatRef fref)
	{
		return new EquippedItem(item, pawn, inventory: false, fref);
	}

	public static void Recache_Inventory_Items(GroupBox gb, Pawn pawn, List<Thing> items)
	{
		gb.InLayout.Childs.Clear();
		foreach (Thing item in items)
		{
			InventoryItem inventoryItem = new InventoryItem(item, pawn);
			inventoryItem.UpdateStats();
			gb.AddChild(inventoryItem);
		}
	}
}
