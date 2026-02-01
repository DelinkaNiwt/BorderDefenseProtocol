using System.Linq;
using RimWorld;
using UnityEngine;
using VanillaPsycastsExpanded.Harmonist;
using VEF.Buildings;
using Verse;
using Verse.Sound;

namespace VanillaPsycastsExpanded.Chronopath;

[StaticConstructorOnStartup]
public class GameCondition_TimeQuake : GameCondition_TimeSnow
{
	private static readonly Material DistortionMat = DistortedMaterialsPool.DistortedMaterial("Things/Mote/Black", "Things/Mote/PsycastDistortionMask", 1E-05f, 1f);

	public float SafeRadius;

	public Pawn Pawn;

	private Sustainer sustainer;

	public override void GameConditionTick()
	{
		base.GameConditionTick();
		if (base.TicksPassed % 60 == 0)
		{
			foreach (Map affectedMap in base.AffectedMaps)
			{
				for (int i = 0; i < 2000; i++)
				{
					affectedMap.wildPlantSpawner.WildPlantSpawnerTick();
				}
				foreach (Pawn item in affectedMap.mapPawns.AllPawnsSpawned.Where(CanEffect).ToList())
				{
					AbilityExtension_Age.Age(item, 1f);
				}
				foreach (Plant item2 in affectedMap.listerThings.ThingsInGroup(ThingRequestGroup.Plant).OfType<Plant>().Where(CanEffect)
					.ToList())
				{
					if (item2.Growth < 1f)
					{
						item2.Growth = 1f;
					}
					else if (item2.def.useHitPoints)
					{
						item2.TakeDamage(new DamageInfo(DamageDefOf.Rotting, 0.01f * (float)item2.MaxHitPoints));
					}
					else
					{
						item2.Age = int.MaxValue;
					}
				}
				foreach (Building item3 in affectedMap.listerBuildings.allBuildingsColonist.Concat(affectedMap.listerBuildings.allBuildingsNonColonist).Where(CanEffect).ToList())
				{
					if (item3.def.useHitPoints)
					{
						item3.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 0.01f * (float)item3.MaxHitPoints));
					}
				}
			}
		}
		if (base.TicksPassed % 300 == 250)
		{
			foreach (Map affectedMap2 in base.AffectedMaps)
			{
				Ability_RandomEvent.DoRandomEvent(affectedMap2);
			}
		}
		if (sustainer == null)
		{
			sustainer = VPE_DefOf.Psycast_Neuroquake_CastLoop.TrySpawnSustainer(Pawn);
		}
		else
		{
			sustainer.Maintain();
		}
		Find.CameraDriver.shaker.DoShake(1.5f);
	}

	public override void End()
	{
		sustainer.End();
		VPE_DefOf.Psycast_Neuroquake_CastEnd.PlayOneShot(Pawn);
		base.End();
	}

	private bool CanEffect(Thing thing)
	{
		return !thing.Position.InHorDistOf(Pawn.Position, SafeRadius);
	}

	public override void GameConditionDraw(Map map)
	{
		base.GameConditionDraw(map);
		if (Find.Selector.IsSelected(Pawn))
		{
			GenDraw.DrawRadiusRing(Pawn.Position, SafeRadius, Color.yellow);
		}
		Matrix4x4 matrix = Matrix4x4.TRS(Pawn.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MoteOverhead), Quaternion.AngleAxis(0f, Vector3.up), Vector3.one * SafeRadius * 2f);
		UnityEngine.Graphics.DrawMesh(MeshPool.plane10, matrix, DistortionMat, 0);
	}
}
