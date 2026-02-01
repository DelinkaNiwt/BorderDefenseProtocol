using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Wildspeaker;

public class Ability_Animal : Ability
{
	public override void Cast(params GlobalTargetInfo[] targets)
	{
		((Ability)this).Cast(targets);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			if (!(globalTargetInfo.Thing is Pawn pawn) || !pawn.AnimalOrWildMan())
			{
				continue;
			}
			bool flag = pawn.MentalStateDef == MentalStateDefOf.Manhunter || pawn.MentalStateDef == MentalStateDefOf.ManhunterPermanent;
			if (Rand.Chance(GetSuccessChanceOn(pawn)))
			{
				if (flag)
				{
					pawn.MentalState.RecoverFromState();
				}
				else
				{
					InteractionWorker_RecruitAttempt.DoRecruit(base.pawn, pawn);
				}
			}
			else if (!flag)
			{
				pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, "AnimalManhunterFromTaming".Translate(), forced: true, forceWake: false, causedByMood: false, null, transitionSilently: false, causedByDamage: false, causedByPsycast: true);
			}
		}
	}

	private float GetSuccessChanceOn(Pawn target)
	{
		return base.pawn.GetStatValue(StatDefOf.PsychicSensitivity) - target.def.GetStatValueAbstract(StatDefOf.Wildness);
	}

	public override void OnGUI(LocalTargetInfo target)
	{
		((Ability)this).OnGUI(target);
		List<GlobalTargetInfo> list = base.currentTargets.Where((GlobalTargetInfo t) => t.IsValid && t.Map != null).ToList();
		GlobalTargetInfo[] array = new GlobalTargetInfo[list.Count + 1];
		list.CopyTo(array, 0);
		array[array.Length - 1] = target.ToGlobalTargetInfo(list?.LastOrDefault().Map ?? base.pawn.Map);
		((Ability)this).ModifyTargets(ref array);
		GlobalTargetInfo[] array2 = array;
		foreach (GlobalTargetInfo globalTargetInfo in array2)
		{
			if (globalTargetInfo.Thing is Pawn pawn)
			{
				if (pawn.AnimalOrWildMan() && GetSuccessChanceOn(pawn) > float.Epsilon)
				{
					float successChanceOn = GetSuccessChanceOn(pawn);
					Vector3 drawPos = pawn.DrawPos;
					drawPos.z += 1f;
					Color color = ((successChanceOn < 0.33f) ? Color.yellow : ((!(successChanceOn < 0.66f)) ? Color.green : Color.white));
					Color textColor = color;
					GenMapUI.DrawText(new Vector2(drawPos.x, drawPos.z), "VPE.SuccessChance".Translate() + ": " + successChanceOn.ToStringPercent(), textColor);
				}
				else
				{
					Vector3 drawPos2 = pawn.DrawPos;
					drawPos2.z += 1f;
					GenMapUI.DrawText(new Vector2(drawPos2.x, drawPos2.z), "Ineffective".Translate(), Color.red);
				}
			}
		}
	}
}
