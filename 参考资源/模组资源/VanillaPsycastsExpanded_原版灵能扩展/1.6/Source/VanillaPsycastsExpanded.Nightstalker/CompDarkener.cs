using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace VanillaPsycastsExpanded.Nightstalker;

[HarmonyPatch]
[StaticConstructorOnStartup]
public class CompDarkener : ThingComp
{
	private static readonly Dictionary<Map, Dictionary<IntVec3, int>> darkCells = new Dictionary<Map, Dictionary<IntVec3, int>>();

	private CompProperties_Darkness Props => (CompProperties_Darkness)props;

	private static Dictionary<IntVec3, int> DarkCellsFor(Map map, bool create = true)
	{
		if (!darkCells.TryGetValue(map, out var value))
		{
			if (!create)
			{
				return null;
			}
			value = new Dictionary<IntVec3, int>();
			darkCells[map] = value;
		}
		return value;
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		Dictionary<IntVec3, int> dictionary = DarkCellsFor(parent.Map);
		foreach (IntVec3 item in GenRadial.RadialCellsAround(parent.Position, Props.darknessRange, useCenter: true))
		{
			if (dictionary.TryGetValue(item, out var value))
			{
				dictionary[item] = value + 1;
			}
			else
			{
				dictionary[item] = 1;
			}
			parent.Map.glowGrid.LightBlockerAdded(item);
		}
	}

	public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
	{
		Dictionary<IntVec3, int> dictionary = DarkCellsFor(map);
		foreach (IntVec3 item in GenRadial.RadialCellsAround(parent.Position, Props.darknessRange, useCenter: true))
		{
			if (dictionary.TryGetValue(item, out var value))
			{
				if (value == 1)
				{
					dictionary.Remove(item);
				}
				else
				{
					dictionary[item] = value - 1;
				}
			}
			else
			{
				value = 0;
			}
			bool flag = false;
			List<Thing> list = map.thingGrid.ThingsListAt(item);
			for (int i = 0; i < list.Count; i++)
			{
				if (value > 0 || IsLightBlocker(list[i]))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				parent.Map.glowGrid.LightBlockerRemoved(item);
			}
		}
		if (!dictionary.Any())
		{
			darkCells.Remove(map);
		}
	}

	private static bool IsLightBlocker(Thing thing)
	{
		if (thing.def.blockLight)
		{
			return thing is Building;
		}
		return false;
	}

	[HarmonyPatch(typeof(GlowGrid), "GroundGlowAt")]
	[HarmonyPrefix]
	public static void IgnoreSkyDark(IntVec3 c, ref bool ignoreSky, Map ___map)
	{
		if (darkCells.TryGetValue(___map, out var value) && value.ContainsKey(c))
		{
			ignoreSky = true;
		}
	}
}
