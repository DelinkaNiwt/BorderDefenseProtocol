using UnityEngine;
using Verse;

namespace RimWorld;

public class Verb_ShootFromAbove : Verb_Shoot
{
	private const float HeightOffset = -2f;

	private Vector3 GetSourcePosition()
	{
		Vector3 vector = base.caster?.DrawPos ?? base.caster.Position.ToVector3Shifted();
		return new Vector3(vector.x, vector.y, vector.z + -2f);
	}

	protected override bool TryCastShot()
	{
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
				ShootLine shootLine;
				bool flag3 = TryFindShootLineFromTo(caster.Position, currentTarget, out shootLine);
				if (verbProps.stopBurstWithoutLos && !flag3)
				{
					result = false;
				}
				else
				{
					if (base.EquipmentSource != null)
					{
						base.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
					}
					lastShotTick = Find.TickManager.TicksGame;
					Thing thing = caster;
					Thing equipment = base.EquipmentSource;
					CompMannable compMannable = caster.TryGetComp<CompMannable>();
					if (compMannable?.ManningPawn != null)
					{
						thing = compMannable.ManningPawn;
						equipment = caster;
					}
					Vector3 sourcePosition = GetSourcePosition();
					Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, shootLine.Source, caster.Map);
					if (verbProps.ForcedMissRadius > 0.5f)
					{
						float num = verbProps.ForcedMissRadius;
						if (thing is Pawn pawn)
						{
							num *= verbProps.GetForceMissFactorFor(equipment, pawn);
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
								projectile2.Launch(thing, sourcePosition, forcedMissTarget, currentTarget, projectileHitFlags, preventFriendlyFire, equipment);
								return true;
							}
						}
					}
					ShotReport report = ShotReport.HitReportFor(caster, this, currentTarget);
					ThingDef coverDef = report.GetRandomCoverToMissInto()?.def;
					ShotReport shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
					Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
					ThingDef targetCoverDef = randomCoverToMissInto?.def;
					if (verbProps.canGoWild && !Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
					{
						shootLine.ChangeDestToMissWild(report.AimOnTargetChance_StandardTarget, canHitNonTargetPawnsNow, caster.Map);
						ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
						if (!canHitNonTargetPawnsNow)
						{
							projectileHitFlags2 &= ~ProjectileHitFlags.NonTargetPawns;
						}
						projectile2.Launch(thing, sourcePosition, shootLine.Dest, currentTarget, projectileHitFlags2, preventFriendlyFire, equipment, targetCoverDef);
						result = true;
					}
					else if (currentTarget.Thing != null && currentTarget.Thing.def.category == ThingCategory.Pawn && !Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture) && !Rand.Chance(shotReport.PassCoverChance))
					{
						ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
						if (canHitNonTargetPawnsNow)
						{
							projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
						}
						projectile2.Launch(thing, sourcePosition, randomCoverToMissInto, currentTarget, projectileHitFlags3, preventFriendlyFire, equipment, targetCoverDef);
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
							projectile2.Launch(thing, sourcePosition, currentTarget, currentTarget, projectileHitFlags4, preventFriendlyFire, equipment, targetCoverDef);
						}
						else
						{
							projectile2.Launch(thing, sourcePosition, shootLine.Dest, currentTarget, projectileHitFlags4, preventFriendlyFire, equipment, targetCoverDef);
						}
						result = true;
					}
				}
			}
		}
		return result;
	}

	public override void DrawHighlight(LocalTargetInfo target)
	{
		base.DrawHighlight(target);
		if (target.IsValid)
		{
			Vector3 sourcePosition = GetSourcePosition();
			GenDraw.DrawLineBetween(sourcePosition, target.CenterVector3, SimpleColor.Red);
		}
	}
}
