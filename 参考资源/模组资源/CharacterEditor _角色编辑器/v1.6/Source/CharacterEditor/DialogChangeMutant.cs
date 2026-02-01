using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogChangeMutant : Window
{
	private Pawn pawn;

	private Vector2 scrollPos;

	private Faction selectedFaction;

	private int iChangeTick;

	private bool doOnce;

	private MutantDef selectedMutantDef;

	private RotStage selectedRotStage;

	internal Listing_X view;

	internal int x;

	internal int y;

	internal int frameW;

	internal int frameH;

	internal int xPosOffset;

	internal int hScrollParam;

	internal Vector2 scrollPosParam;

	private int mHscroll;

	private HashSet<RotStage> rotStages = new HashSet<RotStage>();

	public override Vector2 InitialSize => new Vector2(500f, WindowTool.MaxH);

	internal int WPARAM => 410;

	internal DialogChangeMutant()
	{
		xPosOffset = 0;
		view = new Listing_X();
		scrollPosParam = default(Vector2);
		x = 0;
		y = 0;
		mHscroll = 0;
		pawn = CEditor.API.Pawn;
		scrollPos = default(Vector2);
		iChangeTick = 0;
		doOnce = true;
		SearchTool.Update(SearchTool.SIndex.ChangeMutant);
		selectedMutantDef = (pawn.HasMutantTracker() ? pawn.mutant.Def : null);
		foreach (RotStage value in Enum.GetValues(typeof(RotStage)))
		{
			rotStages.Add(value);
		}
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		draggable = true;
		layer = CEditor.Layer;
	}

	public override void DoWindowContents(Rect inRect)
	{
		SizeAndPosition();
		DrawEditLabel(0f, 0f, 340f, 30f);
		DrawViewList();
		DrawLowerButtons();
	}

	private void DrawLowerButtons()
	{
		WindowTool.SimpleCustomButton(this, 0, ATurnHuman, "Turn to human", "", 120);
		WindowTool.SimpleCustomButton(this, 120, ATurnMutant, "Turn to mutant", "", 120);
		WindowTool.SimpleAcceptButton(this, DoAndClose);
	}

	private void SizeAndPosition()
	{
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.ChangeMutant, ref windowRect, ref doOnce, 370);
		}
		frameW = (int)InitialSize.x - 40;
		frameH = (int)InitialSize.y - 115;
		y = 0;
		x = 0;
	}

	internal void CalcHSCROLL()
	{
		hScrollParam = 4000;
		if (mHscroll > 800)
		{
			hScrollParam = mHscroll;
		}
	}

	private void DrawViewList()
	{
		CalcHSCROLL();
		Rect outRect = new Rect(0f, 40f, WPARAM, frameH - 40);
		Rect rect = new Rect(0f, 0f, outRect.width - 16f, hScrollParam - 30);
		Widgets.BeginScrollView(outRect, ref scrollPosParam, rect);
		Rect rect2 = rect.ContractedBy(4f);
		rect2.y -= 4f;
		rect2.height = hScrollParam;
		view.Begin(rect2);
		view.verticalSpacing = 30f;
		DrawParameters(400);
		view.End();
		Widgets.EndScrollView();
	}

	private void DrawParameters(int w)
	{
		view.CurY += 5f;
		SZWidgets.DefSelectorSimple(view.GetRect(22f), w, PawnxTool.AllMutantDefs, ref selectedMutantDef, "MutantDef: ", FLabel.DefLabel, delegate(MutantDef d)
		{
			OnChangeDef(d);
		});
		view.Gap(2f);
		SZWidgets.NonDefSelectorSimple(view.GetRect(22f), w, rotStages, ref selectedRotStage, "RotStage: ", (RotStage r) => Enum.GetName(typeof(RotStage), r), delegate(RotStage r)
		{
			OnChangeRotStage(r);
		});
		view.Gap(2f);
		mHscroll = (int)view.CurY + 50;
	}

	private void OnChangeRotStage(RotStage r)
	{
		if (pawn.HasMutantTracker())
		{
			pawn.mutant.rotStage = r;
		}
		selectedRotStage = r;
	}

	private void OnChangeDef(MutantDef d)
	{
		if (pawn.HasMutantTracker() && pawn.mutant.HasTurned)
		{
			pawn.mutant.Revert();
		}
		selectedMutantDef = d;
		if (d != null)
		{
			pawn.mutant = new Pawn_MutantTracker(pawn, selectedMutantDef, RotStage.Fresh);
			pawn.mutant.Turn();
		}
		CEditor.API.UpdateGraphics();
	}

	private void ATurnHuman()
	{
		if (pawn.HasMutantTracker())
		{
			pawn.mutant.Revert();
		}
		CEditor.API.UpdateGraphics();
	}

	private void ATurnMutant()
	{
		OnChangeDef(selectedMutantDef);
	}

	private void DoAndClose()
	{
		Close();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.ChangeMutant, windowRect.position);
		base.Close(doCloseSound);
	}

	internal string Mutantlabel()
	{
		return (pawn.mutant == null || pawn.mutant.Def == null) ? ("Mutations: " + Label.NONE) : ("Mutations: " + pawn.mutant.Def.LabelCap.RawText);
	}

	private void DrawEditLabel(float x, float y, float w, float h)
	{
		Text.Font = GameFont.Medium;
		GUI.color = Color.red;
		Rect rect = new Rect(x, y, w, h);
		SZWidgets.Label(rect, Mutantlabel(), delegate
		{
			iChangeTick = 400;
		});
		GUI.color = Color.white;
	}
}
