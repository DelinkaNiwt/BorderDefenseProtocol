using RimWorld;
using Verse;

namespace AncotLibrary;

public class CompAbilityCheckApparelReloadable : CompAbilityEffect
{
	public new CompProperties_AbilityCheckApparelReloadable Props => (CompProperties_AbilityCheckApparelReloadable)props;

	public Apparel Apparel
	{
		get
		{
			Apparel apparel = parent.pawn.apparel.WornApparel.Find((Apparel a) => a.def == Props.apparel);
			if (apparel != null)
			{
				return apparel;
			}
			return null;
		}
	}

	public CompApparelReloadable_Custom CompReloadable => Apparel?.TryGetComp<CompApparelReloadable_Custom>();

	public override bool GizmoDisabled(out string reason)
	{
		Pawn pawn = parent.pawn;
		if (CompReloadable == null || CompReloadable.RemainingCharges < Props.consumeChargeAmount)
		{
			reason = "Ancot.Ability_ChargeLow".Translate();
			return true;
		}
		reason = "";
		return false;
	}

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		if (CompReloadable != null)
		{
			CompReloadable.UsedOnce(Props.consumeChargeAmount);
		}
	}
}
