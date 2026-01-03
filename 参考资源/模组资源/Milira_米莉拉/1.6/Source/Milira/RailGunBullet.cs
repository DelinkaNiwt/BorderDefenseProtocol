using AncotLibrary;
using UnityEngine;
using Verse;

namespace Milira;

public class RailGunBullet : Projectile_Custom
{
	public override bool AnimalsFleeImpact => true;

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Find.CameraDriver.shaker.DoShake(2f);
		Map map = ((Thing)this).Map;
		IntVec3 position = ((Projectile)this).launcher.Position;
		IntVec3 position2 = ((Thing)this).Position;
		Vector3 v = (position2 - position).ToVector3();
		v.Normalize();
		float num = v.ToAngleFlat();
		if (hitThing != null)
		{
			position2 = hitThing.Position;
		}
		IntVec3 center = position2;
		DamageDef milira_KineticBomb = MiliraDefOf.Milira_KineticBomb;
		Thing launcher = ((Projectile)this).launcher;
		int damAmount = ((Projectile)(object)this).DamageAmount / 2;
		float armorPenetration = ((Projectile)(object)this).ArmorPenetration;
		ThingDef equipmentDef = ((Projectile)this).equipmentDef;
		ThingDef def = ((Thing)this).def;
		Thing thing = ((Projectile)this).intendedTarget.Thing;
		ThingDef postExplosionSpawnThingDef = ((Thing)this).def.projectile.postExplosionSpawnThingDef;
		ThingDef postExplosionSpawnThingDefWater = ((Thing)this).def.projectile.postExplosionSpawnThingDefWater;
		float postExplosionSpawnChance = ((Thing)this).def.projectile.postExplosionSpawnChance;
		int postExplosionSpawnThingCount = ((Thing)this).def.projectile.postExplosionSpawnThingCount;
		GasType? postExplosionGasType = ((Thing)this).def.projectile.postExplosionGasType;
		ThingDef preExplosionSpawnThingDef = ((Thing)this).def.projectile.preExplosionSpawnThingDef;
		float preExplosionSpawnChance = ((Thing)this).def.projectile.preExplosionSpawnChance;
		int preExplosionSpawnThingCount = ((Thing)this).def.projectile.preExplosionSpawnThingCount;
		bool applyDamageToExplosionCellsNeighbors = ((Thing)this).def.projectile.applyDamageToExplosionCellsNeighbors;
		float explosionChanceToStartFire = ((Thing)this).def.projectile.explosionChanceToStartFire;
		bool explosionDamageFalloff = ((Thing)this).def.projectile.explosionDamageFalloff;
		float? direction = ((Projectile)this).origin.AngleToFlat(((Projectile)this).destination);
		float screenShakeFactor = ((Thing)this).def.projectile.screenShakeFactor;
		GenExplosion.DoExplosion(center, map, 1.5f, milira_KineticBomb, launcher, damAmount, armorPenetration, null, equipmentDef, def, thing, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, postExplosionGasType, null, 255, applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, explosionChanceToStartFire, explosionDamageFalloff, direction, null, null, doVisualEffects: false, 1f, 0f, doSoundEffects: false, postExplosionSpawnThingDefWater, screenShakeFactor);
		if (num - 15f < -180f || num + 15f > 180f)
		{
			if (num < 0f)
			{
				IntVec3 center2 = position2;
				DamageDef milira_KineticBomb2 = MiliraDefOf.Milira_KineticBomb;
				Thing launcher2 = ((Projectile)this).launcher;
				int damAmount2 = ((Projectile)(object)this).DamageAmount / 2;
				float armorPenetration2 = ((Projectile)(object)this).ArmorPenetration;
				ThingDef equipmentDef2 = ((Projectile)this).equipmentDef;
				ThingDef def2 = ((Thing)this).def;
				Thing thing2 = ((Projectile)this).intendedTarget.Thing;
				ThingDef postExplosionSpawnThingDef2 = ((Thing)this).def.projectile.postExplosionSpawnThingDef;
				preExplosionSpawnThingDef = ((Thing)this).def.projectile.postExplosionSpawnThingDefWater;
				float postExplosionSpawnChance2 = ((Thing)this).def.projectile.postExplosionSpawnChance;
				int postExplosionSpawnThingCount2 = ((Thing)this).def.projectile.postExplosionSpawnThingCount;
				GasType? postExplosionGasType2 = ((Thing)this).def.projectile.postExplosionGasType;
				postExplosionSpawnThingDefWater = ((Thing)this).def.projectile.preExplosionSpawnThingDef;
				screenShakeFactor = ((Thing)this).def.projectile.preExplosionSpawnChance;
				preExplosionSpawnThingCount = ((Thing)this).def.projectile.preExplosionSpawnThingCount;
				explosionDamageFalloff = ((Thing)this).def.projectile.applyDamageToExplosionCellsNeighbors;
				explosionChanceToStartFire = ((Thing)this).def.projectile.explosionChanceToStartFire;
				applyDamageToExplosionCellsNeighbors = ((Thing)this).def.projectile.explosionDamageFalloff;
				direction = ((Projectile)this).origin.AngleToFlat(((Projectile)this).destination);
				preExplosionSpawnChance = ((Thing)this).def.projectile.screenShakeFactor;
				FloatRange? affectedAngle = new FloatRange(-180f, num + 15f);
				GenExplosion.DoExplosion(center2, map, 6f, milira_KineticBomb2, launcher2, damAmount2, armorPenetration2, null, equipmentDef2, def2, thing2, postExplosionSpawnThingDef2, postExplosionSpawnChance2, postExplosionSpawnThingCount2, postExplosionGasType2, null, 255, explosionDamageFalloff, postExplosionSpawnThingDefWater, screenShakeFactor, preExplosionSpawnThingCount, explosionChanceToStartFire, applyDamageToExplosionCellsNeighbors, direction, null, affectedAngle, doVisualEffects: false, 1f, 0f, doSoundEffects: false, preExplosionSpawnThingDef, preExplosionSpawnChance);
				IntVec3 center3 = position2;
				DamageDef milira_KineticBomb3 = MiliraDefOf.Milira_KineticBomb;
				Thing launcher3 = ((Projectile)this).launcher;
				int damAmount3 = ((Projectile)(object)this).DamageAmount / 2;
				float armorPenetration3 = ((Projectile)(object)this).ArmorPenetration;
				ThingDef equipmentDef3 = ((Projectile)this).equipmentDef;
				ThingDef def3 = ((Thing)this).def;
				Thing thing3 = ((Projectile)this).intendedTarget.Thing;
				ThingDef postExplosionSpawnThingDef3 = ((Thing)this).def.projectile.postExplosionSpawnThingDef;
				postExplosionSpawnThingDefWater = ((Thing)this).def.projectile.postExplosionSpawnThingDefWater;
				float postExplosionSpawnChance3 = ((Thing)this).def.projectile.postExplosionSpawnChance;
				int postExplosionSpawnThingCount3 = ((Thing)this).def.projectile.postExplosionSpawnThingCount;
				GasType? postExplosionGasType3 = ((Thing)this).def.projectile.postExplosionGasType;
				preExplosionSpawnThingDef = ((Thing)this).def.projectile.preExplosionSpawnThingDef;
				preExplosionSpawnChance = ((Thing)this).def.projectile.preExplosionSpawnChance;
				preExplosionSpawnThingCount = ((Thing)this).def.projectile.preExplosionSpawnThingCount;
				applyDamageToExplosionCellsNeighbors = ((Thing)this).def.projectile.applyDamageToExplosionCellsNeighbors;
				explosionChanceToStartFire = ((Thing)this).def.projectile.explosionChanceToStartFire;
				explosionDamageFalloff = ((Thing)this).def.projectile.explosionDamageFalloff;
				direction = ((Projectile)this).origin.AngleToFlat(((Projectile)this).destination);
				screenShakeFactor = ((Thing)this).def.projectile.screenShakeFactor;
				affectedAngle = new FloatRange(360f + num - 15f, 180f);
				GenExplosion.DoExplosion(center3, map, 6f, milira_KineticBomb3, launcher3, damAmount3, armorPenetration3, null, equipmentDef3, def3, thing3, postExplosionSpawnThingDef3, postExplosionSpawnChance3, postExplosionSpawnThingCount3, postExplosionGasType3, null, 255, applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, explosionChanceToStartFire, explosionDamageFalloff, direction, null, affectedAngle, doVisualEffects: false, 1f, 0f, doSoundEffects: false, postExplosionSpawnThingDefWater, screenShakeFactor);
			}
			else
			{
				IntVec3 center4 = position2;
				float explosionRadius = ((Thing)this).def.projectile.explosionRadius;
				DamageDef milira_KineticBomb4 = MiliraDefOf.Milira_KineticBomb;
				Thing launcher4 = ((Projectile)this).launcher;
				int damAmount4 = ((Projectile)(object)this).DamageAmount / 2;
				float armorPenetration4 = ((Projectile)(object)this).ArmorPenetration;
				ThingDef equipmentDef4 = ((Projectile)this).equipmentDef;
				ThingDef def4 = ((Thing)this).def;
				Thing thing4 = ((Projectile)this).intendedTarget.Thing;
				ThingDef postExplosionSpawnThingDef4 = ((Thing)this).def.projectile.postExplosionSpawnThingDef;
				preExplosionSpawnThingDef = ((Thing)this).def.projectile.postExplosionSpawnThingDefWater;
				float postExplosionSpawnChance4 = ((Thing)this).def.projectile.postExplosionSpawnChance;
				int postExplosionSpawnThingCount4 = ((Thing)this).def.projectile.postExplosionSpawnThingCount;
				GasType? postExplosionGasType4 = ((Thing)this).def.projectile.postExplosionGasType;
				postExplosionSpawnThingDefWater = ((Thing)this).def.projectile.preExplosionSpawnThingDef;
				screenShakeFactor = ((Thing)this).def.projectile.preExplosionSpawnChance;
				preExplosionSpawnThingCount = ((Thing)this).def.projectile.preExplosionSpawnThingCount;
				explosionDamageFalloff = ((Thing)this).def.projectile.applyDamageToExplosionCellsNeighbors;
				explosionChanceToStartFire = ((Thing)this).def.projectile.explosionChanceToStartFire;
				applyDamageToExplosionCellsNeighbors = ((Thing)this).def.projectile.explosionDamageFalloff;
				direction = ((Projectile)this).origin.AngleToFlat(((Projectile)this).destination);
				preExplosionSpawnChance = ((Thing)this).def.projectile.screenShakeFactor;
				FloatRange? affectedAngle = new FloatRange(num - 15f, 180f);
				GenExplosion.DoExplosion(center4, map, explosionRadius, milira_KineticBomb4, launcher4, damAmount4, armorPenetration4, null, equipmentDef4, def4, thing4, postExplosionSpawnThingDef4, postExplosionSpawnChance4, postExplosionSpawnThingCount4, postExplosionGasType4, null, 255, explosionDamageFalloff, postExplosionSpawnThingDefWater, screenShakeFactor, preExplosionSpawnThingCount, explosionChanceToStartFire, applyDamageToExplosionCellsNeighbors, direction, null, affectedAngle, doVisualEffects: false, 3f, 0f, doSoundEffects: false, preExplosionSpawnThingDef, preExplosionSpawnChance);
				IntVec3 center5 = position2;
				DamageDef milira_KineticBomb5 = MiliraDefOf.Milira_KineticBomb;
				Thing launcher5 = ((Projectile)this).launcher;
				int damAmount5 = ((Projectile)(object)this).DamageAmount / 2;
				float armorPenetration5 = ((Projectile)(object)this).ArmorPenetration;
				ThingDef equipmentDef5 = ((Projectile)this).equipmentDef;
				ThingDef def5 = ((Thing)this).def;
				Thing thing5 = ((Projectile)this).intendedTarget.Thing;
				ThingDef postExplosionSpawnThingDef5 = ((Thing)this).def.projectile.postExplosionSpawnThingDef;
				postExplosionSpawnThingDefWater = ((Thing)this).def.projectile.postExplosionSpawnThingDefWater;
				float postExplosionSpawnChance5 = ((Thing)this).def.projectile.postExplosionSpawnChance;
				int postExplosionSpawnThingCount5 = ((Thing)this).def.projectile.postExplosionSpawnThingCount;
				GasType? postExplosionGasType5 = ((Thing)this).def.projectile.postExplosionGasType;
				preExplosionSpawnThingDef = ((Thing)this).def.projectile.preExplosionSpawnThingDef;
				preExplosionSpawnChance = ((Thing)this).def.projectile.preExplosionSpawnChance;
				preExplosionSpawnThingCount = ((Thing)this).def.projectile.preExplosionSpawnThingCount;
				applyDamageToExplosionCellsNeighbors = ((Thing)this).def.projectile.applyDamageToExplosionCellsNeighbors;
				explosionChanceToStartFire = ((Thing)this).def.projectile.explosionChanceToStartFire;
				explosionDamageFalloff = ((Thing)this).def.projectile.explosionDamageFalloff;
				direction = ((Projectile)this).origin.AngleToFlat(((Projectile)this).destination);
				screenShakeFactor = ((Thing)this).def.projectile.screenShakeFactor;
				affectedAngle = new FloatRange(-180f, -360f + num + 15f);
				GenExplosion.DoExplosion(center5, map, 6f, milira_KineticBomb5, launcher5, damAmount5, armorPenetration5, null, equipmentDef5, def5, thing5, postExplosionSpawnThingDef5, postExplosionSpawnChance5, postExplosionSpawnThingCount5, postExplosionGasType5, null, 255, applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, explosionChanceToStartFire, explosionDamageFalloff, direction, null, affectedAngle, doVisualEffects: false, 1f, 0f, doSoundEffects: false, postExplosionSpawnThingDefWater, screenShakeFactor);
			}
		}
		else
		{
			IntVec3 center6 = position2;
			DamageDef milira_KineticBomb6 = MiliraDefOf.Milira_KineticBomb;
			Thing launcher6 = ((Projectile)this).launcher;
			int damAmount6 = ((Projectile)(object)this).DamageAmount / 2;
			float armorPenetration6 = ((Projectile)(object)this).ArmorPenetration;
			ThingDef equipmentDef6 = ((Projectile)this).equipmentDef;
			ThingDef def6 = ((Thing)this).def;
			Thing thing6 = ((Projectile)this).intendedTarget.Thing;
			ThingDef postExplosionSpawnThingDef6 = ((Thing)this).def.projectile.postExplosionSpawnThingDef;
			preExplosionSpawnThingDef = ((Thing)this).def.projectile.postExplosionSpawnThingDefWater;
			float postExplosionSpawnChance6 = ((Thing)this).def.projectile.postExplosionSpawnChance;
			int postExplosionSpawnThingCount6 = ((Thing)this).def.projectile.postExplosionSpawnThingCount;
			GasType? postExplosionGasType6 = ((Thing)this).def.projectile.postExplosionGasType;
			postExplosionSpawnThingDefWater = ((Thing)this).def.projectile.preExplosionSpawnThingDef;
			screenShakeFactor = ((Thing)this).def.projectile.preExplosionSpawnChance;
			preExplosionSpawnThingCount = ((Thing)this).def.projectile.preExplosionSpawnThingCount;
			explosionDamageFalloff = ((Thing)this).def.projectile.applyDamageToExplosionCellsNeighbors;
			explosionChanceToStartFire = ((Thing)this).def.projectile.explosionChanceToStartFire;
			applyDamageToExplosionCellsNeighbors = ((Thing)this).def.projectile.explosionDamageFalloff;
			direction = ((Projectile)this).origin.AngleToFlat(((Projectile)this).destination);
			preExplosionSpawnChance = ((Thing)this).def.projectile.screenShakeFactor;
			FloatRange? affectedAngle = new FloatRange(num - 15f, num + 15f);
			GenExplosion.DoExplosion(center6, map, 6f, milira_KineticBomb6, launcher6, damAmount6, armorPenetration6, null, equipmentDef6, def6, thing6, postExplosionSpawnThingDef6, postExplosionSpawnChance6, postExplosionSpawnThingCount6, postExplosionGasType6, null, 255, explosionDamageFalloff, postExplosionSpawnThingDefWater, screenShakeFactor, preExplosionSpawnThingCount, explosionChanceToStartFire, applyDamageToExplosionCellsNeighbors, direction, null, affectedAngle, doVisualEffects: false, 1f, 0f, doSoundEffects: false, preExplosionSpawnThingDef, preExplosionSpawnChance);
		}
		((Projectile_Custom)this).Impact(hitThing, blockedByShield);
	}
}
