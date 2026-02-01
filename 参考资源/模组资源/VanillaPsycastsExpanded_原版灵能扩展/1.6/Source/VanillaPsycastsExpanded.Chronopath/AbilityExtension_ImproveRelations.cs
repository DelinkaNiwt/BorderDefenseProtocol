using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Chronopath;

public class AbilityExtension_ImproveRelations : AbilityExtension_AbilityMod
{
	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Thing thing = globalTargetInfo.Thing;
			if (thing is Pawn pawn)
			{
				Faction faction = thing.Faction;
				if (faction != null && !faction.IsPlayer && pawn.Faction.RelationKindWith(ability.pawn.Faction) != FactionRelationKind.Hostile && pawn.guest.HostFaction == null)
				{
					pawn.Faction.TryAffectGoodwillWith(ability.pawn.Faction, 20, canSendMessage: true, canSendHostilityLetter: true, VPE_DefOf.VPE_Foretelling);
				}
			}
		}
	}
}
