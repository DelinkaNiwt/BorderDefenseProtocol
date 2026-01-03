using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Verb_Shoot_UAV : Verb_Shoot
{
	public Comp_FloatingGunRework TurretComp;

	protected override int ShotsPerBurst => verbProps.burstShotCount;

	private Comp_FloatingGunRework CompFloatingGunRework
	{
		get
		{
			if (TurretComp == null && caster is Pawn { apparel: var apparel })
			{
				List<Apparel> wornApparel = apparel.WornApparel;
				if (!wornApparel.NullOrEmpty())
				{
					foreach (Apparel item in wornApparel)
					{
						Comp_FloatingGunRework comp_FloatingGunRework = item.TryGetComp<Comp_FloatingGunRework>();
						if (comp_FloatingGunRework != null && comp_FloatingGunRework.launching)
						{
							TurretComp = comp_FloatingGunRework;
							break;
						}
					}
				}
			}
			return TurretComp;
		}
	}

	protected override bool TryCastShot()
	{
		if (CompFloatingGunRework == null)
		{
			return false;
		}
		if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
		{
			return false;
		}
		ThingDef projectile = Projectile;
		if (projectile == null)
		{
			return false;
		}
		ShootLine resultingLine;
		bool flag = TryFindShootLineFromTo(CompFloatingGunRework.currentPosition.ToIntVec3(), currentTarget, out resultingLine);
		if (verbProps.stopBurstWithoutLos && !flag)
		{
			return false;
		}
		if (base.EquipmentSource != null)
		{
			base.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
			base.EquipmentSource.GetComp<CompApparelVerbOwner_Charged>()?.UsedOnce();
		}
		lastShotTick = Find.TickManager.TicksGame;
		Thing manningPawn = caster;
		Thing equipmentSource = base.EquipmentSource;
		CompMannable compMannable = caster.TryGetComp<CompMannable>();
		if (compMannable?.ManningPawn != null)
		{
			manningPawn = compMannable.ManningPawn;
			equipmentSource = caster;
		}
		Vector3 currentPosition = CompFloatingGunRework.currentPosition;
		Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, resultingLine.Source, caster.Map);
		if (equipmentSource.TryGetComp(out CompUniqueWeapon comp))
		{
			foreach (WeaponTraitDef item in comp.TraitsListForReading)
			{
				if (item.damageDefOverride != null)
				{
					projectile2.damageDefOverride = item.damageDefOverride;
				}
				if (!item.extraDamages.NullOrEmpty())
				{
					Projectile projectile3 = projectile2;
					if (projectile3.extraDamages == null)
					{
						projectile3.extraDamages = new List<ExtraDamage>();
					}
					projectile2.extraDamages.AddRange(item.extraDamages);
				}
			}
		}
		if (verbProps.ForcedMissRadius > 0.5f)
		{
			float num = verbProps.ForcedMissRadius;
			if (manningPawn is Pawn pawn)
			{
				num *= verbProps.GetForceMissFactorFor(equipmentSource, pawn);
			}
			float num2 = VerbUtility.CalculateAdjustedForcedMiss(num, currentTarget.Cell - CompFloatingGunRework.currentPosition.ToIntVec3());
			if (num2 > 0.5f)
			{
				IntVec3 forcedMissTarget = GetForcedMissTarget(num2);
				if (forcedMissTarget != currentTarget.Cell)
				{
					ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
					if (Rand.Chance(0.5f))
					{
						projectileHitFlags = ProjectileHitFlags.All;
					}
					if (!canHitNonTargetPawnsNow)
					{
						projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
					}
					projectile2.Launch(manningPawn, currentPosition, forcedMissTarget, currentTarget, projectileHitFlags, preventFriendlyFire, equipmentSource);
					return true;
				}
			}
		}
		ShotReport shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
		Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
		ThingDef targetCoverDef = randomCoverToMissInto?.def;
		if (verbProps.canGoWild && !Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
		{
			bool flag2 = projectile2 != null && projectile2.def?.projectile != null;
			bool flyOverhead = flag2 && projectile2.def.projectile.flyOverhead;
			resultingLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget, flyOverhead, caster.Map);
			ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
			if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
			{
				projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
			}
			projectile2.Launch(manningPawn, currentPosition, resultingLine.Dest, currentTarget, projectileHitFlags2, preventFriendlyFire, equipmentSource, targetCoverDef);
			return true;
		}
		if (currentTarget.Thing != null && currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(shotReport.PassCoverChance))
		{
			ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
			if (canHitNonTargetPawnsNow)
			{
				projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
			}
			projectile2.Launch(manningPawn, currentPosition, randomCoverToMissInto, currentTarget, projectileHitFlags3, preventFriendlyFire, equipmentSource, targetCoverDef);
			return true;
		}
		ProjectileHitFlags projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
		if (canHitNonTargetPawnsNow)
		{
			projectileHitFlags4 |= ProjectileHitFlags.NonTargetPawns;
		}
		if (!currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full)
		{
			projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
		}
		if (currentTarget.Thing != null)
		{
			projectile2.Launch(manningPawn, currentPosition, currentTarget, currentTarget, projectileHitFlags4, preventFriendlyFire, equipmentSource, targetCoverDef);
		}
		else
		{
			projectile2.Launch(manningPawn, currentPosition, resultingLine.Dest, currentTarget, projectileHitFlags4, preventFriendlyFire, equipmentSource, targetCoverDef);
		}
		return true;
	}
}
