using System;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public static class EffectUtility
{
	public static EffectMapComponent EccentricProjectilesEffectComp(this Map map)
	{
		if (EffectMapComponent.cachedInstance != null && EffectMapComponent.cachedInstance.map.uniqueID == map.uniqueID)
		{
			return EffectMapComponent.cachedInstance;
		}
		if (EffectMapComponent.CachedInstances.TryGetValue(map.uniqueID, out var value))
		{
			EffectMapComponent.cachedInstance = value;
		}
		else
		{
			EffectMapComponent.cachedInstance = map.GetComponent<EffectMapComponent>();
			EffectMapComponent.CachedInstances[map.uniqueID] = EffectMapComponent.cachedInstance;
		}
		return EffectMapComponent.cachedInstance;
	}

	public static EffectContext CreateContext(EffectDef effectDef, Pawn caster, Thing target, bool positionAtTarget = false)
	{
		return CreateContext(effectDef, caster.MapHeld, caster.DrawPos, target.DrawPos, caster, target, positionAtTarget);
	}

	public static EffectContext CreateContext(EffectDef effectDef, Map map, Vector3 casterPos, Vector3 targetPos, Thing anchor = null, Thing destinationAnchor = null, bool positionAtTarget = false)
	{
		Quaternion rotation = ((targetPos == casterPos) ? Quaternion.identity : Quaternion.LookRotation((targetPos - casterPos).Yto0()));
		float y = rotation.eulerAngles.y;
		EffectContext result = new EffectContext(map, effectDef);
		result.anchor = anchor;
		result.destinationAnchor = destinationAnchor;
		result.position = ((positionAtTarget || effectDef.attachToTarget) ? targetPos : casterPos);
		result.origin = casterPos;
		result.destination = targetPos;
		result.rotation = rotation;
		result.angle = y;
		return result;
	}

	public static Vector3 CalculateDriftOffset(float drawDriftDistance)
	{
		float value = Rand.Value;
		value = drawDriftDistance * (1f - value * value);
		if (value > 0f)
		{
			float f = (float)Math.PI * 2f * Rand.Value;
			return new Vector3(value * Mathf.Cos(f), 0f, value * Mathf.Sin(f));
		}
		return Vector3.zero;
	}

	public static bool ShouldBeVisibleFrom(this CellRect cellRect, IntVec3 position, Vector3 drawSize)
	{
		int num = Mathf.CeilToInt(drawSize.x / 2f);
		if (position.x < cellRect.minX - num || position.x > cellRect.maxX + num)
		{
			return false;
		}
		int num2 = Mathf.CeilToInt(drawSize.z / 2f);
		return position.z >= cellRect.minZ - num2 && position.z <= cellRect.maxZ + num2;
	}

	public static void DrawLine(Vector3 from, Vector3 to, Material lineMaterial, float alpha)
	{
		GenDraw.DrawLineBetween(from, to, FadedMaterialPool.FadedVersionOf(lineMaterial, alpha));
	}

	public static void DrawPulsedLine(Vector3 from, Vector3 to, float progress, float width = 0.1f)
	{
		if (!(from == to))
		{
			Vector3 vector = to - from;
			Quaternion q = Quaternion.LookRotation(from - to);
			float num = AnimationUtility.EaseInOutCubic(progress);
			float num2 = ((num > 0.5f) ? (1f - 2f * (num - 0.5f)) : (2f * num));
			Vector3 s = new Vector3(width, 1f, num2 * (from - to).MagnitudeHorizontal());
			Matrix4x4 matrix4x = default(Matrix4x4);
			Vector3 pos = from + vector * num;
			pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
			matrix4x.SetTRS(pos, q, s);
		}
	}
}
