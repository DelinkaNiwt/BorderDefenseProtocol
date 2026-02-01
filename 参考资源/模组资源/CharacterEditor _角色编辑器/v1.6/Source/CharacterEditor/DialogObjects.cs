using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogObjects : DialogTemplate<ThingDef>
{
	internal const string CO_DAMAGEAMOUNTBASE = "damageAmountBase";

	internal const string CO_ARMORPENETRATIONBASE = "armorPenetrationBase";

	internal const string CO_FORCEDMISSRADIUS = "forcedMissRadius";

	internal const string CO_FORCEDMISSRADIUSCLASSICMORTARS = "forcedMissRadiusClassicMortars";

	internal const string CO_ISMORTAR = "isMortar";

	internal const string CO_NAME = "name";

	internal const string CO_BIOCODED = "biocoded";

	internal const string CO_CACHEDLABELCAP = "cachedLabelCap";

	internal const string CO_DESCRIPTIONDETAILEDCACHED = "descriptionDetailedCached";

	private int iTick120 = 120;

	private ThingDef selected_CostDef = null;

	private ThingDef selected_CostDefDiff = null;

	private ResearchProjectDef selected_ResearchDef = null;

	private ApparelLayerDef selected_ApparelLayer = null;

	private BodyPartGroupDef selected_BodyPartGroup = null;

	private StuffCategoryDef selected_StuffCategorie = null;

	private WeaponTraitDef selected_WeaponTrait = null;

	private ThingCategoryDef selected_Cat = null;

	private ThingCategoryDef selected_Cat2 = null;

	private ThingCategory selected_TextCat = ThingCategory.None;

	private ThingCategory selected_TextCat2 = ThingCategory.None;

	private Pawn tempPawn;

	private string tempFaction;

	private HashSet<ThingDef> lOfThingsByCat = new HashSet<ThingDef>();

	private HashSet<ThingDef> lOfThingsByCat2 = new HashSet<ThingDef>();

	private HashSet<DialogType> lType;

	private HashSet<string> lFactions;

	private int gender = 0;

	private const int W400 = 400;

	private int WFULL;

	private int mCostTemp;

	private int mStackLimit;

	private int mHscroll;

	private bool mChangeFirstWeapon;

	private bool mUseAllSounds = false;

	private bool mDoesExplosion;

	private string dummy = "";

	private DialogType mDialogType;

	private DialogCapsuleUI mCapsuleUI;

	private ThingWithComps mThing;

	private ThingDef mTurretDef = null;

	private bool HasBullet = false;

	private bool HasVerb = false;

	private int rangeDamageAmountBase;

	private float armorPenetrationBase;

	private float forcedMissRadius;

	private float forcedMissRadiusMortar;

	private int ceWeaponFuelCapacity;

	private float ceWeaponReloadTime;

	private bool IsTurret => mTurretDef != null;

	private VerbProperties sverb => selectedDef.Verbs[0];

	private ProjectileProperties sbullet => selectedDef.Verbs[0].defaultProjectile.projectile;

	private int RangeDamageBase
	{
		get
		{
			return sbullet.GetMemberValue("damageAmountBase", 0);
		}
		set
		{
			sbullet.SetMemberValue("damageAmountBase", value);
		}
	}

	private float PenetrationBase
	{
		get
		{
			return sbullet.GetMemberValue("armorPenetrationBase", 0f);
		}
		set
		{
			sbullet.SetMemberValue("armorPenetrationBase", value);
		}
	}

	private float MissRadius
	{
		get
		{
			return sverb.GetMemberValue("forcedMissRadius", 0f);
		}
		set
		{
			sverb.SetMemberValue("forcedMissRadius", value);
		}
	}

	private float MissRadiusMortar
	{
		get
		{
			return sverb.GetMemberValue("forcedMissRadiusClassicMortars", 0f);
		}
		set
		{
			sverb.SetMemberValue("forcedMissRadiusClassicMortars", value);
		}
	}

	private int CEWeaponFuelCapacity
	{
		get
		{
			return WeaponTool.GetCompRefuelable(mTurretDef, selectedDef);
		}
		set
		{
			WeaponTool.SetCompRefuelable(mTurretDef, selectedDef, value);
		}
	}

	private float CEWeaponReloadTime
	{
		get
		{
			return WeaponTool.GetCompReloadTime(mTurretDef, selectedDef);
		}
		set
		{
			WeaponTool.SetCompReloadTime(mTurretDef, selectedDef, value);
		}
	}

	private bool IsPrimary(ThingWithComps t)
	{
		return t != null && tempPawn.HasEquipmentTracker() && tempPawn.equipment.Primary != null && tempPawn.equipment.Primary.ThingID == t.ThingID;
	}

	internal DialogObjects(DialogType type, DialogCapsuleUI capsuleUI = null, ThingWithComps thing = null, bool addAnimals = false)
		: base(SearchTool.SIndex.Weapon, Label.ADD_WEAPON, 105)
	{
		customAcceptLabel = "OK".Translate();
		tempPawn = CEditor.API.Pawn;
		lFactions = CEditor.API.DicFactions.Keys.ToHashSet();
		tempFaction = CEditor.ListName;
		mThing = thing;
		mDialogType = type;
		mCapsuleUI = capsuleUI;
		mChangeFirstWeapon = IsPrimary(thing);
		mHscroll = 0;
		if (mThing != null)
		{
			search.weaponType = ((mThing.def == null) ? WeaponType.Ranged : (mThing.def.IsRangedWeapon ? WeaponType.Ranged : ((!mThing.def.IsMeleeWeapon) ? (mThing.def.IsTurret() ? WeaponType.Turret : WeaponType.TurretGun) : WeaponType.Melee)));
		}
		lType = EnumTool.GetAllEnumsOfType<DialogType>().ToHashSet();
		lMods = ListModnames();
		base.Preselection();
		if (mThing != null)
		{
			SearchTool.ClearSearch(SearchTool.SIndex.Weapon);
			selectedDef = mThing.def;
		}
		lOfThingsByCat = ThingTool.ListOfItemsWithNull(null, null, ThingCategory.None);
		lOfThingsByCat2 = ThingTool.ListOfItemsWithNull(null, null, ThingCategory.None);
		if (addAnimals)
		{
			search.modName = null;
			search.thingCategoryDef = ThingCategoryDefOf.Animals;
			search.thingCategory = ThingCategory.None;
			lDefs = TList();
		}
	}

	internal override HashSet<string> ListModnames()
	{
		if (mDialogType == DialogType.Weapon)
		{
			return DefTool.ListModnamesWithNull((ThingDef t) => t.IsWeapon).ToHashSet();
		}
		if (mDialogType == DialogType.Apparel)
		{
			return DefTool.ListModnamesWithNull((ThingDef t) => t.IsApparel).ToHashSet();
		}
		if (mDialogType == DialogType.Object)
		{
			return DefTool.ListModnamesWithNull(DefTool.CONDITION_IS_ITEM(null, null, ThingCategory.None));
		}
		return new HashSet<string>();
	}

	internal override HashSet<ThingDef> TList()
	{
		if (mDialogType == DialogType.Weapon)
		{
			return WeaponTool.ListOfWeapons(search.modName, search.weaponType);
		}
		if (mDialogType == DialogType.Apparel)
		{
			return ApparelTool.ListOfApparel(search.modName, search.apparelLayerDef, search.bodyPartGroupDef);
		}
		if (mDialogType == DialogType.Object)
		{
			return ThingTool.ListOfItems(search.modName, search.thingCategoryDef, search.thingCategory);
		}
		return new HashSet<ThingDef>();
	}

	internal override void AReset()
	{
		PresetObject.ResetToDefault(selectedDef?.defName);
	}

	internal override void AResetAll()
	{
		PresetObject.ResetAllToDefaults();
	}

	internal override void ASave()
	{
		PresetObject.SaveModification(selectedDef);
	}

	internal override void CalcHSCROLL()
	{
		hScrollParam = 4000;
		if (mHscroll > 800)
		{
			hScrollParam = mHscroll;
		}
	}

	internal override void Preselection()
	{
	}

	internal override int DrawCustomFilter(int x, int y, int w)
	{
		Text.Font = GameFont.Small;
		Rect rect = new Rect(x, y, w, 30f);
		int result = 0;
		if (mDialogType == DialogType.Weapon)
		{
			SZWidgets.FloatMenuOnButtonText(rect, FLabel.WeaponType(search.weaponType), ThingTool.AllWeaponType, FLabel.WeaponType, delegate(WeaponType v)
			{
				search.weaponType = v;
				lDefs = TList();
			});
			result = 30;
		}
		else if (mDialogType == DialogType.Apparel)
		{
			SZWidgets.FloatMenuOnButtonText(rect, Label.LAYER + ": " + FLabel.DefLabelSimple(search.apparelLayerDef), ThingTool.AllApparelLayerDef, FLabel.DefLabelSimple, delegate(ApparelLayerDef v)
			{
				search.apparelLayerDef = v;
				lDefs = TList();
			});
			SZWidgets.FloatMenuOnButtonText(rect.RectPlusY(30), Label.BODYPARTGROUPS + ": " + FLabel.DefLabelSimple(search.bodyPartGroupDef), ThingTool.AllBodyPartGroupDef, FLabel.DefLabelSimple, delegate(BodyPartGroupDef v)
			{
				search.bodyPartGroupDef = v;
				lDefs = TList();
			});
			result = 60;
		}
		else if (mDialogType == DialogType.Object)
		{
			SZWidgets.FloatMenuOnButtonText(rect, FLabel.DefLabelSimple(search.thingCategoryDef), ThingTool.AllThingCategoryDef, FLabel.DefLabelSimple, delegate(ThingCategoryDef v)
			{
				search.thingCategoryDef = v;
				lDefs = TList();
			});
			SZWidgets.FloatMenuOnButtonText(rect.RectPlusY(30), FLabel.EnumNameAndAll(search.thingCategory), ThingTool.AllThingCategory, FLabel.EnumNameAndAll, delegate(ThingCategory v)
			{
				search.thingCategory = v;
				lDefs = TList();
			});
			result = 60;
		}
		return result;
	}

	internal override void OnAccept()
	{
		if (mInPlacingMode)
		{
			if (ThingTool.SelectedThing.pkd != null)
			{
				PlacingTool.PlaceMultiplePawnsInCustomPosition(ThingTool.SelectedThing, CEditor.API.DicFactions.GetValue(tempFaction));
			}
			else
			{
				PlacingTool.PlaceInCustomPosition(ThingTool.SelectedThing, null);
			}
		}
		else if (mCapsuleUI != null)
		{
			mCapsuleUI.ExternalAddThing(ThingTool.SelectedThing);
		}
		else
		{
			if (ThingTool.SelectedThing.tempThing != null)
			{
				return;
			}
			if (mDialogType == DialogType.Weapon)
			{
				if (search.weaponType == WeaponType.Turret)
				{
					tempPawn.AddItemToInventory(ThingTool.GenerateItem(ThingTool.SelectedThing));
				}
				else if (ThingTool.SelectedThing.tempThing == null)
				{
					tempPawn.Reequip(ThingTool.SelectedThing, (!mChangeFirstWeapon) ? 1 : 0);
				}
			}
			else if (mDialogType == DialogType.Apparel)
			{
				List<Apparel> conflictedApparelList = tempPawn.GetConflictedApparelList(ThingTool.SelectedThing.thingDef);
				if (conflictedApparelList.Count > 1)
				{
					MessageTool.ShowCustomDialog(conflictedApparelList.ListToString(), ThingTool.SelectedThing.thingDef.label.CapitalizeFirst() + " " + Label.WILL_REPLACE, null, Confirmed, null);
				}
				else
				{
					Confirmed();
				}
			}
			else
			{
				if (mDialogType != DialogType.Object)
				{
					return;
				}
				if (ThingTool.SelectedThing.pkd != null)
				{
					if (!CEditor.InStartingScreen)
					{
						PlacingTool.PlaceMultiplePawnsInCustomPosition(ThingTool.SelectedThing, CEditor.API.DicFactions.GetValue(tempFaction));
					}
					else
					{
						MessageTool.Show("not available in starting screen", MessageTypeDefOf.RejectInput);
					}
				}
				else
				{
					tempPawn.AddItemToInventory(ThingTool.GenerateItem(ThingTool.SelectedThing));
				}
			}
		}
	}

	private void Confirmed()
	{
		if (!tempPawn.CreateAndWearApparel(ThingTool.SelectedThing, out var a, showError: true))
		{
			tempPawn.AskToWearIncompatibleApparel(a);
		}
		CEditor.API.UpdateGraphics();
	}

	internal override void OnSelectionChanged()
	{
		mHscroll = 0;
		DebugTools.curTool = null;
		if (mThing != null)
		{
			ThingTool.SelectedThing = Selected.ByThing(mThing);
			mThing = null;
		}
		else
		{
			ThingTool.SelectedThing = Selected.ByThingDef(selectedDef);
		}
		ThingTool.SelectedThing.thingDef.UpdateFreeLists(ThingTool.FreeList.All);
		if (selectedDef != null)
		{
			mTurretDef = (selectedDef.IsTurret() ? selectedDef.GetTurretDef() : null);
			SoundTool.PlayThis(selectedDef.soundInteract);
		}
	}

	internal override void DrawLowerButtons()
	{
		base.DrawLowerButtons();
		if (!CEditor.InStartingScreen)
		{
			if (!mInPlacingMode)
			{
				SZWidgets.ButtonImage(new Rect(frameW - 60, 0f, 30f, 30f), "bnone", APlacingMode, Label.ACTIVATEPLACINGMODE);
				WindowTool.SimpleCustomButton(this, 0, ABuy, Label.BUY, Label.PRICE + ThingTool.SelectedThing.buyPrice);
			}
			else
			{
				SZWidgets.ButtonImage(new Rect(frameW - 30, 0f, 30f, 30f), "brotate", ARotate, "rotation" + TextureTool.Rot4ToString(PlacingTool.rotation));
				SZWidgets.ButtonText(new Rect(0f, InitialSize.y - 66f, 130f, 30f), Label.DESTROY, ADestroy);
			}
		}
	}

	private void ADestroy()
	{
		PlacingTool.Destroy();
	}

	private void APlacingMode()
	{
		doWindowBackground = true;
		absorbInputAroundWindow = false;
		preventCameraMotion = false;
		preventDrawTutor = false;
		draggable = false;
		closeOnClickedOutside = false;
		windowRect.position = new Vector2(0f, 0f);
		PlacingTool.rotation = Rot4.North;
		mInPlacingMode = true;
		CEditor.API.CloseEditor();
	}

	private void ARotate()
	{
		if (PlacingTool.rotation == Rot4.North)
		{
			PlacingTool.rotation = Rot4.East;
		}
		else if (PlacingTool.rotation == Rot4.East)
		{
			PlacingTool.rotation = Rot4.South;
		}
		else if (PlacingTool.rotation == Rot4.South)
		{
			PlacingTool.rotation = Rot4.West;
		}
		else if (PlacingTool.rotation == Rot4.West)
		{
			PlacingTool.rotation = Rot4.North;
		}
		else
		{
			PlacingTool.rotation = Rot4.North;
		}
	}

	private void ABuy()
	{
		ThingTool.BeginBuyItem(ThingTool.SelectedThing);
		CEditor.API.CloseEditor();
		Close();
	}

	private string DialogLabel(DialogType t)
	{
		return t switch
		{
			DialogType.Apparel => "Apparel".Translate(), 
			DialogType.Weapon => Label.WEAPON, 
			DialogType.Object => Label.OBJECT, 
			_ => "", 
		};
	}

	internal override void DrawTitle(int x, int y, int w, int h)
	{
		SZWidgets.NonDefSelectorSimple(new Rect(0f, 0f, 80f, 30f), 80, lType, ref mDialogType, "", (DialogType ty) => DialogLabel(ty), delegate(DialogType v)
		{
			mDialogType = v;
			lMods = ListModnames();
			lDefs = TList();
		});
	}

	internal override void DrawParameter()
	{
		if (selectedDef != null)
		{
			HasBullet = selectedDef.HasProjectile();
			HasVerb = selectedDef.HasVerb();
			WFULL = base.WPARAM - 12;
			DrawLabel();
			DrawDamageLabel();
			DrawObjectPic();
			DrawBulletBig(400);
			view.CurY += 128f;
			DrawBulletSmall(400);
			DrawNavSelectors(400);
			DrawSoundCast(400);
			DrawHitpoints(400);
			DrawDamageSelectors(400);
			DrawRangedBig(400);
			DrawStatFactors(WFULL);
			DrawStatOffsets(WFULL);
			DrawStuffCategories(WFULL);
			DrawCostStuffCount(400);
			DrawCosts(WFULL);
			DrawCostsDiff(WFULL);
			DrawLayers(WFULL);
			DrawCoveredBodyParts(WFULL);
			DrawApparelTags(WFULL);
			DrawOutfitTags(WFULL);
			DrawWeaponTags(WFULL);
			DrawTradeTags(WFULL);
			DrawResearchBuilding(WFULL);
			DrawWeaponTraits(WFULL);
			DrawEnums(400);
			DrawRangedSmall(400);
			DrawOther(400);
			DrawSounds(400);
			DrawExtras(400);
			mHscroll = (int)view.CurY + 50;
			UpdateCachedDescription();
		}
	}

	private void UpdateCachedDescription()
	{
		if (iTick120 <= 0)
		{
			ThingTool.SelectedThing.thingDef.SetMemberValue("descriptionDetailedCached", null);
			ThingTool.SelectedThing.thingDef.SetMemberValue("cachedLabelCap", null);
			iTick120 = 120;
		}
		else
		{
			iTick120--;
		}
	}

	private void DrawOther(int w)
	{
		if (selectedDef.IsStackable())
		{
			mStackLimit = selectedDef.stackLimit;
			SZWidgets.LabelIntFieldSlider(view, w, 99999, FLabel.StackLimit, ref selectedDef.stackLimit, 1, 2000);
			if (mStackLimit != selectedDef.stackLimit)
			{
				mStackLimit = selectedDef.stackLimit;
				selectedDef.UpdateStackLimit();
			}
		}
		view.Gap(2f);
		view.CheckboxLabeled(Label.STEALABLE, 0f, w, ref selectedDef.stealable, null, 2);
	}

	private void DrawEnums(int w)
	{
		if (selectedDef.recipeMaker != null)
		{
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllResearchProjectDef, ref selectedDef.recipeMaker.researchPrerequisite, Label.RECIPEPREREQUISITE, FLabel.DefLabel, delegate(ResearchProjectDef p)
			{
				selectedDef.SetResearchPrerequisite(p);
			});
			view.Gap(2f);
		}
		SZWidgets.NonDefSelectorSimple(view.GetRect(22f), w, ThingTool.AllTradeabilities, ref selectedDef.tradeability, Label.TRADEABILITY + " ", FLabel.Tradeability, delegate(Tradeability s)
		{
			selectedDef.tradeability = s;
		});
		view.Gap(2f);
		SZWidgets.NonDefSelectorSimple(view.GetRect(22f), w, ThingTool.AllTechLevels, ref selectedDef.techLevel, Label.TECHLEVEL + " ", FLabel.TechLevel, delegate(TechLevel s)
		{
			selectedDef.techLevel = s;
		});
		view.Gap(2f);
	}

	private void DrawExtras(int w)
	{
		Text.Font = GameFont.Medium;
		view.GapLine();
		view.Label(Label.GRAPHICS, 500f, 0f);
		GUI.color = Color.gray;
		Text.Font = GameFont.Tiny;
		Text.WordWrap = false;
		if (selectedDef.graphicData != null && selectedDef.graphicData.texPath != null)
		{
			view.Label(Label.TEXPATH + selectedDef.graphicData.texPath, 1000f, 0f);
			view.Label(Label.DRAWSIZE + selectedDef.graphicData.drawSize.ToString(), 1000f, 0f);
		}
		if (selectedDef.apparel != null && selectedDef.apparel.wornGraphicPath != null)
		{
			view.Label(Label.WORNPATH + selectedDef.apparel.wornGraphicPath, 1000f, 0f);
		}
		Text.WordWrap = true;
		Text.Font = GameFont.Small;
		GUI.color = Color.white;
	}

	private void DrawCostStuffCount(int w)
	{
		if (selectedDef.race == null)
		{
			mCostTemp = selectedDef.costStuffCount;
			SZWidgets.LabelIntFieldSlider(view, w, id++, FLabel.CostStuffCount, ref selectedDef.costStuffCount, 0, 99999);
			if (mCostTemp != selectedDef.costStuffCount)
			{
				selectedDef.UpdateRecipes();
			}
			view.Gap(8f);
		}
	}

	private void DrawDamageLabel()
	{
		if (!selectedDef.IsWeapon && !selectedDef.HasVerb())
		{
			return;
		}
		string text = "";
		if (HasBullet)
		{
			text = sverb.defaultProjectile.SLabel();
			if (!text.NullOrEmpty())
			{
				text += " ";
			}
		}
		int num = 0;
		try
		{
			num = selectedDef.GetDmg();
		}
		catch
		{
		}
		view.Label(0f, 0f, 500f, 30f, text + "Damage".Translate() + " [" + num.ToString() + "]", GameFont.Medium);
		view.Gap(30f);
	}

	private void DrawObjectPic()
	{
		GUI.color = selectedDef.GetTColor(ThingTool.SelectedThing.stuff);
		float num = ((selectedDef.graphicData != null) ? selectedDef.graphicData.drawSize.x : 1f);
		float num2 = ((selectedDef.graphicData != null) ? selectedDef.graphicData.drawSize.y : 1f);
		GUI.DrawTextureWithTexCoords(new Rect(view.CurX, view.CurY, (int)(128f / num2 * num), 128f), (Texture)selectedDef.GetTexture(1, ThingTool.SelectedThing.style, Rot4.South), new Rect(0f, 0f, 1f, 1f));
		if (selectedDef.IsTurret())
		{
			ThingDef realWeaponDef = selectedDef.GetRealWeaponDef();
			GUI.color = realWeaponDef.uiIconColor;
			GUI.DrawTextureWithTexCoords(new Rect(view.CurX, view.CurY - 10f, 128f, 128f), (Texture)realWeaponDef.GetTexture(), new Rect(0f, 0f, 1f, 1f));
		}
		GUI.color = Color.white;
	}

	private void DrawBulletBig(int w)
	{
		if (HasVerb)
		{
			Rect rect = new Rect(view.CurX + 200f, view.CurY + 30f, 64f, 64f);
			GUI.DrawTexture(rect, (Texture)sverb.defaultProjectile.GetTexture());
			SZWidgets.DefSelectorSimpleTex(rect, w, ThingTool.AllBullets, ref sverb.defaultProjectile, "", FLabel.DefLabel, null, hasLR: false, sverb.defaultProjectile.GetTexture(), delegate(ThingDef s)
			{
				selectedDef.SetBullet(s);
			}, drawLabel: false);
		}
	}

	private void DrawBulletSmall(int w)
	{
		if (HasVerb)
		{
			SZWidgets.DefSelectorSimpleTex(view.GetRect(22f), 400, ThingTool.AllBullets, ref sverb.defaultProjectile, "", FLabel.DefLabel, delegate(ThingDef b)
			{
				selectedDef.SetBullet(b);
				SoundTool.PlayThis(b.soundInteract);
			}, hasLR: true, sverb.defaultProjectile.GetTexture());
			view.Gap(2f);
		}
	}

	private void DrawNavSelectors(int w)
	{
		view.CurY = SZWidgets.NavSelectorQuality(new Rect(0f, view.CurY, w, 22f), ThingTool.SelectedThing, ThingTool.AllQualityCategory);
		view.CurY = SZWidgets.NavSelectorStuff(new Rect(0f, view.CurY, w, 22f), ThingTool.SelectedThing);
		view.CurY = SZWidgets.NavSelectorStyle(new Rect(0f, view.CurY, w, 22f), ThingTool.SelectedThing);
		if (ThingTool.SelectedThing != null && ThingTool.SelectedThing.HasRace)
		{
			gender = (int)ThingTool.SelectedThing.gender;
			SZWidgets.LabelIntFieldSlider(view, w, id++, FLabel.GenderLabelInt, ref gender, 0, 2);
			ThingTool.SelectedThing.gender = (Gender)gender;
			view.Gap(2f);
			SZWidgets.LabelIntFieldSlider(view, w, id++, FLabel.BiologicalAge, ref ThingTool.SelectedThing.age, 1, 100);
			view.Gap(2f);
			SZWidgets.NonDefSelectorSimple(view.GetRect(22f), w, lFactions, ref tempFaction, "Faction".Translate() + " ", (string s) => s, delegate(string s)
			{
				tempFaction = s;
			});
			view.Gap(2f);
			ThingTool.SelectedThing.pkd = ThingTool.SelectedThing.thingDef.race.AnyPawnKind;
		}
		if (ThingTool.SelectedThing.HasStack)
		{
			SZWidgets.LabelIntFieldSlider(view, w, id++, FLabel.Menge, ref ThingTool.SelectedThing.stackVal, 1, selectedDef.stackLimit);
			if (ThingTool.SelectedThing.tempThing != null)
			{
				ThingTool.SelectedThing.tempThing.stackCount = ThingTool.SelectedThing.stackVal;
			}
			view.Gap(8f);
		}
	}

	private void DrawSoundCast(int w)
	{
		if (HasVerb)
		{
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllGunShotSounds, ref sverb.soundCast, Label.SOUNDCAST, FLabel.Sound, delegate(SoundDef s)
			{
				sverb.soundCast = SoundTool.GetAndPlay(s);
			}, hasLR: true, "bsound", SoundTool.PlayThis);
			view.Gap(2f);
		}
	}

	private void DrawHitpoints(int w)
	{
		if (ThingTool.SelectedThing.tempThing != null)
		{
			int value = ThingTool.SelectedThing.tempThing.HitPoints;
			SZWidgets.LabelIntFieldSlider(view, w, id++, FLabel.HitPoints, ref value, 0, ThingTool.SelectedThing.tempThing.MaxHitPoints);
			ThingTool.SelectedThing.tempThing.HitPoints = value;
			if (!HasBullet)
			{
				view.Gap(8f);
			}
		}
	}

	private void DrawDamageSelectors(int w)
	{
		if (HasBullet)
		{
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllDamageDefs, ref sbullet.damageDef, Label.DAMAGEDEF, FLabel.DefLabel, delegate(DamageDef d)
			{
				sbullet.damageDef = d;
			});
			view.Gap(2f);
			rangeDamageAmountBase = RangeDamageBase;
			SZWidgets.LabelIntFieldSlider(view, w, id++, FLabel.DamageAmountBase, ref rangeDamageAmountBase, 0, 100);
			RangeDamageBase = rangeDamageAmountBase;
			armorPenetrationBase = PenetrationBase;
			SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.ArmorPenetrationBase, ref armorPenetrationBase, 0f, 100f, 2);
			PenetrationBase = armorPenetrationBase;
		}
	}

	private void DrawRangedSmall(int w)
	{
		if (!HasVerb)
		{
			return;
		}
		Text.Font = GameFont.Small;
		forcedMissRadius = MissRadius;
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.Spraying, ref forcedMissRadius, 0f, 100f, 1);
		MissRadius = forcedMissRadius;
		if (sverb.GetMemberValue("isMortar", fallback: false))
		{
			forcedMissRadiusMortar = MissRadiusMortar;
			SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.SprayingMortar, ref forcedMissRadiusMortar, 0f, 10f, 1);
			MissRadiusMortar = forcedMissRadiusMortar;
		}
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.BeamWidth, ref sverb.beamWidth, 0f, 10f, 1);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.BeamFullWidthRange, ref sverb.beamFullWidthRange, 0f, 10f, 1);
		SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllDamageDefs, ref sverb.beamDamageDef, Label.BEAMDAMAGEDEF, FLabel.DefLabel, delegate(DamageDef s)
		{
			sverb.beamDamageDef = s;
		});
		view.Gap(2f);
		if (HasBullet)
		{
			SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.BulletExplosionRadius, ref sbullet.explosionRadius, 0f, 50f, 1);
			SZWidgets.LabelIntFieldSlider(view, w, id++, FLabel.BulletExplosionDelay, ref sbullet.explosionDelay, 0, 10);
			SZWidgets.LabelIntFieldSlider(view, w, id++, FLabel.BulletNumExtraHitCels, ref sbullet.numExtraHitCells, 0, 8);
			view.Gap(10f);
			mDoesExplosion = sverb.CausesExplosion;
			view.CheckboxLabeled(Label.ISEXPLOSIVEREADONLY, 0f, w, ref mDoesExplosion, null, 2);
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllThingCategoryDef, ref selected_Cat, Label.FILTERCATEGORY, FLabel.DefLabel, delegate(ThingCategoryDef cat)
			{
				lOfThingsByCat = ThingTool.ListOfItems(null, cat, selected_TextCat);
				selected_Cat = cat;
			});
			view.Gap(2f);
			SZWidgets.NonDefSelectorSimple(view.GetRect(22f), w, ThingTool.AllThingCategory, ref selected_TextCat, Label.FILTERTYPE, FLabel.EnumNameAndAll, delegate(ThingCategory cat)
			{
				lOfThingsByCat = ThingTool.ListOfItems(null, selected_Cat, cat);
				selected_TextCat = cat;
			});
			view.Gap(2f);
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lOfThingsByCat, ref sbullet.preExplosionSpawnThingDef, Label.PREEXPLOSIONSPAWNTHING, FLabel.DefLabel, delegate(ThingDef t)
			{
				sbullet.preExplosionSpawnThingDef = t;
				selectedDef.ResolveReferences();
			});
			view.Gap(2f);
			SZWidgets.LabelIntFieldSlider(view, w, id++, FLabel.BulletExplosionSpawnThingCount, ref sbullet.preExplosionSpawnThingCount, 0, 20);
			SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.BulletExplosionSpawnChance, ref sbullet.preExplosionSpawnChance, 0f, 100f, 1);
			view.Gap(10f);
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllThingCategoryDef, ref selected_Cat2, Label.FILTERCATEGORY, FLabel.DefLabel, delegate(ThingCategoryDef cat)
			{
				lOfThingsByCat2 = ThingTool.ListOfItems(null, cat, selected_TextCat2);
				selected_Cat2 = cat;
			});
			view.Gap(2f);
			SZWidgets.NonDefSelectorSimple(view.GetRect(22f), w, ThingTool.AllThingCategory, ref selected_TextCat2, Label.FILTERTYPE, FLabel.EnumNameAndAll, delegate(ThingCategory cat)
			{
				lOfThingsByCat2 = ThingTool.ListOfItems(null, selected_Cat2, cat);
				selected_TextCat2 = cat;
			});
			view.Gap(2f);
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lOfThingsByCat2, ref sbullet.postExplosionSpawnThingDef, Label.POSTEXPLOSIONSPAWNTHING, FLabel.DefLabel, delegate(ThingDef t)
			{
				sbullet.postExplosionSpawnThingDef = t;
				selectedDef.ResolveReferences();
			});
			view.Gap(2f);
			SZWidgets.LabelIntFieldSlider(view, w, id++, FLabel.BulletPostExplosionSpawnThingCount, ref sbullet.postExplosionSpawnThingCount, 0, 20);
			SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.BulletPostExplosionSpawnChance, ref sbullet.postExplosionSpawnChance, 0f, 100f, 1);
			SZWidgets.NonDefSelectorSimple(view.GetRect(22f), w, ThingTool.AllGasTypes, ref sbullet.postExplosionGasType, Label.GASTYPE, FLabel.GasType, delegate(GasType? g)
			{
				sbullet.postExplosionGasType = g;
				selectedDef.ResolveReferences();
			});
			view.Gap(2f);
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllEffecterDefs, ref sbullet.explosionEffect, Label.EXPLOSIONEFFECT, FLabel.DefName, delegate(EffecterDef e)
			{
				sbullet.explosionEffect = e;
			});
			view.Gap(2f);
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllEffecterDefs, ref sbullet.landedEffecter, Label.LANDEDEFFECT, FLabel.DefName, delegate(EffecterDef e)
			{
				sbullet.landedEffecter = e;
			});
			view.Gap(2f);
		}
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.ConsumeFuelPerShot, ref sverb.consumeFuelPerShot, 0f, 100f, 2);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.ConsumeFuelPerBurst, ref sverb.consumeFuelPerBurst, 0f, 100f, 2);
		view.Gap(10f);
		if (HasBullet)
		{
			view.CheckboxLabeled(Label.APPLYDAMAGETOEXPLOSIONCELLNEIGHBORS, 0f, w, ref sbullet.applyDamageToExplosionCellsNeighbors, null, 2);
			view.CheckboxLabeled(Label.FLYOVERHEAD, 0f, w, ref sbullet.flyOverhead, null, 2);
		}
		view.CheckboxLabeled(Label.REQUIRELINEOFSIGHT, 0f, w, ref sverb.requireLineOfSight, null, 2);
		view.CheckboxLabeled(Label.BEAMTARGETGROUND, 0f, w, ref sverb.beamTargetsGround, null, 2);
		if (sverb.targetParams != null)
		{
			view.CheckboxLabeled(Label.TARGETGROUND, 0f, w, ref sverb.targetParams.canTargetLocations, null, 2);
		}
	}

	private void DrawSounds(int w)
	{
		view.Gap(10f);
		if (HasBullet)
		{
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllGunRelatedSounds, ref sbullet.soundExplode, Label.SOUNDEXPLODE, FLabel.Sound, delegate(SoundDef s)
			{
				sbullet.soundExplode = SoundTool.GetAndPlay(s);
			}, hasLR: true, "bsound", SoundTool.PlayThis);
			view.Gap(2f);
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllGunRelatedSounds, ref sbullet.soundImpact, Label.SOUNDIMPACT, FLabel.Sound, delegate(SoundDef s)
			{
				sbullet.soundImpact = SoundTool.GetAndPlay(s);
			}, hasLR: true, "bsound", SoundTool.PlayThis);
			view.Gap(2f);
		}
		if (HasVerb)
		{
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllGunRelatedSounds, ref sverb.soundAiming, Label.SOUNDAIMING, FLabel.Sound, delegate(SoundDef s)
			{
				sverb.soundAiming = SoundTool.GetAndPlay(s);
			}, hasLR: true, "bsound", SoundTool.PlayThis);
			view.Gap(2f);
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllGunRelatedSounds, ref sverb.soundCastBeam, Label.SOUNDCASTBEAM, FLabel.Sound, delegate(SoundDef s)
			{
				sverb.soundCastBeam = SoundTool.GetAndPlay(s);
			}, hasLR: true, "bsound", SoundTool.PlayThis);
			view.Gap(2f);
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllGunRelatedSounds, ref sverb.soundCastTail, Label.SOUNDCASTTAIL, FLabel.Sound, delegate(SoundDef s)
			{
				sverb.soundCastTail = SoundTool.GetAndPlay(s);
			}, hasLR: true, "bsound", SoundTool.PlayThis);
			view.Gap(2f);
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, ThingTool.AllGunRelatedSounds, ref sverb.soundLanding, Label.SOUNDLANDING, FLabel.Sound, delegate(SoundDef s)
			{
				sverb.soundLanding = SoundTool.GetAndPlay(s);
			}, hasLR: true, "bsound", SoundTool.PlayThis);
			view.Gap(2f);
		}
	}

	private void DrawRangedBig(int w)
	{
		if (HasVerb)
		{
			Text.Font = GameFont.Medium;
			view.GapLine();
			if (HasBullet)
			{
				view.AddSection(Label.BULLET_SPEED, "cps", ref dummy, ref sbullet.speed, 0f, 150f);
			}
			view.AddIntSection("BurstShotFireRate".Translate(), "rpm", ref dummy, ref sverb.ticksBetweenBurstShots, 0, 100);
			view.AddIntSection("BurstShotCount".Translate(), "", ref dummy, ref sverb.burstShotCount, 0, 150);
			if (CEditor.IsCombatExtendedActive || (IsTurret && HasBullet))
			{
				ceWeaponFuelCapacity = CEWeaponFuelCapacity;
				view.AddIntSection(Label.MAGAZIN, "", ref dummy, ref ceWeaponFuelCapacity, 0, 500);
				CEWeaponFuelCapacity = ceWeaponFuelCapacity;
			}
			view.AddSection("Range".Translate(), "", ref dummy, ref sverb.range, 0f, 100f);
			view.AddSection(Label.MIN + "Range".Translate(), "", ref dummy, ref sverb.minRange, 0f, 50f);
			if (CEditor.IsCombatExtendedActive)
			{
				ceWeaponReloadTime = CEWeaponReloadTime;
				view.AddSection(Label.RELOADTIME, "s", ref dummy, ref ceWeaponReloadTime, 0f, 30f);
				CEWeaponReloadTime = ceWeaponReloadTime;
			}
			if (selectedDef.HasStat(StatDefOf.RangedWeapon_Cooldown))
			{
				float value = selectedDef.GetStatValue(StatDefOf.RangedWeapon_Cooldown);
				view.AddSection(StatDefOf.RangedWeapon_Cooldown.LabelCap, "s", ref dummy, ref value, 0f, 30f);
				selectedDef.SetStatBaseValue(StatDefOf.RangedWeapon_Cooldown, value);
			}
			else if (IsTurret)
			{
				view.AddSection(StatDefOf.RangedWeapon_Cooldown.LabelCap, "s", ref dummy, ref mTurretDef.building.turretBurstCooldownTime, 0f, 30f);
			}
			else
			{
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.CooldownTime, ref sverb.defaultCooldownTime, 0f, 100f, 2);
			}
			if (IsTurret)
			{
				float value2 = mTurretDef.building.turretBurstWarmupTime.min;
				view.AddSection(Label.WARMUP, "s", ref dummy, ref value2, 0f, 30f);
				mTurretDef.building.turretBurstWarmupTime.min = value2;
				float value3 = mTurretDef.building.turretBurstWarmupTime.max;
				view.AddSection(Label.WARMUPMAX, "s", ref dummy, ref value3, 0f, 30f);
				mTurretDef.building.turretBurstWarmupTime.max = value3;
			}
			else
			{
				view.AddSection(Label.WARMUP, "s", ref dummy, ref sverb.warmupTime, 0f, 30f);
			}
			if (HasBullet)
			{
				view.AddSection("StoppingPower".Translate(), "s", ref dummy, ref sbullet.stoppingPower, 0f, 10f);
			}
			if (selectedDef.HasStat(StatDefOf.AccuracyTouch))
			{
				float value4 = selectedDef.GetStatValue(StatDefOf.AccuracyTouch);
				view.AddSection(StatDefOf.AccuracyTouch.label, "%", ref dummy, ref value4, 0f, 1f);
				selectedDef.SetStatBaseValue(StatDefOf.AccuracyTouch, value4);
			}
			else
			{
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.AccuracyTouch, ref sverb.accuracyTouch, 0f, 100f, 2);
			}
			if (selectedDef.HasStat(StatDefOf.AccuracyShort))
			{
				float value5 = selectedDef.GetStatValue(StatDefOf.AccuracyShort);
				view.AddSection(StatDefOf.AccuracyShort.label, "%", ref dummy, ref value5, 0f, 1f);
				selectedDef.SetStatBaseValue(StatDefOf.AccuracyShort, value5);
			}
			else
			{
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.AccuracyShort, ref sverb.accuracyShort, 0f, 100f, 2);
			}
			if (selectedDef.HasStat(StatDefOf.AccuracyMedium))
			{
				float value6 = selectedDef.GetStatValue(StatDefOf.AccuracyMedium);
				view.AddSection(StatDefOf.AccuracyMedium.label, "%", ref dummy, ref value6, 0f, 1f);
				selectedDef.SetStatBaseValue(StatDefOf.AccuracyMedium, value6);
			}
			else
			{
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.AccuracyMedium, ref sverb.accuracyMedium, 0f, 100f, 2);
			}
			if (selectedDef.HasStat(StatDefOf.AccuracyLong))
			{
				float value7 = selectedDef.GetStatValue(StatDefOf.AccuracyLong);
				view.AddSection(StatDefOf.AccuracyLong.label, "%", ref dummy, ref value7, 0f, 1f);
				selectedDef.SetStatBaseValue(StatDefOf.AccuracyLong, value7);
			}
			else
			{
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.AccuracyLong, ref sverb.accuracyLong, 0f, 100f, 2);
			}
		}
	}

	private void DrawStuffCategories(int w)
	{
		if (selectedDef.race != null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.STUFFPROPS, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(ThingTool.lFreeStuffCategories, (StuffCategoryDef cat) => cat.SLabel(), delegate(StuffCategoryDef cat)
			{
				selectedDef.SetStuffCategorie(cat, ThingTool.SelectedThing);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			selectedDef.CopyStuffCategories();
		});
		if (!ThingTool.lCopyStuffCategories.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				selectedDef.PasteStuffCategories();
			});
		}
		view.Gap(30f);
		view.FullListViewParam1(selectedDef.stuffCategories, ref selected_StuffCategorie, bRemoveOnClick, delegate(StuffCategoryDef cat)
		{
			selectedDef.RemoveStuffCategorie(cat);
		});
		view.GapLine(25f);
	}

	private void DrawCosts(int w)
	{
		if (selectedDef.race != null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.COSTS, GameFont.Medium, Label.TIP_BUILDINGCOSTS);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(ThingTool.lFreeCosts, (ThingDef cost) => cost.SLabel(), delegate(ThingDef cost)
			{
				selectedDef.SetCosts(cost, 0);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			selectedDef.CopyCosts();
		});
		if (!ThingTool.lCopyCosts.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				selectedDef.PasteCosts();
			});
		}
		view.Gap(30f);
		view.FullListViewParam(selectedDef.costList, ref selected_CostDef, (ThingDefCountClass t) => t.thingDef, (ThingDefCountClass t) => t.count, null, (ThingDefCountClass t) => 0f, (ThingDefCountClass t) => 99999f, isInt: true, bRemoveOnClick, delegate(ThingDefCountClass t, float val)
		{
			selectedDef.SetCosts(t.thingDef, (int)val);
		}, null, delegate(ThingDef t)
		{
			selectedDef.RemoveCosts(t);
		});
		view.GapLine(25f);
	}

	private void DrawCostsDiff(int w)
	{
		if (selectedDef.costListForDifficulty == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.COSTS + Label.FORDIFFICULTY, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(ThingTool.lFreeCostsDiff, (ThingDef cost) => cost.SLabel(), delegate(ThingDef cost)
			{
				selectedDef.SetCostsDiff(cost, 0);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			selectedDef.CopyCostsDiff();
		});
		if (!ThingTool.lCopyCostsDiff.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				selectedDef.PasteCostsDiff();
			});
		}
		view.Gap(30f);
		view.FullListViewParam(selectedDef.costListForDifficulty.costList, ref selected_CostDefDiff, (ThingDefCountClass t) => t.thingDef, (ThingDefCountClass t) => t.count, null, (ThingDefCountClass t) => 0f, (ThingDefCountClass t) => 99999f, isInt: true, bRemoveOnClick, delegate(ThingDefCountClass t, float val)
		{
			selectedDef.SetCostsDiff(t.thingDef, (int)val);
		}, null, delegate(ThingDef t)
		{
			selectedDef.RemoveCostsDiff(t);
		});
		view.GapLine(25f);
	}

	private void DrawLayers(int w)
	{
		if (selectedDef.apparel == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.APPARELLAYER, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(ThingTool.lFreeApparelLayer, FLabel.DefLabel, delegate(ApparelLayerDef lay)
			{
				selectedDef.SetApparelLayer(lay);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			selectedDef.CopyApparelLayer();
		});
		if (!ThingTool.lCopyApparelLayer.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				selectedDef.PasteApparelLayer();
			});
		}
		view.Gap(30f);
		view.FullListViewParam1(selectedDef.apparel.layers, ref selected_ApparelLayer, bRemoveOnClick, delegate(ApparelLayerDef t)
		{
			selectedDef.RemoveApparelLayer(t);
		});
		view.GapLine(25f);
	}

	private void DrawCoveredBodyParts(int w)
	{
		if (selectedDef.apparel == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.BODYPARTGROUPS, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(ThingTool.lFreeBodyPartGroup, FLabel.DefLabel, delegate(BodyPartGroupDef b)
			{
				selectedDef.SetBodyPartGroup(b);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			selectedDef.CopyBodyPartGroup();
		});
		if (!ThingTool.lCopyBodyPartGroup.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				selectedDef.PasteBodyPartGroup();
			});
		}
		view.Gap(30f);
		view.FullListViewParam1(selectedDef.apparel.bodyPartGroups, ref selected_BodyPartGroup, bRemoveOnClick, delegate(BodyPartGroupDef t)
		{
			selectedDef.RemoveBodyPartGroup(t);
		});
		view.GapLine(25f);
	}

	private void DrawResearchBuilding(int w)
	{
		if (selectedDef.building == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.BUILDINGPREREQUISITES, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(ThingTool.lFreePrerequisites, (ResearchProjectDef p) => p.SLabel(), delegate(ResearchProjectDef p)
			{
				selectedDef.SetPrerequisite(p);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			selectedDef.CopyPrerequisites();
		});
		if (!ThingTool.lCopyPrerequisites.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				selectedDef.PastePrerequisites();
			});
		}
		view.Gap(30f);
		view.FullListViewParam1(selectedDef.researchPrerequisites, ref selected_ResearchDef, bRemoveOnClick, delegate(ResearchProjectDef t)
		{
			selectedDef.RemovePrerequisite(t);
		});
		view.GapLine(25f);
	}

	private void DrawTradeTags(int w)
	{
		SZWidgets.DrawStringList(ref selectedDef.tradeTags, CEditor.API.ListOf<string>(EType.TradeTags), w, view, Label.TRADETAGS, ref ThingTool.lCopyTradeTags, delegate(string s)
		{
			selectedDef.tradeTags?.Remove(s);
		}, delegate(string s)
		{
			Extension.AddElem(ref selectedDef.tradeTags, s);
		});
	}

	private void DrawApparelTags(int w)
	{
		if (selectedDef.apparel != null)
		{
			SZWidgets.DrawStringList(ref selectedDef.apparel.tags, CEditor.API.ListOf<string>(EType.ApparelTags), w, view, Label.APPARELTAGS, ref ThingTool.lCopyApparelTags, delegate(string s)
			{
				selectedDef.apparel.tags?.Remove(s);
			}, delegate(string s)
			{
				Extension.AddElem(ref selectedDef.apparel.tags, s);
			});
		}
	}

	private void DrawOutfitTags(int w)
	{
		if (selectedDef.apparel != null)
		{
			SZWidgets.DrawStringList(ref selectedDef.apparel.defaultOutfitTags, CEditor.API.ListOf<string>(EType.OutfitTags), w, view, Label.OUTFITTAGS, ref ThingTool.lCopyOutfitTags, delegate(string s)
			{
				selectedDef.apparel.defaultOutfitTags?.Remove(s);
			}, delegate(string s)
			{
				Extension.AddElem(ref selectedDef.apparel.defaultOutfitTags, s);
			});
		}
	}

	private void DrawWeaponTags(int w)
	{
		SZWidgets.DrawStringList(ref selectedDef.weaponTags, CEditor.API.ListOf<string>(EType.WeaponTags), w, view, Label.WEAPONTAGS, ref ThingTool.lCopyWeaponTags, delegate(string s)
		{
			selectedDef.weaponTags?.Remove(s);
		}, delegate(string s)
		{
			Extension.AddElem(ref selectedDef.weaponTags, s);
		});
	}

	private void DrawWeaponTraits(int w)
	{
		if (ThingTool.SelectedThing.tempThing == null)
		{
			return;
		}
		CompBladelinkWeapon compBladelinkWeapon = ThingTool.SelectedThing.tempThing.TryGetComp<CompBladelinkWeapon>();
		if (compBladelinkWeapon == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.BLADELINKTRAITS, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(ThingTool.AllWeaponTraitDef, (WeaponTraitDef t) => t.SLabel(), delegate(WeaponTraitDef t)
			{
				ThingTool.SelectedThing.tempThing.SetBladeLinkTrait(t);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			ThingTool.SelectedThing.tempThing.CopyBladeLinkTraits();
		});
		if (!ThingTool.lCopyBladeLinkTraits.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				ThingTool.SelectedThing.tempThing.PasteBladeLinkTraits();
			});
		}
		view.Gap(30f);
		view.FullListViewParam1(compBladelinkWeapon.TraitsListForReading, ref selected_WeaponTrait, bRemoveOnClick, delegate(WeaponTraitDef t)
		{
			ThingTool.SelectedThing.tempThing.RemoveBladeLinkTrait(t);
		});
		view.GapLine(25f);
		bool checkOn = compBladelinkWeapon.GetMemberValue("biocoded", fallback: false);
		view.CheckboxLabeled(Label.BIOCODED, 0f, 400f, ref checkOn, null, 2);
		if (!checkOn && compBladelinkWeapon.CodedPawn != null)
		{
			compBladelinkWeapon.UnCode();
		}
		else if (checkOn && compBladelinkWeapon.CodedPawn != tempPawn)
		{
			compBladelinkWeapon.CodeFor(tempPawn);
		}
	}

	internal void DrawLabel()
	{
		GUI.color = ColorTool.colBeige;
		if (ThingTool.SelectedThing.tempThing != null)
		{
			CompGeneratedNames compGeneratedNames = ThingTool.SelectedThing.tempThing.TryGetComp<CompGeneratedNames>();
			if (compGeneratedNames != null)
			{
				string value = compGeneratedNames.Name;
				SZWidgets.LabelEdit(view.GetRect(30f), id++, "", ref value, GameFont.Medium, capitalize: true);
				compGeneratedNames.SetMemberValue("name", value);
			}
		}
		SZWidgets.LabelEdit(view.GetRect(30f), id++, "", ref selectedDef.label, GameFont.Medium, capitalize: true);
		GUI.color = Color.white;
		view.Gap(4f);
	}

	internal void DrawStatFactors(int w)
	{
		view.GapLine(10f);
		view.Label(0f, 0f, 500f, 30f, Label.STAT_FACTORS, GameFont.Medium);
		view.ButtonImage(w - 380, 5f, 24f, 24f, "bnone", ThingTool.UseAllCategories ? Color.white : Color.gray, delegate
		{
			ThingTool.UseAllCategories = !ThingTool.UseAllCategories;
		}, ThingTool.UseAllCategories, Label.TIP_TOGGLECATEGORIES);
		Text.Font = GameFont.Tiny;
		view.FloatMenuButtonWithLabelDef("", w - 350, 200f, DefTool.CategoryLabel(ThingTool.StatFactorCategory), ThingTool.lCategoryDef_Factors, DefTool.CategoryLabel, delegate(StatCategoryDef cat)
		{
			ThingTool.StatFactorCategory = cat;
		}, 0f);
		Text.Font = GameFont.Small;
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(ThingTool.lFreeStatDefFactors, (StatDef s) => s.SLabel(), delegate(StatDef stat)
			{
				ThingTool.SelectedThing.thingDef.SetStatFactor(stat, 0f);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			ThingTool.SelectedThing.thingDef.CopyStatFactors();
		});
		if (!ThingTool.lCopyStatFactors.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				ThingTool.SelectedThing.thingDef.PasteStatFactors();
			});
		}
		view.Gap(30f);
		view.FullListViewParam(ThingTool.SelectedThing.thingDef.statBases, ref selected_StatFactor, (StatModifier s) => s.stat, (StatModifier s) => s.value, null, (StatModifier s) => ThingTool.UseAllCategories ? float.MinValue : s.stat.minValue, (StatModifier s) => ThingTool.UseAllCategories ? float.MaxValue : s.stat.maxValue, isInt: false, bRemoveOnClick, delegate(StatModifier s, float val)
		{
			s.value = val;
		}, null, delegate(StatDef stat)
		{
			ThingTool.SelectedThing.thingDef.RemoveStatFactor(stat);
		});
		view.GapLine(25f);
	}

	internal void DrawStatOffsets(int w)
	{
		view.Label(0f, 0f, 500f, 30f, Label.STAT_OFFSETS, GameFont.Medium);
		Text.Font = GameFont.Tiny;
		view.FloatMenuButtonWithLabelDef("", w - 350, 200f, DefTool.CategoryLabel(ThingTool.StatOffsetCategory), ThingTool.lCategoryDef_Offsets, DefTool.CategoryLabel, delegate(StatCategoryDef cat)
		{
			ThingTool.StatOffsetCategory = cat;
		}, 0f);
		Text.Font = GameFont.Small;
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(ThingTool.lFreeStatDefOffsets, (StatDef s) => s.SLabel(), delegate(StatDef stat)
			{
				ThingTool.SelectedThing.thingDef.SetStatOffset(stat, 0f);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			ThingTool.SelectedThing.thingDef.CopyStatOffsets();
		});
		if (!ThingTool.lCopyStatOffsets.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				ThingTool.SelectedThing.thingDef.PasteStatOffsets();
			});
		}
		view.Gap(30f);
		view.FullListViewParam(ThingTool.SelectedThing.thingDef.equippedStatOffsets, ref selected_StatOffset, (StatModifier s) => s.stat, (StatModifier s) => s.value, null, (StatModifier s) => ThingTool.UseAllCategories ? float.MinValue : s.stat.minValue, (StatModifier s) => ThingTool.UseAllCategories ? float.MaxValue : s.stat.maxValue, isInt: false, bRemoveOnClick, delegate(StatModifier s, float val)
		{
			s.value = val;
		}, null, delegate(StatDef stat)
		{
			ThingTool.SelectedThing.thingDef.RemoveStatOffset(stat);
		});
		view.GapLine(25f);
	}
}
