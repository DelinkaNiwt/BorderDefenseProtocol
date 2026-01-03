using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Projectile_Split : Bullet
{
	public ModExtension_Splitedbullet modExtension_Splitedbullet => def.GetModExtension<ModExtension_Splitedbullet>();

	protected override void Tick()
	{
		base.Tick();
		if (base.DistanceCoveredFraction > modExtension_Splitedbullet.SplitTime && !this.DestroyedOrNull())
		{
			Split();
		}
	}

	protected void Split()
	{
		if (modExtension_Splitedbullet != null && modExtension_Splitedbullet.BulletDef != null)
		{
			int num = modExtension_Splitedbullet.SplitAmount + Rand.Range(-1, 1);
			for (int i = 0; i < num; i++)
			{
				ProjectileHitFlags hitFlags = ProjectileHitFlags.All;
				Projectile projectile = ThingMaker.MakeThing(modExtension_Splitedbullet.BulletDef) as Projectile;
				typeof(ProjectileProperties).GetField("damageAmountBase", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(projectile.def.projectile, def.projectile.GetDamageAmount(launcher) / modExtension_Splitedbullet.SplitAmount);
				typeof(ProjectileProperties).GetField("armorPenetrationBase", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(projectile.def.projectile, def.projectile.GetArmorPenetration(launcher));
				Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, base.Position, base.Map);
				if (Rand.Chance(Hitchance()))
				{
					projectile2.Launch(launcher, DrawPos, intendedTarget, intendedTarget, hitFlags, preventFriendlyFire, null, targetCoverDef);
				}
				else
				{
					projectile2.Launch(launcher, DrawPos, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, null, targetCoverDef);
				}
			}
		}
		Destroy();
	}

	private float Hitchance()
	{
		Pawn pawn = launcher as Pawn;
		bool flag = pawn != null && !pawn.NonHumanlikeOrWildMan();
		int num = 0;
		if (flag)
		{
			SkillDef named = DefDatabase<SkillDef>.GetNamed("Intellectual");
			num = pawn.skills.GetSkill(named)?.GetLevel() ?? 10;
		}
		else
		{
			num = 8;
		}
		float num2 = Mathf.Clamp01((float)num / 20f);
		float num3 = 3f * num2 * num2 - 2f * num2 * num2 * num2;
		return 0.33f + 0.62f * num3;
	}
}
