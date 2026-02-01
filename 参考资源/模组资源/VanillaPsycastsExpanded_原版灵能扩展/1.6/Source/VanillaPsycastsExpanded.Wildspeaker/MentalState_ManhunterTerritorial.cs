using RimWorld;
using Verse;
using Verse.AI;

namespace VanillaPsycastsExpanded.Wildspeaker;

public class MentalState_ManhunterTerritorial : MentalState_Manhunter
{
	public override bool ForceHostileTo(Faction f)
	{
		return f.HostileTo(Faction.OfPlayer);
	}

	public override bool ForceHostileTo(Thing t)
	{
		return t.HostileTo(Faction.OfPlayer);
	}
}
