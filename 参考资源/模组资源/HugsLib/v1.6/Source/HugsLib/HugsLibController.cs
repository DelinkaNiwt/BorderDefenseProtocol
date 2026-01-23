using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HugsLib.Core;
using HugsLib.Logs;
using HugsLib.News;
using HugsLib.Quickstart;
using HugsLib.Settings;
using HugsLib.Spotter;
using HugsLib.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Verse;

namespace HugsLib;

/// <summary>
/// The hub of the library. Instantiates classes that extend ModBase and forwards some of the more useful events to them.
/// The assembly version of the library should reflect the current major Rimworld version, i.e.: 0.18.0.0 for B18.
/// This gives us the ability to release updates to the library without breaking compatibility with the mods that implement it.
/// See Core.HugsLibMod for the entry point.
/// </summary>
public class HugsLibController
{
	private const string SceneObjectName = "HugsLibProxy";

	private const string ModIdentifier = "HugsLib";

	private const string ModPackName = "HugsLib";

	private const string HarmonyInstanceIdentifier = "UnlimitedHugs.HugsLib";

	private const string HarmonyDebugCommandLineArg = "harmony_debug";

	private static bool earlyInitializationCompleted;

	private static bool lateInitializationCompleted;

	private static HugsLibController instance;

	private static VersionFile libraryVersionFile;

	private static AssemblyVersionInfo libraryVersionInfo;

	private static ModLogger _logger;

	private readonly List<ModBase> childMods = new List<ModBase>();

	private readonly List<ModBase> earlyInitializedMods = new List<ModBase>();

	private readonly List<ModBase> initializedMods = new List<ModBase>();

	private readonly HashSet<Assembly> autoHarmonyPatchedAssemblies = new HashSet<Assembly>();

	private Dictionary<Assembly, ModContentPack> assemblyContentPacks;

	private bool initializationInProgress;

	public static HugsLibController Instance => instance ?? (instance = new HugsLibController());

	public static Version LibraryVersion
	{
		get
		{
			if (libraryVersionInfo == null)
			{
				ReadOwnVersion();
			}
			if (libraryVersionFile != null && libraryVersionFile.OverrideVersion != null)
			{
				return libraryVersionFile.OverrideVersion;
			}
			if (libraryVersionInfo != null)
			{
				return libraryVersionInfo.HighestVersion;
			}
			return typeof(HugsLibController).Assembly.GetName().Version;
		}
	}

	public static ModSettingsManager SettingsManager => Instance.Settings;

	internal static ModContentPack OwnContentPack { get; private set; }

	internal static ModSettingsPack OwnSettingsPack { get; private set; }

	internal static ModLogger Logger => _logger ?? (_logger = new ModLogger("HugsLib"));

	public ModSettingsManager Settings { get; private set; }

	public UpdateFeatureManager UpdateFeatures { get; private set; }

	public TickDelayScheduler TickDelayScheduler { get; private set; }

	public DistributedTickScheduler DistributedTicker { get; private set; }

	public DoLaterScheduler DoLater { get; private set; }

	public LogPublisher LogUploader { get; private set; }

	public ModSpottingManager ModSpotter { get; private set; }

	internal Harmony HarmonyInst { get; private set; }

	internal IEnumerable<ModBase> InitializedMods => initializedMods;

	internal static void EarlyInitialize(ModContentPack contentPack)
	{
		OwnContentPack = contentPack;
		try
		{
			if (earlyInitializationCompleted)
			{
				Logger.Warning("Attempted repeated early initialization of controller: " + Environment.StackTrace);
				return;
			}
			earlyInitializationCompleted = true;
			CreateSceneObject();
			Instance.InitializeController();
		}
		catch (Exception ex)
		{
			Logger.Error("An exception occurred during early initialization: " + ex);
		}
	}

	private static void CreateSceneObject()
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			if (GameObject.Find("HugsLibProxy") != null)
			{
				Logger.Error("Another version of the library is already loaded. The HugsLib assembly should be loaded as a standalone mod.");
			}
			else
			{
				GameObject gameObject = new GameObject("HugsLibProxy");
				UnityEngine.Object.DontDestroyOnLoad(gameObject);
				gameObject.AddComponent<UnityProxyComponent>();
			}
		});
	}

	private static void ReadOwnVersion()
	{
		Assembly assembly = typeof(HugsLibController).Assembly;
		if (OwnContentPack != null)
		{
			libraryVersionFile = VersionFile.TryParseVersionFile(OwnContentPack);
			libraryVersionInfo = AssemblyVersionInfo.ReadModAssembly(assembly, OwnContentPack);
		}
		else
		{
			Logger.Error("Failed to identify own ModContentPack");
		}
	}

	private HugsLibController()
	{
	}

	private void InitializeController()
	{
		try
		{
			ReadOwnVersion();
			Logger.Message("version {0}", LibraryVersion);
			PrepareReflection();
			ApplyHarmonyPatches();
			Settings = new ModSettingsManager();
			Settings.BeforeModSettingsSaved += OnBeforeModSettingsSaved;
			UpdateFeatures = new UpdateFeatureManager();
			UpdateFeatures.OnEarlyInitialize();
			TickDelayScheduler = new TickDelayScheduler();
			DistributedTicker = new DistributedTickScheduler();
			DoLater = new DoLaterScheduler();
			LogUploader = new LogPublisher();
			ModSettingsPack modSettings = Settings.GetModSettings("HugsLib");
			QuickstartController.OnEarlyInitialize(modSettings);
			ModSpotter = new ModSpottingManager();
			ModSpotter.OnEarlyInitialize();
			new LibraryVersionChecker(LibraryVersion, Logger).OnEarlyInitialize();
			LoadOrderChecker.ValidateLoadOrder();
			EnumerateModAssemblies();
			EarlyInitializeChildMods();
		}
		catch (Exception e)
		{
			Logger.ReportException(e);
		}
	}

	private void EarlyInitializeChildMods()
	{
		try
		{
			initializationInProgress = true;
			EnumerateChildMods(earlyInitMode: true);
			for (int i = 0; i < childMods.Count; i++)
			{
				ModBase modBase = childMods[i];
				if (!earlyInitializedMods.Contains(modBase))
				{
					earlyInitializedMods.Add(modBase);
					string logIdentifierSafe = modBase.LogIdentifierSafe;
					try
					{
						modBase.EarlyInitialize();
					}
					catch (Exception e)
					{
						Logger.ReportException(e, logIdentifierSafe);
					}
				}
			}
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2);
		}
		finally
		{
			initializationInProgress = false;
		}
	}

	internal void LateInitialize()
	{
		try
		{
			if (!earlyInitializationCompleted)
			{
				Logger.Error("Attempted late initialization before early initialization: " + Environment.StackTrace);
				return;
			}
			if (lateInitializationCompleted)
			{
				Logger.Warning("Attempted repeated late initialization of controller: " + Environment.StackTrace);
				return;
			}
			lateInitializationCompleted = true;
			RegisterOwnSettings();
			QuickstartController.OnLateInitialize();
			LongEventHandler.ExecuteWhenFinished(HarmonyUtility.LogHarmonyPatchIssueErrors);
			LongEventHandler.QueueLongEvent(LoadReloadInitialize, "Initializing", doAsynchronously: true, null);
		}
		catch (Exception ex)
		{
			Logger.Error("An exception occurred during late initialization: " + ex);
		}
	}

	internal void LoadReloadInitialize()
	{
		try
		{
			initializationInProgress = true;
			CheckForIncludedHugsLibAssembly();
			EnumerateModAssemblies();
			EnumerateChildMods(earlyInitMode: false);
			for (int i = 0; i < childMods.Count; i++)
			{
				ModBase modBase = childMods[i];
				modBase.ModIsActive = assemblyContentPacks.ContainsKey(modBase.GetType().Assembly);
				if (!initializedMods.Contains(modBase))
				{
					initializedMods.Add(modBase);
					string logIdentifierSafe = modBase.LogIdentifierSafe;
					try
					{
						modBase.StaticInitialize();
						modBase.Initialize();
					}
					catch (Exception e)
					{
						Logger.ReportException(e, logIdentifierSafe);
					}
				}
			}
			OnDefsLoaded();
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2);
		}
		finally
		{
			initializationInProgress = false;
		}
	}

	internal void OnUpdate()
	{
		if (initializationInProgress)
		{
			return;
		}
		try
		{
			if (DoLater != null)
			{
				DoLater.OnUpdate();
			}
			for (int i = 0; i < initializedMods.Count; i++)
			{
				try
				{
					initializedMods[i].Update();
				}
				catch (Exception e)
				{
					Logger.ReportException(e, initializedMods[i].LogIdentifierSafe, reportOnceOnly: true);
				}
			}
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2, null, reportOnceOnly: true);
		}
	}

	internal void OnTick()
	{
		if (initializationInProgress)
		{
			return;
		}
		try
		{
			DoLater.OnTick();
			int ticksGame = Find.TickManager.TicksGame;
			for (int i = 0; i < initializedMods.Count; i++)
			{
				try
				{
					initializedMods[i].Tick(ticksGame);
				}
				catch (Exception e)
				{
					Logger.ReportException(e, initializedMods[i].LogIdentifierSafe, reportOnceOnly: true);
				}
			}
			TickDelayScheduler.Tick(ticksGame);
			DistributedTicker.Tick(ticksGame);
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2, null, reportOnceOnly: true);
		}
	}

	internal void OnFixedUpdate()
	{
		if (initializationInProgress)
		{
			return;
		}
		try
		{
			for (int i = 0; i < initializedMods.Count; i++)
			{
				try
				{
					initializedMods[i].FixedUpdate();
				}
				catch (Exception e)
				{
					Logger.ReportException(e, initializedMods[i].LogIdentifierSafe, reportOnceOnly: true);
				}
			}
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2, null, reportOnceOnly: true);
		}
	}

	internal void OnGUI()
	{
		if (initializationInProgress)
		{
			return;
		}
		try
		{
			if (DoLater != null)
			{
				DoLater.OnGUI();
			}
			KeyBindingHandler.OnGUI();
			for (int i = 0; i < initializedMods.Count; i++)
			{
				try
				{
					initializedMods[i].OnGUI();
				}
				catch (Exception e)
				{
					Logger.ReportException(e, initializedMods[i].LogIdentifierSafe, reportOnceOnly: true);
				}
			}
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2, null, reportOnceOnly: true);
		}
	}

	internal void OnGUIUnfiltered()
	{
		QuickstartController.OnGUIUnfiltered();
	}

	internal void OnSceneLoaded(Scene scene)
	{
		try
		{
			for (int i = 0; i < childMods.Count; i++)
			{
				try
				{
					childMods[i].SceneLoaded(scene);
				}
				catch (Exception e)
				{
					Logger.ReportException(e, childMods[i].LogIdentifierSafe);
				}
			}
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2);
		}
	}

	internal void OnApplicationQuit()
	{
		try
		{
			for (int i = 0; i < childMods.Count; i++)
			{
				try
				{
					childMods[i].ApplicationQuit();
				}
				catch (Exception e)
				{
					Logger.ReportException(e, childMods[i].LogIdentifierSafe);
				}
			}
			Settings.SaveChanges();
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2);
		}
	}

	internal void OnGameInitializationStart(Game game)
	{
		try
		{
			int ticksGame = game.tickManager.TicksGame;
			TickDelayScheduler.Initialize(ticksGame);
			DistributedTicker.Initialize(ticksGame);
			game.tickManager.RegisterAllTickabilityFor(new HugsTickProxy
			{
				CreatedByController = true
			});
		}
		catch (Exception e)
		{
			Logger.ReportException(e);
		}
	}

	internal void OnPlayingStateEntered()
	{
		try
		{
			UtilityWorldObjectManager.OnWorldLoaded();
			for (int i = 0; i < childMods.Count; i++)
			{
				try
				{
					childMods[i].WorldLoaded();
				}
				catch (Exception e)
				{
					Logger.ReportException(e, childMods[i].LogIdentifierSafe);
				}
			}
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2);
		}
	}

	internal void OnMapGenerated(Map map)
	{
		try
		{
			for (int i = 0; i < childMods.Count; i++)
			{
				try
				{
					childMods[i].MapGenerated(map);
				}
				catch (Exception e)
				{
					Logger.ReportException(e, childMods[i].LogIdentifierSafe);
				}
			}
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2);
		}
	}

	internal void OnMapComponentsConstructed(Map map)
	{
		try
		{
			for (int i = 0; i < childMods.Count; i++)
			{
				try
				{
					childMods[i].MapComponentsInitializing(map);
				}
				catch (Exception e)
				{
					Logger.ReportException(e, childMods[i].LogIdentifierSafe);
				}
			}
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2);
		}
	}

	internal void OnMapInitFinalized(Map map)
	{
		LongEventHandler.QueueLongEvent(delegate
		{
			OnMapLoaded(map);
		}, null, doAsynchronously: false, null);
	}

	internal bool ShouldHarmonyAutoPatch(Assembly assembly, string modId)
	{
		if (autoHarmonyPatchedAssemblies.Contains(assembly))
		{
			Logger.Warning("The {0} assembly contains multiple ModBase mods with HarmonyAutoPatch set to true. This warning was caused by modId {1}.", assembly.GetName().Name, modId);
			return false;
		}
		autoHarmonyPatchedAssemblies.Add(assembly);
		return true;
	}

	private void OnMapLoaded(Map map)
	{
		try
		{
			DoLater.OnMapLoaded(map);
			for (int i = 0; i < childMods.Count; i++)
			{
				try
				{
					childMods[i].MapLoaded(map);
				}
				catch (Exception e)
				{
					Logger.ReportException(e, childMods[i].LogIdentifierSafe);
				}
			}
			UpdateFeatures.TryShowDialog(manuallyOpened: false);
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2);
		}
	}

	internal void OnMapDiscarded(Map map)
	{
		try
		{
			for (int i = 0; i < childMods.Count; i++)
			{
				try
				{
					childMods[i].MapDiscarded(map);
				}
				catch (Exception e)
				{
					Logger.ReportException(e, childMods[i].LogIdentifierSafe);
				}
			}
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2);
		}
	}

	private void OnBeforeModSettingsSaved()
	{
		try
		{
			for (int i = 0; i < initializedMods.Count; i++)
			{
				try
				{
					ModBase modBase = initializedMods[i];
					if (modBase.SettingsPackInternalAccess != null && modBase.SettingsPackInternalAccess.HasUnsavedChanges)
					{
						initializedMods[i].SettingsChanged();
					}
				}
				catch (Exception e)
				{
					Logger.ReportException(e, initializedMods[i].LogIdentifierSafe);
				}
			}
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2);
		}
	}

	private void OnDefsLoaded()
	{
		try
		{
			UtilityWorldObjectManager.OnDefsLoaded();
			for (int i = 0; i < childMods.Count; i++)
			{
				try
				{
					childMods[i].DefsLoaded();
				}
				catch (Exception e)
				{
					Logger.ReportException(e, childMods[i].LogIdentifierSafe);
				}
			}
		}
		catch (Exception e2)
		{
			Logger.ReportException(e2);
		}
	}

	private void EnumerateChildMods(bool earlyInitMode)
	{
		Pair<Type, ModContentPack>[] array = (from t in typeof(ModBase).InstantiableDescendantsAndSelf()
			select new Pair<Type, ModContentPack>(t, assemblyContentPacks.TryGetValue(t.Assembly)) into pair2
			where pair2.Second != null
			orderby pair2.Second.loadOrder
			select pair2).ToArray();
		List<string> list = new List<string>();
		Pair<Type, ModContentPack>[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			Pair<Type, ModContentPack> pair = array2[num];
			Type subclass = pair.First;
			ModContentPack second = pair.Second;
			bool flag = subclass.HasAttribute<EarlyInitAttribute>();
			if (flag == earlyInitMode && childMods.Find((ModBase cm) => cm.GetType() == subclass) == null)
			{
				try
				{
					ModBase.CurrentlyProcessedContentPack = second;
					ModBase modBase = (ModBase)Activator.CreateInstance(subclass, nonPublic: true);
					ModBase.CurrentlyProcessedContentPack = null;
					modBase.ApplyHarmonyPatches();
					modBase.VersionInfo = AssemblyVersionInfo.ReadModAssembly(subclass.Assembly, second);
					childMods.Add(modBase);
					list.Add(modBase.LogIdentifierSafe);
				}
				catch (Exception e)
				{
					Logger.ReportException(e, subclass.ToString(), reportOnceOnly: false, "child mod instantiation");
				}
			}
		}
		if (list.Count > 0)
		{
			string message = (earlyInitMode ? "early-initializing {0}" : "initializing {0}");
			Logger.Message(message, list.ListElements());
		}
	}

	private void EnumerateModAssemblies()
	{
		assemblyContentPacks = new Dictionary<Assembly, ModContentPack>();
		foreach (ModContentPack runningMod in LoadedModManager.RunningMods)
		{
			foreach (Assembly loadedAssembly in runningMod.assemblies.loadedAssemblies)
			{
				assemblyContentPacks[loadedAssembly] = runningMod;
			}
		}
	}

	private void CheckForIncludedHugsLibAssembly()
	{
		string fullName = GetType().FullName;
		if (fullName == null)
		{
			throw new NullReferenceException();
		}
		foreach (ModContentPack runningMod in LoadedModManager.RunningMods)
		{
			foreach (Assembly loadedAssembly in runningMod.assemblies.loadedAssemblies)
			{
				if (loadedAssembly.GetType(fullName, throwOnError: false) != null && runningMod.Name != "HugsLib")
				{
					Logger.Error("Found HugsLib assembly included by mod {0}. The dll should never be included by other mods.", runningMod.Name);
				}
			}
		}
	}

	private void ApplyHarmonyPatches()
	{
		try
		{
			if (ShouldHarmonyAutoPatch(typeof(HugsLibController).Assembly, "HugsLib"))
			{
				Harmony.DEBUG = GenCommandLine.CommandLineArgPassed("harmony_debug");
				HarmonyInst = new Harmony("UnlimitedHugs.HugsLib");
				HarmonyInst.PatchAll(typeof(HugsLibController).Assembly);
			}
		}
		catch (Exception e)
		{
			Logger.ReportException(e);
		}
	}

	private void PrepareReflection()
	{
		InjectedDefHasher.PrepareReflection();
		LogWindowExtensions.PrepareReflection();
		OptionsDialogExtensions.PrepareReflection();
	}

	private void RegisterOwnSettings()
	{
		try
		{
			ModSettingsPack modSettingsPack = (OwnSettingsPack = Settings.GetModSettings("HugsLib"));
			modSettingsPack.EntryName = assemblyContentPacks[Assembly.GetCallingAssembly()]?.Name ?? "HugsLib";
			UpdateFeatures.RegisterSettings(modSettingsPack);
			LogPublisher.RegisterSettings(modSettingsPack);
		}
		catch (Exception e)
		{
			Logger.ReportException(e);
		}
	}
}
