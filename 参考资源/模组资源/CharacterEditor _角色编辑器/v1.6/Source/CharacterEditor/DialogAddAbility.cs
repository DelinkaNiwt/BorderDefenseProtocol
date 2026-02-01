using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogAddAbility : Window
{
	private List<AbilityDef> lOfAbilities;

	private List<string> lOfStufen;

	private Vector2 scrollPos;

	private AbilityDef selectedAbility;

	private AbilityDef oldSelectedAbility;

	private string selectedModName;

	private string selectedStufe;

	private string prepend;

	private string alllevels;

	private bool doOnce;

	private Func<AbilityDef, string> FAbilityLabel = (AbilityDef a) => (!a.LabelCap.NullOrEmpty()) ? a.LabelCap.ToString() : (a.label.NullOrEmpty() ? "" : a.label);

	private Func<AbilityDef, string> FAbilityTooltip = (AbilityDef a) => a.GetTooltip();

	private Func<AbilityDef, AbilityDef, bool> FAbilityComparator = (AbilityDef a1, AbilityDef a2) => a1.defName == a2.defName;

	public override Vector2 InitialSize => WindowTool.DefaultToolWindow;

	internal DialogAddAbility()
	{
		scrollPos = default(Vector2);
		doOnce = true;
		SearchTool.Update(SearchTool.SIndex.AbilityDef);
		selectedModName = null;
		selectedStufe = null;
		prepend = "Level".Translate() + " ";
		alllevels = Label.ALL;
		int num = 6;
		lOfStufen = new List<string>();
		lOfStufen.Add(alllevels);
		for (int num2 = 0; num2 <= num; num2++)
		{
			lOfStufen.Add(prepend + num2);
		}
		lOfAbilities = DefTool.ListByMod<AbilityDef>(null).ToList();
		selectedAbility = lOfAbilities.FirstOrDefault();
		oldSelectedAbility = selectedAbility;
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
			SearchTool.SetPosition(SearchTool.SIndex.AbilityDef, ref windowRect, ref doOnce, 105);
		}
		int num = (int)InitialSize.x - 40;
		int num2 = (int)InitialSize.y - 115;
		int num3 = 0;
		int num4 = 0;
		DrawTitle(num4, num3, num, 30);
		num3 += 30;
		SZWidgets.ButtonImage(num - 25, 0f, 25f, 25f, "brandom", ARandomAbility);
		DrawDropdown(num4, num3, num, 30);
		num3 += 30;
		DrawFilter(num4, num3, num, 30);
		num3 += 30;
		Text.Font = GameFont.Small;
		SZWidgets.ListView(num4, num3, num, num2 - 64, lOfAbilities, FAbilityLabel, FAbilityTooltip, FAbilityComparator, ref selectedAbility, ref scrollPos);
		if (!FAbilityComparator(oldSelectedAbility, selectedAbility))
		{
			oldSelectedAbility = selectedAbility;
			if (Prefs.DevMode)
			{
				MessageTool.Show(selectedAbility.defName);
			}
		}
		WindowTool.SimpleAcceptButton(this, DoAndClose);
	}

	private void DrawTitle(int x, int y, int w, int h)
	{
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(x, y, w, h), Label.PSYTALENTE);
	}

	private void DrawDropdown(int x, int y, int w, int h)
	{
		Text.Font = GameFont.Small;
		Rect rect = new Rect(x, y, w, h);
		SZWidgets.FloatMenuOnButtonText(rect, selectedModName ?? Label.ALL, CEditor.API.Get<HashSet<string>>(EType.ModsAbilityDef), (string s) => s ?? Label.ALL, ASelectedModName);
	}

	private void DrawFilter(int x, int y, int w, int h)
	{
		Text.Font = GameFont.Small;
		Rect rect = new Rect(x, y, w, h);
		SZWidgets.FloatMenuOnButtonText(rect, selectedStufe ?? Label.ALL, lOfStufen, (string s) => s ?? Label.ALL, ASelectedStufe);
	}

	private void ASelectedModName(string val)
	{
		selectedModName = val;
		lOfAbilities = DefTool.ListByMod<AbilityDef>(selectedModName).ToList();
	}

	private void ASelectedStufe(string val)
	{
		int level = val.Replace(alllevels, "").Replace(prepend, "").AsInt32();
		selectedStufe = val;
		bool bAll = val == null || val == Label.ALL;
		lOfAbilities = (from td in DefTool.ListByMod<AbilityDef>(selectedModName).ToList()
			where bAll || td.level == level
			orderby td.label
			select td).ToList();
	}

	private void DoAndClose()
	{
		if (selectedAbility != null)
		{
			CEditor.API.Pawn.CheckAddPsylink();
			CEditor.API.Pawn.abilities.GainAbility(selectedAbility);
		}
		Close();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.AbilityDef, windowRect.position);
		base.Close(doCloseSound);
	}

	public override void OnAcceptKeyPressed()
	{
		base.OnAcceptKeyPressed();
		DoAndClose();
	}

	private void ARandomAbility()
	{
		selectedAbility = lOfAbilities.RandomElement();
		SZWidgets.sFind = FAbilityLabel(selectedAbility);
	}
}
