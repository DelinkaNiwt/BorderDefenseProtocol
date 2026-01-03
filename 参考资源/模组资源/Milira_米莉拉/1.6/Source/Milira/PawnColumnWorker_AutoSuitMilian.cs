using RimWorld;
using Verse;

namespace Milira;

public class PawnColumnWorker_AutoSuitMilian : PawnColumnWorker_Checkbox
{
	protected override bool HasCheckbox(Pawn pawn)
	{
		CompMilianApparelRender compMilianApparelRender = pawn.TryGetComp<CompMilianApparelRender>();
		if (compMilianApparelRender != null)
		{
			return true;
		}
		return false;
	}

	protected override bool GetValue(Pawn pawn)
	{
		CompMilianApparelRender compMilianApparelRender = pawn.TryGetComp<CompMilianApparelRender>();
		return compMilianApparelRender.autoSuitUp;
	}

	protected override void SetValue(Pawn pawn, bool value, PawnTable table)
	{
		CompMilianApparelRender compMilianApparelRender = pawn.TryGetComp<CompMilianApparelRender>();
		compMilianApparelRender.autoSuitUp = value;
	}
}
