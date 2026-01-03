using RimWorld;
using Verse;

namespace Milira;

public class PawnColumnWorker_BillWorkerDisplayMilian : PawnColumnWorker_Checkbox
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
		return compMilianApparelRender.displayInBillWorker;
	}

	protected override void SetValue(Pawn pawn, bool value, PawnTable table)
	{
		CompMilianApparelRender compMilianApparelRender = pawn.TryGetComp<CompMilianApparelRender>();
		compMilianApparelRender.displayInBillWorker = value;
	}
}
