using Verse;

namespace AncotLibrary;

public class Verb_ShootThingCarrier : Verb_Shoot
{
	public VerbProperties_Custom VerbProps_Custom => (VerbProperties_Custom)verbProps;

	protected override bool TryCastShot()
	{
		CompThingCarrier_Custom compThingCarrier_Custom = Caster.TryGetComp<CompThingCarrier_Custom>();
		compThingCarrier_Custom?.TryRemoveThingInCarrier(VerbProps_Custom.chargeCostPerBurstShot);
		if (compThingCarrier_Custom != null && compThingCarrier_Custom.IngredientCount < VerbProps_Custom.chargeCostPerBurstShot)
		{
			return false;
		}
		return base.TryCastShot();
	}
}
