using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class Comp_FragmentedExplosive : ThingComp
{
	private Thing originalLauncher;

	private CompProperties_FragmentedExplosive Props => (CompProperties_FragmentedExplosive)props;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_References.Look(ref originalLauncher, "originalLauncher");
	}

	public override void PostPostMake()
	{
		base.PostPostMake();
		if (parent is Projectile projectile)
		{
			originalLauncher = projectile.Launcher;
		}
	}

	public void TriggerExplosion()
	{
		if (parent.Map != null)
		{
			Detonate(parent.Map);
		}
	}

	public void TriggerExplosion(Map map)
	{
		if (map != null)
		{
			Detonate(map);
		}
	}

	private void TryTriggerByDamage(DamageInfo dinfo)
	{
		if (parent.HitPoints <= 0)
		{
			TriggerExplosion();
		}
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		TriggerExplosion(previousMap);
	}

	protected void Detonate(Map map)
	{
		if (map != null)
		{
			Vector3 position = parent.DrawPos;
			FleckMaker.ThrowFireGlow(position, map, Props.fireGlowSize);
			FleckMaker.ThrowSmoke(position, map, Props.smokeSize);
			FleckMaker.ThrowHeatGlow(parent.Position, map, Props.heatGlowSize);
			CreateFragments(map);
		}
	}

	private void CreateFragments(Map map)
	{
		IntVec3 center = parent.Position;
		float radius = Props.explosionRadius;
		ThingDef fragmentDef = GetFragmentProjectileDef();
		if (fragmentDef == null)
		{
			return;
		}
		Thing launcher = originalLauncher;
		int guaranteedCenterCount = Mathf.CeilToInt((float)Props.fragmentCount * Props.guaranteedCenterFraction);
		int randomCount = Props.fragmentCount - guaranteedCenterCount;
		for (int i = 0; i < guaranteedCenterCount; i++)
		{
			SpawnAndLaunchFragment(map, center, center, launcher, fragmentDef);
		}
		if (radius > 0f && randomCount > 0)
		{
			List<IntVec3> targetCells = GetTargetCells(center, radius, map);
			for (int j = 0; j < randomCount; j++)
			{
				IntVec3 targetCell = ((targetCells.Count > 0) ? targetCells.RandomElement() : center);
				SpawnAndLaunchFragment(map, center, targetCell, launcher, fragmentDef);
			}
		}
	}

	private List<IntVec3> GetTargetCells(IntVec3 center, float radius, Map map)
	{
		List<IntVec3> targetCells = new List<IntVec3>();
		foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
		{
			if (cell.InBounds(map))
			{
				targetCells.Add(cell);
			}
		}
		return targetCells;
	}

	private void SpawnAndLaunchFragment(Map map, IntVec3 center, IntVec3 target, Thing launcher, ThingDef fragmentDef)
	{
		Projectile projectile = (Projectile)ThingMaker.MakeThing(fragmentDef);
		if (projectile != null)
		{
			GenSpawn.Spawn(projectile, center, map);
			LaunchFragment(projectile, center, target, launcher);
		}
	}

	private void LaunchFragment(Projectile projectile, IntVec3 origin, IntVec3 target, Thing launcher)
	{
		if (launcher == null || launcher.Destroyed)
		{
			projectile.Launch(parent, origin.ToVector3Shifted(), new LocalTargetInfo(target), new LocalTargetInfo(target), ProjectileHitFlags.All);
		}
		else
		{
			projectile.Launch(launcher, origin.ToVector3Shifted(), new LocalTargetInfo(target), new LocalTargetInfo(target), ProjectileHitFlags.All);
		}
	}

	private ThingDef GetFragmentProjectileDef()
	{
		return Props.fragmentProjectileDef ?? DefDatabase<ThingDef>.GetNamedSilentFail("Bullet_NCL");
	}
}
