using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Chronopath;

public class AbilityExtension_ReduceResistance : AbilityExtension_AbilityMod
{
	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (globalTargetInfo.Thing is Pawn { HostFaction: { } hostFaction, GuestStatus: GuestStatus.Prisoner } pawn && hostFaction == ability.pawn.Faction)
			{
				pawn.guest.resistance -= 20f;
			}
		}
	}
}
