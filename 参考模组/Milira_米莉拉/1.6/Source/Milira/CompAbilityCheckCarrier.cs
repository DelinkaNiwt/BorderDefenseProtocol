using AncotLibrary;
using RimWorld;
using Verse;

namespace Milira;

public class CompAbilityCheckCarrier : CompAbilityEffect
{
	public new CompProperties_AbilityCheckCarrier Props => (CompProperties_AbilityCheckCarrier)props;

	private Pawn Pawn => parent.pawn;

	private CompThingCarrier_Custom Carrier => ((Thing)Pawn).TryGetComp<CompThingCarrier_Custom>();

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		if (Carrier != null && Props.ingredientCost != 0)
		{
			Carrier.TryRemoveThingInCarrier(Props.ingredientCost);
		}
	}

	public override bool GizmoDisabled(out string reason)
	{
		if (Carrier == null || Carrier.IngredientCount < Props.minIngredientCount)
		{
			reason = "Ancot.NoIngredientAbility".Translate();
			return true;
		}
		reason = "";
		return false;
	}
}
