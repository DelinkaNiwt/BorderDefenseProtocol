using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Verb_RocketShoot : Verb_LaunchProjectile
{
	protected override int ShotsPerBurst => verbProps.burstShotCount;

	protected override bool TryCastShot()
	{
		Vector3 drawPos = caster.DrawPos;
		float num = (base.CurrentTarget.CenterVector3 - drawPos).AngleFlat();
		float num2 = num + 36f - 90f;
		float num3 = num - 36f - 90f;
		if (num2 > 180f)
		{
			num2 -= 360f;
		}
		if (num2 < -180f)
		{
			num2 += 360f;
		}
		if (num3 > 180f)
		{
			num3 -= 360f;
		}
		if (num3 < -180f)
		{
			num3 += 360f;
		}
		Vector3 item = drawPos + new Vector3(1.5f, 0f, 0f).RotatedBy(num2);
		Vector3 item2 = drawPos + new Vector3(1.5f, 0f, 0f).RotatedBy(num3);
		List<Vector3> list = new List<Vector3> { item, item2 };
		bool result;
		if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
		{
			result = false;
		}
		else
		{
			ThingDef projectile = Projectile;
			if (projectile == null)
			{
				result = false;
			}
			else
			{
				ShootLine resultingLine;
				bool flag = TryFindShootLineFromTo(caster.Position, currentTarget, out resultingLine);
				if (verbProps.stopBurstWithoutLos && !flag)
				{
					result = false;
				}
				else
				{
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
					Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, resultingLine.Source, caster.Map);
					if (verbProps.ForcedMissRadius > 0.5f)
					{
						float num4 = verbProps.ForcedMissRadius;
						if (manningPawn is Pawn pawn)
						{
							num4 *= verbProps.GetForceMissFactorFor(equipmentSource, pawn);
						}
						float num5 = VerbUtility.CalculateAdjustedForcedMiss(num4, currentTarget.Cell - caster.Position);
						if (num5 > 0.5f)
						{
							IntVec3 forcedMissTarget = GetForcedMissTarget(num5);
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
								projectile2.Launch(manningPawn, list[burstShotsLeft % 2], forcedMissTarget, currentTarget, projectileHitFlags, preventFriendlyFire, equipmentSource);
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
						bool flag3 = flag2 && projectile2.def.projectile.flyOverhead;
						ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
						if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
						{
							projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
						}
						projectile2.Launch(manningPawn, list[burstShotsLeft % 2], resultingLine.Dest, currentTarget, projectileHitFlags2, preventFriendlyFire, equipmentSource, targetCoverDef);
						result = true;
					}
					else if (currentTarget.Thing != null && currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(shotReport.PassCoverChance))
					{
						ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
						if (canHitNonTargetPawnsNow)
						{
							projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
						}
						projectile2.Launch(manningPawn, list[burstShotsLeft % 2], randomCoverToMissInto, currentTarget, projectileHitFlags3, preventFriendlyFire, equipmentSource, targetCoverDef);
						result = true;
					}
					else
					{
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
							projectile2.Launch(manningPawn, list[burstShotsLeft % 2], currentTarget, currentTarget, projectileHitFlags4, preventFriendlyFire, equipmentSource, targetCoverDef);
						}
						else
						{
							projectile2.Launch(manningPawn, list[burstShotsLeft % 2], resultingLine.Dest, currentTarget, projectileHitFlags4, preventFriendlyFire, equipmentSource, targetCoverDef);
						}
						result = true;
					}
				}
			}
		}
		return result;
	}
}
