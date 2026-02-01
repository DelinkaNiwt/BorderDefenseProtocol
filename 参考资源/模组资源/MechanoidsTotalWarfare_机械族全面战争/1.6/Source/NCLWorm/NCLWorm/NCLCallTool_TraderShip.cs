using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCLWorm;

public class NCLCallTool_TraderShip : NCLCallTool
{
	public List<TraderKindDef> TraderKindDefs;

	public int CooldownTick = 300000;

	public string ChooseTrader;

	public string NoChoose;

	public override void Action()
	{
		Pawn usedBy = windows.usedBy;
		windows.Close();
		if ((bool)Canuse())
		{
			Find.WindowStack.Add(new Window_NCLcall(usedBy, NCLCall, ChooseTrader, useBaseFunction: false, this));
		}
		else
		{
			Find.WindowStack.Add(new Window_NCLcall(usedBy, NCLCall, NoChoose, useBaseFunction: false, this));
		}
	}

	public void SecAction(TraderKindDef tradeShips)
	{
		Pawn usedBy = windows.usedBy;
		windows.Close();
		Map map = usedBy.Map;
		TradeShip tradeShip = new TradeShip(tradeShips);
		if (map.listerBuildings.allBuildingsColonist.Any((Building b) => b.def.IsCommsConsole && (b.GetComp<CompPowerTrader>() == null || b.GetComp<CompPowerTrader>().PowerOn)))
		{
			Find.LetterStack.ReceiveLetter(tradeShip.def.LabelCap, "TraderArrival".Translate(tradeShip.name, tradeShip.def.label, (tradeShip.Faction == null) ? "TraderArrivalNoFaction".Translate() : "TraderArrivalFromFaction".Translate(tradeShip.Faction.Named("FACTION"))), LetterDefOf.PositiveEvent, null, null, null, null, null, 0, playSound: true);
		}
		map.passingShipManager.AddShip(tradeShip);
		tradeShip.GenerateThings();
		Current.Game.GetComponent<GameComp_NCLWorm>().tradetime = CooldownTick;
	}

	public void TriAction()
	{
		Pawn usedBy = windows.usedBy;
		windows.Close();
		Find.WindowStack.Add(new Window_NCLcall(usedBy, NCLCall));
	}

	public override AcceptanceReport Canuse()
	{
		if (Current.Game.GetComponent<GameComp_NCLWorm>().tradetime > 0)
		{
			return "NCLSradeShipCooldownTime".Translate(Current.Game.GetComponent<GameComp_NCLWorm>().tradetime.TicksToDays().ToString("F2"));
		}
		return true;
	}
}
