using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TOT_DLL_test;

public class Projectile_ChargedThunderer : Projectile_ChargedNormal
{
	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (hitThing != null && (hitThing.Faction == null || (hitThing.Faction != null && hitThing.Faction.HostileTo(launcher.Faction))) && Rand.Chance(0.1f))
		{
			base.Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(base.Map, base.Position));
			GenExplosion.DoExplosion(base.Position, base.Map, 2.5f, DamageDefOf.EMP, launcher, 30, 99f);
		}
		base.Impact(hitThing, blockedByShield);
	}

	public static void DoStrike(IntVec3 strikeLoc, Map map, ref Mesh boltMesh)
	{
		SoundDefOf.Thunder_OffMap.PlayOneShotOnCamera(map);
		if (!strikeLoc.IsValid)
		{
			strikeLoc = CellFinderLoose.RandomCellWith((IntVec3 sq) => sq.Standable(map) && !map.roofGrid.Roofed(sq), map);
		}
		boltMesh = LightningBoltMeshPool.RandomBoltMesh;
		if (!strikeLoc.Fogged(map))
		{
			GenExplosion.DoExplosion(strikeLoc, map, 1.9f, DamageDefOf.Flame, null);
			Vector3 loc = strikeLoc.ToVector3Shifted();
			for (int num = 0; num < 4; num++)
			{
				FleckMaker.ThrowSmoke(loc, map, 1.5f);
				FleckMaker.ThrowMicroSparks(loc, map);
				FleckMaker.ThrowLightningGlow(loc, map, 1.5f);
			}
		}
		SoundInfo info = SoundInfo.InMap(new TargetInfo(strikeLoc, map));
		SoundDefOf.Thunder_OnMap.PlayOneShot(info);
	}

	private void NotifyImpact(Thing hitThing, Map map, IntVec3 position)
	{
		BulletImpactData impactData = new BulletImpactData
		{
			bullet = this,
			hitThing = hitThing,
			impactPosition = position
		};
		hitThing?.Notify_BulletImpactNearby(impactData);
		int num = 9;
		for (int i = 0; i < num; i++)
		{
			IntVec3 c = position + GenRadial.RadialPattern[i];
			if (!c.InBounds(map))
			{
				continue;
			}
			List<Thing> thingList = c.GetThingList(map);
			for (int j = 0; j < thingList.Count; j++)
			{
				if (thingList[j] != hitThing)
				{
					thingList[j].Notify_BulletImpactNearby(impactData);
				}
			}
		}
	}
}
