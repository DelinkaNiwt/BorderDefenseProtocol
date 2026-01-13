using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ECT;

public class Verb_ShootWithOffset : Verb_Shoot
{
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
		if (currentTarget.HasThing && (currentTarget.Thing.Map == null || currentTarget.Thing.Destroyed || currentTarget.Thing is Pawn { Dead: not false }))
		{
			currentTarget = new LocalTargetInfo(currentTarget.Cell);
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
		Thing manningPawn = caster;
		Thing equipmentSource = base.EquipmentSource;
		CompMannable compMannable = caster.TryGetComp<CompMannable>();
		if (compMannable?.ManningPawn != null)
		{
			manningPawn = compMannable.ManningPawn;
			equipmentSource = caster;
		}
		Vector3 vector = CalculateMuzzlePosition(caster.DrawPos, equipmentSource);
		ModExtension_ShootWithOffset modExtension_ShootWithOffset = equipmentSource?.def?.GetModExtension<ModExtension_ShootWithOffset>();
		if (modExtension_ShootWithOffset != null && caster.Map != null && modExtension_ShootWithOffset.muzzleFlashArcLength > 0f)
		{
			Vector3 vector2;
			if (caster is Building_TurretGunHasSpeed building_TurretGunHasSpeed)
			{
				vector2 = Vector3.forward.RotatedBy(building_TurretGunHasSpeed.curAngle);
			}
			else
			{
				vector2 = (currentTarget.CenterVector3 - vector).Yto0().normalized;
				if (vector2 == Vector3.zero)
				{
					vector2 = Vector3.forward;
				}
			}
			int num = Mathf.Max(1, modExtension_ShootWithOffset.muzzleFlashArcCount);
			Vector3 vector3 = vector2.RotatedBy(90f);
			for (int i = 0; i < num; i++)
			{
				float num2 = (float)i - (float)(num - 1) / 2f;
				Vector3 start = vector + vector3 * (num2 * modExtension_ShootWithOffset.muzzleFlashArcSpacing);
				WeatherEvent_LightningTrail newEvent = new WeatherEvent_LightningTrail(caster.Map, start, vector2, modExtension_ShootWithOffset.muzzleFlashArcLength, modExtension_ShootWithOffset.muzzleFlashArcDuration, 3, modExtension_ShootWithOffset.muzzleFlashArcVariance, modExtension_ShootWithOffset.muzzleFlashArcWidth);
				caster.Map.weatherManager.eventHandler.AddEvent(newEvent);
			}
		}
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
					if (projectile2.extraDamages == null)
					{
						projectile2.extraDamages = new List<ExtraDamage>();
					}
					projectile2.extraDamages.AddRange(item.extraDamages);
				}
			}
		}
		if (verbProps.ForcedMissRadius > 0.5f)
		{
			float num3 = verbProps.ForcedMissRadius;
			if (manningPawn is Pawn pawn2)
			{
				num3 *= verbProps.GetForceMissFactorFor(equipmentSource, pawn2);
			}
			float num4 = VerbUtility.CalculateAdjustedForcedMiss(num3, currentTarget.Cell - caster.Position);
			if (num4 > 0.5f)
			{
				IntVec3 forcedMissTarget = GetForcedMissTarget(num4);
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
					projectile2.Launch(manningPawn, vector, forcedMissTarget, currentTarget, projectileHitFlags, preventFriendlyFire, equipmentSource);
					return true;
				}
			}
		}
		ShotReport shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
		Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
		ThingDef targetCoverDef = randomCoverToMissInto?.def;
		if (verbProps.canGoWild && !Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
		{
			bool flyOverhead = projectile2?.def?.projectile != null && projectile2.def.projectile.flyOverhead;
			resultingLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget, flyOverhead, caster.Map);
			ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
			if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
			{
				projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
			}
			projectile2.Launch(manningPawn, vector, resultingLine.Dest, currentTarget, projectileHitFlags2, preventFriendlyFire, equipmentSource, targetCoverDef);
			return true;
		}
		if (currentTarget.Thing != null && currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(shotReport.PassCoverChance))
		{
			ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
			if (canHitNonTargetPawnsNow)
			{
				projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
			}
			projectile2.Launch(manningPawn, vector, randomCoverToMissInto, currentTarget, projectileHitFlags3, preventFriendlyFire, equipmentSource, targetCoverDef);
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
			projectile2.Launch(manningPawn, vector, currentTarget, currentTarget, projectileHitFlags4, preventFriendlyFire, equipmentSource, targetCoverDef);
		}
		else
		{
			projectile2.Launch(manningPawn, vector, resultingLine.Dest, currentTarget, projectileHitFlags4, preventFriendlyFire, equipmentSource, targetCoverDef);
		}
		return true;
	}

	private Vector3 CalculateMuzzlePosition(Vector3 originalDrawPos, Thing equipmentSource)
	{
		if (equipmentSource != null)
		{
			ModExtension_ShootWithOffset modExtension = equipmentSource.def.GetModExtension<ModExtension_ShootWithOffset>();
			if (modExtension != null && !modExtension.offsets.NullOrEmpty())
			{
				int num = ((burstShotsLeft >= 0) ? burstShotsLeft : 0);
				int burstShotCount = verbProps.burstShotCount;
				int num2 = burstShotCount - num;
				if (caster is Building_TurretGunHasSpeed building_TurretGunHasSpeed)
				{
					int barrelIndex = num2 % modExtension.offsets.Count;
					building_TurretGunHasSpeed.Notify_BarrelFired(barrelIndex);
				}
				Vector2 vector = (modExtension.muzzleOffsets.NullOrEmpty() ? modExtension.GetOffsetFor(num2) : modExtension.GetMuzzleOffsetFor(num2));
				Quaternion quaternion;
				if (caster is Building_TurretGunHasSpeed building_TurretGunHasSpeed2)
				{
					quaternion = building_TurretGunHasSpeed2.curAngle.ToQuat();
				}
				else
				{
					float ang = caster.DrawPos.AngleToFlat(currentTarget.CenterVector3);
					quaternion = ang.ToQuat();
				}
				Vector3 vector2 = new Vector3(vector.x, 0f, vector.y);
				Vector3 vector3 = quaternion * vector2;
				originalDrawPos += vector3;
			}
		}
		return originalDrawPos;
	}
}
