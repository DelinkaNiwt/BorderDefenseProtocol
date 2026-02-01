using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NiceInventoryTab;

public class InventoryItem : Widget
{
	public Thing Item;

	public Pawn pawn;

	private (float cur, float max) mass;

	private static readonly MethodInfo InterfaceDrop = typeof(ITab_Pawn_Gear).GetMethod("InterfaceDrop", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly PropertyInfo CanControl = typeof(ITab_Pawn_Gear).GetProperty("CanControl", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly PropertyInfo CanControlColonist = typeof(ITab_Pawn_Gear).GetProperty("CanControlColonist", BindingFlags.Instance | BindingFlags.NonPublic);

	private bool toolTipShowed;

	private bool IsButtonSelected;

	public InventoryItem(Thing item, Pawn p)
	{
		pawn = p;
		Item = item;
		MinimalHeight = 100f;
		MaximalHeight = MinimalHeight;
		MinimalWidth = 115f;
		MaximalWidth = MinimalWidth;
	}

	public void UpdateStats()
	{
		float a = MassUtility.InventoryMass(pawn) * 0.9f;
		mass = (cur: Item.GetStatValue(StatDefOf.Mass) * (float)Item.stackCount, max: Mathf.Max(a, 1f));
	}

	private bool UnderControl()
	{
		return CommandUtility.CanControl(pawn);
	}

	public override void Draw()
	{
		toolTipShowed = false;
		Rect org = Geometry.ContractedBy(6f);
		Widgets.DrawBoxSolid(Geometry, Assets.ColorBGD);
		var (iconRect, org2) = Utils.SplitRectByBottomPart(org, 30f, 6f);
		DrawIcon(iconRect);
		var (rect, rect2) = Utils.SplitRectByBottomPart(org2, 12f, 2f);
		DrawLabelAndButtons(rect);
		DrawBar(rect2, "Mass".Translate() + (": " + mass.cur.ToString(Assets.Format_KG)), mass.cur, mass.max, Assets.ICMass, 3);
		if (!toolTipShowed && Mouse.IsOver(Geometry))
		{
			GUI.color = Color.white;
			Widgets.DrawHighlight(Geometry.ContractedBy(2f));
			TooltipHandler.TipRegion(Geometry, Item.GetTooltip());
		}
		if (Widgets.ButtonInvisible(Geometry))
		{
			if (Event.current.button == 0)
			{
				Find.WindowStack.Add(new Dialog_InfoCard(Item));
				Event.current.Use();
			}
			else if (Event.current.button == 1)
			{
				OpenContextMenu();
				Event.current.Use();
			}
		}
		DrawStars(Geometry);
	}

	private void OpenContextMenu()
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>();
		list.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
		{
			Find.WindowStack.Add(new Dialog_InfoCard(Item));
		}));
		bool flag = UnderControl();
		int num;
		if (flag)
		{
			num = (pawn.IsColonistPlayerControlled ? 1 : 0);
			if (num != 0 && pawn.equipment != null)
			{
				if (Item.def.IsWeapon)
				{
					Thing item = Item;
					ThingWithComps ItemComps = item as ThingWithComps;
					if (ItemComps != null)
					{
						string text = "NIT_Equip".Translate();
						if (Item.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
						{
							text += " " + "EquipWarningBrawler".Translate();
						}
						list.Add(new FloatMenuOption(text, (pawn.story != null && pawn.WorkTagIsDisabled(WorkTags.Violent)) ? null : ((Action)delegate
						{
							CommandUtility.CommandEquipWeaponFromInventory(pawn, ItemComps);
						})));
					}
				}
			}
		}
		else
		{
			num = 0;
		}
		if (num != 0 && Item is Apparel)
		{
			list.Add(new FloatMenuOption("NIT_WearFromInventory".Translate(), delegate
			{
				CommandUtility.CommandWearFromInventory(pawn, Item);
			}));
		}
		bool flag2 = pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanUnequip(Item, pawn);
		bool flag3 = Item is Apparel apparel && pawn.apparel != null && pawn.apparel.IsLocked(apparel);
		bool flag4 = !(flag2 || flag3);
		if (flag && flag4)
		{
			if (flag3)
			{
				list.Add(new FloatMenuOption("DropThingLocked".Translate(), null));
			}
			else if (flag2)
			{
				list.Add(new FloatMenuOption("DropThingLodger".Translate(), null));
			}
			else
			{
				list.Add(new FloatMenuOption("DropThing".Translate(), delegate
				{
					CommandUtility.CommandDrop(pawn, Item);
				}));
			}
		}
		else
		{
			list.Add(new FloatMenuOption("NIT_CannotDrop".Translate(), null));
		}
		if (num != 0 && FoodUtility.WillIngestFromInventoryNow(pawn, Item))
		{
			list.Add(new FloatMenuOption("ConsumeThing".Translate(Item.LabelNoCount, Item), delegate
			{
				FoodUtility.IngestFromInventoryNow(pawn, Item);
			}));
		}
		if (list.Count > 0)
		{
			Find.WindowStack.Add(new FloatMenu(list));
		}
	}

	private void DrawLabelAndButtons(Rect rect)
	{
		Text.Font = GameFont.Small;
		IsButtonSelected = false;
		bool num = UnderControl();
		bool flag = num && pawn.IsColonistPlayerControlled;
		if (ModIntegration.NUPActive && NonUnoPinataIntegration.CanStrip(pawn, Item))
		{
			(Rect left, Rect right) tuple = Utils.SplitRectByRightPart(rect, rect.height, 4f);
			Rect item = tuple.left;
			Rect item2 = tuple.right;
			rect = item;
			NonUnoPinataIntegration.CacheTex();
			if (NonUnoPinataIntegration.ShouldStrip(Item))
			{
				if (DrawWhiteButton(item2, delegate(Rect r)
				{
					TooltipHandler.TipRegion(r, "StripThingCancel".Translate());
				}, NonUnoPinataIntegration.Strip_Thing_Cancel, active: true))
				{
					NonUnoPinataIntegration.SetShouldStrip(v: false, pawn, Item);
				}
			}
			else if (DrawWhiteButton(item2, delegate(Rect r)
			{
				TooltipHandler.TipRegion(r, "StripThing".Translate());
			}, NonUnoPinataIntegration.Strip_Thing, active: true))
			{
				NonUnoPinataIntegration.SetShouldStrip(v: true, pawn, Item);
			}
		}
		if (num)
		{
			bool lodgerCantDrop = pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanUnequip(Item, pawn);
			bool apparelLocked = Item is Apparel apparel && pawn.apparel != null && pawn.apparel.IsLocked(apparel);
			bool active = !(lodgerCantDrop || apparelLocked);
			(Rect left, Rect right) tuple2 = Utils.SplitRectByRightPart(rect, rect.height, 4f);
			Rect item3 = tuple2.left;
			Rect item4 = tuple2.right;
			rect = item3;
			if (DrawWhiteButton(item4, delegate(Rect r)
			{
				if (apparelLocked)
				{
					TooltipHandler.TipRegion(r, "DropThingLocked".Translate());
				}
				else if (lodgerCantDrop)
				{
					TooltipHandler.TipRegion(r, "DropThingLodger".Translate());
				}
				else
				{
					TooltipHandler.TipRegion(r, "DropThing".Translate());
				}
			}, TexButton.Drop, active) && (!ModsConfig.BiotechActive || !MechanitorUtility.TryConfirmBandwidthLossFromDroppingThing(pawn, Item, dropAction)))
			{
				dropAction();
			}
		}
		if (flag && FoodUtility.WillIngestFromInventoryNow(pawn, Item))
		{
			(Rect left, Rect right) tuple3 = Utils.SplitRectByRightPart(rect, rect.height, 4f);
			Rect item5 = tuple3.left;
			Rect item6 = tuple3.right;
			rect = item5;
			if (DrawWhiteButton(item6, delegate(Rect r)
			{
				TooltipHandler.TipRegionByKey(r, "ConsumeThing", Item.LabelNoCount, Item);
			}, TexButton.Ingest, active: true))
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				FoodUtility.IngestFromInventoryNow(pawn, Item);
			}
		}
		if (flag && pawn.equipment != null && Item.def.IsWeapon && Item is ThingWithComps item7)
		{
			(Rect left, Rect right) tuple4 = Utils.SplitRectByRightPart(rect, rect.height, 4f);
			Rect item8 = tuple4.left;
			Rect item9 = tuple4.right;
			rect = item8;
			bool active2 = pawn.story == null || !pawn.WorkTagIsDisabled(WorkTags.Violent);
			if (DrawWhiteButton(item9, delegate(Rect r)
			{
				string text = "NIT_Equip".Translate();
				if (Item.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
				{
					text += " (" + "EquipWarningBrawler".Translate() + ")";
				}
				TooltipHandler.TipRegion(r, text);
			}, Assets.SwapButtonTex, active2))
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				CommandUtility.CommandEquipWeaponFromInventory(pawn, item7);
			}
		}
		if (flag && Item is Apparel)
		{
			(Rect left, Rect right) tuple5 = Utils.SplitRectByRightPart(rect, rect.height, 4f);
			Rect item10 = tuple5.left;
			Rect item11 = tuple5.right;
			rect = item10;
			if (DrawWhiteButton(item11, delegate(Rect r)
			{
				string text = "NIT_Equip".Translate();
				TooltipHandler.TipRegion(r, text);
			}, Assets.SwapButtonTex, active: true))
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				CommandUtility.CommandWearFromInventory(pawn, Item);
			}
		}
		Text.Anchor = TextAnchor.MiddleLeft;
		GUI.color = Assets.ColorStat;
		Assets.DrawCroppedText(rect, Item.LabelCapNoCount);
		Text.Anchor = TextAnchor.UpperLeft;
		GUI.color = Color.white;
		void dropAction()
		{
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
			CommandUtility.CommandDrop(pawn, Item);
		}
	}

	private bool DrawWhiteButton(Rect rect, Action<Rect> drawTooltip, Texture2D tex, bool active)
	{
		bool flag = false;
		rect = rect.ContractedBy(-2f);
		Rect rect2 = rect.ContractedBy(-4f);
		if (Mouse.IsOver(rect2) && !IsButtonSelected)
		{
			IsButtonSelected = true;
			drawTooltip(rect2);
			toolTipShowed = true;
			flag = true;
		}
		if (!active)
		{
			GUI.color = Color.gray;
		}
		else if (flag)
		{
			GUI.color = GenUI.MouseoverColor;
		}
		else
		{
			GUI.color = Color.white;
		}
		GUI.DrawTexture(rect, (Texture)tex);
		GUI.color = Color.white;
		if (Widgets.ButtonInvisible(rect2) && active)
		{
			return true;
		}
		return false;
	}

	private void DrawIcon(Rect iconRect)
	{
		Widgets.DrawBoxSolid(iconRect, Assets.ColorBG);
		ItemIconHelper.ThingIcon(Utils.RectCentered(iconRect.center, iconRect.height), Item);
		if (Item.stackCount > 1)
		{
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.LowerRight;
			Assets.LabelShadowed(iconRect, $"x{Item.stackCount}", Color.white);
			Text.Anchor = TextAnchor.UpperLeft;
		}
	}

	private void DrawStars(Rect globalRect)
	{
		if (Settings.EXR_QualityStars && Item.TryGetQuality(out var qc) && (!Settings.ShowQualityOnlyOnHover(qc) || Mouse.IsOver(Geometry)))
		{
			int num = (int)qc;
			float num2 = Settings.StarSize * 6f;
			Rect rect = new Rect(globalRect.center.x - num2 * 0.5f, globalRect.y - Settings.StarSize * 0.5f + 2f, num2, Settings.StarSize);
			for (int i = 0; i < 6; i++)
			{
				GUI.color = Settings.GetQualityColor(qc, i < num);
				GUI.DrawTexture(new Rect(rect.x + Settings.StarSize * (float)i, rect.y, Settings.StarSize, Settings.StarSize), (Texture)Assets.QualityTex);
			}
		}
	}

	private void DrawBar(Rect rect, string tooltip, float value, float maxValue, Assets.IconColor iconAndColor, int ticks = 6)
	{
		rect.height = 12f;
		(Rect left, Rect right) tuple = Utils.SplitRectByLeftPart(rect, rect.height, 2f);
		Rect item = tuple.left;
		Rect item2 = tuple.right;
		item2.y = rect.center.y - EquippedItem.barMaxHeight * 0.5f;
		item2.height = EquippedItem.barMaxHeight;
		GUI.color = Color.white;
		GUI.DrawTexture(item, (Texture)iconAndColor.Icon);
		float pct = Mathf.Clamp01(value / maxValue);
		Color color = iconAndColor.Color;
		Widgets.DrawBoxSolid(item2, Color.Lerp(Assets.ColorBGD, color, 0.3f));
		Widgets.DrawBoxSolid(item2.LeftPart(pct), color);
		Rect rect2 = item2;
		float num = rect2.width / (float)ticks;
		int num2 = Mathf.FloorToInt(rect2.width / num);
		GUI.color = Assets.ColorBG;
		for (int i = 1; i < num2; i++)
		{
			GUI.DrawTexture(new Rect(rect2.x + (float)i * num, rect2.y + rect2.height / 2f, 2f, rect2.height / 2f), (Texture)BaseContent.WhiteTex);
		}
		if (!toolTipShowed && Mouse.IsOver(rect.ContractedBy(0f, -2f)))
		{
			GUI.color = Color.white;
			Widgets.DrawHighlight(rect);
			TooltipHandler.TipRegion(rect, tooltip);
			toolTipShowed = true;
		}
	}
}
