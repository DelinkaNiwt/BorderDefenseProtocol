using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CharacterEditor;

internal class DialogCapsuleUI : Window
{
	private bool addToOwn;

	private bool addToTaken;

	private bool bInterstellarView = false;

	private bool bShowRemove;

	private bool bShowShift;

	private bool doOnce;

	private bool right = false;

	private bool up = false;

	private const bool showBeta = false;

	private Dictionary<int, string> dicSlots;

	private float moveX = 0f;

	private float moveY = 0f;

	private float WTITLE = 300f;

	private int astronautPic;

	private int check = -50;

	private int iTick = 0;

	internal static List<Pawn> interstellarAnimals = new List<Pawn>();

	internal static List<Pawn> interstellarPawns = new List<Pawn>();

	private ScenPart selectedPart_Animal;

	private ScenPart selectedPart_Interstellar;

	private ScenPart selectedPart_Map;

	private ScenPart selectedPart_Scatter;

	private ScenPart selectedPart_Taken;

	private static List<ScenPart> lParts;

	private static List<ScenPart> lPartsAnimal;

	private static List<ScenPart> lPartsInterstellar;

	private static List<ScenPart> lPartsMap;

	private static List<ScenPart> lPartsScatter;

	private static List<ScenPart> lPartsTaken;

	private Vector2 scrollPos_Animal;

	private Vector2 scrollPos_Interstellar;

	private Vector2 scrollPos_Map;

	private Vector2 scrollPos_Scatter;

	private Vector2 scrollPos_Taken;

	private System.Random random = new System.Random();

	public override Vector2 InitialSize => new Vector2(1120f, WindowTool.MaxH);

	private Rect rectLoad => new Rect(0f, (int)InitialSize.y - 70, 120f, 30f);

	private Rect rectSave => new Rect(120f, (int)InitialSize.y - 70, 120f, 30f);

	private Rect rectGrab => new Rect(240f, (int)InitialSize.y - 70, 120f, 30f);

	private Rect rectEntladen => new Rect(360f, (int)InitialSize.y - 70, 120f, 30f);

	private Rect rectLiftOff => new Rect(480f, (int)InitialSize.y - 70, 120f, 30f);

	private Rect rectLiftOff2 => new Rect(600f, (int)InitialSize.y - 70, 120f, 30f);

	internal DialogCapsuleUI()
	{
		scrollPos_Animal = default(Vector2);
		scrollPos_Map = default(Vector2);
		scrollPos_Interstellar = default(Vector2);
		scrollPos_Taken = default(Vector2);
		scrollPos_Scatter = default(Vector2);
		selectedPart_Animal = null;
		selectedPart_Map = null;
		selectedPart_Interstellar = null;
		selectedPart_Taken = null;
		selectedPart_Scatter = null;
		lPartsMap = new List<ScenPart>();
		if (lPartsInterstellar == null)
		{
			lPartsInterstellar = new List<ScenPart>();
		}
		bShowRemove = false;
		bInterstellarView = !CEditor.InStartingScreen;
		doOnce = true;
		astronautPic = 1;
		SearchTool.Update(SearchTool.SIndex.Capsule);
		UpdateLists();
		InitSlots();
		if (!CEditor.InStartingScreen)
		{
			ScanMap();
		}
		doCloseX = true;
		absorbInputAroundWindow = true;
		closeOnCancel = true;
		closeOnClickedOutside = true;
		draggable = true;
		layer = CEditor.Layer;
	}

	internal static string SLOTPATH(int index)
	{
		return pathCapsuleSets() + Path.DirectorySeparatorChar + "capsulesetup" + index + ".txt";
	}

	internal static string pathCapsuleSets()
	{
		string text = GenFilePaths.ConfigFolderPath.Replace("Config", "");
		text = text.Remove(text.Length - 1);
		text = text + Path.DirectorySeparatorChar + "CharacterEditor";
		FileIO.CheckOrCreateDir(text);
		return text;
	}

	private void InitSlots()
	{
		dicSlots = new Dictionary<int, string>();
		int numCapsuleSlots = CEditor.API.NumCapsuleSlots;
		for (int i = 0; i < numCapsuleSlots; i++)
		{
			dicSlots.Add(i, "");
		}
		UpdateSlots();
	}

	private void UpdateSlots()
	{
		int numCapsuleSlots = CEditor.API.NumCapsuleSlots;
		for (int i = 0; i < numCapsuleSlots; i++)
		{
			if (!FileIO.Exists(SLOTPATH(i)))
			{
				continue;
			}
			string text = FileIO.ReadFile(SLOTPATH(i)).AsString(Encoding.UTF8);
			if (!text.NullOrEmpty())
			{
				string[] array = text.SplitNo("\n");
				if (array.Length > 1)
				{
					dicSlots[i] = array[0];
				}
			}
		}
	}

	public override void DoWindowContents(Rect inRect)
	{
		if (doOnce)
		{
			SearchTool.SetPosition(SearchTool.SIndex.Capsule, ref windowRect, ref doOnce, 105);
		}
		GUI.DrawTextureWithTexCoords(new Rect(moveX, moveY, 1332f, 850f), (Texture)ContentFinder<Texture2D>.Get("bcapsule_back"), new Rect(0f, 0f, 1f, 1f));
		DrawUIObjects();
		if (!bInterstellarView)
		{
			DrawTakenContainer(13, 428, 392, 256);
		}
		else
		{
			DrawInterstellarContainer(13, 428, 392, 256);
		}
		if (!bInterstellarView)
		{
			DrawScatterContainer(673, 428, 392, 256);
		}
		else
		{
			DrawMapContainer(673, 428, 392, 256);
		}
		if (!bInterstellarView)
		{
			DrawAnimalContainer(805, 148, 240, 190);
		}
		DrawButtons(0, (int)InitialSize.y - 70, (int)InitialSize.x - 32, 30);
		WindowTool.SimpleCloseButton(this);
		ShakingImage();
	}

	public override void Close(bool doCloseSound = true)
	{
		SearchTool.Save(SearchTool.SIndex.Capsule, windowRect.position);
		foreach (ScenPart scenarioPart in ScenarioTool.ScenarioParts)
		{
			scenarioPart.ExposeData();
		}
		base.Close(doCloseSound);
	}

	private void ShakingImage()
	{
		if (astronautPic != 2)
		{
			if (right)
			{
				moveX += 0.15f;
			}
			else
			{
				moveX -= 0.15f;
			}
			if (moveX <= -200f)
			{
				right = true;
			}
			else if (moveX >= 0f)
			{
				right = false;
			}
			int num = random.Next((int)moveY, 2);
			up = num > 0 || num > check;
			if (up && moveY >= -100f)
			{
				moveY -= 0.15f;
			}
			else if (!up && moveY < 0f)
			{
				moveY += 0.15f;
			}
			iTick++;
			if (iTick > 1000)
			{
				iTick = 0;
				check = random.Next(-100, 30);
			}
		}
	}

	private void DrawUIObjects()
	{
		Text.Font = GameFont.Medium;
		if (CEditor.InStartingScreen)
		{
			Widgets.Label(new Rect(0f, 0f, WTITLE, 30f), Label.RETTUNGSKAPSEL);
		}
		else
		{
			Widgets.DrawHighlight(new Rect(0f, 0f, WTITLE, 30f));
			SZWidgets.ButtonTextureTextHighlight(new Rect(0f, 0f, WTITLE, 30f), bInterstellarView ? Label.INTERSTELLARLINK : Label.RETTUNGSKAPSEL, null, ColorTool.colAsche, delegate
			{
				bInterstellarView = !bInterstellarView;
			}, Label.INTERSTELLARLINK_DESC);
		}
		Text.Font = GameFont.Small;
		Rect rect = new Rect(480f, 50f, 120f, 30f);
		Widgets.DrawBoxSolid(rect, new Color(0.8f, 0.2f, 0.2f, 0.5f));
		Text.Anchor = TextAnchor.MiddleCenter;
		Widgets.Label(rect, CEditor.InStartingScreen ? Label.START : "Scan Map");
		if (CEditor.InStartingScreen)
		{
			SZWidgets.ButtonInvisible(rect, AStart);
		}
		else
		{
			SZWidgets.ButtonInvisible(rect, ScanMap);
		}
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		Text.Anchor = TextAnchor.UpperLeft;
		Rect rect2 = new Rect(15f, 50f, 160f, 160f);
		if (Mouse.IsOver(rect2))
		{
			GUI.DrawTexture(rect2, (Texture)ContentFinder<Texture2D>.Get("bastronaut1a"));
		}
		else
		{
			GUI.DrawTexture(rect2, (Texture)ContentFinder<Texture2D>.Get("bastronaut" + astronautPic));
		}
		SZWidgets.ButtonInvisible(new Rect(15f, 50f, 160f, 160f), AOnAstronaut);
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(150f, 176f, 30f, 30f), ScenarioTool.CurrentTakenPawnCount.ToString());
		if (!bInterstellarView)
		{
			Widgets.DrawBoxSolid(new Rect(802f, 110f, 248f, 250f), new Color(0.2f, 0.2f, 0.2f, 0.5f));
			Widgets.Label(new Rect(807f, 115f, 200f, 30f), Label.TIERKAPSEL);
		}
		Widgets.DrawBoxSolid(new Rect(10f, 390f, 400f, 320f), new Color(0.2f, 0.2f, 0.2f, 0.5f));
		Widgets.Label(new Rect(15f, 395f, 395f, 30f), bInterstellarView ? Label.BACKUPCONTAINER : Label.KAPSELCONTAINER);
		Widgets.DrawBoxSolid(new Rect(670f, 390f, 400f, 320f), new Color(0.2f, 0.2f, 0.2f, 0.5f));
		Widgets.Label(new Rect(675f, 395f, 395f, 30f), bInterstellarView ? Label.AUFDERKARTE : Label.TEILEIMSCHIFFSWRACK);
		Text.Font = GameFont.Small;
	}

	private void DrawButtons(int x, int y, int w, int h)
	{
		if (bInterstellarView)
		{
			SZWidgets.ButtonText(rectEntladen, Label.UNLOAD, Entladen, Label.UNLOAD_DESC);
			SZWidgets.ButtonText(rectGrab, Label.LOAD, Beladen, Label.LOAD_DESC);
			SZWidgets.ButtonText(rectLiftOff, Label.STARTTONEWWORLDS, NeuenPlanetSuchen, Label.TONEWWORLD_DESC);
			bool flag = false;
		}
		else
		{
			SZWidgets.FloatMenuOnButtonText(rectLoad, "Load".Translate(), dicSlots.Keys.ToList(), (int s) => Label.LOADSLOT + s + "\n" + dicSlots[s], AOnLoadSlot);
			SZWidgets.FloatMenuOnButtonText(rectSave, "Save".Translate(), dicSlots.Keys.ToList(), (int s) => Label.SAVESLOT + s + "\n" + dicSlots[s], AOnSaveSlot);
		}
	}

	private void DrawInterstellarContainer(int x, int y, int w, int h)
	{
		SZWidgets.ButtonImage(x + w - 25, y - 30, 24f, 24f, "UI/Buttons/Dev/Add", AAddOwnParts);
		SZWidgets.ButtonImage(x + w - 50, y - 30, 24f, 24f, "bminus", AAllowRemoveParts, "", bShowRemove ? Color.red : Color.white);
		SZWidgets.ButtonImage(x + w - 75, y - 30, 24f, 24f, "bmoveright", AShowShift, "", bShowShift ? Color.red : Color.white);
		SZWidgets.FullListviewScenPart(new Rect(x, y, w, h), lPartsInterstellar, bShowRemove, delegate(ScenPart p)
		{
			lPartsInterstellar.Remove(p);
		}, bShowShift ? "bmoveright" : null, ADropWithPod, showPosition: false, withSearch: true, ref scrollPos_Interstellar, ref selectedPart_Interstellar);
	}

	private void DrawTakenContainer(int x, int y, int w, int h)
	{
		SZWidgets.ButtonImage(x + w - 25, y - 30, 24f, 24f, "UI/Buttons/Dev/Add", AAddParts);
		SZWidgets.ButtonImage(x + w - 50, y - 30, 24f, 24f, "bminus", AAllowRemoveParts, "", bShowRemove ? Color.red : Color.white);
		SZWidgets.ButtonImage(x + w - 75, y - 30, 24f, 24f, "bmoveright", AShowShift, "", bShowShift ? Color.red : Color.white);
		SZWidgets.FullListviewScenPart(new Rect(x, y, w, h), lPartsTaken, bShowRemove, delegate(ScenPart p)
		{
			lPartsTaken.Remove(p);
			lParts.Remove(p);
		}, bShowShift ? "bmoveright" : null, AMoveToMap, showPosition: false, withSearch: true, ref scrollPos_Taken, ref selectedPart_Taken);
	}

	private void DrawScatterContainer(int x, int y, int w, int h)
	{
		SZWidgets.ButtonImage(x + w - 25, y - 30, 24f, 24f, "UI/Buttons/Dev/Add", AAddPartsMap);
		SZWidgets.ButtonImage(x + w - 50, y - 30, 24f, 24f, "bminus", AAllowRemoveParts, "", bShowRemove ? Color.red : Color.white);
		SZWidgets.ButtonImage(x + w - 75, y - 30, 24f, 24f, "bmoveleft", AShowShift, "", bShowShift ? Color.red : Color.white);
		SZWidgets.FullListviewScenPart(new Rect(x, y, w, h), lPartsScatter, bShowRemove, delegate(ScenPart p)
		{
			lPartsScatter.Remove(p);
			lParts.Remove(p);
		}, bShowShift ? "bmoveleft" : null, AMoveToContainer, showPosition: false, withSearch: true, ref scrollPos_Scatter, ref selectedPart_Scatter);
	}

	private void DrawMapContainer(int x, int y, int w, int h)
	{
		SZWidgets.ButtonImage(x + w - 50, y - 30, 24f, 24f, "bminus", AAllowRemoveParts, "", bShowRemove ? Color.red : Color.white);
		SZWidgets.ButtonImage(x + w - 75, y - 30, 24f, 24f, "bmoveleft", AShowShift, "", bShowShift ? Color.red : Color.white);
		SZWidgets.FullListviewScenPart(new Rect(x, y, w, h), lPartsMap, bShowRemove, delegate(ScenPart p)
		{
			Selected selectedScenarioPart = p.GetSelectedScenarioPart();
			lPartsMap.Remove(p);
			ThingTool.FindThingOnMap(selectedScenarioPart, Find.CurrentMap, selectedScenarioPart.stackVal, doDestroy: true);
		}, bShowShift ? "bmoveleft" : null, AMoveToInterstellar, showPosition: true, withSearch: true, ref scrollPos_Map, ref selectedPart_Map);
	}

	private void DrawAnimalContainer(int x, int y, int w, int h)
	{
		SZWidgets.ButtonImage(x + w - 25, y - 30, 24f, 24f, "UI/Buttons/Dev/Add", AAddPartsAnimal);
		SZWidgets.ButtonImage(x + w - 50, y - 30, 24f, 24f, "bminus", AAllowRemoveParts);
		SZWidgets.FullListviewScenPart(new Rect(x, y, w, h), lPartsAnimal, bShowRemove, delegate(ScenPart p)
		{
			lPartsAnimal.Remove(p);
			lParts.Remove(p);
		}, null, null, showPosition: false, withSearch: true, ref scrollPos_Animal, ref selectedPart_Animal);
	}

	private static void UpdateLists()
	{
		lParts = ScenarioTool.ScenarioParts;
		lPartsTaken = new List<ScenPart>();
		lPartsScatter = new List<ScenPart>();
		lPartsAnimal = new List<ScenPart>();
		foreach (ScenPart lPart in lParts)
		{
			if (lPart.IsSupportedScenarioPart())
			{
				if (lPart.IsScatterAnywherePart())
				{
					lPartsScatter.Add(lPart);
				}
				else if (lPart.IsScenarioAnimal())
				{
					lPartsAnimal.Add(lPart);
				}
				else
				{
					lPartsTaken.Add(lPart);
				}
			}
		}
	}

	private void ScanMap()
	{
		lPartsMap.Clear();
		List<Selected> source = ThingTool.GrabAllThingsFromMap(Find.CurrentMap, doDestroy: false);
		source = (from x in source
			orderby x.thingDef.BaseMarketValue descending, x.stackVal descending
			select x).ToList();
		foreach (Selected item in source)
		{
			ScenarioTool.MergeNonScenarioPart(item, ref lPartsMap, doMerge: false);
		}
	}

	private void InsOrbit()
	{
		AOnSaveSlot(-1);
		CEditor.bStartNewGame2 = true;
		CEditor.bGamePlus = true;
		CEditor.oldScenario = Find.Scenario;
		CEditor.oldStoryteller = Find.Storyteller;
		Find.WindowStack.Add(new ITickWin2());
		Close();
		CEditor.API.CloseEditor();
	}

	private void NeuenPlanetSuchen()
	{
		for (int num = ScenarioTool.ScenarioParts.Count - 1; num >= 0; num--)
		{
			ScenPart scenPart = ScenarioTool.ScenarioParts[num];
			if (scenPart.IsScenarioAnimal())
			{
				lPartsAnimal.Remove(scenPart);
				lParts.Remove(scenPart);
			}
		}
		foreach (Pawn spawnedColonyAnimal in Find.CurrentMap.mapPawns.SpawnedColonyAnimals)
		{
			Selected selected = Selected.ByThing(spawnedColonyAnimal);
			selected.age = spawnedColonyAnimal.ageTracker.AgeBiologicalYears;
			selected.gender = spawnedColonyAnimal.gender;
			selected.pawnName = spawnedColonyAnimal.Name;
			ScenarioTool.AddScenarioPartAnimal(selected);
		}
		AOnSaveSlot(-1);
		CEditor.bStartNewGame = true;
		CEditor.bGamePlus = true;
		CEditor.oldScenario = null;
		CEditor.oldStoryteller = null;
		SoundDefOf.Archotech_Invoked.PlayOneShot(new Building_ArchonexusCore());
		ScreenFader.StartFade(Color.white, 6f);
		Find.WindowStack.Add(new ITickWin());
		Close();
		CEditor.API.CloseEditor();
	}

	private void Entladen()
	{
		List<Selected> list = new List<Selected>();
		for (int num = lPartsInterstellar.Count - 1; num >= 0; num--)
		{
			Selected selectedScenarioPart = lPartsInterstellar[num].GetSelectedScenarioPart();
			list.Add(selectedScenarioPart);
			lPartsInterstellar.Remove(lPartsInterstellar[num]);
		}
		PlacingTool.DropAllSelectedWithPod(list);
		Close();
		CEditor.API.CloseEditor();
	}

	private void Beladen()
	{
		Map currentMap = Find.CurrentMap;
		List<Selected> list = ThingTool.GrabAllThingsFromMap(currentMap);
		foreach (Selected item in list)
		{
			FleckMaker.ThrowAirPuffUp(item.location.ToVector3(), currentMap);
			FleckMaker.ThrowMicroSparks(item.location.ToVector3(), currentMap);
			FleckMaker.ThrowHeatGlow(item.location, currentMap, 2f);
			FleckMaker.ThrowLightningGlow(item.location.ToVector3(), currentMap, 5f);
			ScenarioTool.MergeNonScenarioPart(item, ref lPartsInterstellar);
		}
		ScanMap();
	}

	private void AOnLoadSlot(int index)
	{
		ScenarioTool.LoadCapsuleSetup(SLOTPATH(index));
		UpdateLists();
	}

	private void AOnSaveSlot(int index)
	{
		ScenarioTool.SaveCapsuleSetup(SLOTPATH(index));
		UpdateSlots();
	}

	private void AMoveToInterstellar(ScenPart part)
	{
		lPartsMap.Remove(part);
		Selected selectedScenarioPart = part.GetSelectedScenarioPart();
		Thing thing = ThingTool.FindThingOnMap(selectedScenarioPart, Find.CurrentMap, selectedScenarioPart.stackVal, doDestroy: true);
		FleckMaker.ThrowAirPuffUp(selectedScenarioPart.location.ToVector3(), Find.CurrentMap);
		FleckMaker.ThrowMicroSparks(selectedScenarioPart.location.ToVector3(), Find.CurrentMap);
		FleckMaker.ThrowHeatGlow(selectedScenarioPart.location, Find.CurrentMap, 2f);
		FleckMaker.ThrowLightningGlow(selectedScenarioPart.location.ToVector3(), Find.CurrentMap, 5f);
		if (thing != null)
		{
			lPartsInterstellar.Add(part);
		}
		else
		{
			MessageTool.Show("try to rescan. could not find " + selectedScenarioPart.thingDef.defName + " x" + selectedScenarioPart.stackVal, MessageTypeDefOf.RejectInput);
		}
	}

	private void ADropWithPod(ScenPart part)
	{
		lPartsInterstellar.Remove(part);
		Selected selectedScenarioPart = part.GetSelectedScenarioPart();
		PlacingTool.DropSelectedWithPod(selectedScenarioPart);
		Close();
		CEditor.API.CloseEditor();
	}

	private void AMoveToMap(ScenPart part)
	{
		lParts.Remove(part);
		Selected selectedScenarioPart = part.GetSelectedScenarioPart();
		ScenarioTool.AddScenarioPartMerged(selectedScenarioPart, addToTaken: false);
		UpdateLists();
	}

	private void AMoveToContainer(ScenPart part)
	{
		lParts.Remove(part);
		Selected selectedScenarioPart = part.GetSelectedScenarioPart();
		ScenarioTool.AddScenarioPartMerged(selectedScenarioPart, addToTaken: true);
		UpdateLists();
	}

	private void AStart()
	{
		Window windowOf = WindowTool.GetWindowOf<Page_ConfigureStartingPawns>();
		if (windowOf != null)
		{
			((Page_ConfigureStartingPawns)windowOf).nextAct();
		}
		Close(doCloseSound: false);
		CEditor.API.CloseEditor();
	}

	private void AOnAstronaut()
	{
		astronautPic = ((astronautPic == 2) ? 1 : 2);
	}

	internal void ExternalAddThing(Selected s)
	{
		if (addToOwn)
		{
			ScenarioTool.MergeNonScenarioPart(s, ref lPartsInterstellar);
			addToOwn = false;
		}
		else
		{
			ScenarioTool.AddScenarioPartMerged(s, addToTaken);
			UpdateLists();
		}
	}

	private void AAddOwnParts()
	{
		addToOwn = true;
		WindowTool.Open(new DialogObjects(DialogType.Object, this));
	}

	private void AAddParts()
	{
		addToTaken = true;
		WindowTool.Open(new DialogObjects(DialogType.Object, this));
	}

	private void AAddPartsMap()
	{
		addToTaken = false;
		WindowTool.Open(new DialogObjects(DialogType.Object, this));
	}

	private void AAddPartsAnimal()
	{
		addToTaken = false;
		addToOwn = false;
		WindowTool.Open(new DialogObjects(DialogType.Object, this, null, addAnimals: true));
	}

	private void AAllowRemoveParts()
	{
		bShowRemove = !bShowRemove;
	}

	private void AShowShift()
	{
		bShowShift = !bShowShift;
	}
}
