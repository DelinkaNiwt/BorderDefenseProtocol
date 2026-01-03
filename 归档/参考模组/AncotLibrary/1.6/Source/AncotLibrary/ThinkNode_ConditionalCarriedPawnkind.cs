using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalCarriedPawnkind : ThinkNode_Conditional
{
	public List<string> pawnKindDefs = new List<string>();

	protected override bool Satisfied(Pawn pawn)
	{
		CompMechCarrier_Custom compMechCarrier_Custom = pawn.TryGetComp<CompMechCarrier_Custom>();
		if (compMechCarrier_Custom != null && !pawnKindDefs.NullOrEmpty())
		{
			return pawnKindDefs.Contains(compMechCarrier_Custom.SpawnPawnKind.ToString());
		}
		return false;
	}
}
