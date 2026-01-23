using System;
using System.Collections.Generic;
using Verse;

namespace HugsLib.Utils;

public static class MapComponentUtility
{
	/// <summary>
	/// Injects a map component into the current map if it does not already exist. 
	/// Required for new MapComponents that were not active at map creation.
	/// The injection is performed at ExecuteWhenFinished to allow calling this method in MapComponent constructors.
	/// </summary>
	/// <param name="mapComponent">The MapComponent that is expected to be present is the map's component list</param>
	[Obsolete("Map components are now automatically injected by the game, including the ones added after map creation")]
	public static void EnsureIsActive(this MapComponent mapComponent)
	{
		if (mapComponent == null)
		{
			throw new Exception("MapComponent is null");
		}
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			if (mapComponent.map == null || mapComponent.map.components == null)
			{
				throw new Exception("The map component requires a loaded map to be made active.");
			}
			List<MapComponent> components = mapComponent.map.components;
			if (!components.Any((MapComponent c) => c == mapComponent))
			{
				components.Add(mapComponent);
			}
		});
	}

	/// <summary>
	/// Gets the map component of the given type from a map.
	/// Will throw an exception if a component of the requested type is not found.
	/// </summary>
	/// <typeparam name="T">The type of your MapComponent</typeparam>
	/// <param name="map">The map to get the component from</param>
	[Obsolete("Base game now has Verse.Map.GetComponent<T>() which does the same thing")]
	public static T GetMapComponent<T>(this Map map) where T : MapComponent
	{
		if (map == null || map.components == null)
		{
			throw new Exception("Cannot get component from null or uninitialized map");
		}
		T val = (T)map.components.Find((MapComponent c) => c is T);
		if (val == null)
		{
			throw new Exception($"Map component of type {typeof(T)} not found in map");
		}
		return val;
	}
}
