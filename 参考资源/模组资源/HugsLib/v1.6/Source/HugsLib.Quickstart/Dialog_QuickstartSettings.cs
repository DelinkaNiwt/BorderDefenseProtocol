using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HugsLib.Quickstart;

/// <summary>
/// Allows to change settings related to the custom quickstart functionality.
/// Strings are not translated, since this is a tool exclusively for modders.
/// </summary>
public class Dialog_QuickstartSettings : Window
{
	private class FileEntry
	{
		public readonly string Name;

		public readonly string Label;

		public readonly string VersionLabel;

		public readonly SaveFileInfo FileInfo;

		public FileEntry(FileInfo file)
		{
			FileInfo = new SaveFileInfo(file);
			Name = Path.GetFileNameWithoutExtension(FileInfo.FileInfo.Name);
			Label = Name;
			VersionLabel = $"({FileInfo.GameVersion})";
		}
	}

	private readonly List<FileEntry> saveFiles = new List<FileEntry>();

	public override Vector2 InitialSize => new Vector2(600f, 500f);

	public Dialog_QuickstartSettings()
	{
		closeOnCancel = true;
		closeOnAccept = false;
		doCloseButton = false;
		doCloseX = true;
		resizeable = false;
		draggable = true;
	}

	public override void PreOpen()
	{
		base.PreOpen();
		CacheSavedGameFiles();
		EnsureSettingsHaveValidFiles(QuickstartController.Settings);
	}

	public override void PostClose()
	{
		QuickstartController.SaveSettings();
	}

	public override void DoWindowContents(Rect inRect)
	{
		QuickstartSettings settings = QuickstartController.Settings;
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.verticalSpacing = 6f;
		listing_Standard.Begin(inRect);
		Text.Font = GameFont.Medium;
		listing_Standard.Label("Quickstart settings");
		Text.Font = GameFont.Small;
		listing_Standard.GapLine();
		listing_Standard.Gap();
		OperationModeRadioButton(listing_Standard, 40f, "Quickstart off", settings, QuickstartSettings.QuickstartMode.Disabled, "Quickstart functionality is disabled.\nThe game starts normally.");
		OperationModeRadioButton(listing_Standard, 40f, "Quickstart: load save file", settings, QuickstartSettings.QuickstartMode.LoadMap, "Load the selected saved game right after launch.");
		float allocatedHeight = 56f;
		MakeSubListing(listing_Standard, 0f, allocatedHeight, 10f, 30f, 6f, delegate(Listing_Standard sub, float width)
		{
			sub.ColumnWidth = 100f;
			Text.Anchor = TextAnchor.MiddleLeft;
			Rect rect = sub.GetRect(30f);
			Widgets.Label(rect, "Save file:");
			Text.Anchor = TextAnchor.UpperLeft;
			sub.NewColumn();
			sub.ColumnWidth = width - 100f - 17f;
			MakeSelectSaveButton(sub, settings);
		});
		OperationModeRadioButton(listing_Standard, 40f, "Quickstart: generate map", settings, QuickstartSettings.QuickstartMode.GenerateMap, "Generate a new map right after launch.\nWorks the same as using the \"quicktest\" command line option.");
		allocatedHeight = 92f;
		MakeSubListing(listing_Standard, 0f, allocatedHeight, 10f, 30f, 6f, delegate(Listing_Standard sub, float width)
		{
			sub.ColumnWidth = 100f;
			Text.Anchor = TextAnchor.MiddleLeft;
			Rect rect = sub.GetRect(30f);
			Widgets.Label(rect, "Scenario:");
			sub.Gap(6f);
			rect = sub.GetRect(30f);
			Widgets.Label(rect, "Map size:");
			Text.Anchor = TextAnchor.UpperLeft;
			sub.NewColumn();
			sub.ColumnWidth = width - 100f - 17f;
			MakeSelectScenarioButton(sub, settings);
			MakeSelectMapSizeButton(sub, settings);
		});
		allocatedHeight = 128f;
		MakeSubListing(listing_Standard, 280f, allocatedHeight, 10f, 0f, 6f, delegate(Listing_Standard sub, float width)
		{
			sub.CheckboxLabeled("Abort quickstart on error", ref settings.StopOnErrors, "Prevent quickstart if errors are detected during startup.");
			sub.CheckboxLabeled("Abort quickstart on warning", ref settings.StopOnWarnings, "Prevent quickstart if warnings are detected during startup.");
			sub.CheckboxLabeled("Ignore version & mod config mismatch", ref settings.BypassSafetyDialog, "Skip the mod config mismatch dialog and load all saved games regardless.");
		});
		listing_Standard.End();
		Text.Anchor = TextAnchor.UpperLeft;
		Vector2 vector = new Vector2(180f, 40f);
		float y = inRect.height - vector.y;
		if (Widgets.ButtonText(new Rect(inRect.width - vector.x, y, vector.x, vector.y), "Close"))
		{
			Close();
		}
	}

	private void OperationModeRadioButton(Listing_Standard listing, float labelInset, string label, QuickstartSettings settings, QuickstartSettings.QuickstartMode assignedMode, string tooltip)
	{
		float lineHeight = Text.LineHeight;
		Rect rect = listing.GetRect(lineHeight + listing.verticalSpacing);
		Rect rect2 = new Rect(rect.x + labelInset, rect.y + -4f, rect.width - labelInset, rect.height - -4f);
		Rect rect3 = new Rect(rect.x, rect.y, rect.width, rect.height);
		if (tooltip != null)
		{
			if (Mouse.IsOver(rect3))
			{
				Widgets.DrawHighlight(rect3);
			}
			TooltipHandler.TipRegion(rect3, tooltip);
		}
		if (Widgets.ButtonInvisible(rect3) && settings.OperationMode != assignedMode)
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			QuickstartController.Settings.OperationMode = assignedMode;
		}
		Widgets.RadioButton(rect.x, rect.y, settings.OperationMode == assignedMode);
		Text.Font = GameFont.Medium;
		string label2 = $"<size={16f}>{label}</size>";
		Widgets.Label(rect2, label2);
		Text.Font = GameFont.Small;
	}

	private void MakeSubListing(Listing_Standard mainListing, float width, float allocatedHeight, float padding, float extraInset, float verticalSpacing, Action<Listing_Standard, float> drawContents)
	{
		Rect rect = mainListing.GetRect(allocatedHeight);
		width = ((width > 0f) ? width : (rect.width - (padding + extraInset)));
		rect = new Rect(rect.x + padding + extraInset, rect.y + padding, width, rect.height - padding * 2f);
		Listing_Standard listing_Standard = new Listing_Standard
		{
			verticalSpacing = verticalSpacing
		};
		listing_Standard.Begin(rect);
		drawContents(listing_Standard, width);
		listing_Standard.End();
	}

	private void MakeSelectSaveButton(Listing_Standard sub, QuickstartSettings settings)
	{
		Rect rect = sub.GetRect(30f);
		Rect rect2 = new Rect(rect);
		rect2.xMax = rect.xMax - 126f;
		Rect rect3 = rect2;
		rect2 = new Rect(rect);
		rect2.xMin = rect.xMin + rect3.width + 6f;
		Rect rect4 = rect2;
		string label = settings.SaveFileToLoad ?? "Most recent save file";
		if (Widgets.ButtonText(rect3, label))
		{
			ShowSaveFileSelectionFloatMenu();
		}
		if (Widgets.ButtonText(rect4, "Load now"))
		{
			if (HugsLibUtility.ShiftIsHeld)
			{
				settings.OperationMode = QuickstartSettings.QuickstartMode.LoadMap;
			}
			QuickstartController.InitiateSaveLoading();
			Close();
		}
		sub.Gap(sub.verticalSpacing);
		void ShowSaveFileSelectionFloatMenu()
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>
			{
				new FloatMenuOption("Most recent save file", delegate
				{
					settings.SaveFileToLoad = null;
				})
			};
			list.AddRange(GetSaveFileFloatMenuOptions(settings));
			Find.WindowStack.Add(new FloatMenu(list));
		}
	}

	private IEnumerable<FloatMenuOption> GetSaveFileFloatMenuOptions(QuickstartSettings settings)
	{
		return saveFiles.Select((FileEntry s) => new FloatMenuOption(s.Label, delegate
		{
			settings.SaveFileToLoad = s.Name;
		}, MenuOptionPriority.Default, null, null, Text.CalcSize(s.VersionLabel).x + 10f, delegate(Rect rect)
		{
			Color color = GUI.color;
			GUI.color = s.FileInfo.VersionColor;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(new Rect(rect.x + 10f, rect.y, 200f, rect.height), s.VersionLabel);
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = color;
			return false;
		}));
	}

	private void MakeSelectScenarioButton(Listing_Standard sub, QuickstartSettings settings)
	{
		Rect rect = sub.GetRect(30f);
		Rect rect2 = new Rect(rect);
		rect2.xMax = rect.xMax - 126f;
		Rect rect3 = rect2;
		rect2 = new Rect(rect);
		rect2.xMin = rect.xMin + rect3.width + 6f;
		Rect rect4 = rect2;
		string scenarioToGen = settings.ScenarioToGen;
		if (Widgets.ButtonText(rect3, scenarioToGen ?? "Select a scenario"))
		{
			FloatMenu window = new FloatMenu((from s in ScenarioLister.AllScenarios()
				select new FloatMenuOption(s.name, delegate
				{
					settings.ScenarioToGen = s.name;
				})).ToList());
			Find.WindowStack.Add(window);
		}
		if (Widgets.ButtonText(rect4, "Generate now"))
		{
			if (HugsLibUtility.ShiftIsHeld)
			{
				settings.OperationMode = QuickstartSettings.QuickstartMode.GenerateMap;
			}
			QuickstartController.InitiateMapGeneration();
			Close();
		}
		sub.Gap(sub.verticalSpacing);
	}

	private void MakeSelectMapSizeButton(Listing_Standard sub, QuickstartSettings settings)
	{
		List<QuickstartController.MapSizeEntry> mapSizes = QuickstartController.MapSizes;
		string text = mapSizes.Select((QuickstartController.MapSizeEntry s) => (s.Size == settings.MapSizeToGen) ? s.Label : null).FirstOrDefault((string s) => s != null);
		if (!sub.ButtonText(text ?? "Select a map size"))
		{
			return;
		}
		FloatMenu window = new FloatMenu(mapSizes.Select((QuickstartController.MapSizeEntry s) => new FloatMenuOption(s.Label, delegate
		{
			settings.MapSizeToGen = s.Size;
		})).ToList());
		Find.WindowStack.Add(window);
	}

	private void CacheSavedGameFiles()
	{
		saveFiles.Clear();
		foreach (FileInfo allSavedGameFile in GenFilePaths.AllSavedGameFiles)
		{
			try
			{
				saveFiles.Add(new FileEntry(allSavedGameFile));
			}
			catch (Exception)
			{
			}
		}
	}

	private void EnsureSettingsHaveValidFiles(QuickstartSettings settings)
	{
		if (saveFiles.Select((FileEntry s) => s.Name).All((string s) => s != settings.SaveFileToLoad))
		{
			settings.SaveFileToLoad = null;
		}
		if (settings.ScenarioToGen != null && ScenarioLister.AllScenarios().All((Scenario s) => s.name != settings.ScenarioToGen))
		{
			settings.ScenarioToGen = null;
		}
		if (settings.ScenarioToGen == null)
		{
			settings.ScenarioToGen = ScenarioDefOf.Crashlanded.defName;
		}
	}
}
