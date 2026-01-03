using Verse;

namespace AncotLibrary;

public class Verb_ChargeShoot : Verb_Shoot
{
	protected CompWeaponCharge compCharge => base.EquipmentSource.GetComp<CompWeaponCharge>();

	private CompRangeWeaponVerbSwitch_EnergyPassive compSwitch_EnergyPassive => base.EquipmentSource.GetComp<CompRangeWeaponVerbSwitch_EnergyPassive>();

	public VerbProperties_Custom verbProps_Custom => verbProps as VerbProperties_Custom;

	public override ThingDef Projectile
	{
		get
		{
			if (base.EquipmentSource != null && compCharge != null && compCharge.projectileCharged != null && compCharge.Charge > 0)
			{
				return compCharge.projectileCharged;
			}
			return verbProps.defaultProjectile;
		}
	}

	protected override bool TryCastShot()
	{
		if (verbProps_Custom != null && verbProps_Custom.disableWhenChargeEmpty && compCharge.ChargeState != An_ChargeState.Active)
		{
			return false;
		}
		bool flag = base.TryCastShot();
		if (flag && compCharge != null && compCharge.Charge > 0)
		{
			compCharge.ChargeFireEffect(caster, base.CurrentTarget.ToTargetInfo(caster.Map));
			if (verbProps_Custom != null)
			{
				compCharge.UsedOnce(verbProps_Custom.chargeCostPerBurstShot);
			}
			else
			{
				compCharge.UsedOnce();
			}
		}
		if (burstShotsLeft == 1)
		{
			compCharge?.Notify_AISwitchSecondVerb();
		}
		compSwitch_EnergyPassive?.Notify_SwitchPassive();
		return flag;
	}
}
