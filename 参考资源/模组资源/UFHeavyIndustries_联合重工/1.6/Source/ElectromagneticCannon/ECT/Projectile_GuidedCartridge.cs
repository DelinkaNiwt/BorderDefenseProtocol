using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace ECT;

public class Projectile_GuidedCartridge : Bullet
{
	private static readonly FieldInfo startingTicksToImpactField = typeof(Projectile).GetField("startingTicksToImpact", BindingFlags.Instance | BindingFlags.NonPublic);

	private Vector3 trueOrigin;

	private ModExtension_GuidedCartridge extInt;

	private ModExtension_GuidedCartridge Ext
	{
		get
		{
			if (extInt == null)
			{
				extInt = def.GetModExtension<ModExtension_GuidedCartridge>();
			}
			return extInt;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref trueOrigin, "trueOrigin");
	}

	public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
	{
		base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
		trueOrigin = origin;
	}

	protected override void Tick()
	{
		if (intendedTarget.Thing is Pawn { Dead: false, Spawned: not false } pawn && pawn.Map == base.Map && (destination - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.1f)
		{
			Vector3 vector = (destination - origin).Yto0();
			if (vector == Vector3.zero)
			{
				vector = (destination - trueOrigin).Yto0();
			}
			Vector3 to = (pawn.DrawPos - ExactPosition).Yto0();
			if (vector.sqrMagnitude > 0.1f && to.sqrMagnitude > 0.1f)
			{
				float num = Ext?.maxHomingAngle ?? 35f;
				float num2 = Vector3.Angle(vector, to);
				if (num2 <= num)
				{
					UpdateDestination(pawn.DrawPos);
				}
			}
		}
		base.Tick();
	}

	private void UpdateDestination(Vector3 newDest)
	{
		origin = ExactPosition;
		destination = newDest;
		float num = (destination - origin).MagnitudeHorizontal();
		if (num <= 0f)
		{
			num = 0.001f;
		}
		ticksToImpact = Mathf.CeilToInt(num / def.projectile.SpeedTilesPerTick);
		if (ticksToImpact < 1)
		{
			ticksToImpact = 1;
		}
		if (startingTicksToImpactField != null)
		{
			startingTicksToImpactField.SetValue(this, ticksToImpact);
		}
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (blockedByShield)
		{
			DoFinalExplosion(ExactPosition.ToIntVec3());
			Destroy();
			return;
		}
		IntVec3 pos = ExactPosition.ToIntVec3();
		if (hitThing != null)
		{
			pos = hitThing.Position;
		}
		DoFinalExplosion(pos);
		Destroy();
	}

	private void DoFinalExplosion(IntVec3 pos)
	{
		float explosionRadius = def.projectile.explosionRadius;
		if (!(explosionRadius <= 0f))
		{
			Map map = base.Map;
			DamageDef damageDef = def.projectile.damageDef;
			Thing instigator = launcher;
			int damageAmount = DamageAmount;
			float armorPenetration = ArmorPenetration;
			SoundDef soundExplode = def.projectile.soundExplode;
			ThingDef weapon = equipmentDef;
			ThingDef projectile = def;
			Thing thing = intendedTarget.Thing;
			ThingDef postExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDef;
			float postExplosionSpawnChance = def.projectile.postExplosionSpawnChance;
			int postExplosionSpawnThingCount = def.projectile.postExplosionSpawnThingCount;
			ThingDef preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
			float preExplosionSpawnChance = def.projectile.preExplosionSpawnChance;
			int preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
			float explosionChanceToStartFire = def.projectile.explosionChanceToStartFire;
			bool explosionDamageFalloff = def.projectile.explosionDamageFalloff;
			GenExplosion.DoExplosion(pos, map, explosionRadius, damageDef, instigator, damageAmount, armorPenetration, soundExplode, weapon, projectile, thing, postExplosionSpawnThingDef, postExplosionSpawnChance, postExplosionSpawnThingCount, null, null, 255, applyDamageToExplosionCellsNeighbors: false, preExplosionSpawnThingDef, preExplosionSpawnChance, preExplosionSpawnThingCount, explosionChanceToStartFire, explosionDamageFalloff);
		}
	}
}
