using System.Collections.Generic;
using System.Linq;
using VEF.Abilities;
using Verse;

namespace VanillaPsycastsExpanded.Skipmaster;

public class AbilityExtension_Wallraise : AbilityExtension_AbilityMod
{
	public List<IntVec2> pattern;

	public float screenShakeIntensity;

	internal IEnumerable<IntVec3> AffectedCells(LocalTargetInfo target, Map map)
	{
		return from intVec in pattern
			select target.Cell + new IntVec3(intVec.x, 0, intVec.z) into intVec2
			where intVec2.InBounds(map)
			select intVec2;
	}
}
