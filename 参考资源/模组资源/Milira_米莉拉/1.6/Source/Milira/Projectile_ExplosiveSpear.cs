using UnityEngine;
using Verse;

namespace Milira;

public class Projectile_ExplosiveSpear : Projectile
{
	private int ticksToDetonation;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref ticksToDetonation, "ticksToDetonation", 0);
	}

	protected override void Tick()
	{
		base.Tick();
		if (ticksToDetonation > 0)
		{
			ticksToDetonation--;
			if (ticksToDetonation <= 0)
			{
				Explode();
			}
		}
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (blockedByShield || def.projectile.explosionDelay == 0)
		{
			Explode();
			return;
		}
		landed = true;
		ticksToDetonation = def.projectile.explosionDelay;
		GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, def.projectile.damageDef, launcher.Faction, launcher);
	}

	protected virtual void Explode()
	{
		Map map = base.Map;
		Destroy();
		if (def.projectile.explosionEffect != null)
		{
			Effecter effecter = def.projectile.explosionEffect.Spawn();
			effecter.Trigger(new TargetInfo(base.Position, map), new TargetInfo(base.Position, map));
			effecter.Cleanup();
		}
		IntVec3 position = launcher.Position;
		IntVec3 position2 = base.Position;
		Vector3 v = (position2 - position).ToVector3();
		v.Normalize();
		float num = v.ToAngleFlat();
		IntVec3 position3 = base.Position;
		DamageDef damageDef = def.projectile.damageDef;
		Thing instigator = launcher;
		int damageAmount = DamageAmount;
		float armorPenetration = ArmorPenetration;
		SoundDef soundExplode = def.projectile.soundExplode;
		ThingDef weapon = equipmentDef;
		ThingDef projectile = def;
		Thing thing = intendedTarget.Thing;
		ThingDef postExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDef;
		ThingDef postExplosionSpawnThingDefWater = def.projectile.postExplosionSpawnThingDefWater;
		float postExplosionSpawnChance = def.projectile.postExplosionSpawnChance;
		int postExplosionSpawnThingCount = def.projectile.postExplosionSpawnThingCount;
		GasType? postExplosionGasType = def.projectile.postExplosionGasType;
		ThingDef preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
		float preExplosionSpawnChance = def.projectile.preExplosionSpawnChance;
		int preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
		bool applyDamageToExplosionCellsNeighbors = def.projectile.applyDamageToExplosionCellsNeighbors;
		float explosionChanceToStartFire = def.projectile.explosionChanceToStartFire;
		bool explosionDamageFalloff = def.projectile.explosionDamageFalloff;
		float? direction = origin.AngleToFlat(destination);
		float expolosionPropagationSpeed = def.projectile.damageDef.expolosionPropagationSpeed;
		float screenShakeFactor = def.projectile.screenShakeFactor;
		GenExplosion.DoExplosion(position3, map, 1.5f, damageDef, instigator, damageAmount, armorPenetration, soundExplode, weapon, projectile, thing, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, postExplosionGasType, null, 255, applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, explosionChanceToStartFire, explosionDamageFalloff, direction, null, null, doVisualEffects: true, expolosionPropagationSpeed, 0f, doSoundEffects: true, postExplosionSpawnThingDefWater, screenShakeFactor);
		if (num - 30f < -180f || num + 30f > 180f)
		{
			if (num < 0f)
			{
				IntVec3 position4 = base.Position;
				float explosionRadius = def.projectile.explosionRadius;
				DamageDef damageDef2 = def.projectile.damageDef;
				Thing instigator2 = launcher;
				int damageAmount2 = DamageAmount;
				float armorPenetration2 = ArmorPenetration;
				SoundDef soundExplode2 = def.projectile.soundExplode;
				ThingDef weapon2 = equipmentDef;
				ThingDef projectile2 = def;
				Thing thing2 = intendedTarget.Thing;
				ThingDef postExplosionSpawnThingDef2 = def.projectile.postExplosionSpawnThingDef;
				preExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDefWater;
				float postExplosionSpawnChance2 = def.projectile.postExplosionSpawnChance;
				int postExplosionSpawnThingCount2 = def.projectile.postExplosionSpawnThingCount;
				GasType? postExplosionGasType2 = def.projectile.postExplosionGasType;
				postExplosionSpawnThingDefWater = def.projectile.preExplosionSpawnThingDef;
				screenShakeFactor = def.projectile.preExplosionSpawnChance;
				preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
				explosionDamageFalloff = def.projectile.applyDamageToExplosionCellsNeighbors;
				expolosionPropagationSpeed = def.projectile.explosionChanceToStartFire;
				applyDamageToExplosionCellsNeighbors = def.projectile.explosionDamageFalloff;
				direction = origin.AngleToFlat(destination);
				explosionChanceToStartFire = def.projectile.damageDef.expolosionPropagationSpeed;
				preExplosionSpawnChance = def.projectile.screenShakeFactor;
				FloatRange? affectedAngle = new FloatRange(-180f, num + 30f);
				GenExplosion.DoExplosion(position4, map, explosionRadius, damageDef2, instigator2, damageAmount2, armorPenetration2, soundExplode2, weapon2, projectile2, thing2, postExplosionSpawnThingDef2, postExplosionSpawnChance2, postExplosionSpawnThingCount2, postExplosionGasType2, null, 255, explosionDamageFalloff, postExplosionSpawnThingDefWater, screenShakeFactor, preExplosionSpawnThingCount, expolosionPropagationSpeed, applyDamageToExplosionCellsNeighbors, direction, null, affectedAngle, doVisualEffects: true, explosionChanceToStartFire, 0f, doSoundEffects: true, preExplosionSpawnThingDef, preExplosionSpawnChance);
				IntVec3 position5 = base.Position;
				float explosionRadius2 = def.projectile.explosionRadius;
				DamageDef damageDef3 = def.projectile.damageDef;
				Thing instigator3 = launcher;
				int damageAmount3 = DamageAmount;
				float armorPenetration3 = ArmorPenetration;
				SoundDef soundExplode3 = def.projectile.soundExplode;
				ThingDef weapon3 = equipmentDef;
				ThingDef projectile3 = def;
				Thing thing3 = intendedTarget.Thing;
				ThingDef postExplosionSpawnThingDef3 = def.projectile.postExplosionSpawnThingDef;
				postExplosionSpawnThingDefWater = def.projectile.postExplosionSpawnThingDefWater;
				float postExplosionSpawnChance3 = def.projectile.postExplosionSpawnChance;
				int postExplosionSpawnThingCount3 = def.projectile.postExplosionSpawnThingCount;
				GasType? postExplosionGasType3 = def.projectile.postExplosionGasType;
				preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
				preExplosionSpawnChance = def.projectile.preExplosionSpawnChance;
				preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
				applyDamageToExplosionCellsNeighbors = def.projectile.applyDamageToExplosionCellsNeighbors;
				explosionChanceToStartFire = def.projectile.explosionChanceToStartFire;
				explosionDamageFalloff = def.projectile.explosionDamageFalloff;
				direction = origin.AngleToFlat(destination);
				expolosionPropagationSpeed = def.projectile.damageDef.expolosionPropagationSpeed;
				screenShakeFactor = def.projectile.screenShakeFactor;
				affectedAngle = new FloatRange(360f + num - 30f, 180f);
				GenExplosion.DoExplosion(position5, map, explosionRadius2, damageDef3, instigator3, damageAmount3, armorPenetration3, soundExplode3, weapon3, projectile3, thing3, postExplosionSpawnThingDef3, postExplosionSpawnChance3, postExplosionSpawnThingCount3, postExplosionGasType3, null, 255, applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, explosionChanceToStartFire, explosionDamageFalloff, direction, null, affectedAngle, doVisualEffects: true, expolosionPropagationSpeed, 0f, doSoundEffects: true, postExplosionSpawnThingDefWater, screenShakeFactor);
			}
			else
			{
				IntVec3 position6 = base.Position;
				float explosionRadius3 = def.projectile.explosionRadius;
				DamageDef damageDef4 = def.projectile.damageDef;
				Thing instigator4 = launcher;
				int damageAmount4 = DamageAmount;
				float armorPenetration4 = ArmorPenetration;
				SoundDef soundExplode4 = def.projectile.soundExplode;
				ThingDef weapon4 = equipmentDef;
				ThingDef projectile4 = def;
				Thing thing4 = intendedTarget.Thing;
				ThingDef postExplosionSpawnThingDef4 = def.projectile.postExplosionSpawnThingDef;
				preExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDefWater;
				float postExplosionSpawnChance4 = def.projectile.postExplosionSpawnChance;
				int postExplosionSpawnThingCount4 = def.projectile.postExplosionSpawnThingCount;
				GasType? postExplosionGasType4 = def.projectile.postExplosionGasType;
				postExplosionSpawnThingDefWater = def.projectile.preExplosionSpawnThingDef;
				screenShakeFactor = def.projectile.preExplosionSpawnChance;
				preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
				explosionDamageFalloff = def.projectile.applyDamageToExplosionCellsNeighbors;
				expolosionPropagationSpeed = def.projectile.explosionChanceToStartFire;
				applyDamageToExplosionCellsNeighbors = def.projectile.explosionDamageFalloff;
				direction = origin.AngleToFlat(destination);
				explosionChanceToStartFire = def.projectile.damageDef.expolosionPropagationSpeed;
				preExplosionSpawnChance = def.projectile.screenShakeFactor;
				FloatRange? affectedAngle = new FloatRange(num - 30f, 180f);
				GenExplosion.DoExplosion(position6, map, explosionRadius3, damageDef4, instigator4, damageAmount4, armorPenetration4, soundExplode4, weapon4, projectile4, thing4, postExplosionSpawnThingDef4, postExplosionSpawnChance4, postExplosionSpawnThingCount4, postExplosionGasType4, null, 255, explosionDamageFalloff, postExplosionSpawnThingDefWater, screenShakeFactor, preExplosionSpawnThingCount, expolosionPropagationSpeed, applyDamageToExplosionCellsNeighbors, direction, null, affectedAngle, doVisualEffects: true, explosionChanceToStartFire, 0f, doSoundEffects: true, preExplosionSpawnThingDef, preExplosionSpawnChance);
				IntVec3 position7 = base.Position;
				float explosionRadius4 = def.projectile.explosionRadius;
				DamageDef damageDef5 = def.projectile.damageDef;
				Thing instigator5 = launcher;
				int damageAmount5 = DamageAmount;
				float armorPenetration5 = ArmorPenetration;
				SoundDef soundExplode5 = def.projectile.soundExplode;
				ThingDef weapon5 = equipmentDef;
				ThingDef projectile5 = def;
				Thing thing5 = intendedTarget.Thing;
				ThingDef postExplosionSpawnThingDef5 = def.projectile.postExplosionSpawnThingDef;
				postExplosionSpawnThingDefWater = def.projectile.postExplosionSpawnThingDefWater;
				float postExplosionSpawnChance5 = def.projectile.postExplosionSpawnChance;
				int postExplosionSpawnThingCount5 = def.projectile.postExplosionSpawnThingCount;
				GasType? postExplosionGasType5 = def.projectile.postExplosionGasType;
				preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
				preExplosionSpawnChance = def.projectile.preExplosionSpawnChance;
				preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
				applyDamageToExplosionCellsNeighbors = def.projectile.applyDamageToExplosionCellsNeighbors;
				explosionChanceToStartFire = def.projectile.explosionChanceToStartFire;
				explosionDamageFalloff = def.projectile.explosionDamageFalloff;
				direction = origin.AngleToFlat(destination);
				expolosionPropagationSpeed = def.projectile.damageDef.expolosionPropagationSpeed;
				screenShakeFactor = def.projectile.screenShakeFactor;
				affectedAngle = new FloatRange(-180f, -360f + num + 30f);
				GenExplosion.DoExplosion(position7, map, explosionRadius4, damageDef5, instigator5, damageAmount5, armorPenetration5, soundExplode5, weapon5, projectile5, thing5, postExplosionSpawnThingDef5, postExplosionSpawnChance5, postExplosionSpawnThingCount5, postExplosionGasType5, null, 255, applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, explosionChanceToStartFire, explosionDamageFalloff, direction, null, affectedAngle, doVisualEffects: true, expolosionPropagationSpeed, 0f, doSoundEffects: true, postExplosionSpawnThingDefWater, screenShakeFactor);
			}
		}
		else
		{
			IntVec3 position8 = base.Position;
			float explosionRadius5 = def.projectile.explosionRadius;
			DamageDef damageDef6 = def.projectile.damageDef;
			Thing instigator6 = launcher;
			int damageAmount6 = DamageAmount;
			float armorPenetration6 = ArmorPenetration;
			SoundDef soundExplode6 = def.projectile.soundExplode;
			ThingDef weapon6 = equipmentDef;
			ThingDef projectile6 = def;
			Thing thing6 = intendedTarget.Thing;
			ThingDef postExplosionSpawnThingDef6 = def.projectile.postExplosionSpawnThingDef;
			preExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDefWater;
			float postExplosionSpawnChance6 = def.projectile.postExplosionSpawnChance;
			int postExplosionSpawnThingCount6 = def.projectile.postExplosionSpawnThingCount;
			GasType? postExplosionGasType6 = def.projectile.postExplosionGasType;
			postExplosionSpawnThingDefWater = def.projectile.preExplosionSpawnThingDef;
			screenShakeFactor = def.projectile.preExplosionSpawnChance;
			preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
			explosionDamageFalloff = def.projectile.applyDamageToExplosionCellsNeighbors;
			expolosionPropagationSpeed = def.projectile.explosionChanceToStartFire;
			applyDamageToExplosionCellsNeighbors = def.projectile.explosionDamageFalloff;
			direction = origin.AngleToFlat(destination);
			explosionChanceToStartFire = def.projectile.damageDef.expolosionPropagationSpeed;
			preExplosionSpawnChance = def.projectile.screenShakeFactor;
			FloatRange? affectedAngle = new FloatRange(num - 30f, num + 30f);
			GenExplosion.DoExplosion(position8, map, explosionRadius5, damageDef6, instigator6, damageAmount6, armorPenetration6, soundExplode6, weapon6, projectile6, thing6, postExplosionSpawnThingDef6, postExplosionSpawnChance6, postExplosionSpawnThingCount6, postExplosionGasType6, null, 255, explosionDamageFalloff, postExplosionSpawnThingDefWater, screenShakeFactor, preExplosionSpawnThingCount, expolosionPropagationSpeed, applyDamageToExplosionCellsNeighbors, direction, null, affectedAngle, doVisualEffects: true, explosionChanceToStartFire, 0f, doSoundEffects: true, preExplosionSpawnThingDef, preExplosionSpawnChance);
		}
	}
}
