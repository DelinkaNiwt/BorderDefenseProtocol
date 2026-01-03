using RimWorld;

namespace Milira;

public class IncidentWorker_RaidMilira : IncidentWorker_RaidEnemy
{
	protected override bool CanFireNowSub(IncidentParms parms)
	{
		if (!base.CanFireNowSub(parms))
		{
			return false;
		}
		if (FiredTooRecently(parms.target))
		{
			return false;
		}
		return true;
	}
}
