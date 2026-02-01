using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NyarsModPackTwo;

public class Verb_ShootRandomBullet : Verb_Shoot
{
	private static readonly List<ThingDef> BulletCache = new List<ThingDef>();

	public List<ThingDef> BulletDefs => base.EquipmentSource.def.GetModExtension<ModExtension_BulletsDefs>().bullets;

	public int MaxCastCount => base.EquipmentSource.def.GetModExtension<ModExtension_BulletsDefs>().castCount.RandomInRange;

	protected override bool TryCastShot()
	{
		lastShotTick = Find.TickManager.TicksGame;
		int i = 0;
		while (i <= MaxCastCount)
		{
			if (BulletCache.Count == 0)
			{
				BulletCache.AddRange(BulletDefs);
			}
			int index = Rand.Range(0, BulletCache.Count);
			ThingDef bulletDef = BulletCache[index];
			BulletCache.RemoveAt(index);
			i++;
			LaunchBullet(bulletDef);
		}
		return true;
	}

	private void LaunchBullet(ThingDef bulletDef)
	{
		Bullet_TracingEnemies bullet_TracingEnemies = (Bullet_TracingEnemies)GenSpawn.Spawn(bulletDef, caster.Position, caster.Map);
		bullet_TracingEnemies.flyingAngle = Rand.Value * 360f;
		bullet_TracingEnemies.trackingPosNow = Caster.TrueCenter();
		bullet_TracingEnemies.Launch(caster, Caster.TrueCenter(), default(LocalTargetInfo), default(LocalTargetInfo), ProjectileHitFlags.IntendedTarget, preventFriendlyFire, base.EquipmentSource);
	}
}
