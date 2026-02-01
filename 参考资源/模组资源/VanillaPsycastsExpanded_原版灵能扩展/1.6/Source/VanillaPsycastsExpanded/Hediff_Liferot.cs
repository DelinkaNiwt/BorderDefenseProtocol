using System.Linq;
using Verse;

namespace VanillaPsycastsExpanded;

public class Hediff_Liferot : HediffWithComps
{
	public override void Tick()
	{
		base.Tick();
		if (pawn.IsHashIntervalTick(60) && (from x in pawn.health.hediffSet.GetNotMissingParts()
			where x.coverageAbs > 0f
			select x).TryRandomElement(out var result))
		{
			pawn.TakeDamage(new DamageInfo(VPE_DefOf.VPE_Rot, 1f, 0f, -1f, null, result));
		}
	}
}
