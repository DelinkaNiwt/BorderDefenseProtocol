using System;
using HarmonyLib;
using HugsLib.Core;
using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine.SceneManagement;
using Verse;

namespace HugsLib;

/// <summary>
/// The base class for all mods using HugsLib library. All classes extending ModBase will be instantiated 
/// automatically by <see cref="T:HugsLib.HugsLibController" /> at game initialization.
/// Can be annotated with <see cref="T:HugsLib.EarlyInitAttribute" /> to initialize the mod at <see cref="T:Verse.Mod" />
/// initialization time and have <see cref="M:HugsLib.ModBase.EarlyInitialize" /> be called.
/// </summary>
public abstract class ModBase
{
	public const string HarmonyInstancePrefix = "HugsLib.";

	protected ModContentPack modContentPackInt;

	/// <summary>
	/// This can be used to log messages specific to your mod.
	/// It will prefix everything with your ModIdentifier.
	/// </summary>
	protected ModLogger Logger { get; private set; }

	/// <summary>
	/// The ModSettingsPack specific to your mod.
	/// Use this to create settings handles that represent the values of saved settings.
	/// </summary>
	protected ModSettingsPack Settings { get; private set; }

	/// <summary>
	/// Override this and return false to prevent a Harmony instance from being automatically created and scanning your assembly for patches.
	/// </summary>
	protected virtual bool HarmonyAutoPatch => true;

	/// <summary>
	/// The reference to Harmony instance that applied the patches in your assembly.
	/// </summary>
	protected Harmony HarmonyInst { get; set; }

	/// <summary>
	/// A unique identifier used both as <see cref="P:HugsLib.ModBase.SettingsIdentifier" /> and <see cref="P:HugsLib.ModBase.LogIdentifier" />.
	/// Override them separately if different identifiers are needed or no <see cref="T:HugsLib.Settings.ModSettingsPack" /> should be assigned to <see cref="P:HugsLib.ModBase.Settings" />.
	/// Must start with a letter and contain any of [A-z, 0-9, -, _, :] (identifier must be valid as an XML tag name).
	/// </summary>
	/// <remarks>
	/// This is no longer used to identify mods since 7.0 (Rimworld 1.1). Use ModBase.ModContentPack.PackageId to that end instead.
	/// </remarks>
	public virtual string ModIdentifier => null;

	/// <summary>
	/// A unique identifier to use as a key when settings are stored for this mod by <see cref="T:HugsLib.Settings.ModSettingsManager" />.
	/// Must start with a letter and contain any of [A-z, 0-9, -, _, :] (identifier must be valid as an XML tag name).
	/// By default uses the PackageId of the implementing mod.
	/// Returning null will prevent the <see cref="P:HugsLib.ModBase.Settings" /> property from being assigned.
	/// </summary>
	public virtual string SettingsIdentifier => ModIdentifier ?? ModContentPack?.PackageId;

	/// <summary>
	/// A readable identifier for the mod, used as a prefix by <see cref="P:HugsLib.ModBase.Logger" /> and in various error messages.
	/// Appear as "[LogIdentifier] message" when using <see cref="P:HugsLib.ModBase.Logger" />.
	/// By default uses the non-lowercase PackageId of the implementing mod or the type name if that is not set.
	/// </summary>
	public virtual string LogIdentifier => ModIdentifier ?? ModContentPack?.PackageIdPlayerFacing ?? GetType().FullName;

	/// <summary>
	/// The null-checked version of <see cref="P:HugsLib.ModBase.LogIdentifier" />. 
	/// Returns the type name if <see cref="P:HugsLib.ModBase.LogIdentifier" /> is null.
	/// </summary>
	public string LogIdentifierSafe => LogIdentifier ?? GetType().FullName;

	/// <summary>
	/// The content pack for the mod containing the assembly this class belongs to
	/// </summary>
	public virtual ModContentPack ModContentPack
	{
		get
		{
			return modContentPackInt;
		}
		internal set
		{
			modContentPackInt = value;
		}
	}

	/// <summary>
	/// Can be false if the mod was enabled at game start and then disabled in the mods menu.
	/// Always true, unless the <see cref="T:Verse.ModContentPack" /> of the declaring mod can't be 
	/// identified for some unexpected reason.
	/// </summary>
	public bool ModIsActive { get; internal set; }

	/// <summary>
	/// Contains the AssemblyVersion and AssemblyFileVersion of the mod. Used by <see cref="M:HugsLib.ModBase.GetVersion" />.
	/// </summary>
	public AssemblyVersionInfo VersionInfo { get; internal set; }

	internal static ModContentPack CurrentlyProcessedContentPack { get; set; }

	internal ModSettingsPack SettingsPackInternalAccess => Settings;

	protected ModBase()
	{
		modContentPackInt = CurrentlyProcessedContentPack;
		Logger = new ModLogger(LogIdentifierSafe);
		string settingsIdentifier = SettingsIdentifier;
		if (!string.IsNullOrEmpty(settingsIdentifier))
		{
			if (PersistentDataManager.IsValidElementName(settingsIdentifier))
			{
				Settings = HugsLibController.Instance.Settings.GetModSettings(settingsIdentifier, modContentPackInt?.Name);
			}
			else
			{
				Logger.Error("string \"" + settingsIdentifier + "\" cannot be used as a settings identifier. Override ModBase.SettingsIdentifier to manually specify one. See SettingsIdentifier autocomplete documentation for expected format.");
			}
		}
	}

	internal void ApplyHarmonyPatches()
	{
		if (!HarmonyAutoPatch)
		{
			return;
		}
		string text = ModContentPack?.PackageIdPlayerFacing;
		if (text == null)
		{
			text = "HugsLib." + LogIdentifierSafe;
			GetLogger().Warning("Failed to identify PackageId, using \"" + text + "\" as Harmony id instead.");
		}
		try
		{
			if (HugsLibController.Instance.ShouldHarmonyAutoPatch(GetType().Assembly, text))
			{
				HarmonyInst = new Harmony(text);
				HarmonyInst.PatchAll(GetType().Assembly);
			}
		}
		catch (Exception ex)
		{
			GetLogger().Error("Failed to apply Harmony patches for {0}. Exception was: {1}", text, ex);
		}
		ModLogger GetLogger()
		{
			return Logger ?? new ModLogger(LogIdentifierSafe);
		}
	}

	/// <summary>
	/// Return the override version from the Version.xml file if specified, 
	/// or the higher one between AssemblyVersion and AssemblyFileVersion
	/// </summary>
	public virtual Version GetVersion()
	{
		VersionFile versionFile = VersionFile.TryParseVersionFile(ModContentPack);
		if (versionFile != null && versionFile.OverrideVersion != null)
		{
			return versionFile.OverrideVersion;
		}
		return VersionInfo.HighestVersion;
	}

	/// <summary>
	/// Called during HugsLib <see cref="T:Verse.Mod" /> instantiation, accounting for mod load order. 
	/// Load order among mods implementing <see cref="T:HugsLib.ModBase" /> is respected.
	/// and only if the implementing class is annotated with <see cref="T:HugsLib.EarlyInitAttribute" />.
	/// </summary>
	public virtual void EarlyInitialize()
	{
	}

	/// <summary>
	/// Called when HugsLib receives the <see cref="T:Verse.StaticConstructorOnStartup" /> call.
	/// Load order among mods implementing <see cref="T:HugsLib.ModBase" /> is respected.
	/// Called after the static constructors for non-HugsLib mods have executed. Is not called again on def reload
	/// </summary>
	public virtual void StaticInitialize()
	{
	}

	/// <summary>
	/// An alias for <see cref="M:HugsLib.ModBase.StaticInitialize" />, both or either can be used,
	/// although <see cref="M:HugsLib.ModBase.StaticInitialize" /> makes for clearer code by indicating when the method is called.
	/// </summary>
	public virtual void Initialize()
	{
	}

	/// <summary>
	/// Called on each tick when in Play scene
	/// </summary>
	/// <param name="currentTick">The sequential number of the tick being processed</param>
	public virtual void Tick(int currentTick)
	{
	}

	/// <summary>
	/// Called each frame
	/// </summary>
	public virtual void Update()
	{
	}

	/// <summary>
	/// Called each unity physics update
	/// </summary>
	public virtual void FixedUpdate()
	{
	}

	/// <summary>
	/// Called on each unity gui event, after UIRoot.UIRootOnGUI.
	/// Respects UI scaling and screen fading. Will not be called during loading screens.
	/// This is a good place to listen for hotkey events.
	/// </summary>
	public virtual void OnGUI()
	{
	}

	/// <summary>
	/// Called when GameState.Playing has been entered and the world is fully loaded in the Play scene.
	/// Will not be called during world generation and landing site selection.
	/// </summary>
	public virtual void WorldLoaded()
	{
	}

	/// <summary>
	/// Called right after Map.ConstructComponents() (before MapLoaded)
	/// </summary>
	/// <param name="map">The map being initialized</param>
	public virtual void MapComponentsInitializing(Map map)
	{
	}

	/// <summary>
	/// Called right after a new map has been generated.
	/// This is the equivalent of MapComponent.MapGenerated().
	/// </summary>
	/// <param name="map">The new map that has just finished generating</param>
	public virtual void MapGenerated(Map map)
	{
	}

	/// <summary>
	/// Called when the map was fully loaded
	/// </summary>
	/// <param name="map">The map that has finished loading</param>
	public virtual void MapLoaded(Map map)
	{
	}

	/// <summary>
	/// Called after a map has been abandoned or otherwise made inaccessible.
	/// Works on player bases, encounter maps, destroyed faction bases, etc.
	/// </summary>
	/// <param name="map">The map that has been discarded</param>
	public virtual void MapDiscarded(Map map)
	{
	}

	/// <summary>
	/// Called after each scene change
	/// </summary>
	/// <param name="scene">The scene that has been loaded</param>
	public virtual void SceneLoaded(Scene scene)
	{
	}

	/// <summary>
	/// Called after settings menu changes have been confirmed.
	/// This is called for all mods, regardless if their own settings have been modified, or not.
	/// </summary>
	public virtual void SettingsChanged()
	{
	}

	/// <summary>
	/// Called after StaticInitialize and when defs have been reloaded. This is a good place to inject defs.
	/// Get your settings handles here, so that the labels will properly update on language change.
	/// If the mod is disabled after being loaded, this method will STILL execute. Use ModIsActive to check.
	/// </summary>
	/// <remarks>
	/// There is no scenario in which defs are reloaded without the game restarting, save for a mod manually initiating a reload. 
	/// When def reloading is not an issue, anything done by this method can be safely done in StaticInitialize.
	/// </remarks>
	public virtual void DefsLoaded()
	{
	}

	/// <summary>
	/// Called before the game process shuts down.
	/// "Quit to OS", clicking the "X" button on the window, and pressing Alt+F4 all execute this event.
	/// There are still ways to forcibly terminate the game process, so this callback is not 100% reliable.
	/// </summary>
	/// <remarks>
	/// Modified <see cref="T:HugsLib.Settings.SettingHandle" />s are automatically saved after this call.
	/// </remarks>
	public virtual void ApplicationQuit()
	{
	}
}
