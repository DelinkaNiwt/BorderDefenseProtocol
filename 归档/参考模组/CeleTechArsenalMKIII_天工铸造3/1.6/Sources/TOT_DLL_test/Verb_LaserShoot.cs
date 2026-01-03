using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Verb_LaserShoot : Verb_LaunchProjectile
{
	protected List<Building> turrets = new List<Building>();

	protected List<IntVec3> cells = new List<IntVec3>();

	public override void WarmupComplete()
	{
		base.WarmupComplete();
	}

	protected override bool TryCastShot()
	{
		bool flag = base.TryCastShot();
		if (flag && base.EquipmentCompSource.parent.def.defName == "Gun_LaserSniper")
		{
			int num = 0;
			turrets.Clear();
			turrets.Add((Building)caster);
			for (int i = 0; (float)i < 6f; i++)
			{
				cells.Clear();
				cells = GenRadial.RadialCellsAround(turrets[num].Position, 10f, useCenter: true).ToList();
				foreach (IntVec3 cell in cells)
				{
					if (!(cell.GetFirstBuilding(caster.Map) is Building_Turret building_Turret) || !(building_Turret.def.defName == caster.def.defName))
					{
						continue;
					}
					bool flag2 = false;
					Building_TurretGun building_TurretGun = cell.GetFirstBuilding(caster.Map) as Building_TurretGun;
					foreach (Building turret in turrets)
					{
						if (turret == building_TurretGun)
						{
							flag2 = true;
							break;
						}
					}
					if ((!flag2 & building_TurretGun.GetComp<CompPowerTrader>().PowerOn) && building_TurretGun.CurrentTarget == null)
					{
						turrets.Add(building_TurretGun);
						num++;
						break;
					}
				}
			}
			int num2 = 0;
			Comp_LaserData_Instant comp_LaserData_Instant = base.EquipmentSource.TryGetComp<Comp_LaserData_Instant>();
			foreach (Building turret2 in turrets)
			{
				if (num2 + 1 < turrets.Count)
				{
					float num3 = comp_LaserData_Instant.Props.Color_Red;
					float num4 = comp_LaserData_Instant.Props.Color_Green;
					float num5 = comp_LaserData_Instant.Props.Color_Blue;
					num3 /= 255f;
					num4 /= 255f;
					num5 /= 255f;
					Vector3 vector = new Vector3(0f, 0f, 1.35f);
					Map map = caster.Map;
					Vector3 vector2 = turrets[num2 + 1].DrawPos - turrets[num2].DrawPos;
					float x = vector2.MagnitudeHorizontal();
					FleckCreationData dataStatic = FleckMaker.GetDataStatic(turrets[num2].DrawPos + vector2 * 0.5f + vector, map, comp_LaserData_Instant.Props.LaserLine_FleckDef);
					FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(turrets[num2].DrawPos + vector2 * 0.5f + vector, map, comp_LaserData_Instant.Props.LaserLine_FleckDef);
					float randomInRange = new FloatRange(0.4f, 0.8f).RandomInRange;
					dataStatic.exactScale = new Vector3(x, 1f, randomInRange * 1.5f);
					dataStatic.rotation = Mathf.Atan2(0f - vector2.z, vector2.x) * 57.29578f;
					dataStatic.instanceColor = new Color(num3, num4, num5, 0.2f);
					map.flecks.CreateFleck(dataStatic);
					dataStatic2.exactScale = new Vector3(x, 1f, randomInRange * 0.2f);
					dataStatic2.rotation = Mathf.Atan2(0f - vector2.z, vector2.x) * 57.29578f;
					dataStatic2.instanceColor = new Color(Mathf.Max(num3 * 1.1f, 1f), Mathf.Max(num4 * 1.1f, 1f), Mathf.Max(num5 * 1.1f, 1f), 0.5f);
					map.flecks.CreateFleck(dataStatic2);
					FleckCreationData dataStatic3 = FleckMaker.GetDataStatic(turrets[num2 + 1].DrawPos + vector, map, comp_LaserData_Instant.Props.MuzzleGlow);
					dataStatic3.exactScale = new Vector3(1.53f, 1.53f, 1.53f);
					dataStatic3.rotation = Mathf.Atan2(0f - vector2.z, vector2.x) * 57.29578f;
					dataStatic3.instanceColor = new Color(Mathf.Max(num3 * 1.5f, 1f), Mathf.Max(num4 * 1.5f, 1f), Mathf.Max(num5 * 1.5f, 1f), 1f);
					map.flecks.CreateFleck(dataStatic3);
					FleckCreationData dataStatic4 = FleckMaker.GetDataStatic(turrets[num2 + 1].DrawPos + vector, map, comp_LaserData_Instant.Props.MuzzleGlow);
					dataStatic4.exactScale = new Vector3(3f, 3f, 3f);
					dataStatic4.rotation = Mathf.Atan2(0f - vector2.z, vector2.x) * 57.29578f;
					dataStatic4.instanceColor = new Color(num3, num4, num5, 1f);
					map.flecks.CreateFleck(dataStatic4);
					FleckCreationData dataStatic5 = FleckMaker.GetDataStatic(turrets[num2].DrawPos + vector, map, comp_LaserData_Instant.Props.MuzzleGlow);
					dataStatic5.exactScale = new Vector3(1.33f, 1.33f, 1.33f);
					dataStatic5.rotation = Mathf.Atan2(0f - vector2.z, vector2.x) * 57.29578f;
					dataStatic5.instanceColor = new Color(Mathf.Max(num3 * 1.5f, 1f), Mathf.Max(num4 * 1.5f, 1f), Mathf.Max(num5 * 1.5f, 1f), 1f);
					map.flecks.CreateFleck(dataStatic5);
					FleckCreationData dataStatic6 = FleckMaker.GetDataStatic(turrets[num2].DrawPos + vector, map, comp_LaserData_Instant.Props.MuzzleGlow);
					dataStatic6.exactScale = new Vector3(2f, 2f, 2f);
					dataStatic6.rotation = Mathf.Atan2(0f - vector2.z, vector2.x) * 57.29578f;
					dataStatic6.instanceColor = new Color(num3, num4, num5, 1f);
					map.flecks.CreateFleck(dataStatic6);
				}
				num2++;
			}
			comp_LaserData_Instant.DMGmp = turrets.Count;
		}
		return flag;
	}
}
