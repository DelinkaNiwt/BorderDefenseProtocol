using Verse;

namespace AncotLibrary;

public class Projectile_ExplosiveWithTrail : Projectile_Explosive
{
	public ExplosiveProjectileExtension Props => def.GetModExtension<ExplosiveProjectileExtension>();

	protected override void Tick()
	{
		base.Tick();
		LeaveTrail();
	}

	private void LeaveTrail()
	{
		if (Props.doTrail && base.Map != null && GenTicks.TicksGame % Props.trailFreauency == 0)
		{
			AncotFleckMaker.ThrowTrailFleckUp(DrawPos, base.Map, Props.trailColor, Props.trailFleck);
		}
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		base.Impact(hitThing, blockedByShield);
		IntVec3 position = base.Position;
		if (Props.extraExplosionRadius == 0f)
		{
			Props.extraExplosionRadius = def.projectile.explosionRadius;
		}
		if (!Props.doExtraExplosion && def.projectile.explosionDelay != 0)
		{
			return;
		}
		if (Props.extraExplosionRandomPositionRadius == 0)
		{
			for (int i = 0; i < Props.extraExplosionCount; i++)
			{
				DoExplosion(position, map, Props.extraExplosionRadius);
			}
			return;
		}
		CellRect cellRect = CellRect.CenteredOn(base.Position, Props.extraExplosionRandomPositionRadius);
		cellRect.ClipInsideMap(map);
		for (int j = 0; j < Props.extraExplosionCount; j++)
		{
			IntVec3 randomCell = cellRect.RandomCell;
			DoExplosion(randomCell, map, Props.extraExplosionRadius);
		}
	}

	protected void DoExplosion(IntVec3 pos, Map map, float radius)
	{
		int damAmount = ((Props.extraExplosionDamageAmount != 0) ? Props.extraExplosionDamageAmount : DamageAmount);
		DamageDef damType = Props.extraExplosionDamageType ?? def.projectile.damageDef;
		float armorPenetration = ((Props.extraExplosionArmorPenetration != 0f) ? Props.extraExplosionArmorPenetration : ArmorPenetration);
		GenExplosion.DoExplosion(pos, map, radius, damType, launcher, damAmount, armorPenetration, null, equipmentDef, def, intendedTarget.Thing);
	}
}
