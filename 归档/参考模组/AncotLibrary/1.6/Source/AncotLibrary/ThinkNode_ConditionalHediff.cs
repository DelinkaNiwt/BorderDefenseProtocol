using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalHediff : ThinkNode_Conditional
{
	public HediffDef hediffDef;

	protected override bool Satisfied(Pawn pawn)
	{
		Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
		return firstHediffOfDef != null;
	}
}
