using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NCL;

internal class CompDetonate : CompAbilityEffect
{
	public new CompProperties_Detonate Props => (CompProperties_Detonate)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		Detonate();
	}

	public void Detonate()
	{
		List<Thing> ignoredThings = new List<Thing>();
		if (!Props.damageUser)
		{
			ignoredThings.Add(parent.pawn);
		}
		IntVec3 position = parent.pawn.Position;
		Map map = parent.pawn.Map;
		float radius = Props.radius;
		DamageDef damageType = Props.damageType;
		Pawn pawn = parent.pawn;
		int damageAmount = Props.damageAmount;
		float damagePenetration = Props.damagePenetration;
		SoundDef soundCreated = Props.soundCreated;
		ThingDef thingCreated = Props.thingCreated;
		float thingCreatedChance = Props.thingCreatedChance;
		float chanceToStartFire = Props.chanceToStartFire;
		List<Thing> ignoredThings2 = ignoredThings;
		GenExplosion.DoExplosion(position, map, radius, damageType, pawn, damageAmount, damagePenetration, soundCreated, null, null, null, thingCreated, thingCreatedChance, 1, null, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, chanceToStartFire, damageFalloff: false, null, ignoredThings2);
		if (Props.killUser)
		{
			parent.pawn.Kill(null, null);
		}
	}
}
