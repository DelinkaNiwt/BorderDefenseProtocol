using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TOT_DLL_test;

public class Verb_ShootWorld : Verb_Shoot
{
	public LocalTargetInfo TryRandomTarget()
	{
		MapParent aSEA_observedMap = GameComponent_CeleTech.Instance.ASEA_observedMap;
		if (aSEA_observedMap != null && aSEA_observedMap.Map != null)
		{
			HashSet<IAttackTarget> hashSet = aSEA_observedMap.Map.attackTargetsCache.TargetsHostileToFaction(caster.Faction);
			if (hashSet.Count > 0)
			{
				return hashSet.RandomElement().Thing;
			}
			return aSEA_observedMap.Map.AllCells.RandomElement();
		}
		return null;
	}

	public bool TryCastFireMission()
	{
		Building_CMCTurretGun_MainBattery building_CMCTurretGun_MainBattery = caster as Building_CMCTurretGun_MainBattery;
		Vector3 vector = Vector3.forward.RotatedBy(building_CMCTurretGun_MainBattery.turrettop.DestRotation);
		IntVec3 intVec = (building_CMCTurretGun_MainBattery.DrawPos + vector * 500f).ToIntVec3();
		WorldObject_EMLShell worldObject_EMLShell = (WorldObject_EMLShell)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("CMC_EMLShell"));
		worldObject_EMLShell.railgun = building_CMCTurretGun_MainBattery;
		worldObject_EMLShell.Tile = building_CMCTurretGun_MainBattery.Map.Tile;
		worldObject_EMLShell.destinationTile = GameComponent_CeleTech.Instance.ASEA_observedMap.Map.Tile;
		worldObject_EMLShell.destinationCell = (IntVec3)TryRandomTarget();
		worldObject_EMLShell.spread = 2;
		worldObject_EMLShell.Projectile = Projectile;
		Find.WorldObjects.Add(worldObject_EMLShell);
		Find.CameraDriver.shaker.SetMinShake(0.1f);
		((Projectile)GenSpawn.Spawn(Projectile, building_CMCTurretGun_MainBattery.Position, caster.Map)).Launch(building_CMCTurretGun_MainBattery, building_CMCTurretGun_MainBattery.DrawPos, intVec, null, ProjectileHitFlags.None, preventFriendlyFire: false, base.EquipmentSource);
		(base.EquipmentSource?.GetComp<CompChangeableProjectile>())?.Notify_ProjectileLaunched();
		return true;
	}
}
