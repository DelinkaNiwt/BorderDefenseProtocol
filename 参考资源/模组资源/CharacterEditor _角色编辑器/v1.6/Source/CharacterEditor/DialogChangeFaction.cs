using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogChangeFaction : Window
{
	private List<Faction> lOfFactions;

	private Pawn pawn;

	private Vector2 scrollPos;

	private Faction selectedFaction;

	private int iChangeTick;

	private bool doOnce;

	public override Vector2 InitialSize => WindowTool.DefaultToolWindow;

	internal DialogChangeFaction()
	{
		pawn = CEditor.API.Pawn;
		lOfFactions = Find.World.factionManager.AllFactions.OrderByDescending((Faction td) => td.def.defName).ToList();
		lOfFactions.Insert(0, null);
		selectedFaction = pawn.Faction;
		scrollPos = default(Vector2);
		iChangeTick = 0;
		doOnce = true;
		SearchTool.Update(SearchTool.SIndex.ChangeFaction);
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		draggable = true;
		layer = CEditor.Layer;
	}

	internal string Factionlabel(Faction f)
	{
		return (f == null) ? Label.NONE : (string.IsNullOrEmpty(f.GetCallLabel()) ? f.def.label : f.GetCallLabel());
	}

	internal string Factiondescr(Faction f)
	{
		return (f == null) ? "" : (f.IsPlayer ? f.def.description : f.GetInfoText());
	}

	public override void DoWindowContents(Rect inRect)
	{
		float width = InitialSize.x - 50f;
		float height = InitialSize.y - 220f;
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.ChangeFaction, ref windowRect, ref doOnce, 105);
		}
		DrawEditLabel(0f, 0f, 340f, 30f);
		Rect butRect = new Rect(10f, 30f, 100f, 100f);
		if (selectedFaction != null)
		{
			Widgets.ButtonImage(butRect, selectedFaction.def.FactionIcon, selectedFaction.Color);
		}
		Text.Font = GameFont.Small;
		Rect outRect = new Rect(0f, 130f, width, height);
		Rect rect = new Rect(0f, 30f, outRect.width - 16f, (float)lOfFactions.Count * 35f);
		Widgets.BeginScrollView(outRect, ref scrollPos, rect);
		Rect rect2 = rect.ContractedBy(4f);
		rect2.height = (float)lOfFactions.Count * 35f;
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(rect2);
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		foreach (Faction lOfFaction in lOfFactions)
		{
			string label = Factionlabel(lOfFaction);
			string tooltip = Factiondescr(lOfFaction);
			if (listing_Standard.RadioButton(label, lOfFaction == selectedFaction, 0f, tooltip))
			{
				selectedFaction = lOfFaction;
			}
			listing_Standard.Gap(10f);
		}
		listing_Standard.End();
		Widgets.EndScrollView();
		WindowTool.SimpleAcceptButton(this, DoAndClose);
	}

	private void DoAndClose()
	{
		if (pawn.Faction != selectedFaction)
		{
			if (pawn.Faction == Faction.OfPlayer)
			{
				PawnBanishUtility.Banish(pawn);
			}
			pawn.SetFaction(selectedFaction);
		}
		Close();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.ChangeFaction, windowRect.position);
		base.Close(doCloseSound);
	}

	private void DrawEditLabel(float x, float y, float w, float h)
	{
		Text.Font = GameFont.Medium;
		Rect rect = new Rect(x, y, w, h);
		if (iChangeTick <= 0)
		{
			SZWidgets.Label(rect, Factionlabel(CEditor.API.Pawn.Faction), delegate
			{
				iChangeTick = 400;
			});
			return;
		}
		string text = Factionlabel(CEditor.API.Pawn.Faction);
		string text2 = Widgets.TextField(rect, text);
		if (!text.Equals(text2))
		{
			CEditor.API.Pawn.Faction.Name = text2;
			iChangeTick = 400;
		}
		iChangeTick--;
	}
}
