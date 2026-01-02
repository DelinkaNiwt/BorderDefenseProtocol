using Verse;

namespace AncotLibrary;

public class Verb_ChargeShootSustained : Verb_ShootSustained
{
	protected CompWeaponCharge compCharge => base.EquipmentSource.GetComp<CompWeaponCharge>();

	private CompRangeWeaponVerbSwitch_EnergyPassive compSwitch_EnergyPassive => base.EquipmentSource.GetComp<CompRangeWeaponVerbSwitch_EnergyPassive>();

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
		if (base.VerbProps_Custom.disableWhenChargeEmpty && compCharge.ChargeState != An_ChargeState.Active)
		{
			base.CompSustainedShoot.ResetCached();
			return false;
		}
		bool flag = base.TryCastShot();
		if (flag && compCharge != null && compCharge.Charge > 0)
		{
			compCharge?.ChargeFireEffect(caster, base.CurrentTarget.ToTargetInfo(caster.Map));
			compCharge?.UsedOnce(base.VerbProps_Custom.chargeCostPerBurstShot);
		}
		if (burstShotsLeft == 1)
		{
			compCharge?.Notify_AISwitchSecondVerb();
		}
		compSwitch_EnergyPassive?.Notify_SwitchPassive();
		return flag;
	}
}
