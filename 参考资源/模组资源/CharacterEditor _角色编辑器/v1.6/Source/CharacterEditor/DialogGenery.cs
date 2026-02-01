using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogGenery : DialogTemplate<GeneDef>
{
	internal const string CO_CACHEDDESCRIPTION = "cachedDescription";

	internal const string CO_CACHEDLABELCAP = "cachedLabelCap";

	private int iTick120 = 120;

	private bool bIsXeno;

	private int mHscroll;

	private SkillDef selected_SkillDef;

	private PawnCapacityDef selected_CapacityDef;

	private NeedDef selected_NeedDef;

	private SkillDef selected_PassionSkillDef;

	private HeadTypeDef selected_ForcedHeadType;

	private DamageDef selected_DamageDef;

	private GeneticTraitData selected_ForcedTrait;

	private GeneticTraitData selected_SuppressedTrait;

	private HediffDef selected_HediffDef;

	private GeneCategoryDef selGeneCategoryDef;

	private HashSet<GeneCategoryDef> lCat;

	private Func<GeneCategoryDef, string> FCatLabel = (GeneCategoryDef a) => a.IsNullOrEmpty() ? Label.ALL : a.LabelCap.ToString();

	private HashSet<SoundDef> lAllSounds;

	private HashSet<GeneDef> lAllGenes;

	private HashSet<HistoryEventDef> lAllHistoryEvents;

	private HashSet<NeedDef> lAllNeeds;

	private HashSet<ChemicalDef> lAllChems;

	private HashSet<HairDef> lAllHairs;

	private HashSet<GeneCategoryDef> lAllGeneCategories;

	private HashSet<EndogeneCategory> lAllEndogeneCategories;

	private HashSet<GeneticBodyType?> lAllBodyTypes;

	private HashSet<SkillDef> lAllSkills;

	internal DialogGenery(bool xeno)
		: base(SearchTool.SIndex.GeneDef, Label.ADD_GENE, 105)
	{
		customAcceptLabel = "OK".Translate();
		bIsXeno = xeno;
		lCat = new HashSet<GeneCategoryDef>();
		lCat.Add(null);
		lCat.AddRange(DefTool.ListBy((GeneCategoryDef x) => !x.defName.NullOrEmpty()));
		mHscroll = 0;
		view = new Listing_X();
		lAllSounds = DefTool.AllDefsWithNameWithNull((SoundDef s) => s.defName.StartsWith("Pawn_"));
		lAllGenes = DefTool.AllDefsWithLabelWithNull<GeneDef>();
		lAllHistoryEvents = DefTool.AllDefsWithLabelWithNull<HistoryEventDef>();
		lAllNeeds = DefTool.AllDefsWithLabelWithNull<NeedDef>();
		lAllChems = DefTool.AllDefsWithLabelWithNull<ChemicalDef>();
		lAllHairs = DefTool.AllDefsWithLabelWithNull<HairDef>();
		lAllGeneCategories = DefTool.AllDefsWithLabelWithNull<GeneCategoryDef>();
		lAllEndogeneCategories = EnumTool.GetAllEnumsOfType<EndogeneCategory>().ToHashSet();
		lAllSkills = DefTool.AllDefsWithLabelWithNull<SkillDef>();
		lAllBodyTypes = GeneTool.ListOfGeneticBodyType();
	}

	internal override HashSet<GeneDef> TList()
	{
		return (from x in DefTool.ListByMod<GeneDef>(search.modName)
			orderby x.index
			select x).ToHashSet();
	}

	internal override void AReset()
	{
		PresetGene.ResetToDefault(selectedDef?.defName);
		if (GeneTool.SelectedGene.IsBodySizeGene())
		{
			PresetLifeStage.ResetToDefault(GeneTool.SelectedGene.GetLifeStageDef().defName);
		}
	}

	internal override void AResetAll()
	{
		PresetGene.ResetAllToDefaults();
		PresetLifeStage.ResetAllToDefaults();
	}

	internal override void ASave()
	{
		PresetGene.SaveModification(selectedDef);
		if (GeneTool.SelectedGene.IsBodySizeGene())
		{
			PresetLifeStage.SaveModification(GeneTool.SelectedGene.GetLifeStageDef());
		}
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
		base.ASelectedModName(search.modName);
		selectedDef = GeneTool.SelectedGene ?? lDefs.First();
		oldSelectedDef = null;
		GeneTool.UseAllCategories = false;
		selected_StatFactor = null;
		selected_StatOffset = null;
		selected_CapacityDef = null;
		selected_SkillDef = null;
		selected_HediffDef = null;
		selected_NeedDef = null;
		selected_PassionSkillDef = null;
		selected_ForcedHeadType = null;
		selected_DamageDef = null;
		selected_ForcedTrait = null;
		selected_SuppressedTrait = null;
		if (search.ofilter1 != null)
		{
			ASelectedCategoryDef(search.ofilter1 as GeneCategoryDef);
		}
	}

	internal override int DrawCustomFilter(int x, int y, int w)
	{
		Text.Font = GameFont.Small;
		Rect rect = new Rect(x, y, w, 30f);
		SZWidgets.FloatMenuOnButtonText(rect, FCatLabel(selGeneCategoryDef), lCat, FCatLabel, ASelectedCategoryDef);
		return 30;
	}

	private void ASelectedCategoryDef(GeneCategoryDef def)
	{
		search.ofilter1 = def;
		selGeneCategoryDef = def;
		lDefs = (from td in DefTool.ListByMod<GeneDef>(search.modName).ToHashSet()
			where def == null || td.displayCategory == def
			orderby td.label
			select td).ToHashSet();
	}

	internal override void ASelectedModName(string val)
	{
		base.ASelectedModName(val);
		selGeneCategoryDef = null;
	}

	internal override void OnAccept()
	{
		CEditor.API.Pawn.RememberBackstory();
		CEditor.API.Pawn.AddGeneAsFirst(selectedDef, bIsXeno);
		GeneTool.PrintIfXenotypeIsPrefered(CEditor.API.Pawn);
		CEditor.API.UpdateGraphics();
	}

	internal override void OnSelectionChanged()
	{
		mHscroll = 0;
		GeneTool.SelectedGene = selectedDef;
		GeneTool.SelectedGene.UpdateFreeLists(GeneTool.FreeList.All);
	}

	internal override void DrawParameter()
	{
		DrawLabel();
		DrawBioStats(400);
		int w = base.WPARAM - 12;
		DrawLifeStages(400);
		DrawStatFactors(w);
		DrawStatOffsets(w);
		DrawAptitudes(w);
		DrawCapacities(w);
		DrawAbilities(w);
		DrawForcedTraits(w);
		DrawSuppressedTraits(w);
		DrawImmunities(w);
		DrawProtections(w);
		DrawDamageFactors(w);
		DrawDisabledNeeds(w);
		DrawDisabledWorkTags(w);
		DrawForcedHeadTypes(w);
		DrawStringLists(w);
		DrawSounds(500);
		DrawFloats(400);
		DrawBools(400);
		DrawTypes(400);
		mHscroll = (int)view.CurY + 50;
		UpdateCachedDescription();
	}

	private void UpdateCachedDescription()
	{
		if (iTick120 <= 0)
		{
			GeneTool.SelectedGene.SetMemberValue("cachedDescription", null);
			GeneTool.SelectedGene.SetMemberValue("cachedLabelCap", null);
			if (GeneTool.SelectedGene.IsBodySizeGene())
			{
				CEditor.API.UpdateGraphics();
			}
			iTick120 = 120;
		}
		else
		{
			iTick120--;
		}
	}

	private void DrawLabel()
	{
		GUI.color = ColorTool.colBeige;
		SZWidgets.LabelEdit(view.GetRect(30f), 99, "", ref GeneTool.SelectedGene.label, GameFont.Medium, capitalize: true);
		GUI.color = Color.white;
		view.Gap(8f);
	}

	private void DrawTypes(int w)
	{
		view.Gap(16f);
		GUI.color = Color.gray;
		SZWidgets.Label(view.GetRect(22f), GeneTool.SelectedGene.resourceGizmoType?.ToString());
		SZWidgets.Label(view.GetRect(22f), GeneTool.SelectedGene.geneClass?.ToString());
		SZWidgets.Label(view.GetRect(22f), GeneTool.SelectedGene.SDefname());
		GUI.color = Color.white;
		view.Gap(16f);
	}

	private void DrawCategories(int w)
	{
		SZWidgets.NonDefSelectorSimple(view.GetRect(22f), w, lAllBodyTypes, ref GeneTool.SelectedGene.bodyType, Label.GENETICBODYTYPE + " ", FLabel.GeneticBodytype, delegate(GeneticBodyType? b)
		{
			GeneTool.SelectedGene.bodyType = b;
		});
		view.Gap(4f);
		SZWidgets.NonDefSelectorSimple(view.GetRect(22f), w, lAllEndogeneCategories, ref GeneTool.SelectedGene.endogeneCategory, Label.ENDOGENECATEGORY + " ", FLabel.EndogeneCat, delegate(EndogeneCategory s)
		{
			GeneTool.SelectedGene.endogeneCategory = s;
		});
		view.Gap(4f);
		SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lAllGeneCategories, ref GeneTool.SelectedGene.displayCategory, Label.GENECATEGORY + " ", FLabel.DefLabel, delegate(GeneCategoryDef c)
		{
			GeneTool.SelectedGene.displayCategory = c;
		});
		view.Gap(4f);
	}

	private void DrawSounds(int w)
	{
		SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lAllSounds, ref GeneTool.SelectedGene.soundCall, Label.SOUNDCALL + " ", FLabel.Sound, delegate(SoundDef s)
		{
			GeneTool.SelectedGene.soundCall = SoundTool.GetAndPlayPawnSoundCur(s);
		}, hasLR: true, "bsound", SoundTool.PlayPawnSoundCur);
		view.Gap(4f);
		SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lAllSounds, ref GeneTool.SelectedGene.soundDeath, Label.SOUNDDEATH + " ", FLabel.Sound, delegate(SoundDef s)
		{
			GeneTool.SelectedGene.soundDeath = SoundTool.GetAndPlayPawnSoundCur(s);
		}, hasLR: true, "bsound", SoundTool.PlayPawnSoundCur);
		view.Gap(4f);
		SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lAllSounds, ref GeneTool.SelectedGene.soundWounded, Label.SOUNDWOUNDED + " ", FLabel.Sound, delegate(SoundDef s)
		{
			GeneTool.SelectedGene.soundWounded = SoundTool.GetAndPlayPawnSoundCur(s);
		}, hasLR: true, "bsound", SoundTool.PlayPawnSoundCur);
		view.Gap(4f);
	}

	private void DrawBioStats(int w)
	{
		SZWidgets.LabelIntFieldSlider(view, w, 30, FLabel.Complexity, ref GeneTool.SelectedGene.biostatCpx, 0, 9);
		SZWidgets.LabelIntFieldSlider(view, w, 40, FLabel.Metabolism, ref GeneTool.SelectedGene.biostatMet, -9, 9);
		SZWidgets.LabelIntFieldSlider(view, w, 50, FLabel.ArchitesRequired, ref GeneTool.SelectedGene.biostatArc, 0, 5);
	}

	private void DrawFloats(int w)
	{
		view.Gap(8f);
		DrawColors(420);
		DrawForcedHair(w);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.RandomBrightnessFactor, ref GeneTool.SelectedGene.randomBrightnessFactor, 0f, 2f, 2);
		view.Gap(8f);
		DrawPassion(w);
		DrawCausedNeed(w);
		DrawChemicalDef(w);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.AddictionChance, ref GeneTool.SelectedGene.addictionChanceFactor, 0f, 50f, 2);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.OverdoseChance, ref GeneTool.SelectedGene.overdoseChanceFactor, 0f, 50f, 2);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.ToleranceFactor, ref GeneTool.SelectedGene.toleranceBuildupFactor, 0f, 50f, 2);
		view.Gap(8f);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.FoodPoisonChance, ref GeneTool.SelectedGene.foodPoisoningChanceFactor, -1f, 10f, 2);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.PainFactor, ref GeneTool.SelectedGene.painFactor, 0f, 100f, 2);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.PainOffset, ref GeneTool.SelectedGene.painOffset, 0f, 100f, 2);
		view.Gap(8f);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.MentalBreakMTB, ref GeneTool.SelectedGene.mentalBreakMtbDays, 0f, 100f, 2);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.MentalBreakChance, ref GeneTool.SelectedGene.aggroMentalBreakSelectionChanceFactor, 0f, 100f, 2);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.SocialFightChance, ref GeneTool.SelectedGene.socialFightChanceFactor, 0f, 100f, 2);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.PrisonBreak, ref GeneTool.SelectedGene.prisonBreakMTBFactor, 0f, 100f, 2);
		view.Gap(8f);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.MarketValue, ref GeneTool.SelectedGene.marketValueFactor, 0f, 100f, 2);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.MinAgeActive, ref GeneTool.SelectedGene.minAgeActive, 0f, 500f, 0);
		DrawHistoryEvent(w);
		view.Gap(8f);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.SelectionWeight, ref GeneTool.SelectedGene.selectionWeight, 0f, 2f, 2);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.SelectionWeightDark, ref GeneTool.SelectedGene.selectionWeightFactorDarkSkin, 0f, 2f, 2);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.DisplayOrderInCat, ref GeneTool.SelectedGene.displayOrderInCategory, -100f, 10000f, 0);
		view.Gap(8f);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.LovinMTBFactor, ref GeneTool.SelectedGene.lovinMTBFactor, 0f, 10f, 2);
		SZWidgets.LabelFloatFieldSlider(view, base.WPARAM - 40, id++, FLabel.MissingRomanceChance, ref GeneTool.SelectedGene.missingGeneRomanceChanceFactor, 0f, 100f, 2);
		view.Gap(8f);
		DrawCategories(w);
		DrawPrerequisite(w);
		SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.ResourceLoss, ref GeneTool.SelectedGene.resourceLossPerDay, 0f, 100f, 2);
		view.LabelEdit(id++, Label.RESOURCELABEL, ref GeneTool.SelectedGene.resourceLabel, GameFont.Small);
		view.LabelEdit(id++, Label.RESOURCEDESC, ref GeneTool.SelectedGene.resourceDescription, GameFont.Small);
		view.Gap(8f);
		view.LabelEdit(id++, Label.LABELADJ, ref GeneTool.SelectedGene.labelShortAdj, GameFont.Small);
		view.LabelEdit(id++, Label.ICONPATH, ref GeneTool.SelectedGene.iconPath, GameFont.Small);
		view.Gap(16f);
	}

	private void DrawBools(int w)
	{
		view.CheckboxLabeled(Label.UNAFFECTEDBYDARK, 0f, w, ref GeneTool.SelectedGene.ignoreDarkness, null, 2);
		view.CheckboxLabeled(Label.CANGENERATEINGENESET, 0f, w, ref GeneTool.SelectedGene.canGenerateInGeneSet, null, 2);
		view.CheckboxLabeled(Label.REMOVEONREDRESS, 0f, w, ref GeneTool.SelectedGene.removeOnRedress, null, 2);
		view.CheckboxLabeled(Label.PASSONDIRECTLY, 0f, w, ref GeneTool.SelectedGene.passOnDirectly, null, 2);
		view.CheckboxLabeled(Label.RANDOMCHOSEN, 0f, w, ref GeneTool.SelectedGene.randomChosen, null, 2);
		view.CheckboxLabeled(Label.STERILIZE, 0f, w, ref GeneTool.SelectedGene.sterilize, null, 2);
		view.CheckboxLabeled(Label.DISLIKESSUNLIGHT, 0f, w, ref GeneTool.SelectedGene.dislikesSunlight, null, 2);
		view.CheckboxLabeled(Label.DONTMINDRAWFOOD, 0f, w, ref GeneTool.SelectedGene.dontMindRawFood, null, 2);
		view.CheckboxLabeled(Label.IMMUNETOTOXGASEXPOSURE, 0f, w, ref GeneTool.SelectedGene.immuneToToxGasExposure, null, 2);
		view.CheckboxLabeled(Label.NEVERGRAYHAIR, 0f, w, ref GeneTool.SelectedGene.neverGrayHair, null, 2);
		view.CheckboxLabeled(Label.WOMENCANHAVEBEARDS, 0f, w, ref GeneTool.SelectedGene.womenCanHaveBeards, null, 2);
		view.CheckboxLabeled(Label.PREVENTPERMANENTWOUNDS, 0f, w, ref GeneTool.SelectedGene.preventPermanentWounds, null, 2);
		view.CheckboxLabeled("showGizmoOnWorldView", 0f, w, ref GeneTool.SelectedGene.showGizmoOnWorldView, null, 2);
		view.CheckboxLabeled("showGizmoWhenDrafted", 0f, w, ref GeneTool.SelectedGene.showGizmoWhenDrafted, null, 2);
		view.CheckboxLabeled("showGizmoOnMultiSelect", 0f, w, ref GeneTool.SelectedGene.showGizmoOnMultiSelect, null, 2);
	}

	private void DrawColors(int w)
	{
		view.NavSelectorColor(w, Label.HAIRCOLOROVERRIDE, "", GeneTool.SelectedGene.hairColorOverride, delegate
		{
			WindowTool.Open(new DialogColorPicker(ColorType.GeneColorHair, _primaryColor: true, null, null, GeneTool.SelectedGene));
		});
		view.Gap(4f);
		view.NavSelectorColor(w, Label.SKINCOLORBASE, "", GeneTool.SelectedGene.skinColorBase, delegate
		{
			WindowTool.Open(new DialogColorPicker(ColorType.GeneColorSkinBase, _primaryColor: true, null, null, GeneTool.SelectedGene));
		});
		view.Gap(4f);
		view.NavSelectorColor(w, Label.SKINCOLOROVERRIDE, "", GeneTool.SelectedGene.skinColorOverride, delegate
		{
			WindowTool.Open(new DialogColorPicker(ColorType.GeneColorSkinOverride, _primaryColor: true, null, null, GeneTool.SelectedGene));
		});
		view.Gap(4f);
	}

	private void DrawPrerequisite(int w)
	{
		SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lAllGenes, ref GeneTool.SelectedGene.prerequisite, Label.PREREQUISITE + " ", FLabel.DefLabel, delegate(GeneDef g)
		{
			GeneTool.SelectedGene.prerequisite = g;
		});
		view.Gap(4f);
	}

	private void DrawHistoryEvent(int w)
	{
		SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lAllHistoryEvents, ref GeneTool.SelectedGene.deathHistoryEvent, Label.HISTORYEVENTONDEATH + " ", FLabel.DefLabel, delegate(HistoryEventDef e)
		{
			GeneTool.SelectedGene.deathHistoryEvent = e;
		});
		view.Gap(4f);
	}

	private void DrawPassion(int w)
	{
		if (GeneTool.SelectedGene.passionMod == null || (GeneTool.SelectedGene.passionMod != null && GeneTool.SelectedGene.passionMod.modType != PassionMod.PassionModType.AddOneLevel))
		{
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lAllSkills, ref selected_PassionSkillDef, Label.PASSIONMODADD + " ", FLabel.PassionModAdd, delegate(SkillDef s)
			{
				GeneTool.SelectedGene.SetPassionMod(s, PassionMod.PassionModType.AddOneLevel);
			});
		}
		else if (GeneTool.SelectedGene.passionMod != null)
		{
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lAllSkills, ref GeneTool.SelectedGene.passionMod.skill, Label.PASSIONMODADD + " ", FLabel.PassionModAdd, delegate(SkillDef s)
			{
				GeneTool.SelectedGene.SetPassionMod(s, PassionMod.PassionModType.AddOneLevel);
			});
		}
		view.Gap(4f);
		if (GeneTool.SelectedGene.passionMod == null || (GeneTool.SelectedGene.passionMod != null && GeneTool.SelectedGene.passionMod.modType != PassionMod.PassionModType.DropAll))
		{
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lAllSkills, ref selected_PassionSkillDef, Label.PASSIONMODSUB + " ", FLabel.PassionModDrop, delegate(SkillDef s)
			{
				GeneTool.SelectedGene.SetPassionMod(s, PassionMod.PassionModType.DropAll);
			});
		}
		else if (GeneTool.SelectedGene.passionMod != null)
		{
			SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lAllSkills, ref GeneTool.SelectedGene.passionMod.skill, Label.PASSIONMODSUB + " ", FLabel.PassionModDrop, delegate(SkillDef s)
			{
				GeneTool.SelectedGene.SetPassionMod(s, PassionMod.PassionModType.DropAll);
			});
		}
		view.Gap(4f);
	}

	private void DrawCausedNeed(int w)
	{
		view.Gap(4f);
	}

	private void DrawChemicalDef(int w)
	{
		SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lAllChems, ref GeneTool.SelectedGene.chemical, "Chemical".Translate() + " ", FLabel.DefLabel, delegate(ChemicalDef c)
		{
			GeneTool.SelectedGene.SetChemicalDef(c);
		});
		view.Gap(4f);
	}

	private void DrawForcedHair(int w)
	{
		SZWidgets.DefSelectorSimple(view.GetRect(22f), w, lAllHairs, ref GeneTool.SelectedGene.forcedHair, "Hair".Translate().CapitalizeFirst() + " ", FLabel.DefLabel, delegate(HairDef h)
		{
			GeneTool.SelectedGene.SetForcedHairDef(h);
		});
		view.Gap(4f);
	}

	private void DrawStatFactors(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.STAT_FACTORS, GameFont.Medium);
		view.ButtonImage(w - 380, 5f, 24f, 24f, "bnone", GeneTool.UseAllCategories ? Color.white : Color.gray, delegate
		{
			GeneTool.UseAllCategories = !GeneTool.UseAllCategories;
		}, GeneTool.UseAllCategories, Label.TIP_TOGGLECATEGORIES);
		Text.Font = GameFont.Tiny;
		view.FloatMenuButtonWithLabelDef("", w - 350, 200f, DefTool.CategoryLabel(GeneTool.StatFactorCategory), GeneTool.lCategoryDef_Factors, DefTool.CategoryLabel, delegate(StatCategoryDef cat)
		{
			GeneTool.StatFactorCategory = cat;
		}, 0f);
		Text.Font = GameFont.Small;
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeStatDefFactors, (StatDef s) => s.SLabel(), delegate(StatDef stat)
			{
				GeneTool.SelectedGene.SetStatFactor(stat, 0f);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyStatFactors();
		});
		if (!GeneTool.lCopyStatFactors.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteStatFactors();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			view.FullListViewParam(GeneTool.SelectedGene.statFactors, ref selected_StatFactor, (StatModifier s) => s.stat, (StatModifier s) => s.value, null, (StatModifier s) => s.stat.minValue, (StatModifier s) => s.stat.maxValue, isInt: false, bRemoveOnClick, delegate(StatModifier s, float val)
			{
				s.value = val;
			}, null, delegate(StatDef stat)
			{
				GeneTool.SelectedGene.RemoveStatFactor(stat);
			});
		}
		view.GapLine(25f);
	}

	private void DrawStatOffsets(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.STAT_OFFSETS, GameFont.Medium);
		Text.Font = GameFont.Tiny;
		view.FloatMenuButtonWithLabelDef("", w - 350, 200f, DefTool.CategoryLabel(GeneTool.StatOffsetCategory), GeneTool.lCategoryDef_Offsets, DefTool.CategoryLabel, delegate(StatCategoryDef cat)
		{
			GeneTool.StatOffsetCategory = cat;
		}, 0f);
		Text.Font = GameFont.Small;
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeStatDefOffsets, (StatDef s) => s.SLabel(), delegate(StatDef stat)
			{
				GeneTool.SelectedGene.SetStatOffset(stat, 0f);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyStatOffsets();
		});
		if (!GeneTool.lCopyStatOffsets.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteStatOffsets();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			view.FullListViewParam(GeneTool.SelectedGene.statOffsets, ref selected_StatOffset, (StatModifier s) => s.stat, (StatModifier s) => s.value, null, (StatModifier s) => s.stat.minValue, (StatModifier s) => s.stat.maxValue, isInt: false, bRemoveOnClick, delegate(StatModifier s, float val)
			{
				s.value = val;
			}, null, delegate(StatDef stat)
			{
				GeneTool.SelectedGene.RemoveStatOffset(stat);
			});
		}
		view.GapLine(25f);
	}

	private void DrawAptitudes(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.APTITUDE, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeAptitudes, (SkillDef skill) => skill.SLabel(), delegate(SkillDef skill)
			{
				GeneTool.SelectedGene.SetAptitude(skill, 0);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyAptitudes();
		});
		if (!GeneTool.lCopyAptitude.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteAptitude();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			view.FullListViewParam(GeneTool.SelectedGene.aptitudes, ref selected_SkillDef, (Aptitude a) => a.skill, (Aptitude a) => a.level, null, (Aptitude a) => -999f, (Aptitude a) => 999f, isInt: true, bRemoveOnClick, delegate(Aptitude a, float val)
			{
				a.level = (int)val;
			}, null, delegate(SkillDef skill)
			{
				GeneTool.SelectedGene.RemoveAptitude(skill);
			});
		}
		view.GapLine(25f);
	}

	private void DrawCapacities(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.CAPACITIES, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeCapacities, (PawnCapacityDef cap) => cap.SLabel(), delegate(PawnCapacityDef cap)
			{
				GeneTool.SelectedGene.SetCapacity(cap, 0f, 0f);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyCapacities();
		});
		if (!GeneTool.lCopyCapacities.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteCapacities();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			view.FullListViewParam(GeneTool.SelectedGene.capMods, ref selected_CapacityDef, (PawnCapacityModifier c) => c.capacity, (PawnCapacityModifier c) => c.offset, (PawnCapacityModifier c) => c.postFactor, (PawnCapacityModifier c) => 0f, (PawnCapacityModifier c) => c.setMax, isInt: false, bRemoveOnClick, delegate(PawnCapacityModifier c, float val)
			{
				c.offset = val;
			}, delegate(PawnCapacityModifier c, float val)
			{
				c.postFactor = val;
			}, delegate(PawnCapacityDef capacity)
			{
				GeneTool.SelectedGene.RemoveCapacity(capacity);
			});
		}
		view.GapLine(25f);
	}

	private void DrawAbilities(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.ABILITIES, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeAbilities, (AbilityDef ability) => ability.SLabel(), delegate(AbilityDef ability)
			{
				GeneTool.SelectedGene.SetAbility(ability);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyAbilities();
		});
		if (!GeneTool.lCopyAbilities.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteAbilities();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			if (SZContainers.DrawElementStack(new Rect(view.CurX, view.CurY, base.WPARAM - 32, 50f), GeneTool.SelectedGene.abilities, bRemoveOnClick, delegate(AbilityDef a)
			{
				GeneTool.SelectedGene.RemoveAbility(a);
			}))
			{
				int num = (int)Math.Floor(((float)base.WPARAM - 32f) / 38f);
				float num2 = (float)GeneTool.SelectedGene.abilities.Count / (float)num;
				num2 = 70f * (float)(int)Math.Ceiling(num2);
				view.GapLineCustom(5f + num2, 10f);
			}
			else
			{
				view.GapLine(25f);
			}
		}
	}

	private void DrawForcedTraits(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.FORCEDTRAITS, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeForcedTraits, TraitTool.FGeneticTraitLabel, delegate(GeneticTraitData gtd)
			{
				GeneTool.SelectedGene.SetForcedTrait(gtd, gtd.def, gtd.degree);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyForcedTraits();
		});
		if (!GeneTool.lCopyForcedTraits.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteForcedTraits();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			view.FullListViewParam2(GeneTool.SelectedGene.forcedTraits, ref selected_ForcedTrait, DefTool.DefGetterGeneticTraitData, TraitTool.FGeneticTraitLabel, bRemoveOnClick, delegate(GeneticTraitData gtd)
			{
				GeneTool.SelectedGene.RemoveForcedTrait(gtd);
			});
		}
		view.GapLine(25f);
	}

	private void DrawSuppressedTraits(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.SUPPRESSEDTRAITS, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeSuppressedTraits, TraitTool.FGeneticTraitLabel, delegate(GeneticTraitData gtd)
			{
				GeneTool.SelectedGene.SetSuppressedTrait(gtd, gtd.def, gtd.degree);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopySuppressedTraits();
		});
		if (!GeneTool.lCopySuppressedTraits.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteSuppressedTraits();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			view.FullListViewParam2(GeneTool.SelectedGene.suppressedTraits, ref selected_SuppressedTrait, DefTool.DefGetterGeneticTraitData, TraitTool.FGeneticTraitLabel, bRemoveOnClick, delegate(GeneticTraitData gtd)
			{
				GeneTool.SelectedGene.RemoveSuppressedTrait(gtd);
			});
		}
		view.GapLine(25f);
	}

	private void DrawImmunities(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.IMMUNETO, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeImmunities, (HediffDef h) => h.SLabel(), delegate(HediffDef h)
			{
				GeneTool.SelectedGene.SetImmunity(h);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyImmunities();
		});
		if (!GeneTool.lCopyImmunities.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteImmunities();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			view.FullListViewParam1(GeneTool.SelectedGene.makeImmuneTo, ref selected_HediffDef, bRemoveOnClick, delegate(HediffDef h)
			{
				GeneTool.SelectedGene.RemoveImmunity(h);
			});
		}
		view.GapLine(25f);
	}

	private void DrawProtections(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.FULLYPROTECTEDFROM, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeProtections, (HediffDef h) => h.SLabel(), delegate(HediffDef h)
			{
				GeneTool.SelectedGene.SetProtection(h);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyProtections();
		});
		if (!GeneTool.lCopyProtections.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteProtections();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			view.FullListViewParam1(GeneTool.SelectedGene.hediffGiversCannotGive, ref selected_HediffDef, bRemoveOnClick, delegate(HediffDef h)
			{
				GeneTool.SelectedGene.RemoveProtection(h);
			});
		}
		view.GapLine(25f);
	}

	private void DrawDamageFactors(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.DAMAGEFACTOR, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeDamageFactors, GeneTool.DamageLabel, delegate(DamageDef d)
			{
				GeneTool.SelectedGene.SetDamageFactor(d, 0f);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyDamageFactors();
		});
		if (!GeneTool.lCopyDamageFactors.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteDamageFactors();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			view.FullListViewParam(GeneTool.SelectedGene.damageFactors, ref selected_DamageDef, (DamageFactor d) => d.damageDef, (DamageFactor d) => d.factor, null, (DamageFactor d) => -999f, (DamageFactor d) => 999f, isInt: false, bRemoveOnClick, delegate(DamageFactor d, float val)
			{
				d.factor = val;
			}, null, delegate(DamageDef d)
			{
				GeneTool.SelectedGene.RemoveDamageFactor(d);
			});
		}
		view.GapLine(25f);
	}

	private void DrawDisabledNeeds(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.DISABLEDNEEDS, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeNeeds, (NeedDef need) => need.SLabel(), delegate(NeedDef need)
			{
				GeneTool.SelectedGene.SetDisabledNeed(need);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyDisabledNeeds();
		});
		if (!GeneTool.lCopyDisabledNeeds.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteDisabledNeeds();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			view.FullListViewParam1(GeneTool.SelectedGene.disablesNeeds, ref selected_NeedDef, bRemoveOnClick, delegate(NeedDef need)
			{
				GeneTool.SelectedGene.RemoveDisabledNeed(need);
			});
		}
		view.GapLine(25f);
	}

	private void DrawForcedHeadTypes(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.FORCEDHEADTYPES, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeForcedHeadTypes, (HeadTypeDef h) => h.SDefname(), delegate(HeadTypeDef h)
			{
				GeneTool.SelectedGene.SetForcedHeadType(h);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyForcedHeadTypes();
		});
		if (!GeneTool.lCopyForcedHeadTypes.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteForcedHeadTypes();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			view.FullListViewParam1(GeneTool.SelectedGene.forcedHeadTypes, ref selected_ForcedHeadType, bRemoveOnClick, delegate(HeadTypeDef h)
			{
				GeneTool.SelectedGene.RemoveForcedHeadType(h);
			});
		}
		view.GapLine(25f);
	}

	private void DrawDisabledWorkTags(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.DISABLEDWORKTAGS, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.FloatMenuOnRect(GeneTool.lFreeWorkTags, (WorkTags work) => work.LabelTranslated(), delegate(WorkTags work)
			{
				GeneTool.SelectedGene.SetDisabledWorkTags(work);
			});
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyDisabledWorkTags();
		});
		if (GeneTool.lCopyDisabledWorkTags != WorkTags.None)
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteDisabledWorkTags();
			});
		}
		view.Gap(30f);
		if (base.IsGeneDef)
		{
			view.FullListViewWorkTags(GeneTool.SelectedGene.disabledWorkTags, bRemoveOnClick, delegate(WorkTags work)
			{
				GeneTool.SelectedGene.RemoveDisabledWorkTags(work);
			});
		}
		view.GapLine(25f);
	}

	private void DrawStringLists(int w)
	{
		if (GeneTool.SelectedGene != null)
		{
			DrawCustomEffectDescriptors(w);
			DrawExclusionTags(w);
			DrawHairTags(w);
			DrawBeardTags(w);
			DrawGizmoThres(w);
		}
	}

	private void DrawCustomEffectDescriptors(int w)
	{
		SZWidgets.DrawStringListCustom(ref GeneTool.SelectedGene.customEffectDescriptions, 1, w, view, Label.CUSTOMEFFECTDESCRIPTIONS, ref GeneTool.lCopyCustomEffectDescriptions, delegate(string s)
		{
			GeneTool.SelectedGene.customEffectDescriptions.Remove(s);
		});
	}

	private void DrawExclusionTags(int w)
	{
		SZWidgets.DrawStringList(ref GeneTool.SelectedGene.exclusionTags, CEditor.API.ListOf<string>(EType.ExclusionTags), w, view, Label.EXCLUSIONTAGS, ref GeneTool.lCopyExclusionTags, delegate(string s)
		{
			GeneTool.SelectedGene.exclusionTags?.Remove(s);
		}, delegate(string s)
		{
			Extension.AddElem(ref GeneTool.SelectedGene.exclusionTags, s);
		});
	}

	private void DrawHairTags(int w)
	{
		SZWidgets.DrawTagFilter(ref GeneTool.SelectedGene.hairTagFilter, CEditor.API.ListOf<string>(EType.HairTags), w, view, Label.HAIRTAGS, ref GeneTool.lCopyHairTags, delegate(string s)
		{
			GeneTool.SelectedGene.hairTagFilter?.tags.Remove(s);
		}, delegate(string s)
		{
			Extension.AddTag(ref GeneTool.SelectedGene.hairTagFilter, s);
		});
	}

	private void DrawBeardTags(int w)
	{
		SZWidgets.DrawTagFilter(ref GeneTool.SelectedGene.beardTagFilter, CEditor.API.ListOf<string>(EType.BeardTags), w, view, Label.BEARDTAGS, ref GeneTool.lCopyBeardTags, delegate(string s)
		{
			GeneTool.SelectedGene.beardTagFilter?.tags.Remove(s);
		}, delegate(string s)
		{
			Extension.AddTag(ref GeneTool.SelectedGene.beardTagFilter, s);
		});
	}

	private void DrawGizmoThres(int w)
	{
		if (GeneTool.SelectedGene == null)
		{
			return;
		}
		view.Label(0f, 0f, 500f, 30f, Label.RESOURCEGIZMOTHRESHOLD, GameFont.Medium);
		view.ButtonImage(w - 60, 5f, 24f, 24f, "UI/Buttons/Dev/Add", delegate
		{
			SZWidgets.ActivateLabelEdit(5);
		});
		view.ButtonImage(w - 85, 5f, 24f, 24f, "bminus", base.ToggleRemove, base.RemoveColor);
		view.ButtonImage(w - 110, 5f, 18f, 24f, "UI/Buttons/Copy", delegate
		{
			GeneTool.SelectedGene.CopyGizmoThres();
		});
		if (!GeneTool.lCopyGizmoThres.NullOrEmpty())
		{
			view.ButtonImage(w - 130, 5f, 18f, 24f, "UI/Buttons/Paste", delegate
			{
				GeneTool.SelectedGene.PasteGizmoThres();
			});
		}
		view.Gap(30f);
		SZWidgets.AddLabelEditToList(view, 5, ref GeneTool.SelectedGene.resourceGizmoThresholds, null);
		if (base.IsGeneDef)
		{
			view.FullListViewFloat(base.WPARAM - 40, GeneTool.SelectedGene.resourceGizmoThresholds, bRemoveOnClick, delegate(float s)
			{
				GeneTool.SelectedGene.resourceGizmoThresholds.Remove(s);
			});
		}
		view.GapLine(25f);
	}

	private void DrawLifeStages(int w)
	{
		if (selectedDef.IsBodySizeGene())
		{
			LifeStageDef lifeStageDef = GeneTool.SelectedGene.GetLifeStageDef();
			if (lifeStageDef != null)
			{
				view.GapLine();
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.BodySizeFactor, ref lifeStageDef.bodySizeFactor, 0.1f, 5f, 2);
				SZWidgets.LabelFloatZeroFieldSlider(view, w, id++, FLabel.BodyWidth, ref lifeStageDef.bodyWidth, 0.1f, 5f, 2);
				SZWidgets.LabelFloatZeroFieldSlider(view, w, id++, FLabel.HeadSizeFactor, ref lifeStageDef.headSizeFactor, 0.1f, 5f, 2);
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.HealthScaleFactor, ref lifeStageDef.healthScaleFactor, 0.1f, 5f, 2);
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.HungerRateFactor, ref lifeStageDef.hungerRateFactor, 0f, 5f, 2);
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.FoodMaxFactor, ref lifeStageDef.foodMaxFactor, 0f, 5f, 2);
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.MeleeDamageFactor, ref lifeStageDef.meleeDamageFactor, 0f, 5f, 2);
				SZWidgets.LabelFloatZeroFieldSlider(view, w, id++, FLabel.EyeSizeFactor, ref lifeStageDef.eyeSizeFactor, 0f, 5f, 2);
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.MarketValueFactor, ref lifeStageDef.marketValueFactor, 0f, 5f, 2);
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.VoicePitch, ref lifeStageDef.voxPitch, 0.1f, 5f, 2);
				SZWidgets.LabelFloatFieldSlider(view, w, id++, FLabel.VoiceVolume, ref lifeStageDef.voxVolume, 0.1f, 5f, 2);
				view.CheckboxLabeled(Label.REPRODUCTIVE, 0f, w, ref lifeStageDef.reproductive, null, 2);
				view.CheckboxLabeled(Label.CARAVANRIDEABLE, 0f, w, ref lifeStageDef.caravanRideable, null, 2);
				view.GapLine();
			}
		}
	}
}
