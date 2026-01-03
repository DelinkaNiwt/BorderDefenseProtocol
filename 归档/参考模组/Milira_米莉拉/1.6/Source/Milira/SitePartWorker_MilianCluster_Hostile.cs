using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Milira;

public class SitePartWorker_MilianCluster_Hostile : SitePartWorker
{
	public const float MinPoints = 750f;

	public override bool IsAvailable()
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		if (base.IsAvailable())
		{
			return faction?.HostileTo(Faction.OfPlayer) ?? false;
		}
		return false;
	}

	public override string GetArrivedLetterPart(Map map, out LetterDef preferredLetterDef, out LookTargets lookTargets)
	{
		string arrivedLetterPart = base.GetArrivedLetterPart(map, out preferredLetterDef, out lookTargets);
		lookTargets = new LookTargets(map.Parent);
		return arrivedLetterPart;
	}

	public override SitePartParams GenerateDefaultParams(float myThreatPoints, PlanetTile tile, Faction faction)
	{
		SitePartParams sitePartParams = base.GenerateDefaultParams(myThreatPoints, tile, faction);
		sitePartParams.threatPoints = Mathf.Max(sitePartParams.threatPoints, 750f);
		return sitePartParams;
	}
}
