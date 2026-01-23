using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.News;

/// <summary>
/// The extended update news dialog, with filtering by mod and a menu button in entry headers for dev mode actions.
/// </summary>
internal class Dialog_UpdateFeaturesFiltered : Dialog_UpdateFeatures
{
	private class PlayerMessageSender : IStatusMessageSender
	{
		public void Send(string message, bool success)
		{
			MessageTypeDef def = (success ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.RejectInput);
			Messages.Message(message, def);
		}
	}

	private readonly IIgnoredNewsProviderStore ignoredNewsProviders;

	private readonly string filterButtonLabel;

	private readonly string allModsFilterLabel;

	private readonly TaggedString currentFilterReadout;

	private readonly TaggedString dropdownEntryTemplate;

	private readonly TaggedString ignoredModLabelSuffix;

	private readonly UpdateFeatureDefFilteringProvider defFilter;

	private readonly UpdateFeaturesDevMenu devMenu;

	private List<UpdateFeatureDef> fullDefList;

	private float bottomButtonWidth;

	public Dialog_UpdateFeaturesFiltered(List<UpdateFeatureDef> featureDefs, UpdateFeatureManager.IgnoredNewsIds ignoredNewsProviders, IUpdateFeaturesDevActions news, IModSpotterDevActions spotter)
		: base(FilterOutIgnoredProviders(featureDefs, ignoredNewsProviders), ignoredNewsProviders)
	{
		fullDefList = featureDefs;
		this.ignoredNewsProviders = ignoredNewsProviders;
		filterButtonLabel = "HugsLib_features_filterBtn".Translate();
		allModsFilterLabel = "HugsLib_features_filterAllMods".Translate();
		currentFilterReadout = "HugsLib_features_filterStatus".Translate();
		dropdownEntryTemplate = "HugsLib_features_filterDropdownEntry".Translate();
		ignoredModLabelSuffix = "HugsLib_features_filterIgnoredModSuffix".Translate();
		defFilter = new UpdateFeatureDefFilteringProvider(featureDefs);
		devMenu = new UpdateFeaturesDevMenu(news, spotter, new PlayerMessageSender());
		devMenu.UpdateFeatureDefsReloaded += DevMenuDefsReloadedHandler;
		AdjustButtonSizeToLabel();
	}

	public override void ExtraOnGUI()
	{
		base.ExtraOnGUI();
		CheckForReloadKeyPress();
	}

	protected override void DrawEntryTitleWidgets(Rect titleRect, UpdateFeatureDef forDef)
	{
		float widgetOffset = DrawEntryLinkWidget(titleRect, forDef);
		if (Prefs.DevMode)
		{
			DrawDevToolsMenuWidget(titleRect, widgetOffset, forDef);
		}
	}

	private void CheckForReloadKeyPress()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 5 && Event.current.keyCode == KeyCode.F5)
		{
			Event.current.Use();
			devMenu.ReloadNewsDefs();
		}
	}

	private void DevMenuDefsReloadedHandler(IEnumerable<UpdateFeatureDef> loadedDefs)
	{
		fullDefList = loadedDefs.ToList();
		InstallFilteredDefs(fullDefList);
	}

	private void InstallFilteredDefs(IEnumerable<UpdateFeatureDef> defs)
	{
		IEnumerable<UpdateFeatureDef> featureDefs = defFilter.MatchingDefsOf(defs);
		if (defFilter.CurrentFilterModIdentifier == null)
		{
			featureDefs = FilterOutIgnoredProviders(featureDefs, ignoredNewsProviders);
		}
		InstallUpdateFeatureDefs(featureDefs);
	}

	private void DrawDevToolsMenuWidget(Rect titleRect, float widgetOffset, UpdateFeatureDef forDef)
	{
		float height = titleRect.height;
		Rect rect = new Rect(titleRect.width - widgetOffset - height, titleRect.y, height, height);
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		Texture2D hLMenuIcon = HugsLibTextures.HLMenuIcon;
		Rect outerRect = new Rect(rect.center.x - (float)hLMenuIcon.width / 2f, rect.center.y - (float)hLMenuIcon.height / 2f, hLMenuIcon.width, hLMenuIcon.height);
		Widgets.DrawTextureFitted(outerRect, hLMenuIcon, 1f);
		if (Widgets.ButtonInvisible(rect))
		{
			OpenDevToolsDropdownMenu(forDef);
		}
	}

	private void OpenDevToolsDropdownMenu(UpdateFeatureDef forDef)
	{
		Find.WindowStack.Add(new FloatMenu((from o in devMenu.GetMenuOptions(forDef)
			select new FloatMenuOption(o.label, o.action)
			{
				Disabled = o.disabled
			}).ToList()));
	}

	protected override void DrawBottomButtonRow(Rect inRect)
	{
		DrawFilterButton(inRect.LeftPartPixels(bottomButtonWidth));
		DrawCurrentFilterLabel(new Rect(inRect.x + bottomButtonWidth + 13f, inRect.y, inRect.width - (bottomButtonWidth * 2f + Margin * 2f), inRect.height));
		DrawCloseButton(inRect.RightPartPixels(bottomButtonWidth));
	}

	private void AdjustButtonSizeToLabel()
	{
		bottomButtonWidth = Mathf.Max(Text.CalcSize(filterButtonLabel).x + 16f, Window.CloseButSize.x);
	}

	private void DrawCurrentFilterLabel(Rect inRect)
	{
		GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
		Color color = GUI.color;
		GUI.color = new Color(0.5f, 0.5f, 0.5f);
		string text = defFilter.CurrentFilterModNameReadable ?? allModsFilterLabel;
		Widgets.Label(inRect, currentFilterReadout.Formatted(text));
		GUI.color = color;
		GenUI.ResetLabelAlign();
	}

	private void DrawFilterButton(Rect inRect)
	{
		if (Widgets.ButtonText(inRect, filterButtonLabel))
		{
			ShowFilterOptionsMenu();
		}
	}

	private void ShowFilterOptionsMenu()
	{
		Find.WindowStack.Add(new FloatMenu(GetFilterMenuOptions()));
	}

	private List<FloatMenuOption> GetFilterMenuOptions()
	{
		List<FloatMenuOption> list = new List<FloatMenuOption>
		{
			new FloatMenuOption(allModsFilterLabel, delegate
			{
				SetFilterAndUpdateShownDefs(null);
			})
		};
		list.AddRange(defFilter.GetAvailableFilters().Select(delegate((string id, string label, int defCount) f)
		{
			string text = (ignoredNewsProviders.Contains(f.id) ? ignoredModLabelSuffix.RawText : string.Empty);
			TaggedString taggedString = dropdownEntryTemplate.Formatted(f.label, f.defCount, text);
			return new FloatMenuOption(taggedString, delegate
			{
				SetFilterAndUpdateShownDefs(f.id);
			});
		}));
		return list;
	}

	private void SetFilterAndUpdateShownDefs(string newFilterModIdentifier)
	{
		if (!(defFilter.CurrentFilterModIdentifier == newFilterModIdentifier))
		{
			defFilter.CurrentFilterModIdentifier = newFilterModIdentifier;
			InstallFilteredDefs(fullDefList);
			ResetScrollPosition();
		}
	}

	private static List<UpdateFeatureDef> FilterOutIgnoredProviders(IEnumerable<UpdateFeatureDef> featureDefs, IIgnoredNewsProviderStore ignoredNewsProviders)
	{
		return featureDefs.Where((UpdateFeatureDef d) => !ignoredNewsProviders.Contains(d.OwningModId)).ToList();
	}
}
