using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Chronopath;

public class Ability_TimeQuake : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		GameCondition_TimeQuake gameCondition_TimeQuake = (GameCondition_TimeQuake)GameConditionMaker.MakeCondition(VPE_DefOf.VPE_TimeQuake, ((Ability)this).GetDurationForPawn());
		gameCondition_TimeQuake.SafeRadius = ((Ability)this).GetRadiusForPawn();
		gameCondition_TimeQuake.Pawn = base.pawn;
		ThingWithComps obj = (ThingWithComps)ThingMaker.MakeThing(VPE_DefOf.VPE_SkyChanger);
		GenSpawn.Spawn(obj, base.pawn.Position, base.pawn.Map);
		obj.TryGetComp<CompAffectsSky>().StartFadeInHoldFadeOut(0, ((Ability)this).GetDurationForPawn(), 0);
		base.pawn.Map.gameConditionManager.RegisterCondition(gameCondition_TimeQuake);
		foreach (Faction item in Find.FactionManager.AllFactionsVisible)
		{
			if (item.CanChangeGoodwillFor(base.pawn.Faction, -10))
			{
				item.TryAffectGoodwillWith(base.pawn.Faction, -10, canSendMessage: true, canSendHostilityLetter: true, HistoryEventDefOf.UsedHarmfulAbility);
			}
			if (item.CanChangeGoodwillFor(base.pawn.Faction, -75) && base.pawn.Map.mapPawns.SpawnedPawnsInFaction(item).Count > 0)
			{
				item.TryAffectGoodwillWith(base.pawn.Faction, -75, canSendMessage: true, canSendHostilityLetter: true, HistoryEventDefOf.UsedHarmfulAbility);
			}
		}
	}
}
