using RimWorld;
using Verse;

namespace TOT_DLL_test;

public class Projectile_FakeBulletLaser : Projectile
{
	private int ticks = 0;

	public ThingWithComps EquipmentSource
	{
		get
		{
			if (launcher is Pawn)
			{
				Pawn pawn = launcher as Pawn;
				return pawn.equipment.Primary;
			}
			Building_TurretGun building_TurretGun = launcher as Building_TurretGun;
			return building_TurretGun.gun as ThingWithComps;
		}
	}

	public Verb EquipmentVerbs
	{
		get
		{
			ThingWithComps equipmentSource = EquipmentSource;
			return equipmentSource.GetComp<CompEquippable>().PrimaryVerb;
		}
	}

	protected override void Tick()
	{
		if (intendedTarget != null)
		{
			if (intendedTarget.Thing != null)
			{
				if (ticks % 15 == 0)
				{
					Impact(intendedTarget.Thing);
					ticks = 0;
				}
			}
			else
			{
				ticks++;
			}
		}
		else
		{
			Destroy();
		}
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		IntVec3 position = base.Position;
		hitThing = intendedTarget.Thing;
		if (hitThing != null)
		{
			BattleLogEntry_RangedImpact entry = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
			Find.BattleLog.Add(entry);
		}
		if (EquipmentSource != null)
		{
			for (int i = 0; i < EquipmentSource.AllComps.Count; i++)
			{
				if (EquipmentSource.AllComps[i] is Comp_LaserData_Instant && EquipmentSource.AllComps[i] is Comp_LaserData_Instant comp_LaserData_Instant)
				{
					comp_LaserData_Instant.TakeDamageToTarget(intendedTarget, launcher, EquipmentVerbs);
				}
			}
		}
		Destroy();
	}
}
