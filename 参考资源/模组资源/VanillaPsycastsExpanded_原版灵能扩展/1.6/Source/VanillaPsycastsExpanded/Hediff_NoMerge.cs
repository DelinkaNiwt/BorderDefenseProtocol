using Verse;

namespace VanillaPsycastsExpanded;

public class Hediff_NoMerge : HediffWithComps
{
	public override bool TryMergeWith(Hediff other)
	{
		return false;
	}
}
