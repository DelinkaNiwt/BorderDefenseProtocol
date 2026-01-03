using AncotLibrary;
using Verse;
using Verse.AI;

namespace Milira;

public class ThinkNode_ConditionalCarrier : ThinkNode_Conditional
{
	public float minIngredientCount = 0f;

	protected override bool Satisfied(Pawn pawn)
	{
		CompThingCarrier_Custom val = ((Thing)pawn).TryGetComp<CompThingCarrier_Custom>();
		if (val == null || (float)val.IngredientCount < minIngredientCount)
		{
			return false;
		}
		return true;
	}
}
