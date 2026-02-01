using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public static class ProjectileUtility
{
	private static Dictionary<(float, int), List<IntVec3>> CachedEffectRadiusCells = new Dictionary<(float, int), List<IntVec3>>();

	public static int GetEffectRadiusCellCount(float radius, int baseSize)
	{
		if (CachedEffectRadiusCells.TryGetValue((radius, baseSize), out var value))
		{
			return value.Count;
		}
		return GetEffectRadiusCells(radius, baseSize).Count;
	}

	public static List<IntVec3> GetEffectRadiusCells(float radius, int baseSize)
	{
		if (!CachedEffectRadiusCells.ContainsKey((radius, baseSize)))
		{
			int num = Mathf.CeilToInt(radius);
			List<IntVec3> list = new List<IntVec3>();
			if (baseSize % 2 == 0)
			{
				int num2 = -num;
				int num3 = num + 1;
				float num4 = Mathf.Pow(radius, 2f);
				for (int i = num2; i <= num3; i++)
				{
					for (int j = num2; j <= num3; j++)
					{
						if (Mathf.Pow(-0.5f + (float)i, 2f) + Mathf.Pow(-0.5f + (float)j, 2f) <= num4)
						{
							list.Add(new IntVec3(i, 0, j));
						}
					}
				}
			}
			else
			{
				list.AddRange(GenRadial.RadialCellsAround(IntVec3.Zero, radius, useCenter: true));
			}
			CachedEffectRadiusCells[(radius, baseSize)] = list;
			return list;
		}
		return CachedEffectRadiusCells[(radius, baseSize)];
	}

	public static List<IntVec3> GetEffectRadiusCellsAround(Thing thing, float radius)
	{
		return GetEffectRadiusCellsAround(thing.Position, radius, thing.def.size.x);
	}

	public static List<IntVec3> GetEffectRadiusCellsAround(IntVec3 origin, float radius, int baseSize)
	{
		List<IntVec3> effectRadiusCells = GetEffectRadiusCells(radius, baseSize);
		List<IntVec3> list = new List<IntVec3>(effectRadiusCells.Count);
		for (int i = 0; i < effectRadiusCells.Count; i++)
		{
			list.Add(origin + effectRadiusCells[i]);
		}
		return list;
	}

	public static Vector3 CalculateStagePositionOffset(ProjectileStageConfiguration stage, Vector3 origin, Vector3 destination)
	{
		Vector3 zero = Vector3.zero;
		if ((stage.positionOffset != Vector3.zero) & stage.alignPositionWithDestination)
		{
			return zero + ((destination == origin) ? Quaternion.identity : Quaternion.LookRotation(destination - origin)) * stage.positionOffset;
		}
		return zero + stage.positionOffset;
	}

	public static void CalculateExactPosition(Thing projectile, ProjectileEffectTracker effects, Vector3 origin, Vector3 destination, float progress)
	{
		float t = ((effects.progress == null) ? progress : effects.progress(progress));
		effects.currentExactPosition = Vector3.Lerp(origin, destination, t);
		if (effects.lateralOffsetMagnitude != 0f && effects.lateralOffset != null)
		{
			effects.currentExactPosition += effects.destinationRotation * new Vector3(effects.lateralOffsetMagnitude * effects.lateralOffset(progress), 0f);
		}
		effects.currentVisualHeight = ((effects.arcFactor > 0f && effects.height != null) ? (effects.arcFactor * effects.height(progress)) : 0f);
		effects.currentVisualPosition = effects.currentExactPosition;
		effects.currentVisualPosition.y = projectile.def.Altitude;
		effects.currentVisualPosition.z = effects.currentVisualPosition.z + effects.currentVisualHeight;
	}

	public static void CalculateExactPosition(Thing projectile, ProjectileEffectTracker effects, ProjectileStagingTracker staging, int ticksSinceLaunch)
	{
		float num = (float)(ticksSinceLaunch - staging.stage.startingTick) / (float)staging.stage.duration;
		if (staging.stageConfig.progress != null)
		{
			num = staging.stageConfig.progress(num);
		}
		effects.currentExactPosition = Vector3.Lerp(staging.stage.origin, staging.stage.destination, (staging.stageConfig.position == null) ? num : staging.stageConfig.position(num));
		effects.currentVisualHeight = Mathf.Lerp(staging.stage.startingHeight, staging.stage.endingHeight, staging.stageConfig.height(num));
		if (staging.stageConfig.arcFactor != 0f && staging.stageConfig.arc != null)
		{
			effects.currentVisualHeight += staging.stageConfig.arcFactor * staging.stageConfig.arc(num);
		}
		effects.currentVisualPosition = effects.currentExactPosition;
		effects.currentVisualPosition.y = projectile.def.Altitude;
		effects.currentVisualPosition.z = effects.currentVisualPosition.z + effects.currentVisualHeight;
	}

	private static void ExtrapolateRotation(ProjectileEffectTracker effects)
	{
		if (effects.previousVisualPosition != effects.currentVisualPosition)
		{
			effects.currentVisualRotation = Quaternion.LookRotation((effects.currentVisualPosition - effects.previousVisualPosition).Yto0());
			effects.currentVisualAngle = effects.currentVisualRotation.eulerAngles.y;
		}
	}

	public static void CalculateExactRotation(Thing projectile, ProjectileEffectTracker effects, ModExtension_ProjectileEffects effectsExtension, Vector3 origin, Vector3 destination, float distance)
	{
		if (effectsExtension != null)
		{
			if (effectsExtension.fixedRotation)
			{
				effects.currentVisualAngle = 0f;
				effects.currentVisualRotation = Quaternion.identity;
				return;
			}
			if (effectsExtension.rotationRate != 0f)
			{
				effects.currentVisualAngle = effectsExtension.rotationRate * (float)effects.ticksSinceLaunch / 60f;
				effects.currentVisualRotation = Quaternion.AngleAxis(effects.currentVisualAngle, Vector3.up);
				return;
			}
			if (effectsExtension.useVariableHeightFactor)
			{
				ExtrapolateRotation(effects);
				return;
			}
		}
		if (effects.arcFactor != 0f)
		{
			ExtrapolateRotation(effects);
			return;
		}
		effects.currentVisualRotation = effects.destinationRotation;
		effects.currentVisualAngle = effects.currentVisualRotation.eulerAngles.y;
	}

	public static void CalculateExactRotation(Thing projectile, ProjectileEffectTracker effects, ProjectileStagingTracker staging, ModExtension_ProjectileEffects effectsExtension, int ticksSinceLaunch)
	{
		if (effectsExtension != null)
		{
			if (effectsExtension.fixedRotation)
			{
				effects.currentVisualAngle = 0f;
				effects.currentVisualRotation = Quaternion.identity;
				return;
			}
			if (effectsExtension.rotationRate != 0f)
			{
				effects.currentVisualAngle = effectsExtension.rotationRate * (float)effects.ticksSinceLaunch / 60f;
				effects.currentVisualRotation = Quaternion.AngleAxis(effects.currentVisualAngle, Vector3.up);
				return;
			}
		}
		if (effects.previousVisualPosition != effects.currentVisualPosition)
		{
			effects.currentVisualRotation = Quaternion.LookRotation(effects.currentVisualPosition - effects.previousVisualPosition);
			effects.currentVisualAngle = effects.currentVisualRotation.eulerAngles.y;
		}
		else if (staging.stageConfig.overrideInitialAngle)
		{
			effects.currentVisualAngle = staging.stageConfig.angle;
			effects.currentVisualRotation = Quaternion.Euler(0f, effects.currentVisualAngle, 0f);
		}
	}

	public static void DrawProjectileMesh(Projectile projectile, ProjectileEffectTracker effects)
	{
		Graphics.DrawMesh(MeshPool.GridPlane(projectile.def.graphicData.drawSize), effects.currentVisualPosition, effects.currentVisualRotation, projectile.def.DrawMatSingle, 0);
	}

	public static void DrawShadow(Projectile projectile, ProjectileEffectTracker effects, Material shadowMaterial)
	{
		float num = effects.currentVisualHeight / projectile.def.projectile.arcHeightFactor;
		float num2 = projectile.def.projectile.shadowSize * 2f * num;
		Vector3 s = new Vector3(num2, 1f, num2);
		Vector3 vector = new Vector3(0f, -0.01f, 0f);
		Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(effects.currentExactPosition + vector, Quaternion.identity, s), FadedMaterialPool.FadedVersionOf(shadowMaterial, Mathf.Lerp(1f, 0.3f, num)), 0);
	}

	public static InterceptorMapComponent GetInterceptorMapComponent(this Map map)
	{
		return map?.GetComponent<InterceptorMapComponent>();
	}

	public static Vector2 ToRimWorldVector2(this Vector3 v)
	{
		return new Vector2(v.x, v.z);
	}

	public static Vector3 ToRimWorldVector3(this Vector2 v2)
	{
		return new Vector3(v2.x, 1f, v2.y);
	}

	public static void ModifyOriginVector(ref Vector3 origin, Vector3 destination, Vector3 originOffset, bool alignOffset, float originDistance, float pawnScaleFactor = 1f)
	{
		originOffset.x *= pawnScaleFactor;
		originOffset.x *= pawnScaleFactor;
		if (alignOffset && origin != destination)
		{
			Quaternion quaternion = Quaternion.LookRotation((destination - origin).Yto0());
			origin += quaternion * originOffset;
			if (originDistance != 0f)
			{
				origin += quaternion * new Vector3(0f, 0f, originDistance * pawnScaleFactor);
			}
			return;
		}
		origin += originOffset;
		if (originDistance != 0f)
		{
			Vector3 vector = (destination - origin).Yto0();
			vector.Normalize();
			origin += pawnScaleFactor * originDistance * vector;
		}
	}
}
