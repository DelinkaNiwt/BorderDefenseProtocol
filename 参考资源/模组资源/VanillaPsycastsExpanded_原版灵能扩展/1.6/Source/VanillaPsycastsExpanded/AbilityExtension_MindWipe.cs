using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using VEF;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded;

public class AbilityExtension_MindWipe : AbilityExtension_AbilityMod
{
	public override void Cast(GlobalTargetInfo[] targets, Ability ability)
	{
		((AbilityExtension_AbilityMod)this).Cast(targets, ability);
		foreach (GlobalTargetInfo globalTargetInfo in targets)
		{
			Pawn pawn = globalTargetInfo.Thing as Pawn;
			if (pawn.Faction != ability.pawn.Faction)
			{
				pawn.SetFaction(ability.pawn.Faction);
			}
			pawn.needs.mood.thoughts.memories.Memories.Clear();
			pawn.relations.ClearAllRelations();
			Dictionary<SkillDef, Passion> dictionary = new Dictionary<SkillDef, Passion>();
			foreach (SkillRecord skill in pawn.skills.skills)
			{
				dictionary[skill.def] = skill.passion;
			}
			pawn.skills = new Pawn_SkillTracker(pawn);
			NonPublicMethods.GenerateSkills(pawn, new PawnGenerationRequest(pawn.kindDef, pawn.Faction));
			foreach (KeyValuePair<SkillDef, Passion> item in dictionary)
			{
				pawn.skills.GetSkill(item.Key).passion = item.Value;
			}
			if (pawn.ideo.Ideo != ability.pawn.Ideo)
			{
				pawn.ideo.SetIdeo(ability.pawn.Ideo);
			}
		}
	}
}
