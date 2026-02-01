using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace GD3
{
	public class Verb_LaunchMultiProjectile : Verb_LaunchProjectile
	{
		protected override bool TryCastShot()
		{
			if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
			{
				return false;
			}
			ThingWithComps equip = this.EquipmentSource;
			MultiShootExtension extension = equip.def.GetModExtension<MultiShootExtension>();
			for (int i = 0; i < extension.projectiles.Count; i++)
            {
				ThingDef projectile = extension.projectiles[i];
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
				Vector3 drawPos = caster.DrawPos;
				Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, resultingLine.Source, caster.Map);
				if (verbProps.ForcedMissRadius > 0.5f)
				{
					float num = verbProps.ForcedMissRadius;
					if (manningPawn is Pawn pawn)
					{
						num *= verbProps.GetForceMissFactorFor(equipmentSource, pawn);
					}
					float num2 = VerbUtility.CalculateAdjustedForcedMiss(num, currentTarget.Cell - caster.Position);
					if (num2 > 0.5f)
					{
						IntVec3 forcedMissTarget = GetForcedMissTarget(num2);
						if (forcedMissTarget != currentTarget.Cell)
						{
							ThrowDebugText("ToRadius");
							ThrowDebugText("Rad\nDest", forcedMissTarget);
							ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
							if (Rand.Chance(0.5f))
							{
								projectileHitFlags = ProjectileHitFlags.All;
							}
							if (!canHitNonTargetPawnsNow)
							{
								projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
							}
							projectile2.Launch(manningPawn, drawPos, forcedMissTarget, currentTarget, projectileHitFlags, preventFriendlyFire, equipmentSource);
						}
					}
				}
				ShotReport shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
				Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
				ThingDef targetCoverDef = randomCoverToMissInto?.def;
				if (currentTarget.Thing != null && currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(shotReport.PassCoverChance))
				{
					ThrowDebugText("ToCover" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
					ThrowDebugText("Cover\nDest", randomCoverToMissInto.Position);
					ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
					if (canHitNonTargetPawnsNow)
					{
						projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
					}
					projectile2.Launch(manningPawn, drawPos, randomCoverToMissInto, currentTarget, projectileHitFlags3, preventFriendlyFire, equipmentSource, targetCoverDef);
					continue;
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
				ThrowDebugText("ToHit" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
				if (currentTarget.Thing != null)
				{
					projectile2.Launch(manningPawn, drawPos, currentTarget, currentTarget, projectileHitFlags4, preventFriendlyFire, equipmentSource, targetCoverDef);
					ThrowDebugText("Hit\nDest", currentTarget.Cell);
				}
				else
				{
					projectile2.Launch(manningPawn, drawPos, resultingLine.Dest, currentTarget, projectileHitFlags4, preventFriendlyFire, equipmentSource, targetCoverDef);
					ThrowDebugText("Hit\nDest", resultingLine.Dest);
				}
			}
			return true;
		}

		private void ThrowDebugText(string text)
		{
			if (DebugViewSettings.drawShooting)
			{
				MoteMaker.ThrowText(caster.DrawPos, caster.Map, text);
			}
		}

		private void ThrowDebugText(string text, IntVec3 c)
		{
			if (DebugViewSettings.drawShooting)
			{
				MoteMaker.ThrowText(c.ToVector3Shifted(), caster.Map, text);
			}
		}
	}
}
