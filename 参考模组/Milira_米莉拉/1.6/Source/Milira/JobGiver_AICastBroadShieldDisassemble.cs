using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class JobGiver_AICastBroadShieldDisassemble : JobGiver_AICastAbility
{
	protected override LocalTargetInfo GetTarget(Pawn caster, Ability ability)
	{
		Thing thing = GenClosest.ClosestThingReachable(caster.Position, caster.Map, ThingRequest.ForDef(MiliraDefOf.Milian_BroadShieldUnit), PathEndMode.Touch, TraverseParms.For(caster), 15.9f, (Thing t) => t.Faction.HostileTo(caster.Faction));
		if (caster.CanReserve(thing))
		{
			return new LocalTargetInfo(thing);
		}
		return LocalTargetInfo.Invalid;
	}
}
