using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CharacterEditor;

internal class DialogChangeRace : Window
{
	private bool doOnce;

	private HashSet<ThingDef> lraces;

	private HashSet<PawnKindDef> lpkd;

	private Pawn pawn;

	private PawnKindDef selectedPKD;

	private ThingDef raceDef;

	private SearchTool search;

	private string key;

	private Vector2 scrollPos;

	private bool humanlike;

	public override Vector2 InitialSize => WindowTool.DefaultToolWindow;

	internal DialogChangeRace(Pawn _pawn)
	{
		pawn = _pawn;
		humanlike = pawn.RaceProps.Humanlike;
		scrollPos = default(Vector2);
		search = SearchTool.Update(SearchTool.SIndex.Race);
		doOnce = true;
		selectedPKD = null;
		raceDef = pawn.def;
		search.ofilter1 = search.ofilter1 == null || (bool)search.ofilter1;
		selectedPKD = pawn?.kindDef;
		key = ((pawn == null || pawn.RaceProps.Humanlike) ? Label.COLONISTS : Label.WILDANIMALS);
		UpdateRacesList();
		UpdatePKDList();
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		draggable = true;
		layer = CEditor.Layer;
	}

	private void UpdateRacesList()
	{
		bool nonhumanlike = CEditor.ListName == Label.COLONYANIMALS || CEditor.ListName == Label.WILDANIMALS;
		bool flag = CEditor.ListName == Label.HUMANOID || CEditor.ListName == Label.COLONISTS;
		lraces = PawnKindTool.ListOfRaces(flag, nonhumanlike);
	}

	private void UpdatePKDList()
	{
		lpkd = PawnKindTool.ListOfPawnKindDefByRace(raceDef, humanlike, !humanlike);
	}

	public override void DoWindowContents(Rect inRect)
	{
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.Race, ref windowRect, ref doOnce, 105);
		}
		int h = (int)InitialSize.y - 115;
		int num = (int)InitialSize.x - 40;
		int x = 0;
		int y = 0;
		y = DrawTitle(x, y, num, 30);
		y = DrawDropdown(x, y, num);
		y = DrawList(x, y, num, h);
		SZWidgets.CheckBoxOnChange(new Rect(0f, InitialSize.y - 105f, num, 25f), Label.RACESPECIFICDRESS, (bool)search.ofilter1, ARedress);
		WindowTool.SimpleAcceptButton(this, DoAndClose);
	}

	private void DoAndClose()
	{
		RaceTool.ChangeRace(pawn, pawn.RaceProps.Humanlike ? selectedPKD : raceDef.race.AnyPawnKind, (bool)search.ofilter1);
		Close();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.Race, windowRect.position);
		base.Close(doCloseSound);
	}

	private void ARedress(bool val)
	{
		search.ofilter1 = val;
	}

	private int DrawTitle(int x, int y, int w, int h)
	{
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(x, y, w - 40, h), Label.CHANGERACE);
		return h;
	}

	private int DrawDropdown(int x, int y, int w)
	{
		Text.Font = GameFont.Small;
		Rect rect = new Rect(x, y, w, 30f);
		SZWidgets.FloatMenuOnButtonText(rect, CEditor.RACENAME(raceDef), lraces, CEditor.RACENAME, AChangeRace);
		return y + 30;
	}

	private int DrawList(int x, int y, int w, int h)
	{
		SZWidgets.ListView(new Rect(x, y, w, h - y), lpkd, CEditor.PKDNAME, CEditor.PKDTOOLTIP, (PawnKindDef pkd1, PawnKindDef pkd2) => pkd1 == pkd2, ref selectedPKD, ref scrollPos);
		return h - y;
	}

	private void AChangeRace(ThingDef race)
	{
		raceDef = race;
		UpdatePKDList();
	}
}
