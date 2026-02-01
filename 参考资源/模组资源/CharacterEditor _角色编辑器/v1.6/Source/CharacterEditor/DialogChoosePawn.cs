using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogChoosePawn : Window
{
	private Vector2 scrollPos;

	private int id;

	private List<Pawn> lOfPawns;

	private Pawn selectedPawn;

	private string selectedListname;

	private IPawnable callback;

	private Gender choosenGender;

	private string customText;

	private bool doOnce;

	private Dictionary<string, Faction> DicFactions => CEditor.API.DicFactions;

	public override Vector2 InitialSize => WindowTool.DefaultToolWindow;

	private Func<Pawn, string> FPawnLabel => (Pawn p) => p.GetPawnName(needFull: true);

	private Func<Pawn, string> FPawnTooltip => (Pawn p) => p.GetPawnDescription();

	private Func<Pawn, Pawn, bool> FPawnComparator => (Pawn p1, Pawn p2) => p1 == p2;

	internal DialogChoosePawn(IPawnable _callback, int _id = 1, Gender gender = Gender.None, string _customText = "", bool animal = false)
	{
		scrollPos = default(Vector2);
		id = _id;
		doOnce = true;
		SearchTool.Update(SearchTool.SIndex.OtherPawn);
		customText = _customText;
		choosenGender = gender;
		lOfPawns = new List<Pawn>();
		DicFactions.Clear();
		DicFactions.Merge(FactionTool.GetDicOfFactions());
		callback = _callback;
		selectedPawn = null;
		if (!animal)
		{
			AFactionSelected(DicFactions.First().Key);
		}
		else
		{
			foreach (string key in DicFactions.Keys)
			{
				Faction f = DicFactions[key];
				if (!f.IsHumanoid(key))
				{
					AFactionSelected(key);
					break;
				}
			}
		}
		doCloseX = true;
		absorbInputAroundWindow = true;
		draggable = true;
		layer = CEditor.Layer;
	}

	public override void DoWindowContents(Rect inRect)
	{
		int w = (int)InitialSize.x - 40;
		int y = 0;
		int x = 0;
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.OtherPawn, ref windowRect, ref doOnce, 0);
		}
		DrawTitle(x, ref y, w, 30);
		DrawDropdowns(x, ref y, w, 30);
		DrawPawnList(x, ref y, w, (int)InitialSize.y - y - 74);
		WindowTool.SimpleAcceptButton(this, DoAndClose);
	}

	private void DrawTitle(int x, ref int y, int w, int h)
	{
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(x, y, w, h), Label.CHOOSE_PAWN + " " + customText);
		if (choosenGender != Gender.None)
		{
			SZWidgets.Image(new Rect(x + w - 20, y + 8, 20f, 20f), (choosenGender == Gender.Male) ? "bmale" : "bfemale");
		}
		y += 30;
	}

	internal void DrawDropdowns(int x, ref int y, int w, int h)
	{
		Text.Font = GameFont.Small;
		SZWidgets.FloatMenuOnButtonText(new Rect(x, y, w, h), selectedListname, DicFactions.Keys.ToList(), (string s) => s, AFactionSelected);
		y += 32;
	}

	private void AFactionSelected(string listname)
	{
		selectedListname = listname;
		lOfPawns.Clear();
		List<Pawn> pawnList = PawnxTool.GetPawnList(selectedListname, onMap: false, DicFactions.GetValue(selectedListname));
		if (choosenGender != Gender.None)
		{
			foreach (Pawn item in pawnList)
			{
				if (item.gender == choosenGender)
				{
					lOfPawns.Add(item);
				}
			}
		}
		else
		{
			lOfPawns.AddRange(pawnList);
		}
		selectedPawn = lOfPawns.FirstOrFallback();
	}

	private void DrawPawnList(int x, ref int y, int w, int h)
	{
		SZWidgets.ListView(x, y, w, h, lOfPawns, FPawnLabel, FPawnTooltip, FPawnComparator, ref selectedPawn, ref scrollPos);
	}

	private void DoAndClose()
	{
		if (id <= 1)
		{
			callback.SelectedPawn = selectedPawn;
		}
		else if (id == 2)
		{
			callback.SelectedPawn2 = selectedPawn;
		}
		else if (id == 3)
		{
			callback.SelectedPawn3 = selectedPawn;
		}
		else if (id == 4)
		{
			callback.SelectedPawn4 = selectedPawn;
		}
		Close();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.OtherPawn, windowRect.position);
		base.Close(doCloseSound);
	}
}
