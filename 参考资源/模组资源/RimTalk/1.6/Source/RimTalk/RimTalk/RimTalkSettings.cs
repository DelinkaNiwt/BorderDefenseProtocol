using System.Collections.Generic;
using RimTalk.Data;
using RimTalk.Prompt;
using UnityEngine;
using Verse;

namespace RimTalk;

public class RimTalkSettings : ModSettings
{
	public List<ApiConfig> CloudConfigs = new List<ApiConfig>();

	public int CurrentCloudConfigIndex = 0;

	public ApiConfig LocalConfig = new ApiConfig
	{
		Provider = AIProvider.Local
	};

	public bool UseCloudProviders = true;

	public bool UseSimpleConfig = true;

	public string SimpleApiKey = "";

	public bool IsUsingFallbackModel = false;

	public bool IsEnabled = true;

	public int TalkInterval = 7;

	public const int ReplyInterval = 4;

	public bool ProcessNonRimTalkInteractions = true;

	public bool AllowSimultaneousConversations = false;

	public string CustomInstruction = "";

	public PromptManager PromptSystem = new PromptManager();

	public bool UseAdvancedPromptMode = false;

	public Dictionary<string, bool> EnabledArchivableTypes = new Dictionary<string, bool>();

	public bool DisplayTalkWhenDrafted = true;

	public bool AllowMonologue = true;

	public bool AllowSlavesToTalk = true;

	public bool AllowPrisonersToTalk = true;

	public bool AllowOtherFactionsToTalk = false;

	public bool AllowEnemiesToTalk = false;

	public bool AllowCustomConversation = true;

	public Settings.PlayerDialogueMode PlayerDialogueMode = Settings.PlayerDialogueMode.Manual;

	public string PlayerName = "Player";

	public bool ContinueDialogueWhileSleeping = false;

	public bool AllowBabiesToTalk = true;

	public bool AllowNonHumanToTalk = true;

	public bool ApplyMoodAndSocialEffects = false;

	public int DisableAiAtSpeed = 0;

	public Settings.ButtonDisplayMode ButtonDisplay = Settings.ButtonDisplayMode.Toggle;

	public ContextSettings Context = new ContextSettings();

	public bool DebugModeEnabled = false;

	public string DebugSortColumn;

	public bool DebugSortAscending = true;

	public bool OverlayEnabled = false;

	public float OverlayOpacity = 0.5f;

	public float OverlayFontSize = 15f;

	public bool OverlayDrawAboveUI = true;

	public Rect OverlayRectDebug = new Rect(200f, 200f, 600f, 450f);

	public Rect OverlayRectNonDebug = new Rect(200f, 200f, 400f, 250f);

	public ApiConfig GetActiveConfig()
	{
		if (UseSimpleConfig)
		{
			if (!string.IsNullOrWhiteSpace(SimpleApiKey))
			{
				return new ApiConfig
				{
					ApiKey = SimpleApiKey,
					Provider = AIProvider.Google,
					SelectedModel = (IsUsingFallbackModel ? "gemma-3-12b-it" : "gemma-3-27b-it"),
					IsEnabled = true
				};
			}
			return null;
		}
		if (UseCloudProviders)
		{
			if (CloudConfigs.Count == 0)
			{
				return null;
			}
			for (int i = 0; i < CloudConfigs.Count; i++)
			{
				int index = (CurrentCloudConfigIndex + i) % CloudConfigs.Count;
				ApiConfig config = CloudConfigs[index];
				if (config.IsValid())
				{
					CurrentCloudConfigIndex = index;
					return config;
				}
			}
			return null;
		}
		if (LocalConfig != null && LocalConfig.IsValid())
		{
			return LocalConfig;
		}
		return null;
	}

	public void TryNextConfig()
	{
		if (CloudConfigs.Count <= 1)
		{
			return;
		}
		int originalIndex = CurrentCloudConfigIndex;
		for (int i = 1; i < CloudConfigs.Count; i++)
		{
			int nextIndex = (originalIndex + i) % CloudConfigs.Count;
			ApiConfig config = CloudConfigs[nextIndex];
			if (config.IsValid())
			{
				CurrentCloudConfigIndex = nextIndex;
				Write();
				return;
			}
		}
		Write();
	}

	public string GetCurrentModel()
	{
		ApiConfig activeConfig = GetActiveConfig();
		if (activeConfig == null)
		{
			return "gemma-3-27b-it";
		}
		if (activeConfig.SelectedModel == "Custom")
		{
			return activeConfig.CustomModelName;
		}
		return activeConfig.SelectedModel;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref CloudConfigs, "cloudConfigs", LookMode.Deep);
		Scribe_Deep.Look(ref LocalConfig, "localConfig");
		Scribe_Values.Look(ref UseCloudProviders, "useCloudProviders", defaultValue: true);
		Scribe_Values.Look(ref UseSimpleConfig, "useSimpleConfig", defaultValue: true);
		Scribe_Values.Look(ref SimpleApiKey, "simpleApiKey", "");
		Scribe_Values.Look(ref IsEnabled, "isEnabled", defaultValue: true);
		Scribe_Values.Look(ref TalkInterval, "talkInterval", 7);
		Scribe_Values.Look(ref ProcessNonRimTalkInteractions, "processNonRimTalkInteractions", defaultValue: true);
		Scribe_Values.Look(ref AllowSimultaneousConversations, "allowSimultaneousConversations", defaultValue: false);
		Scribe_Values.Look(ref CustomInstruction, "customInstruction", "");
		Scribe_Values.Look(ref DisplayTalkWhenDrafted, "displayTalkWhenDrafted", defaultValue: true);
		Scribe_Values.Look(ref AllowMonologue, "allowMonologue", defaultValue: true);
		Scribe_Values.Look(ref AllowSlavesToTalk, "allowSlavesToTalk", defaultValue: true);
		Scribe_Values.Look(ref AllowPrisonersToTalk, "allowPrisonersToTalk", defaultValue: true);
		Scribe_Values.Look(ref AllowOtherFactionsToTalk, "allowOtherFactionsToTalk", defaultValue: false);
		Scribe_Values.Look(ref AllowEnemiesToTalk, "allowEnemiesToTalk", defaultValue: false);
		Scribe_Values.Look(ref AllowCustomConversation, "allowCustomConversation", defaultValue: true);
		Scribe_Values.Look(ref PlayerDialogueMode, "playerDialogueMode", Settings.PlayerDialogueMode.Manual);
		Scribe_Values.Look(ref PlayerName, "playerName", "Player");
		Scribe_Values.Look(ref ContinueDialogueWhileSleeping, "continueDialogueWhileSleeping", defaultValue: false);
		Scribe_Values.Look(ref DisableAiAtSpeed, "DisableAiAtSpeed", 0);
		Scribe_Collections.Look(ref EnabledArchivableTypes, "enabledArchivableTypes", LookMode.Value, LookMode.Value);
		Scribe_Values.Look(ref AllowBabiesToTalk, "allowBabiesToTalk", defaultValue: true);
		Scribe_Values.Look(ref AllowNonHumanToTalk, "allowNonHumanToTalk", defaultValue: true);
		Scribe_Values.Look(ref ApplyMoodAndSocialEffects, "applyMoodAndSocialEffects", defaultValue: false);
		Scribe_Deep.Look(ref Context, "context");
		Scribe_Deep.Look(ref PromptSystem, "promptSystem");
		Scribe_Values.Look(ref UseAdvancedPromptMode, "useAdvancedPromptMode", defaultValue: false);
		Scribe_Values.Look(ref ButtonDisplay, "buttonDisplay", Settings.ButtonDisplayMode.Toggle, forceSave: true);
		Scribe_Values.Look(ref DebugModeEnabled, "debugModeEnabled", defaultValue: false);
		Scribe_Values.Look(ref DebugSortColumn, "debugSortColumn");
		Scribe_Values.Look(ref DebugSortAscending, "debugSortAscending", defaultValue: true);
		Scribe_Values.Look(ref OverlayEnabled, "overlayEnabled", defaultValue: false);
		Scribe_Values.Look(ref OverlayOpacity, "overlayOpacity", 0.5f);
		Scribe_Values.Look(ref OverlayFontSize, "overlayFontSize", 15f);
		Scribe_Values.Look(ref OverlayDrawAboveUI, "overlayDrawAboveUI", defaultValue: true);
		Rect defaultDebugRect = new Rect(200f, 200f, 600f, 450f);
		float overlayDebugX = OverlayRectDebug.x;
		float overlayDebugY = OverlayRectDebug.y;
		float overlayDebugWidth = OverlayRectDebug.width;
		float overlayDebugHeight = OverlayRectDebug.height;
		Scribe_Values.Look(ref overlayDebugX, "overlayRectDebug_x", defaultDebugRect.x);
		Scribe_Values.Look(ref overlayDebugY, "overlayRectDebug_y", defaultDebugRect.y);
		Scribe_Values.Look(ref overlayDebugWidth, "overlayRectDebug_width", defaultDebugRect.width);
		Scribe_Values.Look(ref overlayDebugHeight, "overlayRectDebug_height", defaultDebugRect.height);
		Rect defaultNonDebugRect = new Rect(200f, 200f, 400f, 250f);
		float overlayNonDebugX = OverlayRectNonDebug.x;
		float overlayNonDebugY = OverlayRectNonDebug.y;
		float overlayNonDebugWidth = OverlayRectNonDebug.width;
		float overlayNonDebugHeight = OverlayRectNonDebug.height;
		Scribe_Values.Look(ref overlayNonDebugX, "overlayRectNonDebug_x", defaultNonDebugRect.x);
		Scribe_Values.Look(ref overlayNonDebugY, "overlayRectNonDebug_y", defaultNonDebugRect.y);
		Scribe_Values.Look(ref overlayNonDebugWidth, "overlayRectNonDebug_width", defaultNonDebugRect.width);
		Scribe_Values.Look(ref overlayNonDebugHeight, "overlayRectNonDebug_height", defaultNonDebugRect.height);
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			OverlayRectDebug = new Rect(overlayDebugX, overlayDebugY, overlayDebugWidth, overlayDebugHeight);
			OverlayRectNonDebug = new Rect(overlayNonDebugX, overlayNonDebugY, overlayNonDebugWidth, overlayNonDebugHeight);
		}
		if (CloudConfigs == null)
		{
			CloudConfigs = new List<ApiConfig>();
		}
		if (LocalConfig == null)
		{
			LocalConfig = new ApiConfig
			{
				Provider = AIProvider.Local
			};
		}
		if (EnabledArchivableTypes == null)
		{
			EnabledArchivableTypes = new Dictionary<string, bool>();
		}
		if (Context == null)
		{
			Context = new ContextSettings();
		}
		if (PromptSystem == null)
		{
			PromptSystem = new PromptManager();
		}
		PromptManager.SetInstance(PromptSystem);
		if (Scribe.mode == LoadSaveMode.PostLoadInit && !string.IsNullOrWhiteSpace(CustomInstruction))
		{
			PromptSystem.MigrateLegacyInstruction(CustomInstruction);
		}
		if (CloudConfigs.Count == 0)
		{
			CloudConfigs.Add(new ApiConfig());
		}
	}
}
