using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TurbojetBackpack;

public class Verb_BezierMissileLaunch : Verb_CastBase
{
	public override void DrawHighlight(LocalTargetInfo target)
	{
		if (target.IsValid)
		{
			GenDraw.DrawTargetHighlight(target);
		}
	}

	protected override bool TryCastShot()
	{
		if (CasterPawn == null || CasterPawn.Map == null)
		{
			return false;
		}
		CompAbility_Magazine compAbility_Magazine = null;
		if (CasterPawn.abilities != null)
		{
			foreach (Ability ability in CasterPawn.abilities.abilities)
			{
				if (!(ability.def.verbProperties.verbClass == GetType()))
				{
					continue;
				}
				if (ability.comps != null)
				{
					for (int i = 0; i < ability.comps.Count; i++)
					{
						if (ability.comps[i] is CompAbility_Magazine compAbility_Magazine2)
						{
							compAbility_Magazine = compAbility_Magazine2;
							break;
						}
					}
				}
				if (compAbility_Magazine != null && !compAbility_Magazine.TryConsumeCharge())
				{
					return false;
				}
				break;
			}
		}
		try
		{
			ThingDef defaultProjectile = verbProps.defaultProjectile;
			if (defaultProjectile == null)
			{
				return false;
			}
			BarrageExtension modExtension = defaultProjectile.GetModExtension<BarrageExtension>();
			int num = modExtension?.burstCount ?? 8;
			float radius = modExtension?.randomFireRadius ?? 3.9f;
			Vector3 drawPos = CasterPawn.DrawPos;
			IntVec3 cell = currentTarget.Cell;
			int maxExclusive = GenRadial.NumCellsInRadius(radius);
			for (int j = 0; j < num; j++)
			{
				IntVec3 intVec = cell + GenRadial.RadialPattern[Rand.Range(0, maxExclusive)];
				if (!intVec.InBounds(CasterPawn.Map))
				{
					intVec = cell;
				}
				LocalTargetInfo localTargetInfo = new LocalTargetInfo(intVec);
				((Projectile)GenSpawn.Spawn(defaultProjectile, CasterPawn.Position, CasterPawn.Map))?.Launch(CasterPawn, drawPos, localTargetInfo, localTargetInfo, ProjectileHitFlags.IntendedTarget);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}
}
