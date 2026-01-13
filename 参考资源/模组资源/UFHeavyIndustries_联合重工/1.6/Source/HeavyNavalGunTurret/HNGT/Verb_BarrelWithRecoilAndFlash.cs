using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HNGT;

public class Verb_BarrelWithRecoilAndFlash : Verb_Shoot
{
	public int offset = 0;

	protected override bool TryCastShot()
	{
		bool flag = BaseTryCastShot();
		if (flag && CasterIsPawn)
		{
			CasterPawn.records.Increment(RecordDefOf.ShotsFired);
		}
		return flag;
	}

	protected bool BaseTryCastShot()
	{
		if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
		{
			return false;
		}
		ThingDef projectile = Projectile;
		if (projectile == null)
		{
			return false;
		}
		if (caster is Building_TurretGunRotateAim { isFiringInterMap: not false })
		{
			Thing manningPawn = caster;
			Thing equipmentSource = base.EquipmentSource;
			CompMannable compMannable = caster.TryGetComp<CompMannable>();
			if (compMannable?.ManningPawn != null)
			{
				manningPawn = compMannable.ManningPawn;
				equipmentSource = caster;
			}
			Vector3 vector = ApplyProjectileOffset(caster.DrawPos, equipmentSource);
			LocalTargetInfo intendedTarget = currentTarget;
			Projectile projectile2 = (Projectile)GenSpawn.Spawn(Projectile, vector.ToIntVec3(), caster.Map);
			projectile2.Launch(manningPawn, vector, intendedTarget.Cell, intendedTarget, ProjectileHitFlags.None, preventFriendlyFire: false, equipmentSource);
			lastShotTick = Find.TickManager.TicksGame;
			return true;
		}
		ShootLine resultingLine;
		bool flag = TryFindShootLineFromTo(caster.Position, currentTarget, out resultingLine);
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
		Thing manningPawn2 = caster;
		Thing equipmentSource2 = base.EquipmentSource;
		CompMannable compMannable2 = caster.TryGetComp<CompMannable>();
		if (compMannable2?.ManningPawn != null)
		{
			manningPawn2 = compMannable2.ManningPawn;
			equipmentSource2 = caster;
		}
		Vector3 drawPos = caster.DrawPos;
		drawPos = ApplyProjectileOffset(drawPos, equipmentSource2);
		Projectile projectile3 = (Projectile)GenSpawn.Spawn(projectile, resultingLine.Source, caster.Map);
		if (equipmentSource2.TryGetComp(out CompUniqueWeapon comp))
		{
			foreach (WeaponTraitDef item in comp.TraitsListForReading)
			{
				if (item.damageDefOverride != null)
				{
					projectile3.damageDefOverride = item.damageDefOverride;
				}
				if (!item.extraDamages.NullOrEmpty())
				{
					Projectile projectile4 = projectile3;
					if (projectile4.extraDamages == null)
					{
						projectile4.extraDamages = new List<ExtraDamage>();
					}
					projectile3.extraDamages.AddRange(item.extraDamages);
				}
			}
		}
		if (verbProps.ForcedMissRadius > 0.5f)
		{
			float num = verbProps.ForcedMissRadius;
			if (manningPawn2 is Pawn pawn)
			{
				num *= verbProps.GetForceMissFactorFor(equipmentSource2, pawn);
			}
			float num2 = VerbUtility.CalculateAdjustedForcedMiss(num, currentTarget.Cell - caster.Position);
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
					projectile3.Launch(manningPawn2, drawPos, forcedMissTarget, currentTarget, projectileHitFlags, preventFriendlyFire, equipmentSource2);
					return true;
				}
			}
		}
		ShotReport shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
		Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
		ThingDef targetCoverDef = randomCoverToMissInto?.def;
		if (verbProps.canGoWild && !Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
		{
			bool flyOverhead = projectile3?.def?.projectile != null && projectile3.def.projectile.flyOverhead;
			resultingLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget, flyOverhead, caster.Map);
			ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
			if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
			{
				projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
			}
			projectile3.Launch(manningPawn2, drawPos, resultingLine.Dest, currentTarget, projectileHitFlags2, preventFriendlyFire, equipmentSource2, targetCoverDef);
			return true;
		}
		if (currentTarget.Thing != null && currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(shotReport.PassCoverChance))
		{
			ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
			if (canHitNonTargetPawnsNow)
			{
				projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
			}
			projectile3.Launch(manningPawn2, drawPos, randomCoverToMissInto, currentTarget, projectileHitFlags3, preventFriendlyFire, equipmentSource2, targetCoverDef);
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
			projectile3.Launch(manningPawn2, drawPos, currentTarget, currentTarget, projectileHitFlags4, preventFriendlyFire, equipmentSource2, targetCoverDef);
		}
		else
		{
			projectile3.Launch(manningPawn2, drawPos, resultingLine.Dest, currentTarget, projectileHitFlags4, preventFriendlyFire, equipmentSource2, targetCoverDef);
		}
		return true;
	}

	private Vector3 ApplyProjectileOffset(Vector3 originalDrawPos, Thing equipmentSource)
	{
		if (equipmentSource == null)
		{
			return originalDrawPos;
		}
		ModExtension_BarrelWithRecoilAndFlash modExtension = equipmentSource.def.GetModExtension<ModExtension_BarrelWithRecoilAndFlash>();
		if (caster is Building_TurretGunRotateAim building_TurretGunRotateAim && modExtension != null && !modExtension.offsets.NullOrEmpty())
		{
			int count = modExtension.offsets.Count;
			if (count == 0)
			{
				return originalDrawPos;
			}
			int burstShotCount = verbProps.burstShotCount;
			int num = GetBurstShotsLeft();
			int num2 = burstShotCount - num;
			int num3 = num2 % count;
			building_TurretGunRotateAim.Notify_BarrelFired(num3);
			Vector2 turretTopOffset = building_TurretGunRotateAim.def.building.turretTopOffset;
			Vector3 vector = new Vector3(turretTopOffset.x, 0f, turretTopOffset.y);
			Vector2 vector2 = modExtension.offsets[num3];
			Vector3 vector3 = new Vector3(vector2.x, 0f, vector2.y);
			float curAngle = building_TurretGunRotateAim.curAngle;
			Quaternion quaternion = curAngle.ToQuat();
			Vector3 vector4 = quaternion * vector3;
			return originalDrawPos + vector + vector4;
		}
		return originalDrawPos;
	}

	private int GetBurstShotsLeft()
	{
		if (burstShotsLeft >= 0)
		{
			return burstShotsLeft;
		}
		return 0;
	}
}
