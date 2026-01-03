using System.Collections.Generic;
using RimWorld;
using RimWorld.Utility;
using Verse;

namespace AncotLibrary;

public static class ReloadableUtilityCustom
{
	public static IReloadableComp FindSomeReloadableComponent(Pawn pawn, bool allowForcedReload)
	{
		if (pawn.apparel != null)
		{
			foreach (Apparel item in pawn.apparel.WornApparel)
			{
				CompApparelReloadable_Custom compApparelReloadable_Custom = item.TryGetComp<CompApparelReloadable_Custom>();
				if (compApparelReloadable_Custom != null && compApparelReloadable_Custom.NeedsReload(allowForcedReload))
				{
					return compApparelReloadable_Custom;
				}
			}
		}
		return null;
	}

	public static List<Thing> FindEnoughAmmo(Pawn pawn, IntVec3 rootCell, IReloadableComp reloadable, bool forceReload)
	{
		if (reloadable == null)
		{
			return null;
		}
		IntRange desiredQuantity = new IntRange(reloadable.MinAmmoNeeded(forceReload), reloadable.MaxAmmoNeeded(forceReload));
		return RefuelWorkGiverUtility.FindEnoughReservableThings(pawn, rootCell, desiredQuantity, (Thing t) => t.def == reloadable.AmmoDef);
	}

	public static List<Thing> FindEnoughAmmoInventory(Pawn pawn, IReloadableComp reloadable, bool forceReload)
	{
		ThingOwner<Thing> innerContainer = pawn.inventory.innerContainer;
		if (reloadable == null || innerContainer.NullOrEmpty())
		{
			return null;
		}
		List<Thing> list = new List<Thing>();
		IntRange intRange = new IntRange(reloadable.MinAmmoNeeded(forceReload), reloadable.MaxAmmoNeeded(forceReload));
		int num = 0;
		foreach (Thing item in innerContainer)
		{
			if (item.def == reloadable.AmmoDef)
			{
				Log.Message("数量" + item.stackCount);
				list.Add(item);
				num += item.stackCount;
			}
			if (list.Count == intRange.max)
			{
				break;
			}
		}
		Log.Message("堆数" + list.Count);
		return list;
	}

	public static IEnumerable<Pair<IReloadableComp, Thing>> FindPotentiallyReloadableGear(Pawn pawn, List<Thing> potentialAmmo)
	{
		if (pawn.apparel == null)
		{
			yield break;
		}
		foreach (Apparel item in pawn.apparel.WornApparel)
		{
			IReloadableComp reloadableComp2 = item.TryGetComp<CompApparelReloadable_Custom>();
			if (TryGetAmmo(reloadableComp2, out var ammo3))
			{
				yield return new Pair<IReloadableComp, Thing>(reloadableComp2, ammo3);
			}
			ammo3 = null;
		}
		bool TryGetAmmo(IReloadableComp reloadable, out Thing reference)
		{
			reference = null;
			if (reloadable?.AmmoDef == null)
			{
				return false;
			}
			foreach (Thing item2 in potentialAmmo)
			{
				if (item2.def == reloadable.AmmoDef)
				{
					reference = item2;
					return true;
				}
			}
			return false;
		}
	}
}
