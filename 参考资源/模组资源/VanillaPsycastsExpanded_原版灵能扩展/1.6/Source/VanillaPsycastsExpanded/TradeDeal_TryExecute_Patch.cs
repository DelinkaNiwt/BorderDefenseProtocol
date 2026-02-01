using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

[HarmonyPatch(typeof(TradeDeal), "TryExecute", new Type[] { typeof(bool) }, new ArgumentType[] { ArgumentType.Out })]
public static class TradeDeal_TryExecute_Patch
{
	public static void Prefix(List<Tradeable> ___tradeables, out int __state)
	{
		__state = 0;
		foreach (Tradeable ___tradeable in ___tradeables)
		{
			__state += ___tradeable.ThingDef.GetEltexOrEltexMaterialCount() * ___tradeable.CountToTransferToDestination;
		}
	}

	public static int GetEltexOrEltexMaterialCount(this ThingDef def)
	{
		if (def != null)
		{
			if (def == VPE_DefOf.VPE_Eltex)
			{
				return 1;
			}
			if (def.costList != null)
			{
				ThingDefCountClass thingDefCountClass = def.costList.FirstOrDefault((ThingDefCountClass x) => x.thingDef == VPE_DefOf.VPE_Eltex);
				if (thingDefCountClass != null)
				{
					return thingDefCountClass.count;
				}
			}
			else
			{
				foreach (RecipeDef allDef in DefDatabase<RecipeDef>.AllDefs)
				{
					if (allDef.ProducedThingDef == def)
					{
						IngredientCount ingredientCount = allDef.ingredients.FirstOrDefault((IngredientCount x) => x.IsFixedIngredient && x.FixedIngredient == VPE_DefOf.VPE_Eltex);
						if (ingredientCount != null)
						{
							return (int)ingredientCount.GetBaseCount();
						}
					}
				}
			}
		}
		return 0;
	}

	public static void Postfix(int __state, bool __result)
	{
		if (__state > 0 && __result && TradeSession.trader.Faction != Faction.OfEmpire && Faction.OfEmpire != null && Rand.Chance(0.5f))
		{
			Current.Game.GetComponent<GameComponent_PsycastsManager>().goodwillImpacts.Add(new GoodwillImpactDelayed
			{
				factionToImpact = Faction.OfEmpire,
				goodwillImpact = -__state,
				historyEvent = (TradeSession.giftMode ? VPE_DefOf.VPE_GiftedEltex : VPE_DefOf.VPE_SoldEltex),
				impactInTicks = Find.TickManager.TicksGame + (int)(60000f * Rand.Range(7f, 14f)),
				letterLabel = "VPE.EmpireAngeredTitle".Translate(),
				letterDesc = "VPE.EmpireAngeredDesc".Translate(TradeSession.giftMode ? "VPE.Gifting".Translate() : "VPE.Trading".Translate()),
				relationInfoKey = "VPE.FactionRelationReducedInfo"
			});
		}
	}
}
