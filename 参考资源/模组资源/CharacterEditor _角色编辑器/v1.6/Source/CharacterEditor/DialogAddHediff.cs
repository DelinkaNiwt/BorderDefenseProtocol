using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogAddHediff : Window, IPawnable, IPartable
{
	private Pawn selPawn;

	private BodyPartRecord selPart;

	private bool isPartSelectionDone = false;

	private bool isPawnSelectionDone = false;

	private bool bNeedsPawn = false;

	private bool bNeedsPart = false;

	private Vector2 scrollPos;

	private HediffDef selectedHediff;

	private HediffDef oldSelectedHediff;

	private float selectedSeverity;

	private int selectedDuration;

	private int selectedPain;

	private int selectedLevel;

	private bool inEditMode;

	private bool isAdjustable;

	private bool isPermanent;

	private bool doOnce;

	private List<string> lOfFilter1;

	private Dictionary<string, BodyPartRecord> DicOfFilter2;

	private List<HediffDef> lOfHediffs;

	private Hediff example;

	private HediffComp_Disappears hDisappears;

	private HediffComp_GetsPermanent hPermanent;

	private string paramName;

	private string extraTipString;

	private SearchTool search;

	private Func<HediffDef, string> FHediffLabel = (HediffDef h) => h.LabelCap;

	private Func<HediffDef, string> FHediffTooltip = (HediffDef h) => h.modContentPack?.Name + "\n" + h.description;

	private Func<HediffDef, HediffDef, bool> FHediffComparator = (HediffDef h1, HediffDef h2) => h1 == h2;

	private int yCalc = 0;

	private float oldSeverity;

	public Pawn SelectedPawn
	{
		get
		{
			return selPawn;
		}
		set
		{
			selPawn = value;
			isPawnSelectionDone = true;
			CheckIsReady();
		}
	}

	public Pawn SelectedPawn2 { get; set; }

	public Pawn SelectedPawn3 { get; set; }

	public Pawn SelectedPawn4 { get; set; }

	public BodyPartRecord SelectedPart
	{
		get
		{
			return selPart;
		}
		set
		{
			selPart = value;
			isPartSelectionDone = true;
			CheckIsReady();
		}
	}

	public override Vector2 InitialSize => WindowTool.DefaultToolWindow;

	internal DialogAddHediff(Hediff toSelect = null)
	{
		scrollPos = default(Vector2);
		selectedHediff = null;
		oldSelectedHediff = null;
		isAdjustable = false;
		isPermanent = false;
		selectedSeverity = 0.01f;
		selectedDuration = -1;
		selectedLevel = -1;
		selectedPain = -1;
		paramName = "";
		selPawn = null;
		selPart = null;
		isPartSelectionDone = false;
		isPawnSelectionDone = false;
		bNeedsPart = true;
		bNeedsPawn = false;
		extraTipString = "";
		doOnce = true;
		search = SearchTool.Update(SearchTool.SIndex.HediffDef);
		lOfFilter1 = new List<string>();
		lOfFilter1.Add(Label.ALL);
		lOfFilter1.Add(Label.HB_ALLIMPLANTS);
		lOfFilter1.Add(Label.HB_ALLADDICTIONS);
		lOfFilter1.Add(Label.HB_ALLDISEASES);
		lOfFilter1.Add(Label.HB_ALLINJURIES);
		lOfFilter1.Add(Label.HB_ALLTIME);
		lOfFilter1.Add(Label.HB_WITHLEVEL);
		DicOfFilter2 = new Dictionary<string, BodyPartRecord>();
		DicOfFilter2.Add(Label.ALL, null);
		DicOfFilter2.Add(Label.HB_WholeBody, null);
		foreach (BodyPartRecord allPart in CEditor.API.Pawn.RaceProps.body.AllParts)
		{
			DicOfFilter2.AddLabeled(allPart.Label.CapitalizeFirst(), allPart);
		}
		UpdateHediffList();
		selectedHediff = toSelect?.def;
		CheckSelectionChanged(toSelect);
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		draggable = true;
		layer = CEditor.Layer;
	}

	public override void DoWindowContents(Rect inRect)
	{
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.HediffDef, ref windowRect, ref doOnce, 105);
		}
		int num = (int)InitialSize.x - 40;
		int num2 = (int)InitialSize.y - 115;
		int num3 = 0;
		int num4 = 0;
		DrawTitle(num4, num3, num, 30);
		num3 += 30;
		GetExtraTipString();
		Text.Font = GameFont.Small;
		DrawDropdown(num4, num3, num, 30);
		num3 += 92;
		SZWidgets.ListView(num4, num3, num, num2 - 110 - yCalc, lOfHediffs, FHediffLabel, FHediffTooltip, FHediffComparator, ref selectedHediff, ref scrollPos);
		num3 += num2 - 110 - yCalc;
		num3 += 10;
		CheckSelectionChanged();
		yCalc = 0;
		yCalc += DrawAdjustableSeverity(num4, ref num3, num, 45);
		yCalc += DrawAdjustableLevel(num4, ref num3, num, 45);
		yCalc += DrawAdjustablePain(num4, ref num3, num, 45);
		yCalc += DrawAdjustableTime(num4, ref num3, num, 45);
		yCalc += DrawPermanent(num4, ref num3, num, 25);
		Rect rect = new Rect(InitialSize.x - 140f, InitialSize.y - 70f, 100f, 30f);
		if (!inEditMode)
		{
			WindowTool.SimpleAcceptButton(this, CheckAndDo);
		}
		SZWidgets.ButtonImageCol(new Rect(num - 30, 0f, 30f, 30f), "bresurrect", AToggleOverride, HealthTool.bIsOverridden ? Color.green : Color.white, "toggle value overriding. white=inactive, green=active\nclick on severity value to input overrides");
	}

	private void CheckAndDo()
	{
		if (selectedHediff != null)
		{
			CheckIsReady();
			return;
		}
		SearchTool.Save(SearchTool.SIndex.HediffDef, windowRect.position);
		Close();
	}

	private void CheckIsReady()
	{
		if (selectedHediff == null)
		{
			return;
		}
		bool flag = true;
		bool flag2 = false;
		bool flag3 = false;
		string customText = ((selectedHediff == HediffDefOf.PregnantHuman) ? ("(" + "Father".Translate().ToString() + ")") : ((selectedHediff == HediffDefOf.Pregnant) ? "(Genitor)" : ""));
		bool animal = selectedHediff == HediffDefOf.Pregnant;
		if (bNeedsPart && !isPartSelectionDone)
		{
			flag = false;
			flag2 = true;
		}
		if (bNeedsPawn && !isPawnSelectionDone)
		{
			flag = false;
			flag3 = true;
		}
		if (flag)
		{
			if ((selectedHediff == HediffDefOf.PregnantHuman || selectedHediff == HediffDefOf.Pregnant) && CEditor.API.Pawn == SelectedPawn)
			{
				MessageTool.Show(Label.WRONGPAWN);
				flag = false;
				flag3 = true;
				isPawnSelectionDone = false;
				WindowTool.Open(new DialogChoosePawn(this, 1, Gender.None, customText, animal));
			}
			else
			{
				DoAndClose();
			}
		}
		else if (flag2)
		{
			List<BodyPartRecord> listOfAllowedBodyPartRecords = CEditor.API.Pawn.GetListOfAllowedBodyPartRecords(selectedHediff);
			if (listOfAllowedBodyPartRecords.CountAllowNull() <= 1)
			{
				SelectedPart = listOfAllowedBodyPartRecords.FirstOrDefault();
			}
			else
			{
				WindowTool.Open(new DialogChoosePart(this, selectedHediff));
			}
		}
		else if (flag3)
		{
			WindowTool.Open(new DialogChoosePawn(this, 1, Gender.None, customText, animal));
		}
	}

	private void DoAndClose()
	{
		if (selectedHediff != null)
		{
			CEditor.API.Pawn.AddHediff2(random: false, selectedHediff, selectedSeverity, SelectedPart, isPermanent, selectedLevel, (int)((selectedPain < 0) ? ((PainCategory)(-1)) : HealthTool.ConvertSliderToPainCategory(selectedPain)), selectedDuration, SelectedPawn);
			StatsReportUtility.Reset();
			CEditor.API.UpdateGraphics();
		}
		Close();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.HediffDef, windowRect.position);
		base.Close(doCloseSound);
	}

	public override void OnAcceptKeyPressed()
	{
		base.OnAcceptKeyPressed();
		DoAndClose();
	}

	private void GetExtraTipString()
	{
		try
		{
			extraTipString = ((example != null) ? example.TipStringExtra : "");
		}
		catch
		{
			extraTipString = "";
		}
	}

	private void CheckSelectionChanged(Hediff toSelect = null)
	{
		if (!FHediffComparator(oldSelectedHediff, selectedHediff))
		{
			oldSelectedHediff = selectedHediff;
			selPawn = null;
			isPartSelectionDone = ((search.filter2 != null && !(search.filter2 == Label.ALL)) ? true : false);
			selPart = (isPartSelectionDone ? DicOfFilter2.GetValue(search.filter2) : null);
			isPawnSelectionDone = false;
			hDisappears = null;
			hPermanent = null;
			isAdjustable = HealthTool.IsAdjustableSeverity(selectedHediff);
			List<BodyPartRecord> listOfAllowedBodyPartRecords = CEditor.API.Pawn.GetListOfAllowedBodyPartRecords(selectedHediff);
			bNeedsPart = listOfAllowedBodyPartRecords.CountAllowNull() > 0;
			bNeedsPawn = selectedHediff.IsHediffWithOtherPawn();
			inEditMode = toSelect != null;
			example = ((toSelect != null) ? toSelect : HediffMaker.MakeHediff(selectedHediff, CEditor.API.Pawn, listOfAllowedBodyPartRecords.FirstOrDefault()));
			selectedSeverity = example.Severity;
			selectedLevel = example.GetLevel();
			selectedPain = HealthTool.ConvertPainCategoryToSliderVal((PainCategory)example.GetPainValue());
			selectedDuration = example.GetDuration();
			hDisappears = example.TryGetComp<HediffComp_Disappears>();
			hPermanent = example.TryGetComp<HediffComp_GetsPermanent>();
			if (hDisappears != null)
			{
				hDisappears.Props.showRemainingTime = true;
			}
			if (hPermanent != null)
			{
				isPermanent = hPermanent.IsPermanent;
			}
			example.ShowDebugInfo();
		}
	}

	private void AToggleOverride()
	{
		HealthTool.bIsOverridden = !HealthTool.bIsOverridden;
	}

	private void DrawTitle(int x, int y, int w, int h)
	{
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(x, y, w - 40, h), inEditMode ? example.LabelCap : Label.ADD_HEDIFF);
	}

	private int DrawPermanent(int x, ref int y, int w, int h)
	{
		if (selectedHediff != null && (isAdjustable || HealthTool.bIsOverridden) && (selectedHediff.injuryProps != null || HealthTool.bIsOverridden))
		{
			SZWidgets.CheckBoxOnChange(new Rect(x, y + 10, w, 24f), "Permanent".Translate(), isPermanent, AChangePermanent);
			y += h;
			return h;
		}
		return 0;
	}

	private void AChangePermanent(bool perma)
	{
		isPermanent = perma;
		if (hPermanent != null)
		{
			hPermanent.IsPermanent = perma;
		}
	}

	private int DrawAdjustableLevel(int x, ref int y, int w, int h)
	{
		if (selectedHediff != null && example != null && selectedHediff.IsHediffWithLevel())
		{
			Text.Font = GameFont.Small;
			Listing_X listing_X = new Listing_X();
			listing_X.Begin(new Rect(x, y, w, h));
			int num = (int)selectedHediff.minSeverity;
			int num2 = (int)HealthTool.GetMaxSeverity(selectedHediff);
			num2 = ((num2 < num) ? 20 : num2);
			listing_X.AddIntSection("Level".Translate(), "", ref paramName, ref selectedLevel, num, num2, small: true);
			listing_X.End();
			example.SetLevel(selectedLevel);
			example.Severity = selectedLevel;
			selectedSeverity = selectedLevel;
			if (selectedHediff.IsHediffPsycastAbilities())
			{
			}
			y += h;
			return h;
		}
		return 0;
	}

	private int DrawAdjustablePain(int x, ref int y, int w, int h)
	{
		if (hPermanent != null && selectedHediff != null)
		{
			Text.Font = GameFont.Small;
			Listing_X listing_X = new Listing_X();
			listing_X.Begin(new Rect(x, y, w, h));
			listing_X.AddIntSection(" ", "pain", ref paramName, ref selectedPain, 0, 3, small: true);
			listing_X.End();
			hPermanent.SetPainCategory(HealthTool.ConvertSliderToPainCategory(selectedPain));
			y += h;
			return h;
		}
		return 0;
	}

	private int DrawAdjustableTime(int x, ref int y, int w, int h)
	{
		if (hDisappears != null && selectedHediff != null)
		{
			Text.Font = GameFont.Small;
			Listing_X listing_X = new Listing_X();
			listing_X.Begin(new Rect(x, y, w, h));
			string format = "dauer" + hDisappears.CompLabelInBracketsExtra;
			listing_X.AddIntSection(Label.DAUER, format, ref paramName, ref selectedDuration, 0, 220000, small: true, extraTipString);
			listing_X.End();
			hDisappears.ticksToDisappear = selectedDuration;
			y += h;
			return h;
		}
		return 0;
	}

	private int DrawAdjustableSeverity(int x, ref int y, int w, int h)
	{
		if (selectedHediff != null && (isAdjustable || HealthTool.bIsOverridden))
		{
			Text.Font = GameFont.Small;
			Listing_X listing_X = new Listing_X();
			listing_X.Begin(new Rect(x, y, w, h));
			float maxSeverity = HealthTool.GetMaxSeverity(selectedHediff);
			selectedSeverity = ((selectedSeverity > maxSeverity && !HealthTool.bIsOverridden) ? maxSeverity : selectedSeverity);
			oldSeverity = selectedSeverity;
			if (selectedHediff.lethalSeverity >= 0f && selectedSeverity >= selectedHediff.lethalSeverity)
			{
				GUI.color = Color.red;
			}
			else
			{
				GUI.color = Color.white;
			}
			example.Severity = selectedSeverity;
			string labelInBrackets = example.LabelInBrackets;
			listing_X.AddSection(format: (!labelInBrackets.NullOrEmpty()) ? ("comp" + labelInBrackets) : (selectedHediff.IsHediffWithLevel() ? "int" : (selectedHediff.IsAddiction ? "addict" : "")), paramName: "ConfigurableSeverity".Translate().ToString().SubstringTo(":"), selectedName: ref paramName, value: ref selectedSeverity, min: selectedHediff.minSeverity, max: maxSeverity, small: true, toolTip: extraTipString);
			listing_X.End();
			if (oldSeverity != selectedSeverity)
			{
				CEditor.API.UpdateGraphics();
			}
			y += h;
			return h;
		}
		return 0;
	}

	private void DrawDropdown(int x, int y, int w, int h)
	{
		Rect rect = new Rect(x, y, w, h);
		SZWidgets.FloatMenuOnButtonText(rect, search.SelectedModName, CEditor.API.Get<HashSet<string>>(EType.ModsHediffDef), (string s) => s ?? Label.ALL, ASelectedModName);
		Rect rect2 = new Rect(rect);
		rect2.y += 30f;
		SZWidgets.FloatMenuOnButtonText(rect2, search.SelectedFilter1, lOfFilter1, (string s) => s, ASelectFilter1);
		Rect rect3 = new Rect(rect2);
		rect3.y += 30f;
		SZWidgets.FloatMenuOnButtonText(rect3, search.SelectedFilter2, DicOfFilter2.Keys.ToList(), (string s) => s, ASelectBodyPartRecord);
	}

	private void ASelectedModName(string val)
	{
		search.modName = val;
		UpdateHediffList();
	}

	private void ASelectFilter1(string val)
	{
		search.filter1 = val;
		UpdateHediffList();
	}

	private void ASelectBodyPartRecord(string val)
	{
		search.filter2 = val;
		selPart = DicOfFilter2.GetValue(val);
		isPartSelectionDone = val != Label.ALL;
		UpdateHediffList();
	}

	private void UpdateHediffList()
	{
		lOfHediffs = HealthTool.ListOfHediffDef(search.modName, CEditor.API.Pawn, DicOfFilter2.GetValue(search.SelectedFilter2), search.SelectedFilter1, search.SelectedFilter2 == Label.HB_WholeBody);
	}
}
