using Verse;
using Verse.AI;

namespace Milira;

public class ThinkNode_ConditionalEquippedMelee : ThinkNode_Conditional
{
	protected override bool Satisfied(Pawn pawn)
	{
		return pawn.equipment.Primary == null || (pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsMeleeWeapon);
	}
}
