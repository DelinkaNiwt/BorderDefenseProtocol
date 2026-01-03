using Verse;

namespace AncotLibrary;

public class Building_TrapRepulsive : Building_TrapArea
{
	protected override void SpringSub(Pawn p)
	{
		GetComp<CompRepulsiveTrap>().StartWick();
	}
}
