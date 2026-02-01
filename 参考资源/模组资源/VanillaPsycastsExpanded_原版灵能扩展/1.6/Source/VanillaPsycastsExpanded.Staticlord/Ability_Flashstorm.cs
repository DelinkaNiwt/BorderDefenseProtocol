using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Staticlord;

public class Ability_Flashstorm : Ability
{
	private readonly HashSet<Faction> affectedFactionCache = new HashSet<Faction>();

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		for (int i = 0; i < targets.Length; i++)
		{
			GlobalTargetInfo target = targets[i];
			Map map = target.Map;
			Thing conditionCauser = GenSpawn.Spawn(ThingDefOf.Flashstorm, target.Cell, base.pawn.Map);
			GameCondition_PsychicFlashstorm gameCondition_PsychicFlashstorm = (GameCondition_PsychicFlashstorm)GameConditionMaker.MakeCondition(VPE_DefOf.VPE_PsychicFlashstorm);
			gameCondition_PsychicFlashstorm.centerLocation = target.Cell.ToIntVec2;
			gameCondition_PsychicFlashstorm.areaRadiusOverride = new IntRange(Mathf.RoundToInt(((Ability)this).GetRadiusForPawn()), Mathf.RoundToInt(((Ability)this).GetRadiusForPawn()));
			gameCondition_PsychicFlashstorm.Duration = ((Ability)this).GetDurationForPawn();
			gameCondition_PsychicFlashstorm.suppressEndMessage = true;
			gameCondition_PsychicFlashstorm.initialStrikeDelay = new IntRange(0, 0);
			gameCondition_PsychicFlashstorm.conditionCauser = conditionCauser;
			gameCondition_PsychicFlashstorm.ambientSound = true;
			gameCondition_PsychicFlashstorm.numStrikes = Mathf.FloorToInt(((Ability)this).GetPowerForPawn());
			map.gameConditionManager.RegisterCondition(gameCondition_PsychicFlashstorm);
			ApplyGoodwillImpact(target, gameCondition_PsychicFlashstorm.AreaRadius);
		}
	}

	private void ApplyGoodwillImpact(GlobalTargetInfo target, int radius)
	{
		if (base.pawn.Faction != Faction.OfPlayer)
		{
			return;
		}
		affectedFactionCache.Clear();
		foreach (Thing item in GenRadial.RadialDistinctThingsAround(target.Cell, target.Map, radius, useCenter: true))
		{
			if (item is Pawn p && item.Faction != null && item.Faction != base.pawn.Faction && !item.Faction.HostileTo(base.pawn.Faction) && !affectedFactionCache.Contains(item.Faction) && (base.def.applyGoodwillImpactToLodgers || !p.IsQuestLodger()))
			{
				affectedFactionCache.Add(item.Faction);
				Faction.OfPlayer.TryAffectGoodwillWith(item.Faction, base.def.goodwillImpact, canSendMessage: true, canSendHostilityLetter: true, HistoryEventDefOf.UsedHarmfulAbility);
			}
		}
		affectedFactionCache.Clear();
	}
}
