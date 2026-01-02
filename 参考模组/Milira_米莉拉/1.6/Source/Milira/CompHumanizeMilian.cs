using RimWorld;
using Verse;

namespace Milira;

public class CompHumanizeMilian : CompUsable
{
	public CompProperties_HumanizeMilian Props_DressMilian => (CompProperties_HumanizeMilian)props;

	protected override string FloatMenuOptionLabel(Pawn pawn)
	{
		return "Milira_HumanizeMilian".Translate().Formatted(parent);
	}
}
