using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Verb_Laser_Instant_UAV : Verb
{
	public Comp_FloatingGunRework TurretComp;

	protected override int ShotsPerBurst => verbProps.burstShotCount;

	public Vector3 TargetPosition_Vector3 => base.CurrentTarget.CenterVector3;

	private Comp_FloatingGunRework CompFloatingGunRework
	{
		get
		{
			if (TurretComp == null && caster is Pawn { apparel: var apparel })
			{
				List<Apparel> wornApparel = apparel.WornApparel;
				if (!wornApparel.NullOrEmpty())
				{
					foreach (Apparel item in wornApparel)
					{
						Comp_FloatingGunRework comp_FloatingGunRework = item.TryGetComp<Comp_FloatingGunRework>();
						if (comp_FloatingGunRework != null && comp_FloatingGunRework.launching)
						{
							TurretComp = comp_FloatingGunRework;
							break;
						}
					}
				}
			}
			return TurretComp;
		}
	}

	protected override bool TryCastShot()
	{
		if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
		{
			return false;
		}
		ShootLine resultingLine;
		bool flag = TryFindShootLineFromTo(CompFloatingGunRework.currentPosition.ToIntVec3(), currentTarget, out resultingLine);
		if (verbProps.stopBurstWithoutLos && !flag)
		{
			return false;
		}
		if (base.EquipmentSource != null)
		{
			base.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
			base.EquipmentSource.GetComp<CompApparelReloadable>()?.UsedOnce();
		}
		for (int i = 0; i < base.EquipmentSource.AllComps.Count; i++)
		{
			if (base.EquipmentSource.AllComps[i] is Comp_LaserData_Instant)
			{
				Comp_LaserData_Instant comp_LaserData_Instant = base.EquipmentSource.AllComps[i] as Comp_LaserData_Instant;
				comp_LaserData_Instant.TakeDamageToTarget(base.CurrentTarget.Thing, CompFloatingGunRework.currentPosition, Caster, this);
			}
		}
		return true;
	}
}
