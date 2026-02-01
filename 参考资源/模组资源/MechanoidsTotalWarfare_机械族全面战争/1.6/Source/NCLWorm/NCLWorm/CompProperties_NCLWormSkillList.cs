using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCLWorm;

public class CompProperties_NCLWormSkillList : CompProperties
{
	public List<AbilityDef> skill;

	public CompProperties_NCLWormSkillList()
	{
		compClass = typeof(Comp_NCLWormSkillList);
	}
}
