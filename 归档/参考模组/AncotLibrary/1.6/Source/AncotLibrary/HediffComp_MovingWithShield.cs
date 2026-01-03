using RimWorld;
using Verse;

namespace AncotLibrary;

public class HediffComp_MovingWithShield : HediffComp_Moving
{
	public bool anyShieldActive
	{
		get
		{
			CompPhysicalShield compPhysicalShield = base.Pawn.TryGetComp<CompPhysicalShield>();
			if (compPhysicalShield != null && compPhysicalShield.ShieldState == An_ShieldState.Active)
			{
				return true;
			}
			foreach (Apparel item in base.Pawn.apparel.WornApparel)
			{
				CompPhysicalShield compPhysicalShield2 = item.TryGetComp<CompPhysicalShield>();
				if (compPhysicalShield2 != null && compPhysicalShield2.ShieldState == An_ShieldState.Active)
				{
					return true;
				}
			}
			return false;
		}
	}

	public override bool InMovingState()
	{
		return base.InMovingState() && anyShieldActive;
	}
}
