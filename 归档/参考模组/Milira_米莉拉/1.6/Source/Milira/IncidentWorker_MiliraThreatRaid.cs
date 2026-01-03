using RimWorld;

namespace Milira;

public class IncidentWorker_MiliraThreatRaid : IncidentWorker_RaidEnemy
{
	public override bool FactionCanBeGroupSource(Faction f, IncidentParms parm, bool desperate = false)
	{
		if (base.FactionCanBeGroupSource(f, parm, desperate) && f.HostileTo(Faction.OfPlayer) && f.def.defName == "Milira_Faction")
		{
			if (!desperate)
			{
				return true;
			}
			return true;
		}
		return false;
	}
}
