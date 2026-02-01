using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Harmonist;

public class Ability_Skillroll : Ability
{
	private static readonly Func<Pawn, SkillDef, PawnGenerationRequest, int> finalLevelOfSkill = AccessTools.Method(typeof(PawnGenerator), "FinalLevelOfSkill").CreateDelegate<Func<Pawn, SkillDef, PawnGenerationRequest, int>>();

	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		Pawn pawn = targets[0].Thing as Pawn;
		int num = 0;
		PawnKindDef kindDef = pawn.kindDef;
		DevelopmentalStage developmentalStage = pawn.DevelopmentalStage;
		PawnGenerationRequest arg = new PawnGenerationRequest(kindDef, null, PawnGenerationContext.NonPlayer, null, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f, developmentalStage);
		foreach (SkillRecord skill in pawn.skills.skills)
		{
			int levelInt = skill.levelInt;
			skill.levelInt = finalLevelOfSkill(pawn, skill.def, arg);
			num += levelInt - skill.levelInt;
		}
		num = Mathf.RoundToInt((float)num * 1.1f);
		for (int i = 0; i < num; i++)
		{
			pawn.skills.skills.Where((SkillRecord skill) => !skill.TotallyDisabled && skill.levelInt < 20).RandomElement().levelInt++;
		}
	}

	public override bool CanHitTarget(LocalTargetInfo target)
	{
		if (((Ability)this).CanHitTarget(target))
		{
			Pawn pawn = target.Pawn;
			if (pawn != null)
			{
				Faction faction = pawn.Faction;
				if (faction != null)
				{
					pawn = base.pawn;
					if (pawn != null)
					{
						Faction faction2 = pawn.Faction;
						if (faction2 != null)
						{
							if (faction2 != faction)
							{
								return faction2.RelationKindWith(faction) == FactionRelationKind.Ally;
							}
							return true;
						}
					}
				}
			}
		}
		return false;
	}
}
