using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_PsychicComa : AbilityExtension_AbilityMod
{
	public float hours;

	public HediffDef coma;

	public StatDef multiplier;

	public int ticks;

	public bool autoApply = true;

	public virtual int GetComaDuration(Ability ability)
	{
		float num = hours * 2500f + (float)ticks;
		float statValue = ability.pawn.GetStatValue(multiplier ?? StatDefOf.PsychicSensitivity);
		return Mathf.FloorToInt(num * (Mathf.Approximately(statValue, 0f) ? 10f : (1f / statValue)));
	}

	public virtual void ApplyComa(Ability ability)
	{
		int comaDuration = GetComaDuration(ability);
		if (comaDuration > 0)
		{
			Hediff hediff = HediffMaker.MakeHediff(coma ?? VPE_DefOf.PsychicComa, ability.pawn);
			hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = comaDuration;
			ability.pawn.health.AddHediff(hediff);
		}
	}

	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		if (autoApply)
		{
			ApplyComa(ability);
		}
	}

	public override string GetDescription(Ability ability)
	{
		int comaDuration = GetComaDuration(ability);
		if (comaDuration > 0)
		{
			return string.Format("{0}: {1}", "VPE.PsychicComaDuration".Translate(), comaDuration.ToStringTicksToPeriod(allowSeconds: false)).Colorize(Color.red);
		}
		return string.Empty;
	}
}
