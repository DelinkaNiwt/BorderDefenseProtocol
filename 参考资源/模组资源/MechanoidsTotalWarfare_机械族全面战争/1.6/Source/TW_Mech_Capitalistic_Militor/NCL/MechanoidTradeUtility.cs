using RimWorld;
using Verse;

namespace NCL;

public class MechanoidTradeUtility
{
	public static void HandleMechanoidTrade(ITrader trader, Thing tradedThing, Pawn playerNegotiator)
	{
		StuffProperties stuffProps = tradedThing.def.stuffProps;
		if (stuffProps != null && stuffProps.categories.Contains(StuffCategoryDefOf.Metallic) && trader is Pawn pawn)
		{
			Comp_MechEmployable comp = pawn.GetComp<Comp_MechEmployable>();
			if (comp != null)
			{
				float num = tradedThing.MarketValue * (float)tradedThing.stackCount;
				comp.Employ(num);
				Messages.Message("NCL.TRADE_EMPLOY_SUCCESS".Translate(tradedThing.LabelCap, pawn.LabelShort, (num / 100f).ToString("F1")), MessageTypeDefOf.PositiveEvent);
			}
		}
	}
}
