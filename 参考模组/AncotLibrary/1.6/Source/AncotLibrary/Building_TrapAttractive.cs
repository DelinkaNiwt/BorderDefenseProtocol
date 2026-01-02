using Verse;

namespace AncotLibrary;

public class Building_TrapAttractive : Building_TrapArea
{
	protected override void SpringSub(Pawn p)
	{
		GetComp<CompGravitationalTrap>().StartWick();
	}
}
