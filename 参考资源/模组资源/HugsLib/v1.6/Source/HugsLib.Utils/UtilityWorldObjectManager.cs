using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace HugsLib.Utils;

/// <summary>
/// Handles utility WorldObjects of custom types.
/// Utility WorldObjects are a map-independent storage method for custom data.
/// All UWOs share the same def and aren't visible on the world, but are saved and loaded with it.
/// </summary>
public static class UtilityWorldObjectManager
{
	public const string InjectedDefName = "UtilityWorldObject";

	public const int UtilityObjectTile = 0;

	/// <summary>
	/// Returns an existing UWO or creates a new one, adding it to the world.
	/// </summary>
	/// <typeparam name="T">Your custom type that extends UtilityWorldObject</typeparam>
	[Obsolete("It is recommended to transition to Verse.GameComponent or RimWorld.Planet.WorldComponent for data storage")]
	public static T GetUtilityWorldObject<T>() where T : UtilityWorldObject
	{
		WorldObjectsHolder holder = GetHolder();
		T val = (T)holder.ObjectsAt(0).FirstOrDefault((WorldObject o) => o is T);
		if (val == null)
		{
			WorldObjectDef named = DefDatabase<WorldObjectDef>.GetNamed("UtilityWorldObject");
			named.worldObjectClass = typeof(T);
			val = (T)WorldObjectMaker.MakeWorldObject(named);
			named.worldObjectClass = typeof(WorldObject);
			val.Tile = 0;
			holder.Add(val);
		}
		return val;
	}

	public static bool UtilityWorldObjectExists<T>() where T : UtilityWorldObject
	{
		return GetHolder().ObjectsAt(0).Any((WorldObject o) => o is T);
	}

	internal static void OnDefsLoaded()
	{
		InjectUtilityObjectDef();
	}

	internal static void OnWorldLoaded()
	{
		CheckForWorldObjectsWithoutDef();
	}

	private static void CheckForWorldObjectsWithoutDef()
	{
		List<WorldObject> allWorldObjects = GetHolder().AllWorldObjects;
		for (int num = allWorldObjects.Count - 1; num >= 0; num--)
		{
			WorldObject worldObject = allWorldObjects[num];
			if (worldObject.def == null && worldObject is UtilityWorldObject)
			{
				HugsLibController.Logger.Error(worldObject.GetType().FullName + ".def is null on load. Forgot to call base.ExposeData()?");
				allWorldObjects.RemoveAt(num);
			}
		}
	}

	private static WorldObjectsHolder GetHolder()
	{
		if (Current.Game == null || Current.Game.World == null)
		{
			throw new Exception("A world must be loaded to get a WorldObject");
		}
		return Current.Game.World.worldObjects;
	}

	private static void InjectUtilityObjectDef()
	{
		WorldObjectDef worldObjectDef = new WorldObjectDef();
		worldObjectDef.defName = "UtilityWorldObject";
		worldObjectDef.worldObjectClass = typeof(WorldObject);
		worldObjectDef.canHaveFaction = false;
		worldObjectDef.selectable = false;
		worldObjectDef.neverMultiSelect = true;
		worldObjectDef.useDynamicDrawer = true;
		WorldObjectDef worldObjectDef2 = worldObjectDef;
		InjectedDefHasher.GiveShortHashToDef(worldObjectDef2, typeof(WorldObject));
		DefDatabase<WorldObjectDef>.Add(worldObjectDef2);
	}
}
