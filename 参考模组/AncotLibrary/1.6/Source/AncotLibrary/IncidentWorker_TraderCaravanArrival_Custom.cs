using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class IncidentWorker_TraderCaravanArrival_Custom : IncidentWorker_TraderCaravanArrival
{
	public ModExtension_TraderKindDef Extension => def.GetModExtension<ModExtension_TraderKindDef>();

	protected override bool TryResolveParmsGeneral(IncidentParms parms)
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(Extension.factionDef);
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
		parms.traderKind = DefDatabase<TraderKindDef>.AllDefsListForReading.Where((TraderKindDef t) => Extension.traderKindDefs.Contains(t)).RandomElementByWeight((TraderKindDef t) => TraderKindCommonality(t, map, parms.faction));
		return true;
	}

	protected override bool CanFireNowSub(IncidentParms parms)
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(Extension.factionDef);
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
