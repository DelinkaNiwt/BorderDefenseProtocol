using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Milira;

public class IncidentWorker_MiliraCluster : IncidentWorker
{
	protected override bool CanFireNowSub(IncidentParms parms)
	{
		if (!base.CanFireNowSub(parms))
		{
			return false;
		}
		if (!MiliraRaceSettings.MiliraRace_ModSetting_MilianClusterInMap)
		{
			return false;
		}
		if (Faction.OfPlayer.def.defName == "Milira_PlayerFaction" || Faction.OfPlayer.def.categoryTag == "Kiiro_PlayerFaction")
		{
			return false;
		}
		if (Current.Game.GetComponent<MiliraGameComponent_OverallControl>().turnToFriend)
		{
			return false;
		}
		return Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction)?.HostileTo(Faction.OfPlayer) ?? false;
	}

	protected override bool TryExecuteWorker(IncidentParms parms)
	{
		Map map = (Map)parms.target;
		MechClusterSketch sketch = MiliraClusterGenerator.GenerateClusterSketch(parms.points, map);
		IntVec3 center = MiliraClusterUtility.FindClusterPosition(map, sketch, 100, 0.5f);
		if (!center.IsValid)
		{
			return false;
		}
		IEnumerable<Thing> targets = from t in MiliraClusterUtility.SpawnCluster(center, map, sketch, dropInPods: true, canAssaultColony: true, parms.questTag)
			where t.def != ThingDefOf.Wall && t.def != ThingDefOf.Barricade
			select t;
		SendStandardLetter(parms, new LookTargets(targets));
		return true;
	}
}
