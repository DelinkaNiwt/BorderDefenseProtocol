using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NiceInventoryTab;

public static class CraftingUtility
{
	public static List<CraftableItem> GetAllCraftableApparel(List<ApparelSlotUtility.PotentialSlot> availableSlots, Map map)
	{
		List<CraftableItem> list = new List<CraftableItem>();
		if (availableSlots == null || availableSlots.Count == 0 || map == null)
		{
			return list;
		}
		HashSet<ThingDef> hashSet = new HashSet<ThingDef>();
		foreach (ApparelSlotUtility.PotentialSlot availableSlot in availableSlots)
		{
			foreach (ThingDef item in availableSlot.possibleApparel)
			{
				hashSet.Add(item);
			}
		}
		if (hashSet.Count == 0)
		{
			return list;
		}
		Dictionary<ThingDef, CraftableItem> dictionary = new Dictionary<ThingDef, CraftableItem>();
		foreach (Building_WorkTable item2 in map.listerBuildings.AllBuildingsColonistOfClass<Building_WorkTable>())
		{
			if (item2.def.AllRecipes == null)
			{
				continue;
			}
			foreach (RecipeDef allRecipe in item2.def.AllRecipes)
			{
				if (!allRecipe.AvailableNow || !allRecipe.AvailableOnNow(item2))
				{
					continue;
				}
				ThingDef producedThingDef = allRecipe.ProducedThingDef;
				if (producedThingDef == null || !producedThingDef.IsApparel || !hashSet.Contains(producedThingDef))
				{
					continue;
				}
				if (!dictionary.TryGetValue(producedThingDef, out var value))
				{
					value = (dictionary[producedThingDef] = new CraftableItem(producedThingDef));
					list.Add(value);
				}
				value.AddWorkTable(item2, allRecipe);
				if (Faction.OfPlayer?.ideos == null)
				{
					continue;
				}
				foreach (Ideo allIdeo in Faction.OfPlayer.ideos.AllIdeos)
				{
					if (allIdeo?.cachedPossibleBuildings == null)
					{
						continue;
					}
					foreach (Precept_Building cachedPossibleBuilding in allIdeo.cachedPossibleBuildings)
					{
						if (cachedPossibleBuilding.ThingDef == producedThingDef)
						{
							string label = "RecipeMake".Translate(cachedPossibleBuilding.LabelCap).CapitalizeFirst();
							value.AddWorkTable(item2, allRecipe, cachedPossibleBuilding, label);
						}
					}
				}
			}
		}
		return list;
	}
}
