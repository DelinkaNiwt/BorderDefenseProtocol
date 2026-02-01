using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogAddTrait : Window
{
	private List<KeyValuePair<TraitDef, TraitDegreeData>> lOfTraits;

	private Vector2 scrollPos;

	private KeyValuePair<TraitDef, TraitDegreeData> selectedTrait;

	private KeyValuePair<TraitDef, TraitDegreeData> oldSelectedTrait;

	private bool doOnce;

	private HashSet<StatModifier> lOfSM;

	private List<string> lOfFilters;

	private SearchTool search;

	private Trait oldTrait;

	private Func<StatModifier, string> FStatLabel = (StatModifier t) => (t == null) ? Label.ALL : t.stat.LabelCap.ToString();

	public override Vector2 InitialSize => WindowTool.DefaultToolWindow;

	internal DialogAddTrait(Trait _trait = null)
	{
		oldTrait = _trait;
		scrollPos = default(Vector2);
		doOnce = true;
		search = SearchTool.Update(SearchTool.SIndex.TraitDef);
		lOfSM = TraitTool.ListOfTraitStatModifier(search.modName, withNull: true);
		lOfFilters = new List<string>();
		lOfFilters.Add(null);
		lOfFilters.Add(Label.STAT);
		lOfFilters.Add(Label.MENTAL);
		lOfFilters.Add(Label.THOUGHTS);
		lOfFilters.Add(Label.INSPIRATIONS);
		lOfFilters.Add(Label.FOCUS);
		lOfFilters.Add(Label.SKILLGAINS);
		lOfFilters.Add(Label.ABILITIES);
		lOfFilters.Add(Label.NEEDS);
		lOfFilters.Add(Label.INGESTIBLEMOD);
		lOfTraits = TraitTool.ListOfTraitsKeyValuePair(search.modName, (StatModifier)search.ofilter1, search.filter1);
		TraitTool.UpdateDicTooltip(lOfTraits);
		selectedTrait = lOfTraits.FirstOrDefault();
		oldSelectedTrait = selectedTrait;
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
			SearchTool.SetPosition(SearchTool.SIndex.TraitDef, ref windowRect, ref doOnce, 105);
		}
		int h = (int)InitialSize.y - 115;
		int num = (int)InitialSize.x - 40;
		int x = 0;
		int y = 0;
		SZWidgets.ButtonImage(num - 25, 0f, 25f, 25f, "brandom", ARandomTrait);
		y = DrawTitle(x, y, num, 30);
		y = DrawDropdown(x, y, num);
		y = DrawList(x, y, num, h);
		WindowTool.SimpleAcceptButton(this, DoAndClose);
	}

	private int DrawTitle(int x, int y, int w, int h)
	{
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(x, y, w, h), Label.ADD_TRAIT);
		return h;
	}

	private int DrawDropdown(int x, int y, int w)
	{
		Text.Font = GameFont.Small;
		Rect rect = new Rect(x, y, w, 30f);
		SZWidgets.FloatMenuOnButtonText(rect, search.SelectedModName, CEditor.API.Get<HashSet<string>>(EType.ModsTraitDef), (string s) => s ?? Label.ALL, AChangedModName);
		Rect rect2 = new Rect(rect);
		rect2.y = rect.y + 30f;
		SZWidgets.FloatMenuOnButtonText(rect2, FStatLabel((StatModifier)search.ofilter1), lOfSM, FStatLabel, AChangedSM);
		Rect rect3 = new Rect(rect2);
		rect3.y = rect2.y + 30f;
		SZWidgets.FloatMenuOnButtonText(rect3, search.SelectedFilter1, lOfFilters, (string s) => (s == null) ? Label.ALL : s, AChangedCategory);
		return y + 90;
	}

	private int DrawList(int x, int y, int w, int h)
	{
		Text.Font = GameFont.Small;
		SZWidgets.ListView(x, y, w, h - y + 30, lOfTraits, TraitTool.FTraitLabel, TraitTool.FTraitTooltip, TraitTool.FTraitComparator, ref selectedTrait, ref scrollPos);
		if (!TraitTool.FTraitComparator(oldSelectedTrait, selectedTrait))
		{
			oldSelectedTrait = selectedTrait;
			if (Prefs.DevMode)
			{
				MessageTool.Show(selectedTrait.Key.defName);
			}
		}
		return h - y;
	}

	private void AChangedModName(string val)
	{
		search.modName = val;
		lOfTraits = TraitTool.ListOfTraitsKeyValuePair(search.modName, (StatModifier)search.ofilter1, search.filter1);
		TraitTool.UpdateDicTooltip(lOfTraits);
	}

	private void AChangedSM(StatModifier val)
	{
		search.ofilter1 = val;
		lOfTraits = TraitTool.ListOfTraitsKeyValuePair(search.modName, (StatModifier)search.ofilter1, search.filter1);
		TraitTool.UpdateDicTooltip(lOfTraits);
	}

	private void AChangedCategory(string val)
	{
		search.filter1 = val;
		lOfTraits = TraitTool.ListOfTraitsKeyValuePair(search.modName, (StatModifier)search.ofilter1, search.filter1);
		TraitTool.UpdateDicTooltip(lOfTraits);
	}

	private void ARandomTrait()
	{
		selectedTrait = lOfTraits.RandomElement();
		SZWidgets.sFind = TraitTool.FTraitLabel(selectedTrait);
	}

	private void DoAndClose()
	{
		if (selectedTrait.Key != null)
		{
			CEditor.API.Pawn.AddTrait(selectedTrait.Key, selectedTrait.Value, random: false, doChangeSkillValue: true, oldTrait);
		}
		Close();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.TraitDef, windowRect.position);
		base.Close(doCloseSound);
	}

	public override void OnAcceptKeyPressed()
	{
		base.OnAcceptKeyPressed();
		DoAndClose();
	}
}
