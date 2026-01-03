using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompProperties_WeaponCharge : CompProperties
{
	public ThingDef projectileCharged;

	public ThingDef projectileCharged_Switched;

	public float chargeOnResetRatio = 1f;

	public int chargePerUse = 1;

	public bool canUseIfHasCharge = false;

	public bool destroyOnEmpty = false;

	public bool autoRecharge = true;

	public bool resetAfterEmpty = true;

	public bool ai_SwitchSecondVerbOnlyIfChargeNotEmpty = false;

	public bool maxChargeCanMultiply = true;

	public bool maxChargeSpeedCanMultiply = true;

	public bool resetVerbOnEmpty = false;

	public float meleeDamageFactorCharged = 1f;

	public float meleeArmorPenetrationFactorCharged = 1f;

	public EffecterDef chargeFireEffecter;

	public Color barColor = new Color(0.35f, 0.35f, 0.2f);

	public CompProperties_WeaponCharge()
	{
		compClass = typeof(CompWeaponCharge);
	}
}
