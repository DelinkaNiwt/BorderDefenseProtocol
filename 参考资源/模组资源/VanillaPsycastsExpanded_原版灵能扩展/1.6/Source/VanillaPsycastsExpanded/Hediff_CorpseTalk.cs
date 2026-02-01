using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded;

public class Hediff_CorpseTalk : HediffWithComps
{
	public Dictionary<SkillDef, int> skillXPDifferences = new Dictionary<SkillDef, int>();

	public override void PostRemoved()
	{
		base.PostRemoved();
		ResetSkills();
	}

	public void ResetSkills()
	{
		foreach (KeyValuePair<SkillDef, int> skillXPDifference in skillXPDifferences)
		{
			pawn.skills.GetSkill(skillXPDifference.Key).Level = Mathf.Max(0, pawn.skills.GetSkill(skillXPDifference.Key).Level - skillXPDifference.Value);
		}
		skillXPDifferences.Clear();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref skillXPDifferences, "skillXPDifferences", LookMode.Def, LookMode.Value);
	}
}
