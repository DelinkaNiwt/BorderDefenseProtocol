using System;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogFindPawn : Window
{
	private Vector2 scrollPos;

	private Pawn selectedPawn;

	private bool doOnce;

	private Func<Pawn, string> FGetInfo;

	public override Vector2 InitialSize => WindowTool.DefaultToolWindow;

	internal DialogFindPawn()
	{
		scrollPos = default(Vector2);
		selectedPawn = null;
		doOnce = true;
		SearchTool.Update(SearchTool.SIndex.FindPawn);
		FGetInfo = GetInfo;
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		draggable = true;
		layer = CEditor.Layer;
	}

	private string GetInfo(Pawn p)
	{
		string text = p.GetPawnName() + " (" + p.KindLabel + "," + p.GetGenderLabel();
		if (p.Faction != null)
		{
			text = text + "," + p.Faction.Name;
		}
		return text + ")";
	}

	public override void DoWindowContents(Rect inRect)
	{
		float num = InitialSize.x - 40f;
		float h = InitialSize.y - 115f;
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.FindPawn, ref windowRect, ref doOnce, 0);
		}
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(0f, 0f, num, 30f), Label.FIND_PAWN);
		Text.Font = GameFont.Small;
		SZWidgets.ListView(0f, 30f, num, h, CEditor.API.ListOf<Pawn>(EType.Pawns), FGetInfo, (Pawn p) => p.MainDesc(writeFaction: true), (Pawn pA, Pawn pB) => pA == pB, ref selectedPawn, ref scrollPos, withRemove: false, ASelectPawn);
		WindowTool.SimpleAcceptButton(this, DoAndClose);
	}

	private void ASelectPawn(Pawn p)
	{
		selectedPawn = p;
		CEditor.API.Pawn = p;
	}

	private void DoAndClose()
	{
		if (selectedPawn != null)
		{
			Current.Game.CurrentMap = selectedPawn.Map;
			CameraJumper.TryJumpAndSelect(selectedPawn);
		}
		Close();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.FindPawn, windowRect.position);
		base.Close(doCloseSound);
	}

	public override void OnAcceptKeyPressed()
	{
		base.OnAcceptKeyPressed();
		DoAndClose();
	}
}
