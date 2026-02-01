using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NyarsModPackTwo;

public class CompAbilityEffect_ShootRandomBullet : CompAbilityEffect
{
	private static List<ThingDef> bulletCache = new List<ThingDef>();

	public new CompProperties_ShootRandomBullet Props => (CompProperties_ShootRandomBullet)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		Pawn pawn = parent.pawn;
		if (bulletCache.Count == 0)
		{
			bulletCache.AddRange(Props.bullets);
		}
		for (int i = 0; i < Props.castCount.RandomInRange; i++)
		{
			if (bulletCache.Count == 0)
			{
				bulletCache.AddRange(Props.bullets);
			}
			int index = Rand.Range(0, bulletCache.Count);
			ThingDef bulletDef = bulletCache[index];
			bulletCache.RemoveAt(index);
			LaunchBullet(pawn, bulletDef);
		}
	}

	private void LaunchBullet(Pawn caster, ThingDef bulletDef)
	{
		Map map = caster.Map;
		Projectile projectile = (Projectile)GenSpawn.Spawn(bulletDef, caster.Position, map);
		if (projectile is Bullet_TracingEnemies bullet_TracingEnemies)
		{
			bullet_TracingEnemies.flyingAngle = Rand.Value * 360f;
			bullet_TracingEnemies.trackingPosNow = caster.TrueCenter();
		}
		projectile.Launch(caster, caster.TrueCenter(), default(LocalTargetInfo), default(LocalTargetInfo), ProjectileHitFlags.IntendedTarget | ProjectileHitFlags.NonTargetWorld);
	}
}
