using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimTalk.Client.Gemini;
using RimTalk.Client.OpenAI;
using RimTalk.Client.Player2;
using RimTalk.Data;
using RimTalk.Prompt;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimTalk;

public class Settings : Mod
{
	private enum SettingsTab
	{
		Basic,
		PromptPreset,
		Context,
		EventFilter
	}

	public enum ButtonDisplayMode
	{
		Tab,
		Toggle,
		None
	}

	public enum PlayerDialogueMode
	{
		Disabled,
		Manual,
		AIDriven
	}

	private enum PresetSection
	{
		System,
		History,
		Prompt
	}

	private Vector2 _mainScrollPosition = Vector2.zero;

	private Vector2 _aiInstructionScrollPos = Vector2.zero;

	private Vector2 _promptContentScrollPos = Vector2.zero;

	private string _textAreaBuffer = "";

	private bool _textAreaInitialized;

	private int _lastTextAreaCursorPos = -1;

	private int _lastPromptEditorCursorPos = -1;

	private List<string> _discoveredArchivableTypes = new List<string>();

	private bool _archivableTypesScanned;

	private int _apiSettingsHash = 0;

	private SettingsTab _currentTab = SettingsTab.Basic;

	private static RimTalkSettings _settings;

	private static readonly Dictionary<string, List<string>> ModelCache = new Dictionary<string, List<string>>();

	private ContextPreset _currentPreset = ContextPreset.Custom;

	private readonly ContextSettings _changeBuffer = new ContextSettings();

	private bool _presetInitialized;

	private static readonly Dictionary<ContextPreset, ContextSettings> PresetDefinitions = new Dictionary<ContextPreset, ContextSettings>
	{
		{
			ContextPreset.Essential,
			new ContextSettings
			{
				EnableContextOptimization = true,
				MaxPawnContextCount = 2,
				ConversationHistoryCount = 1,
				IncludeRace = true,
				IncludeNotableGenes = false,
				IncludeIdeology = false,
				IncludeBackstory = true,
				IncludeTraits = true,
				IncludeSkills = false,
				IncludeHealth = true,
				IncludeMood = true,
				IncludeThoughts = true,
				IncludeRelations = true,
				IncludeEquipment = false,
				IncludePrisonerSlaveStatus = false,
				IncludeTime = false,
				IncludeDate = false,
				IncludeSeason = false,
				IncludeWeather = true,
				IncludeLocationAndTemperature = false,
				IncludeTerrain = false,
				IncludeBeauty = false,
				IncludeCleanliness = false,
				IncludeSurroundings = false,
				IncludeWealth = false
			}
		},
		{
			ContextPreset.Standard,
			new ContextSettings
			{
				EnableContextOptimization = false,
				MaxPawnContextCount = 3,
				ConversationHistoryCount = 1,
				IncludeRace = true,
				IncludeNotableGenes = true,
				IncludeIdeology = true,
				IncludeBackstory = true,
				IncludeTraits = true,
				IncludeSkills = true,
				IncludeHealth = true,
				IncludeMood = true,
				IncludeThoughts = true,
				IncludeRelations = true,
				IncludeEquipment = true,
				IncludePrisonerSlaveStatus = false,
				IncludeTime = true,
				IncludeDate = false,
				IncludeSeason = true,
				IncludeWeather = true,
				IncludeLocationAndTemperature = true,
				IncludeTerrain = false,
				IncludeBeauty = false,
				IncludeCleanliness = false,
				IncludeSurroundings = false,
				IncludeWealth = false
			}
		},
		{
			ContextPreset.Comprehensive,
			new ContextSettings
			{
				EnableContextOptimization = false,
				MaxPawnContextCount = 3,
				ConversationHistoryCount = 3,
				IncludeRace = true,
				IncludeNotableGenes = true,
				IncludeIdeology = true,
				IncludeBackstory = true,
				IncludeTraits = true,
				IncludeSkills = true,
				IncludeHealth = true,
				IncludeMood = true,
				IncludeThoughts = true,
				IncludeRelations = true,
				IncludeEquipment = true,
				IncludePrisonerSlaveStatus = true,
				IncludeTime = true,
				IncludeDate = true,
				IncludeSeason = true,
				IncludeWeather = true,
				IncludeLocationAndTemperature = true,
				IncludeTerrain = true,
				IncludeBeauty = true,
				IncludeCleanliness = true,
				IncludeSurroundings = true,
				IncludeWealth = true
			}
		}
	};

	private static readonly Color LeftPanelBackground = new Color(0.05f, 0.05f, 0.05f, 0.55f);

	private static readonly Color AddGreen = new Color(0.3f, 0.9f, 0.3f);

	private static readonly Color DeleteRed = new Color(1f, 0.4f, 0.4f);

	private const string DefaultPresetName = "RimTalk Default";

	private Vector2 _presetListScrollPos = Vector2.zero;

	private Vector2 _entryListScrollPos = Vector2.zero;

	private Vector2 _auxScrollPos = Vector2.zero;

	private Vector2 _previewScrollPos = Vector2.zero;

	private string _selectedPresetId;

	private string _selectedEntryId;

	private bool _showPreview = false;

	private bool _showSidePanel = false;

	private int _sidePanelMode = 0;

	private float _splitRatioVert = 0.5f;

	private float _splitRatioHoriz = 0.7f;

	private bool _isDraggingVert = false;

	private bool _isDraggingHoriz = false;

	private string _variableSearchQuery = "";

	private string _depthBuffer = "";

	private string _depthBufferEntryId = "";

	public static RimTalkSettings Get()
	{
		return _settings ?? (_settings = LoadedModManager.GetMod<Settings>().GetSettings<RimTalkSettings>());
	}

	public Settings(ModContentPack content)
		: base(content)
	{
		Harmony harmony = new Harmony("cj.rimtalk");
		RimTalkSettings settings = GetSettings<RimTalkSettings>();
		harmony.PatchAll();
		_apiSettingsHash = GetApiSettingsHash(settings);
	}

	public override string SettingsCategory()
	{
		return base.Content?.Name ?? GetType().Assembly.GetName().Name;
	}

	private void ScanForArchivableTypes()
	{
		if (_archivableTypesScanned)
		{
			return;
		}
		HashSet<string> archivableTypes = new HashSet<string>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			try
			{
				List<string> types = (from t in assembly.GetTypes()
					where typeof(IArchivable).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract
					select t.FullName).ToList();
				foreach (string type in types)
				{
					archivableTypes.Add(type);
				}
			}
			catch (Exception ex)
			{
				global::RimTalk.Util.Logger.Warning("Error scanning assembly " + assembly.FullName + ": " + ex.Message);
			}
		}
		if (Current.Game != null && Find.Archive != null)
		{
			foreach (IArchivable archivable in Find.Archive.ArchivablesListForReading)
			{
				archivableTypes.Add(archivable.GetType().FullName);
			}
		}
		IEnumerable<string> letterDefNames = DefDatabase<LetterDef>.AllDefs.Select((LetterDef letterDef) => letterDef.defName);
		IEnumerable<string> messageTypeDefNames = DefDatabase<MessageTypeDef>.AllDefs.Select((MessageTypeDef messageTypeDef) => messageTypeDef.defName);
		foreach (string def in letterDefNames)
		{
			archivableTypes.Add(def);
		}
		foreach (string def2 in messageTypeDefNames)
		{
			archivableTypes.Add(def2);
		}
		_discoveredArchivableTypes = archivableTypes.OrderBy((string x) => x).ToList();
		_archivableTypesScanned = true;
		RimTalkSettings settings = Get();
		foreach (string typeName in _discoveredArchivableTypes)
		{
			if (!settings.EnabledArchivableTypes.ContainsKey(typeName))
			{
				bool defaultEnabled = !typeName.Equals("Verse.Message", StringComparison.OrdinalIgnoreCase);
				settings.EnabledArchivableTypes[typeName] = defaultEnabled;
			}
		}
		Log.Message($"[RimTalk] Discovered {_discoveredArchivableTypes.Count} archivable types");
	}

	public override void WriteSettings()
	{
		base.WriteSettings();
		ClearCache();
		RimTalkSettings settings = Get();
		int newHash = GetApiSettingsHash(settings);
		if (newHash != _apiSettingsHash)
		{
			settings.CurrentCloudConfigIndex = 0;
			_apiSettingsHash = newHash;
			RimTalk.Reset(soft: true);
		}
	}

	private int GetApiSettingsHash(RimTalkSettings settings)
	{
		StringBuilder sb = new StringBuilder();
		if (settings.CloudConfigs != null)
		{
			foreach (ApiConfig config in settings.CloudConfigs)
			{
				sb.AppendLine(config.Provider.ToString());
				sb.AppendLine(config.ApiKey);
				sb.AppendLine(config.SelectedModel);
				sb.AppendLine(config.CustomModelName);
				sb.AppendLine(config.IsEnabled.ToString());
				sb.AppendLine(config.BaseUrl);
			}
		}
		if (settings.LocalConfig != null)
		{
			sb.AppendLine(settings.LocalConfig.Provider.ToString());
			sb.AppendLine(settings.LocalConfig.BaseUrl);
			sb.AppendLine(settings.LocalConfig.CustomModelName);
		}
		sb.AppendLine(settings.CustomInstruction);
		sb.AppendLine(settings.AllowSimultaneousConversations.ToString());
		sb.AppendLine(settings.AllowSlavesToTalk.ToString());
		sb.AppendLine(settings.AllowPrisonersToTalk.ToString());
		sb.AppendLine(settings.AllowOtherFactionsToTalk.ToString());
		sb.AppendLine(settings.AllowEnemiesToTalk.ToString());
		sb.AppendLine(settings.AllowBabiesToTalk.ToString());
		sb.AppendLine(settings.AllowNonHumanToTalk.ToString());
		sb.AppendLine(settings.ApplyMoodAndSocialEffects.ToString());
		sb.AppendLine(settings.PlayerDialogueMode.ToString());
		sb.AppendLine(settings.PlayerName);
		return sb.ToString().GetHashCode();
	}

	private void DrawTabButtons(Rect rect)
	{
		float tabWidth = rect.width / 4f;
		Rect basicTabRect = new Rect(rect.x, rect.y, tabWidth, 30f);
		Rect promptTabRect = new Rect(rect.x + tabWidth, rect.y, tabWidth, 30f);
		Rect contextTabRect = new Rect(rect.x + tabWidth * 2f, rect.y, tabWidth, 30f);
		Rect filterTabRect = new Rect(rect.x + tabWidth * 3f, rect.y, tabWidth, 30f);
		GUI.color = ((_currentTab == SettingsTab.Basic) ? Color.white : Color.gray);
		if (Widgets.ButtonText(basicTabRect, "RimTalk.Settings.BasicSettings".Translate()))
		{
			_currentTab = SettingsTab.Basic;
		}
		GUI.color = ((_currentTab == SettingsTab.PromptPreset) ? Color.white : Color.gray);
		if (Widgets.ButtonText(promptTabRect, "RimTalk.Settings.PromptSetting".Translate()))
		{
			_currentTab = SettingsTab.PromptPreset;
		}
		GUI.color = ((_currentTab == SettingsTab.Context) ? Color.white : Color.gray);
		if (Widgets.ButtonText(contextTabRect, "RimTalk.Settings.ContextFilter".Translate()))
		{
			_currentTab = SettingsTab.Context;
		}
		GUI.color = ((_currentTab == SettingsTab.EventFilter) ? Color.white : Color.gray);
		if (Widgets.ButtonText(filterTabRect, "RimTalk.Settings.EventFilter".Translate()))
		{
			_currentTab = SettingsTab.EventFilter;
			if (!_archivableTypesScanned)
			{
				ScanForArchivableTypes();
			}
		}
		GUI.color = Color.white;
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		RimTalkSettings rtSettings = Get();
		Dialog_ModSettings settingsWindow = Find.WindowStack.WindowOfType<Dialog_ModSettings>();
		if (settingsWindow != null)
		{
			settingsWindow.doCloseX = true;
			settingsWindow.draggable = true;
			settingsWindow.closeOnAccept = false;
			settingsWindow.absorbInputAroundWindow = false;
			settingsWindow.preventCameraMotion = false;
			settingsWindow.closeOnClickedOutside = false;
			float targetWidth;
			float targetHeight;
			if (_currentTab == SettingsTab.PromptPreset && rtSettings.UseAdvancedPromptMode)
			{
				targetWidth = Mathf.Min((float)Verse.UI.screenWidth * 0.9f, 1200f);
				targetHeight = Mathf.Min((float)Verse.UI.screenHeight * 0.9f, 800f);
			}
			else
			{
				targetWidth = 900f;
				targetHeight = 700f;
			}
			if (Mathf.Abs(settingsWindow.windowRect.width - targetWidth) > 1f || Mathf.Abs(settingsWindow.windowRect.height - targetHeight) > 1f)
			{
				settingsWindow.windowRect.width = targetWidth;
				settingsWindow.windowRect.height = targetHeight;
				settingsWindow.windowRect.x = ((float)Verse.UI.screenWidth - targetWidth) / 2f;
				settingsWindow.windowRect.y = ((float)Verse.UI.screenHeight - targetHeight) / 2f;
			}
		}
		Rect tabRect = new Rect(inRect.x, inRect.y, inRect.width, 35f);
		DrawTabButtons(tabRect);
		Rect contentRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f);
		if (_currentTab == SettingsTab.PromptPreset && rtSettings.UseAdvancedPromptMode)
		{
			Listing_Standard promptListing = new Listing_Standard();
			promptListing.Begin(contentRect);
			DrawPromptPresetSettings(promptListing, contentRect);
			promptListing.End();
			return;
		}
		GUI.BeginGroup(new Rect(-9999f, -9999f, 1f, 1f));
		Listing_Standard listing = new Listing_Standard();
		Rect calculationRect = new Rect(0f, 0f, contentRect.width - 16f, 9999f);
		listing.Begin(calculationRect);
		switch (_currentTab)
		{
		case SettingsTab.Basic:
			DrawBasicSettings(listing);
			break;
		case SettingsTab.PromptPreset:
			DrawPromptPresetSettings(listing, contentRect);
			break;
		case SettingsTab.Context:
			DrawContextFilterSettings(listing);
			break;
		case SettingsTab.EventFilter:
			DrawEventFilterSettings(listing);
			break;
		}
		float contentHeight = listing.CurHeight;
		listing.End();
		GUI.EndGroup();
		Rect viewRect = new Rect(0f, 0f, contentRect.width - 16f, contentHeight);
		_mainScrollPosition = GUI.BeginScrollView(contentRect, _mainScrollPosition, viewRect);
		listing.Begin(viewRect);
		switch (_currentTab)
		{
		case SettingsTab.Basic:
			DrawBasicSettings(listing);
			break;
		case SettingsTab.PromptPreset:
			DrawPromptPresetSettings(listing, contentRect);
			break;
		case SettingsTab.Context:
			DrawContextFilterSettings(listing);
			break;
		case SettingsTab.EventFilter:
			DrawEventFilterSettings(listing);
			break;
		}
		listing.End();
		GUI.EndScrollView();
	}

	private static void ClearCache()
	{
		_settings = null;
	}

	private void DrawAIInstructionSettings(Listing_Standard listingStandard, bool showAdvancedSwitch = false)
	{
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03af: Expected O, but got Unknown
		RimTalkSettings settings = Get();
		if (!_textAreaInitialized)
		{
			_textAreaBuffer = (string.IsNullOrWhiteSpace(settings.CustomInstruction) ? Constant.DefaultInstruction : settings.CustomInstruction);
			_textAreaInitialized = true;
		}
		string modelName = settings.GetActiveConfig()?.SelectedModel ?? "N/A";
		TaggedString aiInstructionPrompt = "RimTalk.Settings.AIInstructionPrompt".Translate(modelName);
		float textHeight = Text.CalcHeight(aiInstructionPrompt, listingStandard.ColumnWidth - (showAdvancedSwitch ? 180f : 0f));
		float headerHeight = Mathf.Max(textHeight, 30f);
		Rect headerRect = listingStandard.GetRect(headerHeight);
		if (showAdvancedSwitch)
		{
			float buttonWidth = 170f;
			Rect buttonRect = new Rect(headerRect.xMax - buttonWidth, headerRect.y, buttonWidth, 28f);
			if (Widgets.ButtonText(buttonRect, "RimTalk.Settings.SwitchToAdvancedSettings".Translate()))
			{
				Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("RimTalk.Settings.AdvancedModeWarning".Translate(), delegate
				{
					settings.UseAdvancedPromptMode = true;
				}));
			}
			Rect labelRect = new Rect(headerRect.x, headerRect.y, headerRect.width - buttonWidth - 10f, headerRect.height);
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(labelRect, aiInstructionPrompt);
			Text.Anchor = TextAnchor.UpperLeft;
		}
		else
		{
			Widgets.Label(headerRect, aiInstructionPrompt);
		}
		listingStandard.Gap(6f);
		Text.Font = GameFont.Tiny;
		GUI.color = Color.green;
		Rect contextTipRect = listingStandard.GetRect(Text.LineHeight);
		Widgets.Label(contextTipRect, "RimTalk.Settings.AutoIncludedTip".Translate());
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		listingStandard.Gap(6f);
		Text.Font = GameFont.Tiny;
		GUI.color = Color.yellow;
		Rect rateLimitRect = listingStandard.GetRect(Text.LineHeight);
		Widgets.Label(rateLimitRect, "RimTalk.Settings.RateLimitWarning".Translate());
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		listingStandard.Gap(6f);
		int currentTokens = CommonUtil.EstimateTokenCount(_textAreaBuffer);
		int maxAllowedTokens = CommonUtil.GetMaxAllowedTokens(settings.TalkInterval);
		string tokenInfo = "RimTalk.Settings.TokenInfo".Translate(currentTokens, maxAllowedTokens);
		GUI.color = ((currentTokens > maxAllowedTokens) ? Color.red : Color.green);
		Text.Font = GameFont.Tiny;
		Rect tokenInfoRect = listingStandard.GetRect(Text.LineHeight);
		Widgets.Label(tokenInfoRect, tokenInfo);
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		listingStandard.Gap(6f);
		float textAreaHeight = 350f;
		Rect textAreaRect = listingStandard.GetRect(textAreaHeight);
		float innerWidth = textAreaRect.width - 16f;
		float contentHeight = Mathf.Max(textAreaHeight, Text.CalcHeight(_textAreaBuffer, innerWidth) + 40f);
		Widgets.BeginScrollView(viewRect: new Rect(0f, 0f, innerWidth, contentHeight), outRect: textAreaRect, scrollPosition: ref _aiInstructionScrollPos);
		GUI.SetNextControlName("RimTalk_AIInstruction_TextArea");
		string newInstruction = Widgets.TextArea(new Rect(0f, 0f, innerWidth, contentHeight), _textAreaBuffer);
		if (GUI.GetNameOfFocusedControl() == "RimTalk_AIInstruction_TextArea")
		{
			TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
			if (te != null && te.cursorIndex != _lastTextAreaCursorPos)
			{
				_lastTextAreaCursorPos = te.cursorIndex;
				float cursorY = te.graphicalCursorPos.y;
				if (cursorY < _aiInstructionScrollPos.y)
				{
					_aiInstructionScrollPos.y = cursorY;
				}
				else if (cursorY + 25f > _aiInstructionScrollPos.y + textAreaHeight)
				{
					_aiInstructionScrollPos.y = cursorY + 25f - textAreaHeight;
				}
			}
		}
		Widgets.EndScrollView();
		if (newInstruction != _textAreaBuffer)
		{
			_textAreaBuffer = newInstruction;
			settings.CustomInstruction = ((newInstruction == Constant.DefaultInstruction) ? "" : newInstruction);
		}
		listingStandard.Gap(6f);
		Rect resetButtonRect = listingStandard.GetRect(30f);
		if (Widgets.ButtonText(resetButtonRect, "RimTalk.Settings.ResetToDefault".Translate()))
		{
			settings.CustomInstruction = "";
			_textAreaBuffer = Constant.DefaultInstruction;
		}
		listingStandard.Gap(10f);
	}

	private void DrawSimpleApiSettings(Listing_Standard listingStandard)
	{
		RimTalkSettings settings = Get();
		listingStandard.Label("RimTalk.Settings.GoogleApiKeyLabel".Translate());
		Rect rowRect = listingStandard.GetRect(30f);
		rowRect.width -= 155f;
		settings.SimpleApiKey = Widgets.TextField(rowRect, settings.SimpleApiKey);
		Rect buttonRect = new Rect(rowRect.xMax + 5f, rowRect.y, 150f, rowRect.height);
		if (Widgets.ButtonText(buttonRect, "RimTalk.Settings.GetFreeApiKeyButton".Translate()))
		{
			Application.OpenURL("https://aistudio.google.com/app/apikey");
		}
		Text.Font = GameFont.Tiny;
		GUI.color = Color.gray;
		Rect cloudDescRect = listingStandard.GetRect(Text.LineHeight);
		Widgets.Label(cloudDescRect, "RimTalk.Settings.GoogleApiKeyDesc".Translate());
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		listingStandard.Gap();
		Rect advancedButtonRect = listingStandard.GetRect(30f);
		if (Widgets.ButtonText(advancedButtonRect, "RimTalk.Settings.SwitchToAdvancedSettings".Translate()))
		{
			settings.UseSimpleConfig = false;
		}
	}

	private void DrawAdvancedApiSettings(Listing_Standard listingStandard)
	{
		RimTalkSettings settings = Get();
		Rect simpleButtonRect = listingStandard.GetRect(30f);
		if (Widgets.ButtonText(simpleButtonRect, "RimTalk.Settings.SwitchToSimpleSettings".Translate()))
		{
			if (string.IsNullOrWhiteSpace(settings.SimpleApiKey))
			{
				ApiConfig firstValidCloudConfig = settings.CloudConfigs.FirstOrDefault((ApiConfig c) => c.IsValid());
				if (firstValidCloudConfig != null)
				{
					settings.SimpleApiKey = firstValidCloudConfig.ApiKey;
				}
			}
			settings.UseSimpleConfig = true;
		}
		listingStandard.Gap();
		Rect radioRect1 = listingStandard.GetRect(24f);
		if (Widgets.RadioButtonLabeled(radioRect1, "RimTalk.Settings.CloudProviders".Translate(), settings.UseCloudProviders))
		{
			settings.UseCloudProviders = true;
		}
		Text.Font = GameFont.Tiny;
		GUI.color = Color.gray;
		Rect cloudDescRect = listingStandard.GetRect(Text.LineHeight);
		Widgets.Label(cloudDescRect, "RimTalk.Settings.CloudProvidersDesc".Translate());
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		listingStandard.Gap(3f);
		Rect radioRect2 = listingStandard.GetRect(24f);
		if (Widgets.RadioButtonLabeled(radioRect2, "RimTalk.Settings.LocalProvider".Translate(), !settings.UseCloudProviders))
		{
			settings.UseCloudProviders = false;
			settings.LocalConfig.Provider = AIProvider.Local;
		}
		Text.Font = GameFont.Tiny;
		GUI.color = Color.gray;
		Rect localDescRect = listingStandard.GetRect(Text.LineHeight);
		Widgets.Label(localDescRect, "RimTalk.Settings.LocalProviderDesc".Translate());
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		listingStandard.Gap();
		if (settings.UseCloudProviders)
		{
			DrawCloudProvidersSection(listingStandard, settings);
		}
		else
		{
			DrawLocalProviderSection(listingStandard, settings);
		}
	}

	private void DrawCloudProvidersSection(Listing_Standard listingStandard, RimTalkSettings settings)
	{
		Rect headerRect = listingStandard.GetRect(24f);
		float addBtnSize = 24f;
		Rect addButtonRect = new Rect(headerRect.x + headerRect.width - addBtnSize, headerRect.y, addBtnSize, addBtnSize);
		headerRect.width -= addBtnSize + 5f;
		Widgets.Label(headerRect, "RimTalk.Settings.CloudApiConfigurations".Translate());
		Text.Font = GameFont.Tiny;
		GUI.color = Color.gray;
		Rect cloudDescRect = listingStandard.GetRect(Text.LineHeight * 2f);
		cloudDescRect.width -= 35f;
		Widgets.Label(cloudDescRect, "RimTalk.Settings.CloudApiConfigurationsDesc".Translate());
		GUI.color = Color.white;
		Color prevColor = GUI.color;
		GUI.color = new Color(0.3f, 0.9f, 0.3f);
		if (Widgets.ButtonText(addButtonRect, "+"))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			settings.CloudConfigs.Add(new ApiConfig());
		}
		GUI.color = prevColor;
		listingStandard.Gap(6f);
		Rect tableHeaderRect = listingStandard.GetRect(20f);
		float x = tableHeaderRect.x;
		float y = tableHeaderRect.y;
		float height = tableHeaderRect.height;
		float totalWidth = tableHeaderRect.width;
		float providerWidth = 90f;
		float modelWidth = 190f;
		float controlsWidth = 100f;
		Rect providerHeaderRect = new Rect(x, y, providerWidth, height);
		Widgets.Label(providerHeaderRect, "RimTalk.Settings.ProviderHeader".Translate());
		float middleStartX = x + providerWidth + 5f;
		Rect apiKeyHeaderRect = new Rect(middleStartX, y, 200f, height);
		Widgets.Label(apiKeyHeaderRect, "RimTalk.Settings.ApiKeyHeader".Translate());
		Rect modelHeaderRect = new Rect(totalWidth - controlsWidth - modelWidth - 5f, y, modelWidth, height);
		Widgets.Label(modelHeaderRect, "RimTalk.Settings.ModelHeader".Translate());
		Rect enabledHeaderRect = new Rect(totalWidth - controlsWidth + 5f, y, controlsWidth, height);
		Widgets.Label(enabledHeaderRect, "RimTalk.Settings.EnabledHeader".Translate());
		listingStandard.Gap(3f);
		for (int i = 0; i < settings.CloudConfigs.Count; i++)
		{
			if (DrawCloudConfigRow(listingStandard, settings.CloudConfigs[i], i, settings.CloudConfigs))
			{
				settings.CloudConfigs.RemoveAt(i);
				i--;
			}
			listingStandard.Gap(2f);
		}
		Text.Font = GameFont.Small;
	}

	private bool DrawCloudConfigRow(Listing_Standard listingStandard, ApiConfig config, int index, List<ApiConfig> configs)
	{
		Text.Font = GameFont.Tiny;
		Rect rowRect = listingStandard.GetRect(22f);
		float x = rowRect.x;
		float y = rowRect.y;
		float height = rowRect.height;
		float totalWidth = rowRect.width;
		float providerWidth = 90f;
		float modelWidth = 190f;
		float controlsWidth = 100f;
		float gap = 5f;
		float middleZoneWidth = totalWidth - providerWidth - modelWidth - controlsWidth - gap * 3f;
		float middleStartX = x + providerWidth + gap;
		Color originalColor = GUI.color;
		if (!config.IsEnabled)
		{
			GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
		}
		DrawProviderDropdown(x, y, height, providerWidth, config);
		if (config.Provider == AIProvider.Custom)
		{
			float keyWidth = middleZoneWidth * 0.4f - gap / 2f;
			float urlWidth = middleZoneWidth * 0.6f - gap / 2f;
			DrawApiKeyInput(middleStartX, y, height, keyWidth, config);
			DrawBaseUrlInput(middleStartX + keyWidth + gap, y, height, urlWidth, config);
		}
		else
		{
			DrawApiKeyInput(middleStartX, y, height, middleZoneWidth, config);
		}
		float modelStartX = middleStartX + middleZoneWidth + gap;
		if (config.Provider == AIProvider.Custom)
		{
			DrawCustomModelInput(modelStartX, y, height, modelWidth, config);
		}
		else
		{
			DrawDefaultModelSelector(modelStartX, y, height, modelWidth, config);
		}
		GUI.color = originalColor;
		float btnSize = 22f;
		float btnGap = 2f;
		float deleteX = totalWidth - btnSize;
		float downX = deleteX - btnGap - btnSize;
		float upX = downX - btnGap - btnSize;
		float controlsStartX = totalWidth - controlsWidth;
		float checkboxSpaceWidth = upX - controlsStartX;
		float checkboxX = controlsStartX + (checkboxSpaceWidth - 24f) / 2f;
		Rect toggleRect = new Rect(checkboxX, y, 24f, height);
		Widgets.Checkbox(new Vector2(toggleRect.x, toggleRect.y), ref config.IsEnabled, 20f);
		if (Mouse.IsOver(toggleRect))
		{
			TooltipHandler.TipRegion(toggleRect, "Enable/Disable");
		}
		DrawReorderButtons(upX, y, height, index, configs);
		Rect deleteRect = new Rect(deleteX, y, btnSize, height);
		bool deleteClicked = false;
		bool canDelete = configs.Count > 1;
		Color prevColor = GUI.color;
		if (canDelete)
		{
			GUI.color = new Color(1f, 0.4f, 0.4f);
		}
		else
		{
			GUI.color = Color.gray;
		}
		if (Widgets.ButtonText(deleteRect, "×", drawBackground: true, doMouseoverSound: true, canDelete))
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			deleteClicked = true;
		}
		GUI.color = prevColor;
		Text.Font = GameFont.Tiny;
		return deleteClicked;
	}

	private void DrawReorderButtons(float x, float y, float height, int index, List<ApiConfig> configs)
	{
		float btnSize = 22f;
		Rect upButtonRect = new Rect(x, y, btnSize, height);
		if (Widgets.ButtonText(upButtonRect, "▲") && index > 0)
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			List<ApiConfig> list = configs;
			int index2 = index;
			int index3 = index - 1;
			ApiConfig value = configs[index - 1];
			ApiConfig value2 = configs[index];
			list[index2] = value;
			configs[index3] = value2;
		}
		Rect downButtonRect = new Rect(x + btnSize + 2f, y, btnSize, height);
		if (Widgets.ButtonText(downButtonRect, "▼") && index < configs.Count - 1)
		{
			SoundDefOf.Click.PlayOneShotOnCamera();
			List<ApiConfig> list = configs;
			int index3 = index;
			int index2 = index + 1;
			ApiConfig value2 = configs[index + 1];
			ApiConfig value = configs[index];
			list[index3] = value2;
			configs[index2] = value;
		}
	}

	private void DrawDefaultModelSelector(float x, float y, float height, float width, ApiConfig config)
	{
		Rect modelRect = new Rect(x, y, width, height);
		if (config.SelectedModel == "Custom")
		{
			float xButtonWidth = 22f;
			float textFieldWidth = width - xButtonWidth - 2f;
			Rect textFieldRect = new Rect(x, y, textFieldWidth, height);
			Rect backButtonRect = new Rect(x + textFieldWidth + 2f, y, xButtonWidth, height);
			config.CustomModelName = DrawTextFieldWithPlaceholder(textFieldRect, config.CustomModelName, "Model ID");
			if (Widgets.ButtonText(backButtonRect, "×"))
			{
				SoundDefOf.Click.PlayOneShotOnCamera();
				config.SelectedModel = "(choose model)";
			}
		}
		else if (Widgets.ButtonText(modelRect, config.SelectedModel))
		{
			ShowModelSelectionMenu(config);
		}
	}

	private string DrawTextFieldWithPlaceholder(Rect rect, string text, string placeholder)
	{
		string result = Widgets.TextField(rect, text);
		if (string.IsNullOrEmpty(result))
		{
			TextAnchor originalAnchor = Text.Anchor;
			Color originalColor = GUI.color;
			Text.Anchor = TextAnchor.MiddleLeft;
			GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
			Rect labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 5f, rect.height);
			Widgets.Label(labelRect, placeholder);
			GUI.color = originalColor;
			Text.Anchor = originalAnchor;
		}
		return result;
	}

	private void DrawProviderDropdown(float x, float y, float height, float width, ApiConfig config)
	{
		Rect providerRect = new Rect(x, y, width, height);
		if (!Widgets.ButtonText(providerRect, config.Provider.GetLabel()))
		{
			return;
		}
		List<FloatMenuOption> providerOptions = new List<FloatMenuOption>();
		foreach (AIProvider provider in Enum.GetValues(typeof(AIProvider)))
		{
			AIProvider aIProvider = provider;
			if ((aIProvider == AIProvider.Local || aIProvider == AIProvider.None) ? true : false)
			{
				continue;
			}
			providerOptions.Add(new FloatMenuOption(provider.GetLabel(), delegate
			{
				config.Provider = provider;
				switch (provider)
				{
				case AIProvider.Player2:
					config.SelectedModel = "Default";
					Player2Client.CheckPlayer2StatusAndNotify();
					break;
				case AIProvider.Custom:
					config.SelectedModel = "Custom";
					break;
				default:
					config.SelectedModel = "(choose model)";
					break;
				}
			}));
		}
		Find.WindowStack.Add(new FloatMenu(providerOptions));
	}

	private void DrawApiKeyInput(float x, float y, float height, float width, ApiConfig config)
	{
		Rect apiKeyRect = new Rect(x, y, width, height);
		config.ApiKey = DrawTextFieldWithPlaceholder(apiKeyRect, config.ApiKey, "Paste API Key...");
	}

	private void DrawBaseUrlInput(float x, float y, float height, float width, ApiConfig config)
	{
		Rect baseUrlRect = new Rect(x, y, width, height);
		config.BaseUrl = DrawTextFieldWithPlaceholder(baseUrlRect, config.BaseUrl, "https://...");
		if (Mouse.IsOver(baseUrlRect))
		{
			TooltipHandler.TipRegion(baseUrlRect, "RimTalk_Settings_Api_BaseUrlInfo".Translate());
		}
	}

	private void DrawCustomModelInput(float x, float y, float height, float width, ApiConfig config)
	{
		Rect customModelRect = new Rect(x, y, width, height);
		config.CustomModelName = DrawTextFieldWithPlaceholder(customModelRect, config.CustomModelName, "Model ID");
		config.SelectedModel = (string.IsNullOrWhiteSpace(config.CustomModelName) ? "(choose model)" : config.CustomModelName);
	}

	private void ShowModelSelectionMenu(ApiConfig config)
	{
		if (string.IsNullOrWhiteSpace(config.ApiKey) && config.Provider != AIProvider.Player2)
		{
			Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>(1)
			{
				new FloatMenuOption("RimTalk.Settings.EnterApiKey".Translate(), null)
			}));
			return;
		}
		if (config.Provider == AIProvider.Player2)
		{
			config.SelectedModel = "Default";
			return;
		}
		string url = config.Provider.GetListModelsUrl();
		if (string.IsNullOrEmpty(url))
		{
			return;
		}
		if (ModelCache.ContainsKey(url))
		{
			OpenMenu(ModelCache[url]);
			return;
		}
		Task<List<string>> fetchTask = ((config.Provider == AIProvider.Google) ? GeminiClient.FetchModelsAsync(config.ApiKey, url) : OpenAIClient.FetchModelsAsync(config.ApiKey, url));
		fetchTask.ContinueWith(delegate(Task<List<string>> task)
		{
			List<string> result = task.Result;
			if (result != null && result.Any())
			{
				ModelCache[url] = result;
			}
			OpenMenu(result);
		}, TaskScheduler.FromCurrentSynchronizationContext());
		void OpenMenu(List<string> models)
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();
			if (models != null && models.Any())
			{
				options.AddRange(models.Select((string model) => new FloatMenuOption(model, delegate
				{
					config.SelectedModel = model;
				})));
			}
			else
			{
				options.Add(new FloatMenuOption("(no models found - check API Key)", null));
			}
			options.Add(new FloatMenuOption("Custom", delegate
			{
				config.SelectedModel = "Custom";
			}));
			Find.WindowStack.Add(new FloatMenu(options));
		}
	}

	private void DrawEnableToggle(Rect rowRect, float y, float height, ApiConfig config)
	{
		Rect toggleRect = new Rect(rowRect.xMax - 70f, y, 24f, height);
		Widgets.Checkbox(new Vector2(toggleRect.x, toggleRect.y), ref config.IsEnabled);
		if (Mouse.IsOver(toggleRect))
		{
			TooltipHandler.TipRegion(toggleRect, "RimTalk.Settings.EnableDisableApiConfigTooltip".Translate());
		}
	}

	private void DrawLocalProviderSection(Listing_Standard listingStandard, RimTalkSettings settings)
	{
		listingStandard.Label("RimTalk.Settings.LocalProviderConfiguration".Translate());
		listingStandard.Gap(6f);
		if (settings.LocalConfig == null)
		{
			settings.LocalConfig = new ApiConfig
			{
				Provider = AIProvider.Local
			};
		}
		DrawLocalConfigRow(listingStandard, settings.LocalConfig);
	}

	private void DrawLocalConfigRow(Listing_Standard listingStandard, ApiConfig config)
	{
		Rect rowRect = listingStandard.GetRect(24f);
		float x = rowRect.x;
		float y = rowRect.y;
		float height = rowRect.height;
		Rect baseUrlLabelRect = new Rect(x, y, 80f, height);
		TaggedString labelText = "RimTalk.Settings.BaseUrlLabel".Translate() + " [?]";
		Widgets.Label(baseUrlLabelRect, labelText);
		TooltipHandler.TipRegion(baseUrlLabelRect, "RimTalk_Settings_Api_BaseUrlInfo".Translate());
		x += 85f;
		Rect urlRect = new Rect(x, y, 250f, height);
		config.BaseUrl = Widgets.TextField(urlRect, config.BaseUrl);
		x += 285f;
		Rect modelLabelRect = new Rect(x, y, 70f, height);
		Widgets.Label(modelLabelRect, "RimTalk.Settings.ModelLabel".Translate());
		x += 75f;
		Rect modelRect = new Rect(x, y, 200f, height);
		config.CustomModelName = Widgets.TextField(modelRect, config.CustomModelName);
	}

	private string GetFormattedSpeedLabel(TimeSpeed speed)
	{
		return speed switch
		{
			TimeSpeed.Normal => "1x", 
			TimeSpeed.Fast => "2x", 
			TimeSpeed.Superfast => "3x", 
			TimeSpeed.Ultrafast => "4x", 
			_ => speed.ToString(), 
		};
	}

	private string GetPlayerDialogueModeLabel(PlayerDialogueMode mode)
	{
		return mode switch
		{
			PlayerDialogueMode.Disabled => "RimTalk.Settings.Disabled".Translate().ToString(), 
			PlayerDialogueMode.Manual => "RimTalk.Settings.PlayerDialogueMode.Manual".Translate().ToString(), 
			PlayerDialogueMode.AIDriven => "RimTalk.Settings.PlayerDialogueMode.AIDriven".Translate().ToString(), 
			_ => mode.ToString(), 
		};
	}

	private void DrawBasicSettings(Listing_Standard listingStandard)
	{
		RimTalkSettings settings = Get();
		if (!settings.UseSimpleConfig)
		{
			DrawAdvancedApiSettings(listingStandard);
		}
		else
		{
			DrawSimpleApiSettings(listingStandard);
		}
		listingStandard.Gap(30f);
		string cooldownLabel = "RimTalk.Settings.AICooldown".Translate(settings.TalkInterval).ToString();
		Rect cooldownLabelRect = listingStandard.GetRect(Text.CalcHeight(cooldownLabel, listingStandard.ColumnWidth));
		Widgets.Label(cooldownLabelRect, cooldownLabel);
		settings.TalkInterval = (int)listingStandard.Slider(settings.TalkInterval, 1f, 60f);
		listingStandard.Gap(6f);
		float columnWidth = (listingStandard.ColumnWidth - 200f) / 2f;
		float estimatedHeight = (settings.AllowCustomConversation ? 340f : 240f);
		Rect checkboxSectionRect = listingStandard.GetRect(estimatedHeight);
		Rect leftColumnRect = new Rect(checkboxSectionRect.x, checkboxSectionRect.y, columnWidth, checkboxSectionRect.height);
		Listing_Standard leftListing = new Listing_Standard();
		leftListing.Begin(leftColumnRect);
		leftListing.CheckboxLabeled("RimTalk.Settings.OverrideInteractions".Translate().ToString(), ref settings.ProcessNonRimTalkInteractions, "RimTalk.Settings.OverrideInteractionsTooltip".Translate().ToString());
		leftListing.Gap(6f);
		leftListing.CheckboxLabeled("RimTalk.Settings.AllowSimultaneousConversations".Translate().ToString(), ref settings.AllowSimultaneousConversations, "RimTalk.Settings.AllowSimultaneousConversationsTooltip".Translate().ToString());
		leftListing.Gap(6f);
		leftListing.CheckboxLabeled("RimTalk.Settings.DisplayTalkWhenDrafted".Translate().ToString(), ref settings.DisplayTalkWhenDrafted, "RimTalk.Settings.DisplayTalkWhenDraftedTooltip".Translate().ToString());
		leftListing.Gap(6f);
		leftListing.CheckboxLabeled("RimTalk.Settings.ContinueDialogueWhileSleeping".Translate().ToString(), ref settings.ContinueDialogueWhileSleeping, "RimTalk.Settings.ContinueDialogueWhileSleepingTooltip".Translate().ToString());
		leftListing.Gap(6f);
		leftListing.CheckboxLabeled("RimTalk.Settings.ApplyMoodAndSocialEffects".Translate().ToString(), ref settings.ApplyMoodAndSocialEffects, "RimTalk.Settings.ApplyMoodAndSocialEffectsTooltip".Translate().ToString());
		leftListing.Gap(6f);
		leftListing.CheckboxLabeled("RimTalk.Settings.AllowCustomConversation".Translate().ToString(), ref settings.AllowCustomConversation, "RimTalk.Settings.AllowCustomConversationTooltip".Translate().ToString());
		if (settings.AllowCustomConversation)
		{
			leftListing.Gap(6f);
			DrawCustomConversationOptions(leftListing, settings);
		}
		leftListing.End();
		Rect rightColumnRect = new Rect(leftColumnRect.xMax + 200f, checkboxSectionRect.y, columnWidth, checkboxSectionRect.height);
		Listing_Standard rightListing = new Listing_Standard();
		rightListing.Begin(rightColumnRect);
		rightListing.CheckboxLabeled("RimTalk.Settings.AllowMonologue".Translate().ToString(), ref settings.AllowMonologue, "RimTalk.Settings.AllowMonologueTooltip".Translate().ToString());
		rightListing.Gap(6f);
		rightListing.CheckboxLabeled("RimTalk.Settings.AllowSlavesToTalk".Translate().ToString(), ref settings.AllowSlavesToTalk, "RimTalk.Settings.AllowSlavesToTalkTooltip".Translate().ToString());
		rightListing.Gap(6f);
		rightListing.CheckboxLabeled("RimTalk.Settings.AllowPrisonersToTalk".Translate().ToString(), ref settings.AllowPrisonersToTalk, "RimTalk.Settings.AllowPrisonersToTalkTooltip".Translate().ToString());
		rightListing.Gap(6f);
		rightListing.CheckboxLabeled("RimTalk.Settings.AllowOtherFactionsToTalk".Translate().ToString(), ref settings.AllowOtherFactionsToTalk, "RimTalk.Settings.AllowOtherFactionsToTalkTooltip".Translate().ToString());
		rightListing.Gap(6f);
		rightListing.CheckboxLabeled("RimTalk.Settings.AllowEnemiesToTalk".Translate().ToString(), ref settings.AllowEnemiesToTalk, "RimTalk.Settings.AllowEnemiesToTalkTooltip".Translate().ToString());
		rightListing.Gap(6f);
		rightListing.CheckboxLabeled("RimTalk.Settings.AllowBabiesToTalk".Translate().ToString(), ref settings.AllowBabiesToTalk, "RimTalk.Settings.AllowBabiesToTalkTooltip".Translate().ToString());
		rightListing.Gap(6f);
		rightListing.CheckboxLabeled("RimTalk.Settings.AllowNonHumanToTalk".Translate().ToString(), ref settings.AllowNonHumanToTalk, "RimTalk.Settings.AllowNonHumanToTalkTooltip".Translate().ToString());
		rightListing.End();
		float tallerColumnHeight = Mathf.Max(leftListing.CurHeight, rightListing.CurHeight);
		listingStandard.Gap(tallerColumnHeight - estimatedHeight);
		listingStandard.Gap();
		Rect pauseLineRect = listingStandard.GetRect(30f);
		Rect labelRect = new Rect(pauseLineRect.x, pauseLineRect.y, pauseLineRect.width - 120f - 10f, pauseLineRect.height);
		TextAnchor originalAnchor = Text.Anchor;
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(labelRect, "RimTalk.Settings.PauseAtSpeed".Translate().ToString());
		Text.Anchor = originalAnchor;
		Rect dropdownRect = new Rect(labelRect.xMax + 10f, pauseLineRect.y, 120f, pauseLineRect.height);
		string currentSpeedLabel = ((settings.DisableAiAtSpeed > 1) ? GetFormattedSpeedLabel((TimeSpeed)settings.DisableAiAtSpeed) : "RimTalk.Settings.Disabled".Translate().ToString());
		if (Widgets.ButtonText(dropdownRect, currentSpeedLabel))
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>
			{
				new FloatMenuOption("RimTalk.Settings.Disabled".Translate().ToString(), delegate
				{
					settings.DisableAiAtSpeed = 0;
				})
			};
			foreach (TimeSpeed speed in Enum.GetValues(typeof(TimeSpeed)))
			{
				if ((int)speed > 1)
				{
					string label = GetFormattedSpeedLabel(speed);
					TimeSpeed currentSpeed = speed;
					options.Add(new FloatMenuOption(label, delegate
					{
						settings.DisableAiAtSpeed = (int)currentSpeed;
					}));
				}
			}
			Find.WindowStack.Add(new FloatMenu(options));
		}
		TooltipHandler.TipRegion(pauseLineRect, "RimTalk.Settings.DisableAiAtSpeedTooltip".Translate().ToString());
		listingStandard.Gap();
		Rect buttonDisplayRect = listingStandard.GetRect(30f);
		Rect buttonDisplayLabelRect = new Rect(buttonDisplayRect.x, buttonDisplayRect.y, buttonDisplayRect.width - 120f - 10f, buttonDisplayRect.height);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(buttonDisplayLabelRect, "RimTalk.Settings.ButtonDisplay".Translate().ToString());
		Text.Anchor = originalAnchor;
		Rect buttonDisplayDropdownRect = new Rect(buttonDisplayLabelRect.xMax + 10f, buttonDisplayRect.y, 120f, buttonDisplayRect.height);
		if (Widgets.ButtonText(buttonDisplayDropdownRect, settings.ButtonDisplay.ToString()))
		{
			List<FloatMenuOption> options2 = new List<FloatMenuOption>();
			foreach (ButtonDisplayMode mode in Enum.GetValues(typeof(ButtonDisplayMode)))
			{
				ButtonDisplayMode currentMode = mode;
				options2.Add(new FloatMenuOption(currentMode.ToString(), delegate
				{
					settings.ButtonDisplay = currentMode;
				}));
			}
			Find.WindowStack.Add(new FloatMenu(options2));
		}
		TooltipHandler.TipRegion(buttonDisplayRect, "RimTalk.Settings.ButtonDisplayTooltip".Translate().ToString());
		listingStandard.Gap(24f);
		if (listingStandard.ButtonText("RimTalk.Settings.ResetToDefault".Translate().ToString()))
		{
			settings.TalkInterval = 7;
			settings.ProcessNonRimTalkInteractions = true;
			settings.AllowSimultaneousConversations = false;
			settings.DisplayTalkWhenDrafted = true;
			settings.AllowMonologue = true;
			settings.AllowSlavesToTalk = true;
			settings.AllowPrisonersToTalk = true;
			settings.AllowOtherFactionsToTalk = false;
			settings.AllowEnemiesToTalk = false;
			settings.AllowBabiesToTalk = true;
			settings.AllowNonHumanToTalk = true;
			settings.AllowCustomConversation = true;
			settings.PlayerDialogueMode = PlayerDialogueMode.Manual;
			settings.PlayerName = "Player";
			settings.ContinueDialogueWhileSleeping = false;
			settings.ApplyMoodAndSocialEffects = false;
			settings.UseSimpleConfig = true;
			settings.DisableAiAtSpeed = 0;
			settings.ButtonDisplay = ButtonDisplayMode.Toggle;
		}
	}

	private void DrawCustomConversationOptions(Listing_Standard listingStandard, RimTalkSettings settings)
	{
		Rect playerDialogueRect = listingStandard.GetRect(24f);
		playerDialogueRect.x += 30f;
		playerDialogueRect.width -= 30f;
		float labelWidth = playerDialogueRect.width - 120f - 10f;
		Rect playerToNpcRect = new Rect(playerDialogueRect.x, playerDialogueRect.y, labelWidth, playerDialogueRect.height);
		Rect playerDialogueDropdownRect = new Rect(playerToNpcRect.xMax + 10f, playerDialogueRect.y, 120f, playerDialogueRect.height);
		TextAnchor savedAnchor = Text.Anchor;
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(playerToNpcRect, "RimTalk.Settings.PlayerToNpc".Translate().ToString());
		Text.Anchor = savedAnchor;
		string currentModeLabel = GetPlayerDialogueModeLabel(settings.PlayerDialogueMode);
		if (Widgets.ButtonText(playerDialogueDropdownRect, currentModeLabel))
		{
			List<FloatMenuOption> options = (from PlayerDialogueMode currentMode in Enum.GetValues(typeof(PlayerDialogueMode))
				select new FloatMenuOption(GetPlayerDialogueModeLabel(currentMode), delegate
				{
					settings.PlayerDialogueMode = currentMode;
				})).ToList();
			Find.WindowStack.Add(new FloatMenu(options));
		}
		TooltipHandler.TipRegion(playerDialogueRect, "RimTalk.Settings.PlayerDialogueModeTooltip".Translate().ToString());
		bool isPlayerDialogueEnabled = settings.PlayerDialogueMode != PlayerDialogueMode.Disabled;
		Rect playerNameRect = listingStandard.GetRect(30f);
		playerNameRect.x += 30f;
		playerNameRect.width -= 30f;
		float nameFieldWidth = 120f;
		float nameLabelWidth = playerNameRect.width - nameFieldWidth - 10f;
		Rect playerNameLabelRect = new Rect(playerNameRect.x, playerNameRect.y, nameLabelWidth, playerNameRect.height);
		Rect playerNameFieldRect = new Rect(playerNameLabelRect.xMax + 10f, playerNameRect.y + 3f, nameFieldWidth, 24f);
		Color savedColor = GUI.color;
		if (!isPlayerDialogueEnabled)
		{
			GUI.color = new Color(1f, 1f, 1f, 0.5f);
		}
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(playerNameLabelRect, "RimTalk.Settings.PlayerName".Translate().ToString());
		Text.Anchor = savedAnchor;
		if (isPlayerDialogueEnabled)
		{
			settings.PlayerName = Widgets.TextField(playerNameFieldRect, settings.PlayerName);
		}
		else
		{
			GUI.enabled = false;
			Widgets.TextField(playerNameFieldRect, settings.PlayerName);
			GUI.enabled = true;
		}
		GUI.color = savedColor;
		TooltipHandler.TipRegion(playerNameRect, "RimTalk.Settings.PlayerNameTooltip".Translate().ToString());
	}

	private void DrawContextFilterSettings(Listing_Standard listing)
	{
		RimTalkSettings settings = Get();
		ContextSettings context = settings.Context;
		if (!_presetInitialized)
		{
			DetermineCurrentPreset(context);
			_presetInitialized = true;
		}
		TaggedString contextFilterDesc = "RimTalk.Settings.ContextFilterDescription".Translate();
		Widgets.Label(listing.GetRect(Text.CalcHeight(contextFilterDesc, listing.ColumnWidth)), contextFilterDesc);
		listing.Gap(6f);
		Text.Font = GameFont.Tiny;
		GUI.color = Color.cyan;
		Widgets.Label(listing.GetRect(Text.LineHeight), "RimTalk.Settings.ContextFilterTip".Translate());
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		listing.Gap();
		DrawPresetSelector(listing, context);
		CopyFields(context, _changeBuffer);
		Text.Font = GameFont.Small;
		GUI.color = new Color(1f, 0.85f, 0.5f);
		listing.Label("RimTalk.Settings.ContextOptions".Translate());
		GUI.color = Color.white;
		listing.Gap(6f);
		listing.CheckboxLabeled("RimTalk.Settings.EnableContextOptimization".Translate(), ref context.EnableContextOptimization, "RimTalk.Settings.EnableContextOptimization.Tooltip".Translate());
		listing.Gap(6f);
		DrawDropdown(listing, "RimTalk.Settings.MaxPawnContextCount", context.MaxPawnContextCount, delegate(int val)
		{
			context.MaxPawnContextCount = val;
			_currentPreset = ContextPreset.Custom;
		}, 2, 7);
		listing.Gap(6f);
		DrawDropdown(listing, "RimTalk.Settings.ConversationHistoryCount", context.ConversationHistoryCount, delegate(int val)
		{
			context.ConversationHistoryCount = val;
			_currentPreset = ContextPreset.Custom;
		}, 0, 7);
		listing.Gap();
		DrawColumns(listing, context);
		if (_currentPreset != ContextPreset.Custom && !AreSettingsEqual(_changeBuffer, context))
		{
			_currentPreset = ContextPreset.Custom;
		}
		listing.Gap(24f);
		if (listing.ButtonText("RimTalk.Settings.ResetToDefault".Translate()))
		{
			settings.Context = new ContextSettings();
			ApplyPreset(settings.Context, ContextPreset.Standard);
		}
	}

	private void DrawPresetSelector(Listing_Standard listing, ContextSettings context)
	{
		GUI.color = new Color(1f, 0.85f, 0.5f);
		Widgets.Label(listing.GetRect(Text.LineHeight), "RimTalk.Settings.ContextPresets".Translate());
		GUI.color = Color.white;
		listing.Gap(8f);
		float totalWidth = listing.ColumnWidth;
		float boxWidth = (totalWidth - 36f) / 4f;
		Rect rowRect = listing.GetRect(70f);
		int i = 0;
		foreach (ContextPreset preset in Enum.GetValues(typeof(ContextPreset)))
		{
			Rect boxRect = new Rect(rowRect.x + (boxWidth + 12f) * (float)i, rowRect.y, boxWidth, 70f);
			DrawSinglePresetBox(boxRect, preset, context);
			i++;
		}
		listing.Gap();
	}

	private void DrawSinglePresetBox(Rect rect, ContextPreset preset, ContextSettings context)
	{
		bool isSelected = _currentPreset == preset;
		Widgets.DrawBoxSolid(rect, isSelected ? new Color(0.2f, 0.4f, 0.6f, 0.8f) : new Color(0.2f, 0.2f, 0.2f, 0.5f));
		GUI.color = (isSelected ? new Color(0.4f, 0.7f, 1f, 1f) : new Color(0.3f, 0.3f, 0.3f, 0.5f));
		Widgets.DrawBox(rect, 2);
		GUI.color = Color.white;
		if (Mouse.IsOver(rect))
		{
			Widgets.DrawHighlight(rect);
		}
		if (Widgets.ButtonInvisible(rect))
		{
			_currentPreset = preset;
			if (preset != ContextPreset.Custom)
			{
				ApplyPreset(context, preset);
			}
		}
		Rect content = rect.ContractedBy(8f);
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperCenter;
		GUI.color = (isSelected ? Color.white : new Color(0.8f, 0.8f, 0.8f));
		Widgets.Label(new Rect(content.x, content.y, content.width, Text.LineHeight), $"RimTalk.Settings.Preset.{preset}".Translate());
		Text.Font = GameFont.Tiny;
		GUI.color = (isSelected ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.6f, 0.6f, 0.6f));
		Widgets.Label(new Rect(content.x, content.y + Text.LineHeight + 4f, content.width, content.height - Text.LineHeight - 4f), $"RimTalk.Settings.Preset.{preset}.Desc".Translate());
		Text.Anchor = TextAnchor.UpperLeft;
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
	}

	private void DrawColumns(Listing_Standard listing, ContextSettings context)
	{
		float columnWidth = (listing.ColumnWidth - 200f) / 2f;
		Rect positionRect = listing.GetRect(0f);
		Rect leftRect = new Rect(positionRect.x, positionRect.y, columnWidth, 9999f);
		Listing_Standard leftListing = new Listing_Standard();
		leftListing.Begin(leftRect);
		Text.Font = GameFont.Small;
		GUI.color = Color.yellow;
		leftListing.Label(string.Format("━━ {0} ━━", "RimTalk.Settings.PawnInfo".Translate()));
		GUI.color = Color.white;
		leftListing.Gap(6f);
		leftListing.CheckboxLabeled("RimTalk.Settings.IncludeRace".Translate(), ref context.IncludeRace);
		leftListing.CheckboxLabeled("RimTalk.Settings.IncludeNotableGenes".Translate(), ref context.IncludeNotableGenes);
		leftListing.CheckboxLabeled("RimTalk.Settings.IncludeIdeology".Translate(), ref context.IncludeIdeology);
		leftListing.CheckboxLabeled("RimTalk.Settings.IncludeBackstory".Translate(), ref context.IncludeBackstory);
		leftListing.CheckboxLabeled("RimTalk.Settings.IncludeTraits".Translate(), ref context.IncludeTraits);
		leftListing.CheckboxLabeled("RimTalk.Settings.IncludeSkills".Translate(), ref context.IncludeSkills);
		leftListing.CheckboxLabeled("RimTalk.Settings.IncludeHealth".Translate(), ref context.IncludeHealth);
		leftListing.CheckboxLabeled("RimTalk.Settings.IncludeMood".Translate(), ref context.IncludeMood);
		leftListing.CheckboxLabeled("RimTalk.Settings.IncludeThoughts".Translate(), ref context.IncludeThoughts);
		leftListing.CheckboxLabeled("RimTalk.Settings.IncludeRelations".Translate(), ref context.IncludeRelations);
		leftListing.CheckboxLabeled("RimTalk.Settings.IncludeEquipment".Translate(), ref context.IncludeEquipment);
		leftListing.CheckboxLabeled("RimTalk.Settings.IncludePrisonerSlaveStatus".Translate(), ref context.IncludePrisonerSlaveStatus);
		leftListing.End();
		Rect rightRect = new Rect(leftRect.xMax + 200f, positionRect.y, columnWidth, 9999f);
		Listing_Standard rightListing = new Listing_Standard();
		rightListing.Begin(rightRect);
		Text.Font = GameFont.Small;
		GUI.color = Color.yellow;
		rightListing.Label(string.Format("━━ {0} ━━", "RimTalk.Settings.Environment".Translate()));
		GUI.color = Color.white;
		rightListing.Gap(6f);
		rightListing.CheckboxLabeled("RimTalk.Settings.IncludeTime".Translate(), ref context.IncludeTime);
		rightListing.CheckboxLabeled("RimTalk.Settings.IncludeDate".Translate(), ref context.IncludeDate);
		rightListing.CheckboxLabeled("RimTalk.Settings.IncludeSeason".Translate(), ref context.IncludeSeason);
		rightListing.CheckboxLabeled("RimTalk.Settings.IncludeWeather".Translate(), ref context.IncludeWeather);
		rightListing.CheckboxLabeled("RimTalk.Settings.IncludeLocationAndTemperature".Translate(), ref context.IncludeLocationAndTemperature);
		rightListing.CheckboxLabeled("RimTalk.Settings.IncludeTerrain".Translate(), ref context.IncludeTerrain);
		rightListing.CheckboxLabeled("RimTalk.Settings.IncludeBeauty".Translate(), ref context.IncludeBeauty);
		rightListing.CheckboxLabeled("RimTalk.Settings.IncludeCleanliness".Translate(), ref context.IncludeCleanliness);
		rightListing.CheckboxLabeled("RimTalk.Settings.IncludeSurroundings".Translate(), ref context.IncludeSurroundings);
		rightListing.CheckboxLabeled("RimTalk.Settings.IncludeWealth".Translate(), ref context.IncludeWealth);
		rightListing.End();
		listing.Gap(Mathf.Max(leftListing.CurHeight, rightListing.CurHeight));
	}

	private void ApplyPreset(ContextSettings context, ContextPreset preset)
	{
		if (PresetDefinitions.TryGetValue(preset, out var source))
		{
			CopyFields(source, context);
			_currentPreset = preset;
		}
	}

	private void CopyFields<T>(T source, T target)
	{
		FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo field in fields)
		{
			field.SetValue(target, field.GetValue(source));
		}
	}

	private bool AreSettingsEqual(ContextSettings a, ContextSettings b)
	{
		FieldInfo[] fields = typeof(ContextSettings).GetFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo field in fields)
		{
			object valA = field.GetValue(a);
			object valB = field.GetValue(b);
			if (!object.Equals(valA, valB))
			{
				return false;
			}
		}
		return true;
	}

	private void DrawDropdown(Listing_Standard listing, string labelKey, int currentValue, Action<int> onSelect, int min, int max)
	{
		Rect rowRect = listing.GetRect(24f);
		Rect labelRect = new Rect(rowRect.x, rowRect.y, rowRect.width - 120f - 10f, rowRect.height);
		Rect dropdownRect = new Rect(rowRect.xMax - 120f, rowRect.y, 120f, rowRect.height);
		Widgets.Label(labelRect, labelKey.Translate());
		TooltipHandler.TipRegion(rowRect, (labelKey + ".Tooltip").Translate());
		if (!Widgets.ButtonText(dropdownRect, currentValue.ToString()))
		{
			return;
		}
		List<FloatMenuOption> options = new List<FloatMenuOption>();
		for (int i = min; i <= max; i++)
		{
			int count = i;
			options.Add(new FloatMenuOption(count.ToString(), delegate
			{
				onSelect(count);
			}));
		}
		Find.WindowStack.Add(new FloatMenu(options));
	}

	private void DetermineCurrentPreset(ContextSettings current)
	{
		_currentPreset = ContextPreset.Custom;
		foreach (KeyValuePair<ContextPreset, ContextSettings> entry in PresetDefinitions)
		{
			if (AreSettingsEqual(current, entry.Value))
			{
				_currentPreset = entry.Key;
				break;
			}
		}
	}

	private void DrawEventFilterSettings(Listing_Standard listingStandard)
	{
		RimTalkSettings settings = Get();
		Text.Font = GameFont.Tiny;
		GUI.color = Color.cyan;
		TaggedString eventFilterTip = "RimTalk.Settings.EventFilterTip".Translate();
		Rect eventFilterTipRect = listingStandard.GetRect(Text.CalcHeight(eventFilterTip, listingStandard.ColumnWidth));
		Widgets.Label(eventFilterTipRect, eventFilterTip);
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		listingStandard.Gap(6f);
		List<IGrouping<string, string>> groupedTypes = (from g in _discoveredArchivableTypes.GroupBy(delegate(string text2)
			{
				string text = (text2.Contains(".") ? text2.Substring(text2.LastIndexOf('.') + 1) : text2);
				if (text.Contains("Letter"))
				{
					return "Letters";
				}
				return text.Contains("Message") ? "Messages" : "Other";
			})
			orderby (!(g.Key == "Letters")) ? ((g.Key == "Messages") ? 1 : 2) : 0
			select g).ToList();
		if (_discoveredArchivableTypes.Any())
		{
			foreach (IGrouping<string, string> group in groupedTypes)
			{
				Text.Font = GameFont.Small;
				GUI.color = Color.yellow;
				string categoryHeader = $"━━ {group.Key} ({group.Count()}) ━━";
				Rect categoryHeaderRect = listingStandard.GetRect(Text.CalcHeight(categoryHeader, listingStandard.ColumnWidth));
				Widgets.Label(categoryHeaderRect, categoryHeader);
				GUI.color = Color.white;
				Text.Font = GameFont.Small;
				listingStandard.Gap(6f);
				foreach (string typeName in group.OrderBy((string x) => x))
				{
					bool isEnabled = settings.EnabledArchivableTypes.ContainsKey(typeName) && settings.EnabledArchivableTypes[typeName];
					bool newEnabled = isEnabled;
					if (typeName.Equals("Verse.Message", StringComparison.OrdinalIgnoreCase) && isEnabled)
					{
						GUI.color = Color.red;
					}
					listingStandard.CheckboxLabeled(typeName, ref newEnabled, typeName);
					GUI.color = Color.white;
					if (newEnabled != isEnabled)
					{
						settings.EnabledArchivableTypes[typeName] = newEnabled;
					}
				}
				listingStandard.Gap();
			}
		}
		else
		{
			Text.Font = GameFont.Tiny;
			GUI.color = Color.yellow;
			TaggedString noArchivableTypes = "RimTalk.Settings.NoArchivableTypes".Translate();
			Rect noArchivableTypesRect = listingStandard.GetRect(Text.CalcHeight(noArchivableTypes, listingStandard.ColumnWidth));
			Widgets.Label(noArchivableTypesRect, noArchivableTypes);
			GUI.color = Color.white;
			Text.Font = GameFont.Small;
		}
		listingStandard.Gap(6f);
		Rect resetButtonRect = listingStandard.GetRect(30f);
		if (!Widgets.ButtonText(resetButtonRect, "RimTalk.Settings.ResetToDefault".Translate()))
		{
			return;
		}
		foreach (string typeName2 in _discoveredArchivableTypes)
		{
			bool defaultEnabled = !typeName2.Equals("Verse.Message", StringComparison.OrdinalIgnoreCase);
			settings.EnabledArchivableTypes[typeName2] = defaultEnabled;
		}
	}

	private PresetSection GetSection(PromptEntry entry)
	{
		if (entry.IsMainChatHistory || entry.Position == PromptPosition.InChat)
		{
			return PresetSection.History;
		}
		if (entry.Role == PromptRole.System && entry.Position == PromptPosition.Relative)
		{
			return PresetSection.System;
		}
		return PresetSection.Prompt;
	}

	public void DrawPromptPresetSettings(Listing_Standard listingStandard, Rect inRect)
	{
		RimTalkSettings settings = Get();
		if (settings.UseAdvancedPromptMode)
		{
			DrawAdvancedPromptMode(listingStandard, settings, inRect);
		}
		else
		{
			DrawSimplePromptMode(listingStandard, settings);
		}
	}

	private void DrawSimplePromptMode(Listing_Standard listingStandard, RimTalkSettings settings)
	{
		DrawAIInstructionSettings(listingStandard, showAdvancedSwitch: true);
	}

	private void DrawAdvancedPromptMode(Listing_Standard listingStandard, RimTalkSettings settings, Rect containerRect)
	{
		PromptManager manager = PromptManager.Instance;
		if (string.IsNullOrEmpty(_selectedPresetId))
		{
			PromptPreset active = manager.Presets.FirstOrDefault((PromptPreset p) => p.IsActive) ?? manager.Presets.FirstOrDefault((PromptPreset p) => p.Name == "RimTalk Default") ?? manager.Presets.FirstOrDefault();
			if (active != null)
			{
				_selectedPresetId = active.Id;
				_selectedEntryId = active.Entries.FirstOrDefault()?.Id;
			}
		}
		float currentY = listingStandard.CurHeight;
		float availableHeight = Mathf.Max(300f, containerRect.height - currentY - 10f);
		Rect mainRect = listingStandard.GetRect(availableHeight);
		float leftPanelWidth = 200f;
		float panelGap = 4f;
		DrawPresetListPanel(new Rect(mainRect.x, mainRect.y, leftPanelWidth, mainRect.height), manager);
		Rect rightPanelRect = new Rect(mainRect.x + leftPanelWidth + panelGap, mainRect.y, mainRect.width - (leftPanelWidth + panelGap), mainRect.height);
		DrawEntryEditor(rightPanelRect, manager, settings);
	}

	private void DrawPresetListPanel(Rect rect, PromptManager manager)
	{
		Widgets.DrawBoxSolid(rect, LeftPanelBackground);
		float buttonSize = 20f;
		float listPaddingX = 2f;
		float scrollBarWidth = 16f;
		float listWidth = rect.width - listPaddingX * 2f;
		float viewWidth = listWidth - scrollBarWidth;
		float rowButtonX = viewWidth - buttonSize - 2f;
		float headerButtonX = listPaddingX + rowButtonX;
		float y = rect.y + 5f;
		Text.Font = GameFont.Tiny;
		GUI.color = Color.gray;
		Widgets.Label(new Rect(rect.x + 5f, y, rect.width - 35f, 20f), "RimTalk.Settings.PromptPreset.Presets".Translate());
		GUI.color = AddGreen;
		Rect addPresetRect = new Rect(rect.x + headerButtonX, y, buttonSize, buttonSize);
		if (Widgets.ButtonText(addPresetRect, "+"))
		{
			PromptPreset p = manager.CreateNewPreset("RimTalk.Settings.PromptPreset.NewPresetName".Translate());
			if (p != null)
			{
				_selectedPresetId = p.Id;
				_selectedEntryId = p.Entries.FirstOrDefault()?.Id;
			}
		}
		GUI.color = Color.white;
		TooltipHandler.TipRegion(addPresetRect, "RimTalk.Settings.PromptPreset.NewPreset".Translate());
		y += 22f;
		Text.Font = GameFont.Small;
		Rect listRect = new Rect(rect.x + listPaddingX, y, listWidth, 150f);
		Rect viewRect = new Rect(0f, 0f, viewWidth, (float)manager.Presets.Count * 25f);
		Widgets.BeginScrollView(listRect, ref _presetListScrollPos, viewRect);
		float py = 0f;
		for (int i = 0; i < manager.Presets.Count; i++)
		{
			PromptPreset p2 = manager.Presets[i];
			Rect row = new Rect(0f, py, viewRect.width, 24f);
			if (_selectedPresetId == p2.Id)
			{
				Widgets.DrawHighlight(row);
			}
			if (p2.IsActive)
			{
				GUI.color = Color.green;
				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(new Rect(4f, py, 16f, 24f), "▶");
				Text.Anchor = TextAnchor.UpperLeft;
				GUI.color = Color.white;
			}
			if (Widgets.ButtonText(new Rect(24f, py, viewRect.width - 48f, 24f), p2.Name, drawBackground: false))
			{
				_selectedPresetId = p2.Id;
				_selectedEntryId = p2.Entries.FirstOrDefault()?.Id;
			}
			if (p2.Name != "RimTalk Default" && manager.Presets.Count > 1)
			{
				Rect delRect = new Rect(rowButtonX, py + 2f, buttonSize, buttonSize);
				GUI.color = DeleteRed;
				if (Widgets.ButtonText(delRect, "×"))
				{
					manager.RemovePreset(p2.Id);
					if (_selectedPresetId == p2.Id)
					{
						PromptPreset next = manager.Presets.FirstOrDefault();
						_selectedPresetId = next?.Id;
						_selectedEntryId = next?.Entries.FirstOrDefault()?.Id;
					}
				}
				GUI.color = Color.white;
				TooltipHandler.TipRegion(delRect, "RimTalk.Settings.PromptPreset.Delete".Translate());
			}
			py += 25f;
		}
		Widgets.EndScrollView();
		y += 155f;
		PromptPreset sel = manager.Presets.FirstOrDefault((PromptPreset x) => x.Id == _selectedPresetId);
		if (sel != null)
		{
			float btnW2 = (rect.width - 15f) / 2f;
			bool isAlreadyActive = sel.IsActive;
			if (isAlreadyActive)
			{
				GUI.enabled = false;
			}
			if (Widgets.ButtonText(new Rect(rect.x + 5f, y, btnW2, 24f), "RimTalk.Settings.PromptPreset.Activate".Translate()))
			{
				manager.SetActivePreset(sel.Id);
			}
			if (isAlreadyActive)
			{
				GUI.enabled = true;
			}
			if (Widgets.ButtonText(new Rect(rect.x + 10f + btnW2, y, btnW2, 24f), "RimTalk.Settings.PromptPreset.Duplicate".Translate()))
			{
				PromptPreset c = manager.DuplicatePreset(sel.Id);
				if (c != null)
				{
					_selectedPresetId = c.Id;
					_selectedEntryId = c.Entries.FirstOrDefault()?.Id;
				}
			}
			y += 28f;
			if (Widgets.ButtonText(new Rect(rect.x + 5f, y, btnW2, 24f), "RimTalk.Settings.PromptPreset.Import".Translate()))
			{
				ShowImportMenu(manager);
			}
			if (Widgets.ButtonText(new Rect(rect.x + 10f + btnW2, y, btnW2, 24f), "RimTalk.Settings.PromptPreset.Export".Translate()))
			{
				if (PresetSerializer.ExportToFile(sel))
				{
					string exportDir = PresetSerializer.GetExportDirectory();
					Messages.Message("RimTalk.Settings.PromptPreset.ExportSuccess".Translate(exportDir), MessageTypeDefOf.PositiveEvent, historical: false);
				}
				else
				{
					Messages.Message("RimTalk.Settings.PromptPreset.ExportFailed".Translate(), MessageTypeDefOf.RejectInput, historical: false);
				}
			}
			y += 32f;
		}
		y += 5f;
		if (sel == null)
		{
			return;
		}
		float ey = 0f;
		PresetSection[] sections = new PresetSection[3]
		{
			PresetSection.System,
			PresetSection.History,
			PresetSection.Prompt
		};
		Dictionary<PresetSection, string> sectionNames = new Dictionary<PresetSection, string>
		{
			{
				PresetSection.System,
				"RimTalk.Settings.PromptPreset.SectionSystem".Translate()
			},
			{
				PresetSection.History,
				"RimTalk.Settings.PromptPreset.SectionHistory".Translate()
			},
			{
				PresetSection.Prompt,
				"RimTalk.Settings.PromptPreset.SectionPrompt".Translate()
			}
		};
		float totalHeight = 0f;
		PresetSection[] array = sections;
		foreach (PresetSection section in array)
		{
			totalHeight += 22f;
			totalHeight += (float)sel.Entries.Count((PromptEntry entry2) => GetSection(entry2) == section) * 25f;
		}
		Text.Font = GameFont.Small;
		Rect eListRect = new Rect(rect.x + listPaddingX, y, listWidth, rect.yMax - y - 35f);
		Rect eViewRect = new Rect(0f, 0f, viewWidth, totalHeight);
		Widgets.BeginScrollView(eListRect, ref _entryListScrollPos, eViewRect);
		PresetSection[] array2 = sections;
		foreach (PresetSection section2 in array2)
		{
			Rect headerRect = new Rect(0f, ey, eViewRect.width, 20f);
			Text.Font = GameFont.Tiny;
			GUI.color = Color.gray;
			Widgets.Label(new Rect(5f, ey, eViewRect.width - 30f, 20f), sectionNames[section2]);
			GUI.color = Color.white;
			GUI.color = AddGreen;
			Rect addRect = new Rect(rowButtonX, ey, buttonSize, buttonSize);
			if (Widgets.ButtonText(addRect, "+"))
			{
				PromptEntry newEntry = null;
				switch (section2)
				{
				case PresetSection.System:
					newEntry = new PromptEntry("RimTalk.Settings.PromptPreset.NewEntryName".Translate(), "");
					newEntry.Position = PromptPosition.Relative;
					break;
				case PresetSection.History:
					if (!sel.Entries.Any((PromptEntry promptEntry) => promptEntry.IsMainChatHistory))
					{
						newEntry = new PromptEntry("Chat History", "{{chat.history}}", PromptRole.User);
						newEntry.Position = PromptPosition.Relative;
						newEntry.IsMainChatHistory = true;
					}
					else
					{
						newEntry = new PromptEntry("RimTalk.Settings.PromptPreset.NewEntryName".Translate(), "", PromptRole.User);
						newEntry.Position = PromptPosition.InChat;
						newEntry.InChatDepth = 1;
					}
					break;
				case PresetSection.Prompt:
					newEntry = new PromptEntry("RimTalk.Settings.PromptPreset.NewEntryName".Translate(), "", PromptRole.User);
					newEntry.Position = PromptPosition.Relative;
					break;
				}
				if (newEntry != null)
				{
					int lastIndex = -1;
					for (int i2 = 0; i2 < sel.Entries.Count; i2++)
					{
						if (GetSection(sel.Entries[i2]) == section2)
						{
							lastIndex = i2;
						}
					}
					if (lastIndex == -1)
					{
						if (section2 == PresetSection.System)
						{
							sel.Entries.Insert(0, newEntry);
						}
						else if (section2 == PresetSection.History)
						{
							int sysEnd = sel.Entries.FindLastIndex((PromptEntry entry2) => GetSection(entry2) == PresetSection.System);
							sel.Entries.Insert(sysEnd + 1, newEntry);
						}
						else
						{
							sel.Entries.Add(newEntry);
						}
					}
					else
					{
						sel.Entries.Insert(lastIndex + 1, newEntry);
					}
					_selectedEntryId = newEntry.Id;
				}
			}
			GUI.color = Color.white;
			ey += 22f;
			Text.Font = GameFont.Small;
			List<PromptEntry> sectionEntries = sel.Entries.Where((PromptEntry entry2) => GetSection(entry2) == section2).ToList();
			for (int i3 = 0; i3 < sectionEntries.Count; i3++)
			{
				PromptEntry entry = sectionEntries[i3];
				Rect erow = new Rect(0f, ey, eViewRect.width, 24f);
				if (_selectedEntryId == entry.Id)
				{
					Widgets.DrawHighlight(erow);
				}
				bool isHistoryMarker = entry.IsMainChatHistory;
				bool en = entry.Enabled;
				Widgets.Checkbox(new Vector2(4f, ey + 4f), ref en, 16f);
				entry.Enabled = en;
				if (Widgets.ButtonText(new Rect(24f, ey, eViewRect.width - 48f, 24f), entry.Name, drawBackground: false))
				{
					_selectedEntryId = entry.Id;
				}
				if (!isHistoryMarker)
				{
					Rect edel = new Rect(rowButtonX, ey + 2f, buttonSize, buttonSize);
					GUI.color = DeleteRed;
					if (Widgets.ButtonText(edel, "×"))
					{
						sel.RemoveEntry(entry.Id);
						if (_selectedEntryId == entry.Id)
						{
							_selectedEntryId = sel.Entries.FirstOrDefault()?.Id;
						}
					}
					GUI.color = Color.white;
					TooltipHandler.TipRegion(edel, "RimTalk.Settings.PromptPreset.Delete".Translate());
				}
				ey += 25f;
			}
		}
		Widgets.EndScrollView();
		if (_selectedEntryId == null)
		{
			return;
		}
		PromptEntry selectedEntry = sel.GetEntry(_selectedEntryId);
		if (selectedEntry == null)
		{
			return;
		}
		PresetSection section3 = GetSection(selectedEntry);
		List<PromptEntry> sectionEntries2 = sel.Entries.Where((PromptEntry entry2) => GetSection(entry2) == section3).ToList();
		int indexInSection = sectionEntries2.IndexOf(selectedEntry);
		float sw = (rect.width - 15f) / 2f;
		bool canMoveUp = indexInSection > 0;
		if (canMoveUp && section3 == PresetSection.History)
		{
			PromptEntry prevEntry = sectionEntries2[indexInSection - 1];
			if (prevEntry.IsMainChatHistory)
			{
				canMoveUp = false;
			}
		}
		if (canMoveUp)
		{
			if (Widgets.ButtonText(new Rect(rect.x + 5f, rect.yMax - 32f, sw, 24f), "▲"))
			{
				int actualIndex = sel.Entries.IndexOf(selectedEntry);
				sel.Entries.RemoveAt(actualIndex);
				sel.Entries.Insert(actualIndex - 1, selectedEntry);
			}
		}
		else
		{
			GUI.enabled = false;
			Widgets.ButtonText(new Rect(rect.x + 5f, rect.yMax - 32f, sw, 24f), "▲");
			GUI.enabled = true;
		}
		if (indexInSection < sectionEntries2.Count - 1)
		{
			if (Widgets.ButtonText(new Rect(rect.x + 10f + sw, rect.yMax - 32f, sw, 24f), "▼"))
			{
				int actualIndex2 = sel.Entries.IndexOf(selectedEntry);
				sel.Entries.RemoveAt(actualIndex2);
				sel.Entries.Insert(actualIndex2 + 1, selectedEntry);
			}
		}
		else
		{
			GUI.enabled = false;
			Widgets.ButtonText(new Rect(rect.x + 10f + sw, rect.yMax - 32f, sw, 24f), "▼");
			GUI.enabled = true;
		}
	}

	private void DrawEntryEditor(Rect rect, PromptManager manager, RimTalkSettings settings)
	{
		//IL_090c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0913: Expected O, but got Unknown
		//IL_0a79: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b5b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bb8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bbe: Invalid comparison between Unknown and I4
		//IL_0c24: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2a: Invalid comparison between Unknown and I4
		//IL_0bfc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c02: Invalid comparison between Unknown and I4
		//IL_0c68: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c6e: Invalid comparison between Unknown and I4
		PromptPreset p = manager.Presets.FirstOrDefault((PromptPreset promptPreset) => promptPreset.Id == _selectedPresetId);
		if (p == null)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = Color.gray;
			Widgets.Label(rect, "RimTalk.Settings.PromptPreset.SelectEntryToEdit".Translate());
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			return;
		}
		float y = rect.y + 2f;
		float labelX = rect.x + 10f;
		float inputX = rect.x + 115f;
		float inputWidth = 200f;
		float topButtonWidth = 200f;
		float topButtonX = rect.x + rect.width - topButtonWidth - 10f;
		float dropdownWidth = 120f;
		Widgets.Label(new Rect(labelX, y, 100f, 24f), "RimTalk.Settings.PromptPreset.PresetName".Translate());
		p.Name = Widgets.TextField(new Rect(inputX, y, inputWidth, 24f), p.Name);
		if (Widgets.ButtonText(new Rect(topButtonX, y, topButtonWidth, 24f), "RimTalk.Settings.SwitchToSimpleSettings".Translate()))
		{
			settings.UseAdvancedPromptMode = false;
		}
		y += 28f;
		if (Widgets.ButtonText(new Rect(topButtonX, y, topButtonWidth, 24f), "RimTalk.Settings.ResetToDefault".Translate()))
		{
			Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("RimTalk.Settings.ResetConfirm".Translate(), delegate
			{
				manager.ResetToDefaults();
				_selectedPresetId = null;
				_selectedEntryId = null;
			}));
		}
		PromptEntry e = p.Entries.FirstOrDefault((PromptEntry x) => x.Id == _selectedEntryId);
		if (e == null)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			GUI.color = Color.gray;
			Widgets.Label(new Rect(rect.x, y + 28f, rect.width, rect.height - (y + 28f)), "RimTalk.Settings.PromptPreset.SelectEntryToEdit".Translate());
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			return;
		}
		bool isHistoryMarker = e.IsMainChatHistory;
		PresetSection section = GetSection(e);
		Widgets.Label(new Rect(labelX, y, 100f, 24f), "RimTalk.Settings.PromptPreset.EntryName".Translate());
		if (isHistoryMarker)
		{
			GUI.enabled = false;
			Widgets.TextField(new Rect(inputX, y, inputWidth, 24f), e.Name);
			GUI.enabled = true;
		}
		else
		{
			e.Name = Widgets.TextField(new Rect(inputX, y, inputWidth, 24f), e.Name);
		}
		y += 28f;
		Widgets.Label(new Rect(labelX, y, 80f, 24f), "RimTalk.Settings.PromptPreset.Role".Translate());
		if (section == PresetSection.System)
		{
			Widgets.Label(new Rect(inputX, y, dropdownWidth, 24f), e.Role.ToString());
		}
		else if (isHistoryMarker)
		{
			Widgets.Label(new Rect(inputX, y, dropdownWidth, 24f), "User/Assistant");
		}
		else if (Widgets.ButtonText(new Rect(inputX, y, dropdownWidth, 24f), e.Role.ToString()))
		{
			List<FloatMenuOption> opts = new List<FloatMenuOption>
			{
				new FloatMenuOption("User", delegate
				{
					e.Role = PromptRole.User;
				}),
				new FloatMenuOption("Assistant", delegate
				{
					e.Role = PromptRole.Assistant;
				})
			};
			Find.WindowStack.Add(new FloatMenu(opts));
		}
		float tabRowY;
		y = (tabRowY = y + 28f);
		Widgets.Label(new Rect(labelX, tabRowY, 80f, 24f), "RimTalk.Settings.PromptPreset.Position".Translate());
		Widgets.Label(new Rect(inputX, tabRowY, dropdownWidth, 24f), e.Position.ToString());
		if (e.Position == PromptPosition.InChat)
		{
			float depthLabelX = inputX + dropdownWidth + 10f;
			Widgets.Label(new Rect(depthLabelX, tabRowY, 50f, 24f), "RimTalk.Settings.PromptPreset.Depth".Translate());
			if (_depthBufferEntryId != e.Id)
			{
				_depthBuffer = e.InChatDepth.ToString();
				_depthBufferEntryId = e.Id;
			}
			_depthBuffer = Widgets.TextField(new Rect(depthLabelX + 55f, tabRowY, 60f, 24f), _depthBuffer);
			if (int.TryParse(_depthBuffer, out var res))
			{
				e.InChatDepth = res;
			}
		}
		float tabWidth = 70f;
		float rightEdge = rect.xMax - 5f;
		DrawToggleTab("RimTalk.Settings.PromptHelp".Translate(), ref _showSidePanel, 0, isRadio: true, 1);
		DrawToggleTab("RimTalk.Settings.ShowVariables".Translate(), ref _showSidePanel, 1, isRadio: true);
		DrawToggleTab("RimTalk.Settings.PromptPreset.ModePreview".Translate(), ref _showPreview, 2);
		y += 28f;
		Rect bottomArea = new Rect(rect.x + 10f, y, rect.width - 20f, rect.yMax - y - 5f);
		Rect mainWorkRect = bottomArea;
		Rect sidePanelRect = Rect.zero;
		Rect splitHorizRect = Rect.zero;
		float splitterSize = 6f;
		if (_showSidePanel)
		{
			float minMainW = 150f;
			float minSideW = 150f;
			float maxRatioH = (bottomArea.width - minSideW - splitterSize) / bottomArea.width;
			float minRatioH = minMainW / bottomArea.width;
			_splitRatioHoriz = Mathf.Clamp(_splitRatioHoriz, minRatioH, maxRatioH);
			float leftW = bottomArea.width * _splitRatioHoriz - splitterSize / 2f;
			float rightW = bottomArea.width - leftW - splitterSize;
			mainWorkRect = new Rect(bottomArea.x, bottomArea.y, leftW, bottomArea.height);
			splitHorizRect = new Rect(bottomArea.x + leftW, bottomArea.y, splitterSize, bottomArea.height);
			sidePanelRect = new Rect(bottomArea.x + leftW + splitterSize, bottomArea.y, rightW, bottomArea.height);
		}
		Rect editorRect = mainWorkRect;
		Rect previewRect = Rect.zero;
		Rect splitVertRect = Rect.zero;
		if (_showPreview)
		{
			float minEditorH = 100f;
			float minPrevH = 60f;
			float maxRatioV = (mainWorkRect.height - minPrevH - splitterSize) / mainWorkRect.height;
			float minRatioV = minEditorH / mainWorkRect.height;
			_splitRatioVert = Mathf.Clamp(_splitRatioVert, minRatioV, maxRatioV);
			float topH = mainWorkRect.height * _splitRatioVert - splitterSize / 2f;
			float botH = mainWorkRect.height - topH - splitterSize;
			editorRect = new Rect(mainWorkRect.x, mainWorkRect.y, mainWorkRect.width, topH);
			splitVertRect = new Rect(mainWorkRect.x, mainWorkRect.y + topH, mainWorkRect.width, splitterSize);
			previewRect = new Rect(mainWorkRect.x, mainWorkRect.y + topH + splitterSize, mainWorkRect.width, botH);
		}
		float editorInnerWidth = editorRect.width - 20f;
		float editorContentHeight = Mathf.Ceil(Mathf.Max(editorRect.height, Text.CalcHeight(e.Content, editorInnerWidth) + 25f));
		Widgets.BeginScrollView(viewRect: new Rect(0f, 0f, editorInnerWidth, editorContentHeight), outRect: editorRect, scrollPosition: ref _promptContentScrollPos);
		GUI.SetNextControlName("PromptEntryEditor");
		string newContent;
		if (isHistoryMarker)
		{
			Widgets.TextArea(new Rect(0f, 0f, editorInnerWidth, editorContentHeight), e.Content, readOnly: true);
			newContent = e.Content;
		}
		else
		{
			newContent = Widgets.TextArea(new Rect(0f, 0f, editorInnerWidth, editorContentHeight), e.Content);
		}
		if (GUI.GetNameOfFocusedControl() == "PromptEntryEditor")
		{
			TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
			if (te != null && te.cursorIndex != _lastPromptEditorCursorPos)
			{
				_lastPromptEditorCursorPos = te.cursorIndex;
				float cursorY = te.graphicalCursorPos.y;
				if (cursorY < _promptContentScrollPos.y)
				{
					_promptContentScrollPos.y = cursorY;
				}
				else if (cursorY + 25f > _promptContentScrollPos.y + editorRect.height)
				{
					_promptContentScrollPos.y = cursorY + 25f - editorRect.height;
				}
			}
		}
		Widgets.EndScrollView();
		if (newContent != e.Content)
		{
			e.Content = newContent;
			if (_showSidePanel && _sidePanelMode == 0)
			{
				UpdateSmartFilter(newContent);
			}
		}
		if (_showPreview)
		{
			Widgets.DrawBoxSolid(splitVertRect, new Color(0.2f, 0.2f, 0.2f));
			GUI.DrawTexture(new Rect(splitVertRect.center.x - 10f, splitVertRect.center.y - 2f, 20f, 4f), (Texture)BaseContent.WhiteTex);
			Widgets.DrawHighlightIfMouseover(splitVertRect);
			if ((int)Event.current.type == 0 && splitVertRect.Contains(Event.current.mousePosition))
			{
				_isDraggingVert = true;
				Event.current.Use();
			}
			Widgets.DrawBoxSolid(previewRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
			DrawPreviewContent(previewRect, e.Content);
		}
		if (_showSidePanel)
		{
			Widgets.DrawBoxSolid(splitHorizRect, new Color(0.2f, 0.2f, 0.2f));
			GUI.DrawTexture(new Rect(splitHorizRect.center.x - 2f, splitHorizRect.center.y - 10f, 4f, 20f), (Texture)BaseContent.WhiteTex);
			Widgets.DrawHighlightIfMouseover(splitHorizRect);
			if ((int)Event.current.type == 0 && splitHorizRect.Contains(Event.current.mousePosition))
			{
				_isDraggingHoriz = true;
				Event.current.Use();
			}
			DrawSidePanel(sidePanelRect, manager, e);
		}
		if (_isDraggingVert)
		{
			if ((int)Event.current.type == 3)
			{
				_splitRatioVert += Event.current.delta.y / mainWorkRect.height;
				Event.current.Use();
			}
			if ((int)Event.current.rawType == 1)
			{
				_isDraggingVert = false;
			}
		}
		if (_isDraggingHoriz)
		{
			if ((int)Event.current.type == 3)
			{
				_splitRatioHoriz += Event.current.delta.x / bottomArea.width;
				Event.current.Use();
			}
			if ((int)Event.current.rawType == 1)
			{
				_isDraggingHoriz = false;
			}
		}
		void DrawToggleTab(string label, ref bool isOpen, int indexFromRight, bool isRadio = false, int radioMode = 0)
		{
			Rect tabRect = new Rect(rightEdge - tabWidth * (float)(indexFromRight + 1) - 5f * (float)indexFromRight, tabRowY, tabWidth, 24f);
			bool active = ((!isRadio) ? isOpen : (_showSidePanel && _sidePanelMode == radioMode));
			GUI.color = (active ? Color.green : Color.white);
			if (Widgets.ButtonText(tabRect, label))
			{
				if (isRadio)
				{
					if (active)
					{
						_showSidePanel = false;
					}
					else
					{
						_showSidePanel = true;
						_sidePanelMode = radioMode;
						_auxScrollPos = Vector2.zero;
					}
				}
				else
				{
					isOpen = !isOpen;
					if (isOpen)
					{
						_previewScrollPos = Vector2.zero;
					}
				}
			}
			GUI.color = Color.white;
		}
	}

	private void UpdateSmartFilter(string text)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		if (!_showSidePanel || _sidePanelMode != 0)
		{
			return;
		}
		TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
		if (te == null || te.cursorIndex < 0)
		{
			return;
		}
		int pos = te.cursorIndex;
		if (pos > text.Length)
		{
			pos = text.Length;
		}
		int start = pos - 1;
		while (start >= 0 && !char.IsWhiteSpace(text[start]) && text[start] != '{' && text[start] != '}')
		{
			start--;
		}
		start++;
		string currentWord = "";
		if (start < pos)
		{
			currentWord = text.Substring(start, pos - start);
		}
		bool shouldUpdate = false;
		int check = start - 1;
		while (check >= 0 && char.IsWhiteSpace(text[check]))
		{
			check--;
		}
		if (check >= 1 && text[check] == '{' && text[check - 1] == '{')
		{
			if (currentWord.Contains("."))
			{
				shouldUpdate = true;
			}
			else if (currentWord.Length >= 2)
			{
				shouldUpdate = true;
			}
			else if (_variableSearchQuery.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase) && _variableSearchQuery.Length > currentWord.Length)
			{
				shouldUpdate = true;
			}
			else if (currentWord.Length == 0)
			{
				shouldUpdate = true;
			}
		}
		if (shouldUpdate)
		{
			_variableSearchQuery = currentWord;
		}
	}

	private void DrawPreviewContent(Rect rect, string content)
	{
		string text = PresetPreviewGenerator.GeneratePreview(content);
		Text.Font = GameFont.Small;
		Rect innerRect = rect.ContractedBy(5f);
		float viewWidth = innerRect.width - 16f;
		float height = Text.CalcHeight(text, viewWidth);
		Rect viewRect = new Rect(0f, 0f, viewWidth, height);
		Widgets.BeginScrollView(innerRect, ref _previewScrollPos, viewRect);
		Widgets.TextArea(viewRect, text, readOnly: true);
		Widgets.EndScrollView();
	}

	private void DrawSidePanel(Rect rect, PromptManager manager, PromptEntry entry)
	{
		Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
		Rect contentRect = rect.ContractedBy(5f);
		if (_sidePanelMode == 0)
		{
			DrawVariablePreviewContent(contentRect, manager, entry);
			return;
		}
		string text = "RimTalk.Settings.AdvancedHelpContent".Translate();
		Text.Font = GameFont.Small;
		float viewWidth = contentRect.width - 16f;
		float height = Text.CalcHeight(text, viewWidth);
		Rect viewRect = new Rect(0f, 0f, viewWidth, height);
		Widgets.BeginScrollView(contentRect, ref _auxScrollPos, viewRect);
		Widgets.TextArea(viewRect, text, readOnly: true);
		Widgets.EndScrollView();
	}

	private void DrawVariablePreviewContent(Rect rect, PromptManager manager, PromptEntry entry)
	{
		Rect searchRect = new Rect(rect.x, rect.y, rect.width, 24f);
		_variableSearchQuery = Widgets.TextField(searchRect, _variableSearchQuery);
		if (string.IsNullOrEmpty(_variableSearchQuery))
		{
			GUI.color = new Color(1f, 1f, 1f, 0.3f);
			Widgets.Label(searchRect.ContractedBy(2f, 0f), "RimTalk.Settings.PromptPreset.SearchPlaceholder".Translate());
			GUI.color = Color.white;
		}
		Text.Font = GameFont.Tiny;
		GUI.color = Color.gray;
		Widgets.Label(new Rect(rect.x, rect.y + 26f, rect.width, 20f), "RimTalk.Settings.PromptPreset.VariablePreviewHint".Translate());
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		Rect listRect = new Rect(rect.x, rect.y + 45f, rect.width, rect.height - 45f);
		Dictionary<string, List<(string, string)>> builtin = VariableDefinitions.GetScribanVariables();
		string query = _variableSearchQuery.Trim().ToLowerInvariant();
		Dictionary<string, List<(string, string)>> filteredBuiltin = new Dictionary<string, List<(string, string)>>();
		foreach (KeyValuePair<string, List<(string, string)>> cat in builtin)
		{
			List<(string, string)> matches = cat.Value.Where<(string, string)>(((string name, string description) tuple) => tuple.name.ToLowerInvariant().Contains(query) || tuple.description.ToLowerInvariant().Contains(query) || cat.Key.ToLowerInvariant().Contains(query)).ToList();
			if (matches.Any())
			{
				filteredBuiltin[cat.Key] = matches;
			}
		}
		if (_variableSearchQuery.Contains("."))
		{
			Dictionary<string, List<(string, string)>> dynamicVars = VariableDefinitions.GetDynamicVariables(_variableSearchQuery, entry.Content);
			foreach (KeyValuePair<string, List<(string, string)>> kvp in dynamicVars)
			{
				filteredBuiltin[kvp.Key] = kvp.Value;
			}
		}
		List<KeyValuePair<string, string>> runtimeVars = (from keyValuePair in manager.VariableStore.GetAllVariables()
			where keyValuePair.Key.ToLowerInvariant().Contains(query) || keyValuePair.Value.ToLowerInvariant().Contains(query)
			select keyValuePair).ToList();
		float totalRows = filteredBuiltin.Sum((KeyValuePair<string, List<(string, string)>> c) => c.Value.Count + 1);
		if (runtimeVars.Any())
		{
			totalRows += (float)(runtimeVars.Count + 1);
		}
		Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, totalRows * 22f);
		Widgets.BeginScrollView(listRect, ref _auxScrollPos, viewRect);
		float vy = 0f;
		string prefixToStrip = "";
		int lastDotIndex = _variableSearchQuery.LastIndexOf('.');
		if (lastDotIndex >= 0)
		{
			prefixToStrip = _variableSearchQuery.Substring(0, lastDotIndex + 1);
		}
		foreach (KeyValuePair<string, List<(string, string)>> cat2 in filteredBuiltin)
		{
			GUI.color = Color.cyan;
			Text.Font = GameFont.Tiny;
			Widgets.Label(new Rect(0f, vy, viewRect.width, 20f), "▼ " + cat2.Key);
			Text.Font = GameFont.Small;
			GUI.color = Color.white;
			vy += 22f;
			foreach (var v in cat2.Value)
			{
				string displayLabel = null;
				if (!string.IsNullOrEmpty(prefixToStrip) && v.Item1.StartsWith(prefixToStrip, StringComparison.OrdinalIgnoreCase))
				{
					displayLabel = v.Item1.Substring(prefixToStrip.Length);
				}
				DrawVariableRow(ref vy, viewRect.width, v.Item1, v.Item2, null, entry, displayLabel);
			}
		}
		if (runtimeVars.Any())
		{
			GUI.color = Color.green;
			Text.Font = GameFont.Tiny;
			Widgets.Label(new Rect(0f, vy, viewRect.width, 20f), "▼ Runtime Variables");
			Text.Font = GameFont.Small;
			GUI.color = Color.white;
			vy += 22f;
			foreach (KeyValuePair<string, string> kvp2 in runtimeVars)
			{
				DrawVariableRow(ref vy, viewRect.width, kvp2.Key, "", kvp2.Value, entry);
			}
		}
		Widgets.EndScrollView();
	}

	private void DrawVariableRow(ref float y, float w, string n, string d, string v, PromptEntry entry, string displayLabel = null)
	{
		Rect rowRect = new Rect(0f, y, w, 20f);
		if (Mouse.IsOver(rowRect))
		{
			Widgets.DrawHighlight(rowRect);
		}
		if (Widgets.ButtonInvisible(rowRect))
		{
			InsertVariable(n, entry);
		}
		Text.Font = GameFont.Tiny;
		string label = displayLabel ?? n;
		string fullVar = "{{ " + label + " }}";
		float labelWidth = Text.CalcSize(fullVar).x;
		GUI.color = new Color(0.8f, 1f, 0.8f);
		Widgets.Label(new Rect(2f, y, labelWidth + 5f, 20f), fullVar);
		string typeInfo = v ?? d;
		if (!string.IsNullOrEmpty(typeInfo))
		{
			GUI.color = new Color(0.5f, 0.5f, 0.5f);
			float typeX = labelWidth + 10f;
			float typeW = w - typeX - 5f;
			if (typeW > 10f)
			{
				Widgets.Label(new Rect(typeX, y, typeW, 20f), typeInfo);
			}
		}
		GUI.color = Color.white;
		Text.Font = GameFont.Small;
		y += 20f;
	}

	private void ShowImportMenu(PromptManager manager)
	{
		List<string> files = PresetSerializer.GetAvailablePresetFiles();
		if (files.Count == 0)
		{
			string exportDir = PresetSerializer.GetExportDirectory();
			Find.WindowStack.Add(new Dialog_MessageBox("RimTalk.Settings.PromptPreset.NoPresetsToImport".Translate(exportDir), "OK".Translate()));
			return;
		}
		List<FloatMenuOption> options = new List<FloatMenuOption>();
		foreach (string file in files)
		{
			string fileName = Path.GetFileNameWithoutExtension(file);
			options.Add(new FloatMenuOption(fileName, delegate
			{
				PromptPreset promptPreset = PresetSerializer.ImportFromFile(file);
				if (promptPreset != null)
				{
					promptPreset.Name = manager.GetUniqueName(promptPreset.Name);
					manager.AddPreset(promptPreset);
					_selectedPresetId = promptPreset.Id;
					_selectedEntryId = null;
					Messages.Message("RimTalk.Settings.PromptPreset.ImportSuccess".Translate(promptPreset.Name), MessageTypeDefOf.PositiveEvent, historical: false);
				}
				else
				{
					Messages.Message("RimTalk.Settings.PromptPreset.ImportFailed".Translate(), MessageTypeDefOf.RejectInput, historical: false);
				}
			}));
		}
		options.Add(new FloatMenuOption("RimTalk.Settings.PromptPreset.OpenFolder".Translate(), delegate
		{
			string exportDirectory = PresetSerializer.GetExportDirectory();
			Process.Start(new ProcessStartInfo
			{
				FileName = exportDirectory,
				UseShellExecute = true
			});
		}));
		Find.WindowStack.Add(new FloatMenu(options));
	}

	private void InsertVariable(string variableName, PromptEntry entry)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		SoundDefOf.Click.PlayOneShotOnCamera();
		TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
		if (te != null && te.cursorIndex >= 0 && te.cursorIndex <= entry.Content.Length)
		{
			string text = entry.Content;
			int cursor = te.cursorIndex;
			int start;
			for (start = cursor - 1; start >= 0; start--)
			{
				char c = text[start];
				if (!char.IsLetterOrDigit(c) && c != '.' && c != '_')
				{
					break;
				}
			}
			start++;
			string prefix = "";
			if (start < cursor)
			{
				prefix = text.Substring(start, cursor - start);
			}
			if (!string.IsNullOrEmpty(prefix) && variableName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				text = text.Remove(start, cursor - start);
				cursor = start;
			}
			int ptr = cursor - 1;
			while (ptr >= 0 && char.IsWhiteSpace(text[ptr]))
			{
				ptr--;
			}
			bool hasOpenBrackets = ptr >= 1 && text[ptr] == '{' && text[ptr - 1] == '{';
			string finalInsert;
			if (hasOpenBrackets)
			{
				finalInsert = ((cursor > 0 && text[cursor - 1] == '{') ? " " : "") + variableName;
				int endPtr;
				for (endPtr = cursor; endPtr < text.Length && char.IsWhiteSpace(text[endPtr]); endPtr++)
				{
				}
				if (endPtr >= text.Length - 1 || text[endPtr] != '}' || text[endPtr + 1] != '}')
				{
					finalInsert += " }}";
				}
			}
			else
			{
				finalInsert = "{{ " + variableName + " }}";
			}
			text = (entry.Content = text.Insert(cursor, finalInsert));
			int newCursorPos;
			if (hasOpenBrackets)
			{
				int closeIndex = text.IndexOf("}}", cursor);
				newCursorPos = ((closeIndex == -1) ? (cursor + finalInsert.Length) : (closeIndex + 2));
			}
			else
			{
				newCursorPos = cursor + finalInsert.Length;
			}
			te.text = text;
			te.cursorIndex = newCursorPos;
			te.selectIndex = newCursorPos;
		}
		else
		{
			entry.Content = entry.Content + "{{ " + variableName + " }}";
		}
	}
}
