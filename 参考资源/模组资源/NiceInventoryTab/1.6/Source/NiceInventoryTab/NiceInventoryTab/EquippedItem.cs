using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace NiceInventoryTab;

public class EquippedItem : Widget
{
	public Thing Item;

	public Pawn pawn;

	public bool Inventory;

	public (float cur, float max) mass;

	public float armor_sharp;

	public float armor_blunt;

	public float armor_heat;

	public float dps;

	public float damage;

	public float armor_penetration_sharp;

	public float armor_penetration_blunt;

	private string ProblemDescr;

	private bool HasProblems;

	private bool ApparelFromDeadBody;

	private bool BioCoded;

	public bool toolTipShowed;

	public FloatRef ExtraInfoSep;

	public float? warmupTime;

	public float cooldownTime;

	public float? stoppingPower;

	public DamageDef damageType;

	public float? fireRate;

	public float meleeCooldown;

	public float meleeHitChance;

	public float meleeMaxDamage;

	public bool hasFlameDamage;

	public bool hasEmpDamage;

	public bool forcedWear;

	public static float barMaxHeight = 8f;

	private bool IsButtonSelected;

	public bool ShouldRecache => Item.def.IsWeapon;

	public EquippedItem(Thing item, Pawn p, bool inventory = false, FloatRef tsep = null)
	{
		pawn = p;
		Item = item;
		SetFixedHeight(60f);
		Inventory = inventory;
		ExtraInfoSep = tsep ?? new FloatRef();
	}

	public virtual void UpdateStats()
	{
		mass = (cur: Item.GetStatValue(StatDefOf.Mass), max: MassStatUtility.Capacity(pawn) / 2f);
		if (Item.def.IsWeapon)
		{
			if (Item.def.IsMeleeWeapon)
			{
				dps = DamageUtility.MeleeWeaponDPS(Item, pawn);
				meleeCooldown = DamageUtility.GetMeleeCooldown(pawn, Item.def);
				meleeHitChance = DamageUtility.GetMeleeHitChance(pawn, Item);
				meleeMaxDamage = DamageUtility.GetMaxHitDamage(Item);
				hasFlameDamage = DamageUtility.MeleeWeaponHasExtraDamage(Item, DamageDefOf.Flame);
				hasEmpDamage = DamageUtility.MeleeWeaponHasExtraDamage(Item, DamageDefOf.EMP);
			}
			else if (Item.def.IsRangedWeapon)
			{
				dps = DamageUtility.RangedWeaponDPS(Item);
				damage = DamageUtility.GetOneShotDamage(Item);
				float num = (pawn.WorkTagIsDisabled(WorkTags.Violent) ? 1f : pawn.GetStatValue(StatDefOf.AimingDelayFactor));
				float? warmup = DamageUtility.GetWarmup(Item);
				cooldownTime = Item.def.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown);
				if (warmup.HasValue)
				{
					warmupTime = warmup.Value * num;
				}
				else
				{
					warmupTime = null;
				}
				stoppingPower = DamageUtility.GetStoppingPower(Item);
				damageType = DamageUtility.RangedDamageType(Item);
				fireRate = DamageUtility.GetFireRate(Item);
			}
			armor_penetration_sharp = DamageUtility.GetArmorPenetration(Item).GetValueOrDefault();
		}
		else if (Item.def.IsApparel)
		{
			armor_sharp = Item.GetStatValue(StatDefOf.ArmorRating_Sharp);
			armor_blunt = Item.GetStatValue(StatDefOf.ArmorRating_Blunt);
			armor_heat = Item.GetStatValue(StatDefOf.ArmorRating_Heat);
			ApparelFromDeadBody = (Item as Apparel).WornByCorpse && ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.DeadMansApparel) && !pawn.Dead;
		}
		if (Item is Apparel ap && UnderControl())
		{
			forcedWear = pawn.outfits != null && pawn.outfits.forcedHandler.IsForced(ap);
		}
		BioCoded = false;
		if (Item.HasComp<CompBiocodable>())
		{
			CompBiocodable compBiocodable = Item.TryGetComp<CompBiocodable>();
			BioCoded = compBiocodable.Biocoded;
			if (BioCoded && compBiocodable.CodedPawn == pawn && UnderControl())
			{
				BioCoded = false;
			}
		}
		HasProblems = ShouldShowWarning();
	}

	public virtual void DrawWeaponInfo(Rect rect)
	{
		(Rect top, Rect bottom) tuple = Utils.SplitRectVertical(rect, 0.3333f);
		var (rect2, _) = tuple;
		var (rect3, rect4) = Utils.SplitRectVertical(tuple.bottom, 0.5f);
		if (Item.def.IsRangedWeapon)
		{
			string text = (warmupTime.HasValue ? warmupTime.Value.ToString("F1") : "--");
			DrawIconText(rect2, Assets.ICReload.Icon, text + "+" + cooldownTime.ToString(Assets.Format_Seconds), FieldTooltipHelper.WeaponReloadTimeShort);
			if (damageType != DamageDefOf.Bullet)
			{
				Assets.IconColor iconColor = SelectIconByDamage(damageType);
				DrawIconText(rect3, iconColor.Icon, damageType.LabelCap, (Thing w, Pawn p) => $"{damageType.LabelCap}: {damage:F2}", iconColor.Color);
			}
			else if (stoppingPower.HasValue)
			{
				DrawIconText(rect3, Assets.ICStop.Icon, stoppingPower.Value.ToString("F1"), FieldTooltipHelper.WeaponStoppingPower);
			}
			if (fireRate.HasValue)
			{
				DrawIconText(rect4, Assets.ICFireRate.Icon, fireRate.Value.ToString(Assets.Format_FireRate), FieldTooltipHelper.WeaponFireRate);
			}
			else
			{
				DrawIconText(rect4, Assets.ICDamageRanged.Icon, DamageUtility.GetOneShotDamage(Item).ToString("0.#"), FieldTooltipHelper.RangedWeaponDamage);
			}
		}
		else if (Item.def.IsMeleeWeapon)
		{
			DrawIconText(rect2, Assets.ICReload.Icon, meleeCooldown.ToString(Assets.Format_Seconds), FieldTooltipHelper.WeaponMeleeAttackSpeed);
			DrawIconText(rect3, Assets.ICMeleeAccuracy.Icon, meleeHitChance.ToStringPercent(), FieldTooltipHelper.WeaponMeleeAccuracy);
			if (hasFlameDamage)
			{
				DrawIconText(rect4, Assets.ICFlame.Icon, meleeMaxDamage.ToString("0.#"), FieldTooltipHelper.WeaponMeleeDamage, Assets.ICFlame.Color);
			}
			else if (hasEmpDamage)
			{
				DrawIconText(rect4, Assets.ICEMP.Icon, meleeMaxDamage.ToString("0.#"), FieldTooltipHelper.WeaponMeleeDamage, Assets.ICEMP.Color);
			}
			else
			{
				DrawIconText(rect4, Assets.ICDamageMelee.Icon, meleeMaxDamage.ToString("0.#"), FieldTooltipHelper.WeaponMeleeDamage);
			}
		}
	}

	public override void Draw()
	{
		toolTipShowed = false;
		Rect rect = Geometry.ContractedBy(6f);
		Widgets.DrawBoxSolid(Geometry, Assets.ColorBGD);
		if (HasProblems)
		{
			GUI.color = Assets.ICWarningBG.Color;
			GUI.DrawTexture(Geometry.ContractedBy(2f), (Texture)Assets.ICWarningBG.Icon);
		}
		else if (BioCoded)
		{
			GUI.color = Assets.ICWarningBGBioCoded.Color;
			GUI.DrawTexture(Geometry.ContractedBy(2f), (Texture)Assets.ICWarningBGBioCoded.Icon);
		}
		else if (ApparelFromDeadBody)
		{
			GUI.color = Assets.ICWarningBGDead.Color;
			GUI.DrawTexture(Geometry.ContractedBy(2f), (Texture)Assets.ICWarningBGDead.Icon);
		}
		else
		{
			DrawQualityGlow(Geometry.ContractedBy(2f));
		}
		var (rect2, rect3) = Utils.SplitRectByRightPart(rect, ExtraInfoSep.Value, 4f);
		if (Item.def.IsWeapon)
		{
			rect = rect2;
		}
		Rect iconRect = rect;
		iconRect.width = iconRect.height * 1.2f;
		Rect rect4 = rect;
		rect4.xMin = iconRect.xMax + 6f;
		DrawIcon(iconRect);
		DrawLabelAndButtons(rect4.TopPart(0.4f));
		DrawBars(rect4.BottomPart(0.54f));
		if (Item.def.IsWeapon)
		{
			DrawWeaponInfo(rect3);
		}
		if (!toolTipShowed && Mouse.IsOver(Geometry))
		{
			TooltipHandler.TipRegion(Geometry, Item.GetTooltip());
			GUI.color = Color.white;
			if (HasProblems && !ProblemDescr.NullOrEmpty())
			{
				TooltipHandler.TipRegion(Geometry, ProblemDescr.Colorize(Assets.PenaltyColor));
			}
			else if (BioCoded)
			{
				TooltipHandler.TipRegion(Geometry, "NIT_Biocoded".Translate().Colorize(Assets.PenaltyColor));
			}
			Widgets.DrawHighlight(Geometry.ContractedBy(2f));
		}
		bool flag = UnderControl();
		if (Widgets.ButtonInvisible(Geometry))
		{
			if (Item.def.IsApparel)
			{
				if (Event.current.button == 0)
				{
					Find.WindowStack.Add(new Dialog_InfoCard(Item));
					Event.current.Use();
				}
				else if (Event.current.button == 1 && !Item.def.apparel.layers.NullOrEmpty() && flag)
				{
					ApparelLayerDef apparelLayer = Item.def.apparel.layers.First();
					List<ApparelSlotUtility.PotentialSlot> slots = ApparelSlotUtility.AllPotentialSlots.Where((ApparelSlotUtility.PotentialSlot x) => Item.def.apparel.layers.Contains(x.layer)).ToList();
					List<FloatMenuOption> list = new List<FloatMenuOption>
					{
						new FloatMenuOption("ThingInfo".Translate(), delegate
						{
							Find.WindowStack.Add(new Dialog_InfoCard(Item));
						})
					};
					if (flag)
					{
						if (forcedWear)
						{
							list.Add(new FloatMenuOption("NIT_UnforceApparel".Translate(), delegate
							{
								pawn.outfits.forcedHandler.SetForced(Item as Apparel, forced: false);
								forcedWear = false;
								ITab_Pawn_Gear_Patch.shouldRecache = true;
							}));
						}
						else
						{
							list.Add(new FloatMenuOption("NIT_ForceApparel".Translate(), delegate
							{
								pawn.outfits.forcedHandler.SetForced(Item as Apparel, forced: true);
								forcedWear = true;
								ITab_Pawn_Gear_Patch.shouldRecache = true;
							}));
						}
						list.Add(new FloatMenuOption("NIT_MoveToInventory".Translate(), delegate
						{
							CommandUtility.CommandRemoveToInventory(pawn, Item);
						}));
					}
					ApparelSlotUtility.OpenFloatMenu(slots, apparelLayer, checkCanWear: false, list);
					Event.current.Use();
				}
			}
			else if (Event.current.button == 0)
			{
				Find.WindowStack.Add(new Dialog_InfoCard(Item));
				Event.current.Use();
			}
			else if (Event.current.button == 1)
			{
				List<FloatMenuOption> list2 = new List<FloatMenuOption>();
				list2.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
				{
					Find.WindowStack.Add(new Dialog_InfoCard(Item));
				}));
				if (flag && Item.def.IsWeapon)
				{
					Thing item = Item;
					ThingWithComps ItemComps = item as ThingWithComps;
					if (ItemComps != null)
					{
						if (pawn.equipment?.Primary == Item)
						{
							if (CommandUtility.CanFitInInventory(pawn, Item.def, out var _, ignoreEquipment: true))
							{
								list2.Add(new FloatMenuOption("NIT_MoveToInventory".Translate(), delegate
								{
									CommandUtility.CommandMoveWeaponToInventory(pawn, ItemComps);
								}));
							}
							else
							{
								list2.Add(new FloatMenuOption("NIT_MoveToInventory".Translate(), null));
							}
						}
						else
						{
							string text = "NIT_Equip".Translate();
							if (Item.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
							{
								text += " " + "EquipWarningBrawler".Translate();
							}
							list2.Add(new FloatMenuOption(text, (pawn.story != null && pawn.WorkTagIsDisabled(WorkTags.Violent)) ? null : ((Action)delegate
							{
								CommandUtility.CommandEquipWeaponFromInventory(pawn, ItemComps);
							})));
						}
					}
				}
				if (list2.Count > 0)
				{
					Find.WindowStack.Add(new FloatMenu(list2));
				}
				Event.current.Use();
			}
		}
		if (HasProblems)
		{
			GUI.color = Assets.ICWarning.Color;
			GUI.DrawTexture(Utils.RectCentered(new Vector2(Geometry.x + 6f, Geometry.center.y), 24f), (Texture)Assets.ICWarning.Icon);
		}
		else if (BioCoded)
		{
			GUI.color = Assets.ICWarningBioCoded.Color;
			GUI.DrawTexture(Utils.RectCentered(new Vector2(Geometry.x + 6f, Geometry.center.y), 24f), (Texture)Assets.ICWarningBioCoded.Icon);
		}
		DrawStars(Geometry);
	}

	public virtual Assets.IconColor SelectIconByDamage(DamageDef dam)
	{
		if (dam == DamageDefOf.EMP)
		{
			return Assets.ICEMP;
		}
		if (dam == DamageDefOf.Flame)
		{
			return Assets.ICFlame;
		}
		if (dam == DamageDefOf.Bomb)
		{
			return Assets.ICBoom;
		}
		if (dam == DamageDefOf.Smoke)
		{
			return Assets.ICSmoke;
		}
		if (dam == DamageDefOf.Stun)
		{
			return Assets.ICSmoke;
		}
		if (dam == Assets.DamageDef_Nerve)
		{
			return Assets.ICNerve;
		}
		if (dam == Assets.DamageDef_Beam)
		{
			return Assets.ICBeam;
		}
		return Assets.ICDamageRangedNeutral;
	}

	public void DrawIconText(Rect rect, Texture2D icon, string text, Func<Thing, Pawn, string> tooltipFunc = null, Color? col = null)
	{
		(Rect left, Rect right) tuple = Utils.SplitRectByLeftPart(rect, rect.height, 2f);
		Rect item = tuple.left;
		Rect item2 = tuple.right;
		GUI.color = col.GetValueOrDefault(Color.gray);
		GUI.DrawTexture(item, (Texture)icon);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(item2.ContractedBy(0f, -4f), text);
		Text.Anchor = TextAnchor.UpperLeft;
		float v = Utils.CalcWidth(text) + rect.height + 4f;
		ExtraInfoSep.CompCheck(v);
		if (Mouse.IsOver(rect) && tooltipFunc != null)
		{
			Widgets.DrawHighlight(rect);
			TooltipHandler.TipRegion(rect, tooltipFunc(Item, pawn));
			toolTipShowed = true;
		}
	}

	public virtual void DrawBars(Rect rect)
	{
		if (Item.def.IsWeapon)
		{
			if (Item.def.IsMeleeWeapon)
			{
				DrawBar(rect.TopHalf(), "NIT_MeleeDPSTip".Translate() + $": {dps:0.##}", dps, DamageUtility.GetMaxDPS(null), Assets.ICDamageMelee, "F1");
			}
			else if (Item.def.IsRangedWeapon)
			{
				DrawBar(rect.TopHalf(), "NIT_RangedDPSTip".Translate() + string.Format(": {0:0.##}\n\n{1}: {2:0.##}", dps, "NIT_Damage".Translate(), damage), dps, DamageUtility.GetMaxDPS(null), Assets.ICDamageRanged, "F1");
			}
			var (rect2, rect3) = Utils.SplitRect(rect.BottomHalf(), 0.5f, 4f);
			DrawBar(rect2, "Mass".Translate() + (": " + mass.cur.ToString(Assets.Format_KG)), mass.cur, mass.max, Assets.ICMass, "F1", 3, affect_Color_correction: false);
			DrawBar(rect3, string.Format("{0}: {1}", "NIT_ArmorPenetration".Translate(), armor_penetration_sharp.ToStringPercent()), armor_penetration_sharp, 0.6f, Assets.ICArmorPen, "%F1", 3);
		}
		else if (Item.def.IsApparel)
		{
			var (rect4, rect5) = Utils.SplitRect(rect.TopHalf(), 0.5f, 4f);
			DrawBar(rect4, StatDefOf.ArmorRating_Sharp.LabelCap + (": " + ArmorUtility.SharpFormat.Solve(armor_sharp)), armor_sharp, ArmorUtility.SharpFormat.ItemMax, Assets.ICArmorSharp, "%F1", 3);
			DrawBar(rect5, StatDefOf.ArmorRating_Blunt.LabelCap + (": " + ArmorUtility.BluntFormat.Solve(armor_blunt)), armor_blunt, ArmorUtility.BluntFormat.ItemMax, Assets.ICArmorBlunt, "%F1", 3);
			var (rect6, rect7) = Utils.SplitRect(rect.BottomHalf(), 0.5f, 4f);
			DrawBar(rect6, "Mass".Translate() + (": " + mass.cur.ToString(Assets.Format_KG)), mass.cur, mass.max, Assets.ICMass, "F1", 3, affect_Color_correction: false);
			DrawBar(rect7, StatDefOf.ArmorRating_Heat.LabelCap + (": " + ArmorUtility.HeatFormat.Solve(armor_heat)), armor_heat, ArmorUtility.HeatFormat.ItemMax, Assets.ICArmorHeat, "%F1", 3);
		}
	}

	public void DrawBar(Rect rect, string tooltip, float value, float maxValue, Assets.IconColor iconAndColor, string txtformat, int ticks = 6, bool affect_Color_correction = true)
	{
		rect.height = 12f;
		(Rect left, Rect right) tuple = Utils.SplitRectByLeftPart(rect, rect.height, 2f);
		Rect item = tuple.left;
		Rect item2 = tuple.right;
		item2.y = rect.center.y - barMaxHeight * 0.5f;
		item2.height = barMaxHeight;
		if (Settings.ItemBarsWithText)
		{
			if (txtformat.Contains("%"))
			{
				item2.xMax = DrawBarText(rect, value.ToStringPercent(txtformat.Remove(0, 1)));
			}
			else
			{
				item2.xMax = DrawBarText(rect, value.ToString(txtformat));
			}
		}
		GUI.color = Color.white;
		GUI.DrawTexture(item, (Texture)iconAndColor.Icon);
		float pct = Mathf.Clamp01(value / maxValue);
		Color color = (affect_Color_correction ? Settings.ColorCorrect(iconAndColor.Color) : iconAndColor.Color);
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

	private float DrawBarText(Rect rect, string txt)
	{
		GUI.color = Color.white;
		Text.Font = GameFont.Tiny;
		float num = Utils.CalcWidth(txt);
		Text.Anchor = TextAnchor.MiddleRight;
		rect.yMin -= 10f;
		rect.yMax += 10f;
		Widgets.Label(rect, txt);
		Text.Anchor = TextAnchor.UpperLeft;
		return rect.xMax - num - 2f;
	}

	private bool UnderControl()
	{
		if (CommandUtility.CanControl(pawn))
		{
			return pawn.IsColonistPlayerControlled;
		}
		return false;
	}

	private void DrawLabelAndButtons(Rect rect)
	{
		Text.Font = GameFont.Small;
		IsButtonSelected = false;
		bool flag = UnderControl();
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
		if (flag)
		{
			bool flag2 = false;
			if (pawn.IsQuestLodger())
			{
				flag2 = Inventory || !EquipmentUtility.QuestLodgerCanUnequip(Item, pawn);
			}
			bool flag3 = !Inventory && pawn.kindDef.destroyGearOnDrop;
			bool flag4 = Item is Apparel apparel && pawn.apparel != null && pawn.apparel.IsLocked(apparel);
			bool flag5 = flag2 || flag4 || flag3;
			(Rect left, Rect right) tuple2 = Utils.SplitRectByRightPart(rect, rect.height, 4f);
			Rect item3 = tuple2.left;
			Rect item4 = tuple2.right;
			rect = item3;
			if (DrawWhiteButton(item4, delegate(Rect r)
			{
				if (flag4)
				{
					TooltipHandler.TipRegion(r, "DropThingLocked".Translate());
				}
				else if (flag2)
				{
					TooltipHandler.TipRegion(r, "DropThingLodger".Translate());
				}
				else
				{
					TooltipHandler.TipRegion(r, "DropThing".Translate());
				}
			}, TexButton.Drop, !flag5))
			{
				Action action = delegate
				{
					SoundDefOf.Tick_High.PlayOneShotOnCamera();
					CommandUtility.CommandDrop(ITab_Pawn_Gear_Patch.lastPawn, Item);
				};
				if (!ModsConfig.BiotechActive || !MechanitorUtility.TryConfirmBandwidthLossFromDroppingThing(pawn, Item, action))
				{
					action();
				}
			}
		}
		if (flag && pawn.equipment != null && Item.def.IsWeapon && pawn.equipment.Primary != Item && Item is ThingWithComps item5)
		{
			(Rect left, Rect right) tuple3 = Utils.SplitRectByRightPart(rect, rect.height, 4f);
			Rect item6 = tuple3.left;
			Rect item7 = tuple3.right;
			rect = item6;
			bool active = pawn.story == null || !pawn.WorkTagIsDisabled(WorkTags.Violent);
			if (DrawWhiteButton(item7, delegate(Rect r)
			{
				string text = "NIT_Equip".Translate();
				if (Item.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
				{
					text += " (" + "EquipWarningBrawler".Translate() + ")";
				}
				TooltipHandler.TipRegion(r, text);
			}, Assets.SwapButtonTex, active))
			{
				SoundDefOf.Tick_High.PlayOneShotOnCamera();
				CommandUtility.CommandEquipWeaponFromInventory(pawn, item5);
			}
		}
		if (forcedWear && flag && Item is Apparel ap)
		{
			(Rect left, Rect right) tuple4 = Utils.SplitRectByRightPart(rect, rect.height, 4f);
			Rect item8 = tuple4.left;
			Rect item9 = tuple4.right;
			rect = item8;
			if (DrawWhiteButton(item9, delegate(Rect r)
			{
				TooltipHandler.TipRegion(r, "NIT_UnforceApparel".Translate());
			}, Assets.LockedButtonTex, active: true))
			{
				pawn.outfits.forcedHandler.SetForced(ap, forced: false);
				forcedWear = false;
				ITab_Pawn_Gear_Patch.shouldRecache = true;
			}
		}
		if (flag && FoodUtility.WillIngestFromInventoryNow(pawn, Item))
		{
			(Rect left, Rect right) tuple5 = Utils.SplitRectByRightPart(rect, rect.height, 4f);
			Rect item10 = tuple5.left;
			Rect item11 = tuple5.right;
			rect = item10;
			if (DrawWhiteButton(item11, delegate(Rect r)
			{
				TooltipHandler.TipRegionByKey(r, "ConsumeThing", Item.LabelNoCount, Item);
			}, TexButton.Ingest, active: true))
			{
				FoodUtility.IngestFromInventoryNow(pawn, Item);
			}
		}
		Text.Anchor = TextAnchor.MiddleLeft;
		GUI.color = Assets.ColorStat;
		Assets.DrawCroppedText(rect, Item.LabelCap);
		Text.Anchor = TextAnchor.UpperLeft;
	}

	private bool DrawWhiteButton(Rect rect, Action<Rect> tip, Texture2D tex, bool active)
	{
		bool flag = false;
		Rect rect2 = rect.ContractedBy(-4f);
		if (Mouse.IsOver(rect2) && !IsButtonSelected)
		{
			IsButtonSelected = true;
			tip(rect2);
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
		if (Widgets.ButtonInvisible(rect2) && active && flag)
		{
			SoundDefOf.Tick_High.PlayOneShotOnCamera();
			return true;
		}
		return false;
	}

	private bool ShouldShowWarning()
	{
		if (!UnderControl())
		{
			return false;
		}
		if (Item.def.IsApparel && pawn.apparel != null)
		{
			if (Item.def.useHitPoints && !pawn.apparel.IsLocked(Item as Apparel) && Item.def.apparel.careIfDamaged)
			{
				float num = (float)Item.HitPoints / (float)Item.MaxHitPoints;
				if (num < 0.5f)
				{
					ProblemDescr = "NIT_ApparelDamageWarning".Translate(num.ToStringPercent(), Item.LabelNoCount);
					return true;
				}
			}
			if (ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.ClothedNudist) && !GreatForNudists())
			{
				ProblemDescr = "NIT_ApparelNudistWarning".Translate();
				return true;
			}
		}
		if (Item.def.IsWeapon && !DamageUtility.CanUse(pawn, Item))
		{
			ProblemDescr = "NIT_WeaponNotUsableWithShieldWarning".Translate();
			return true;
		}
		return false;
	}

	private bool GreatForNudists()
	{
		if (Item.def.apparel.countsAsClothingForNudity)
		{
			foreach (BodyPartGroupDef bodyPartGroup in Item.def.apparel.bodyPartGroups)
			{
				if (bodyPartGroup == BodyPartGroupDefOf.Torso || bodyPartGroup == BodyPartGroupDefOf.Legs)
				{
					return false;
				}
			}
		}
		return true;
	}

	private void DrawIcon(Rect iconRect)
	{
		Widgets.DrawBoxSolid(iconRect, Assets.ColorBG);
		Rect rect = Utils.RectCentered(iconRect.center, iconRect.height);
		if (Item.def.IsWeapon && pawn.equipment?.Primary != Item)
		{
			GUI.color = Assets.ColorBGD;
			Assets.DrawTilingTexture(iconRect, Assets.DiagTiledTex, 64f, Vector2.zero);
		}
		ItemIconHelper.ThingIcon(rect, Item);
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

	private void DrawQualityGlow(Rect globalRect)
	{
		if (!Settings.EXR_QualityStars || !Item.TryGetQuality(out var qc))
		{
			return;
		}
		float qualityGlow = Settings.GetQualityGlow(qc);
		if (qualityGlow > 0f)
		{
			GUI.color = ColorUtils.ChangeAlpha(Settings.GetQualityColor(qc, highlighted: true), Mathf.Clamp01(qualityGlow));
			GUI.DrawTexture(globalRect, (Texture)Assets.GlowQualityTex);
			if (qualityGlow > 1f)
			{
				GUI.color = ColorUtils.ChangeAlpha(Settings.GetQualityColor(qc, highlighted: true), Mathf.Clamp01(qualityGlow - 1f));
				GUI.DrawTexture(globalRect, (Texture)Assets.GlowQualityTex);
			}
		}
	}
}
