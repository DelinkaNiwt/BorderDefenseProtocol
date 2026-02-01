using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GD3
{
	public class Verb_LaunchProjectileBackfire : Verb_LaunchProjectile
	{
		private List<IntVec3> forcedMissTargetEvenDispersalCache = new List<IntVec3>();

		private bool shootingAtDowned = false;

		private LocalTargetInfo lastTarget = null;

		private IntVec3 lastTargetPos = IntVec3.Invalid;

		protected bool doRetarget = true;

		protected void resetRetarget()
		{
			shootingAtDowned = false;
			lastTarget = null;
			lastTargetPos = IntVec3.Invalid;
		}

		protected override bool TryCastShot()
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
				ThrowDebugText("ToWild" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
				ThrowDebugText("Wild\nDest", resultingLine.Dest);
				ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
				if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
				{
					projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
				}
				projectile2.Launch(manningPawn, drawPos, resultingLine.Dest, currentTarget, projectileHitFlags2, preventFriendlyFire, equipmentSource, targetCoverDef);
				return true;
			}
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
			return true;
		}

		protected bool Retarget()
		{
			if (!doRetarget)
			{
				return true;
			}
			doRetarget = false;
			if (currentTarget != lastTarget)
			{
				lastTarget = currentTarget;
				lastTargetPos = currentTarget.Cell;
				shootingAtDowned = currentTarget.Pawn?.Downed ?? true;
				return true;
			}
			if (shootingAtDowned)
			{
				return true;
			}
			if (currentTarget.Pawn == null || currentTarget.Pawn.DeadOrDowned || !CanHitFromCellIgnoringRange(Caster.Position, currentTarget, out IntVec3 _))
			{
				Pawn pawn = null;
				Thing thing = Caster;
				foreach (Pawn item in Caster.Map.mapPawns.AllPawnsSpawned.ToList().FindAll(p => p.Position.DistanceTo(lastTargetPos) <= 4.9f))
				{
					if (item.Faction == currentTarget.Pawn?.Faction && item.Faction.HostileTo(thing.Faction) && !item.Downed && CanHitFromCellIgnoringRange(Caster.Position, (LocalTargetInfo)item, out IntVec3 _))
					{
						pawn = item;
						break;
					}
				}
				if (pawn != null)
				{
					currentTarget = new LocalTargetInfo(pawn);
					lastTarget = currentTarget;
					lastTargetPos = currentTarget.Cell;
					shootingAtDowned = false;
					if (thing is Building_TurretGun building_TurretGun)
					{
						float curRotation = (currentTarget.Cell.ToVector3Shifted() - building_TurretGun.DrawPos).AngleFlat();
						building_TurretGun.Top.CurRotation = curRotation;
					}
					return true;
				}
				shootingAtDowned = true;
				return false;
			}
			return true;
		}

		private bool CanHitFromCellIgnoringRange(IntVec3 shotSource, LocalTargetInfo targ, out IntVec3 goodDest)
		{
			if (targ.Thing != null && targ.Thing.Map != caster.Map)
			{
				goodDest = IntVec3.Invalid;
				return false;
			}
			if (verbProps.requireLineOfSight && TryFindShootLineFromTo(shotSource, targ.Cell, out ShootLine _))
			{
				goodDest = targ.Cell;
				return true;
			}
			goodDest = IntVec3.Invalid;
			return false;
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
		public override void Reset()
		{
			base.Reset();
			forcedMissTargetEvenDispersalCache.Clear();
			resetRetarget();
		}

        public override void ExposeData()
        {
            base.ExposeData();
			Scribe_Values.Look(ref shootingAtDowned, "shootingAtDowned", defaultValue: false);
			Scribe_TargetInfo.Look(ref lastTarget, "lastTarget");
			Scribe_Values.Look(ref lastTargetPos, "lastTargetPos", IntVec3.Invalid);
		}
    }
}