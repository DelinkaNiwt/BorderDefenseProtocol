using System.Collections.Generic;
using Verse;

namespace NCLWorm;

public class Comp_DamageAbsorbedAndBoom : ThingComp
{
	public int absorbedTime = 24;

	public int boomTime = 60;

	public CompProperties_DamageAbsorbedAndBoom Props => (CompProperties_DamageAbsorbedAndBoom)props;

	public override void CompTick()
	{
		base.CompTick();
		if (absorbedTime > 0)
		{
			absorbedTime--;
		}
		if (boomTime > 0)
		{
			boomTime--;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref absorbedTime, "absorbedTime", 0);
		Scribe_Values.Look(ref boomTime, "boomTime", 0);
	}

	public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
	{
		base.PostPreApplyDamage(ref dinfo, out absorbed);
		if (absorbedTime <= 0)
		{
			absorbed = true;
			absorbedTime = 24;
		}
		if (boomTime <= 0)
		{
			GenExplosion.DoExplosion(parent.Position, parent.Map, Props.radius, Props.damageDef, parent, Props.amount, Props.armorPenetration, null, null, null, null, null, 0f, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, new List<Thing> { parent });
			boomTime = 60;
		}
	}
}
