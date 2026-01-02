using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Verb_Laser_Instant : Verb
{
	protected override int ShotsPerBurst => verbProps.burstShotCount;

	public Vector3 TargetPosition_Vector3 => base.CurrentTarget.CenterVector3;

	protected override bool TryCastShot()
	{
		if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
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
			base.EquipmentSource.GetComp<CompApparelReloadable>()?.UsedOnce();
		}
		for (int i = 0; i < base.EquipmentSource.AllComps.Count; i++)
		{
			if (base.EquipmentSource.AllComps[i] is Comp_LaserData_Instant)
			{
				Comp_LaserData_Instant comp_LaserData_Instant = base.EquipmentSource.AllComps[i] as Comp_LaserData_Instant;
				comp_LaserData_Instant.TakeDamageToTarget(base.CurrentTarget.Thing, Caster, this);
			}
		}
		return true;
	}
}
