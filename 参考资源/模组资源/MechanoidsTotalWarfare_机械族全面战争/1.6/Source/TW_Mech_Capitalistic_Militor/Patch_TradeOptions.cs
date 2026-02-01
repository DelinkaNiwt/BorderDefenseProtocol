using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NCL;
using RimWorld;
using Verse;

[HarmonyPatch(typeof(Pawn), "GetFloatMenuOptions")]
public static class Patch_TradeOptions
{
	public static void Postfix(Pawn __instance, ref IEnumerable<FloatMenuOption> __result)
	{
		CompTrader trader = __instance.TryGetComp<CompTrader>();
		if (trader != null && trader.CanTradeNow)
		{
			__result = __result.Concat(new FloatMenuOption[1]
			{
				new FloatMenuOption("与机械单位交易", delegate
				{
					StartCustomTrade(trader);
				})
			});
		}
	}

	private static void StartCustomTrade(CompTrader trader)
	{
		if (trader.TraderKind == null)
		{
			Log.Error("[NCL] Aborting trade: TraderKind is null.");
			return;
		}
		Pawn pawn = Find.CurrentMap.mapPawns.FreeColonists.FirstOrDefault();
		if (pawn == null)
		{
			Log.Error("[NCL] No available colonist to negotiate");
			return;
		}
		TradeSession.SetupWith(trader, pawn, giftMode: false);
		TradeSession.deal = new TradeDeal();
		Tradeable_MechanoidEmploy tradeable_MechanoidEmploy = new Tradeable_MechanoidEmploy();
		typeof(TradeDeal).GetMethod("AddTradeable", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(TradeSession.deal, new object[1] { tradeable_MechanoidEmploy });
		Find.WindowStack.Add(new Dialog_Trade(pawn, trader));
	}

	private static void InitTradeDeal(CompTrader trader)
	{
		if (TradeSession.deal == null)
		{
			TradeSession.deal = new TradeDeal();
		}
		try
		{
			Tradeable_MechanoidEmploy tradeable_MechanoidEmploy = new Tradeable_MechanoidEmploy();
			typeof(TradeDeal).GetMethod("AddTradeable", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(TradeSession.deal, new object[1] { tradeable_MechanoidEmploy });
			typeof(TradeDeal).GetMethod("UpdateTradeable", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(TradeSession.deal, null);
		}
		catch (Exception arg)
		{
			Log.Error($"Failed to initialize trade deal: {arg}");
		}
	}
}
