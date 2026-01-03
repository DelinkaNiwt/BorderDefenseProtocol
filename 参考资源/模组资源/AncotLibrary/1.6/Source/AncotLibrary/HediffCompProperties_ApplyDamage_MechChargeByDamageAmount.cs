using System.Collections.Generic;
using Verse;

namespace AncotLibrary;

public class HediffCompProperties_ApplyDamage_MechChargeByDamageAmount : HediffCompProperties
{
	public List<DamageArmorCategoryDef> armorCategory = new List<DamageArmorCategoryDef>();

	public List<DamageDef> damageDefs = new List<DamageDef>();

	public float energyPerDMGTaken = 0.01f;

	public int cooldownTick = 60;

	public HediffCompProperties_ApplyDamage_MechChargeByDamageAmount()
	{
		compClass = typeof(HediffCompApplyDamage_MechChargeByDamageAmount);
	}
}
