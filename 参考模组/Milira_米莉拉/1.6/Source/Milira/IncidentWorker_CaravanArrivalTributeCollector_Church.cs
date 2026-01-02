using System.Linq;
using RimWorld;
using Verse;

namespace Milira;

public class IncidentWorker_CaravanArrivalTributeCollector_Church : IncidentWorker_TraderCaravanArrival
{
	protected override bool TryResolveParmsGeneral(IncidentParms parms)
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_AngelismChurch);
		if (!base.TryResolveParmsGeneral(parms))
		{
			return false;
		}
		if (faction == null)
		{
			return false;
		}
		Map map = (Map)parms.target;
		parms.faction = faction;
		parms.traderKind = DefDatabase<TraderKindDef>.AllDefsListForReading.Where((TraderKindDef t) => t == MiliraDefOf.Milira_Church_Caravan_TributeCollector).RandomElementByWeight((TraderKindDef t) => TraderKindCommonality(t, map, parms.faction));
		return true;
	}

	protected override bool CanFireNowSub(IncidentParms parms)
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_AngelismChurch);
		if (!base.CanFireNowSub(parms) || faction == null)
		{
			return false;
		}
		return FactionCanBeGroupSource(faction, parms);
	}

	protected override float TraderKindCommonality(TraderKindDef traderKind, Map map, Faction faction)
	{
		return traderKind.CalculatedCommonality;
	}
}
