using RimWorld;
using Verse;

namespace Milira;

public class CompDressMilian : CompUsable
{
	public CompProperties_DressMilian Props_DressMilian => (CompProperties_DressMilian)props;

	protected override string FloatMenuOptionLabel(Pawn pawn)
	{
		return "Milira_DressMilian".Translate().Formatted(parent);
	}
}
