using RimWorld;
using UnityEngine;
using Verse;

namespace Milira;

public class Projectile_ExplosiveWithEMP : Projectile_Explosive
{
	private const int ExtraExplosionCount = 1;

	private const int ExtraExplosionRadius = 5;

	public override bool AnimalsFleeImpact => true;

	protected override void Tick()
	{
		base.Tick();
		LeaveSmokeTrail();
	}

	private void LeaveSmokeTrail()
	{
		if (base.Map != null)
		{
			int num = 1;
			if (GenTicks.TicksGame % num == 0)
			{
				MiliraFleckMaker.ThrowPlasmaAirPuffUp(color: new Color(0.6f, 0.8f, 1f), loc: DrawPos, map: base.Map);
			}
		}
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		base.Impact(hitThing, blockedByShield);
		IntVec3 position = base.Position;
		Map map2 = map;
		float explosionRadius = def.projectile.explosionRadius;
		DamageDef damageDef = def.projectile.damageDef;
		Thing instigator = launcher;
		int damageAmount = DamageAmount;
		float armorPenetration = ArmorPenetration;
		SoundDef soundExplode = def.projectile.soundExplode;
		ThingDef weapon = equipmentDef;
		ThingDef projectile = def;
		ThingDef postExplosionSpawnThingDef = null;
		GenExplosion.DoExplosion(position, map2, explosionRadius, damageDef, instigator, damageAmount, armorPenetration, soundExplode, weapon, projectile, intendedTarget.Thing, postExplosionSpawnThingDef, 0f, 0);
		for (int i = 0; i < 1; i++)
		{
			DoExplosion(position, map, explosionRadius);
		}
	}

	protected void DoExplosion(IntVec3 pos, Map map, float radius)
	{
		GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.EMP, launcher, DamageAmount, ArmorPenetration, null, equipmentDef, def, intendedTarget.Thing);
	}
}
