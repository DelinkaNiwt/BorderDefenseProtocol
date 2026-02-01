using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace NCL;

public class CompTrader : ThingComp, ITrader
{
	public Faction Faction => (parent as Pawn)?.Faction;

	public float TradePriceImprovementOffsetForPlayer => 0f;

	public CompProperties_Trader Props => (CompProperties_Trader)props;

	public Pawn TraderPawn => parent as Pawn;

	public TraderKindDef TraderKind
	{
		get
		{
			if (string.IsNullOrEmpty(Props.traderDefName))
			{
				Log.Error("TraderDefName is null or empty in CompTrader!");
				return null;
			}
			TraderKindDef namedSilentFail = DefDatabase<TraderKindDef>.GetNamedSilentFail(Props.traderDefName);
			if (namedSilentFail == null)
			{
				Log.Error("TraderKindDef '" + Props.traderDefName + "' not found!");
			}
			return namedSilentFail;
		}
	}

	public IEnumerable<Thing> Goods
	{
		get
		{
			yield return new Thing
			{
				def = ThingDefOf.Silver,
				stackCount = 10000
			};
		}
	}

	public int RandomPriceFactorSeed => Gen.HashCombineInt(TraderPawn.thingIDNumber, 1149275593);

	public string TraderName => TraderPawn?.LabelShortCap ?? "机械单位";

	public bool CanTradeNow => TraderPawn != null && TraderPawn.Spawned;

	public TradeCurrency TradeCurrency => TradeCurrency.Silver;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		if (TraderPawn != null)
		{
			TraderPawn.playerSettings = new Pawn_PlayerSettings(TraderPawn);
		}
	}

	public void StartTradeWithEmploy()
	{
		Pawn pawn = Find.CurrentMap.mapPawns.FreeColonists.FirstOrDefault();
		if (pawn == null)
		{
			Log.Error("没有可用的殖民者作为谈判代表");
			return;
		}
		TradeSession.SetupWith(this, pawn, giftMode: false);
		TradeSession.deal = new TradeDeal();
		Tradeable tradeable = Props.CreateEmployTradeable();
		typeof(TradeDeal).GetMethod("AddTradeable", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(TradeSession.deal, new object[1] { tradeable });
		Find.WindowStack.Add(new Dialog_Trade(pawn, this));
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
	}

	public IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
	{
		if (playerNegotiator?.Map == null)
		{
			yield break;
		}
		foreach (Thing thing in playerNegotiator.Map.listerThings.AllThings)
		{
			if (thing.def.stuffProps?.categories.Contains(StuffCategoryDefOf.Metallic) ?? false)
			{
				if (thing.IsInAnyStorage() || playerNegotiator.Map.areaManager.Home[thing.Position])
				{
					yield return thing;
				}
			}
			else if (thing.def == ThingDefOf.Silver && (thing.IsInAnyStorage() || playerNegotiator.Map.areaManager.Home[thing.Position]))
			{
				yield return thing;
			}
		}
	}

	public void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
	{
		StuffProperties stuffProps = toGive.def.stuffProps;
		if (stuffProps != null && stuffProps.categories.Contains(StuffCategoryDefOf.Metallic))
		{
			Comp_MechEmployable comp_MechEmployable = (parent as Pawn)?.GetComp<Comp_MechEmployable>();
			if (comp_MechEmployable != null)
			{
				float num = toGive.MarketValue * (float)countToGive;
				comp_MechEmployable.Employ(num);
				Messages.Message($"已用 {toGive.LabelCap} 雇佣 {parent.LabelShort} {num / comp_MechEmployable.Props.silverPerDay:F1}天", MessageTypeDefOf.PositiveEvent);
			}
		}
		toGive.SplitOff(countToGive).Destroy();
	}

	public void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
	{
	}
}
