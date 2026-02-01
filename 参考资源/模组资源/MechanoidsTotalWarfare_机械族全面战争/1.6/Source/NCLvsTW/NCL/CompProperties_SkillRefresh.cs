using System.Collections.Generic;
using Verse;

namespace NCL;

public class CompProperties_SkillRefresh : CompProperties
{
	public List<ThingDef> targetPawnDefs;

	public int checkIntervalTicks = 60;

	public CompProperties_SkillRefresh()
	{
		compClass = typeof(Comp_SkillRefresh);
	}
}
