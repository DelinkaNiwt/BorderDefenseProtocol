using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityCheckArea : CompAbilityEffect
{
	private Pawn pawn => parent.pawn;

	public new CompProperties_AbilityCheckClearArea Props => (CompProperties_AbilityCheckClearArea)props;

	public override bool CanCast => ClearAreaAround();

	public override bool GizmoDisabled(out string reason)
	{
		if (!ClearAreaAround())
		{
			reason = "Milira_NotEnoughSpace".Translate();
			return true;
		}
		reason = "";
		return false;
	}

	public bool ClearAreaAround()
	{
		IntVec3 position = pawn.Position;
		foreach (IntVec3 item in GenRadial.RadialCellsAround(position, Props.radius, useCenter: false))
		{
			if (!item.IsValid || !item.InBounds(pawn.Map) || !item.Walkable(pawn.Map) || !item.GetEdifice(pawn.Map).DestroyedOrNull() || (!Props.canRoofed && item.Roofed(pawn.Map)))
			{
				return false;
			}
		}
		return true;
	}
}
