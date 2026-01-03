using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary.Utility;

public static class AncotProjectileUtility
{
	public static Vector3 HitShieldPos(Projectile projectile, CompProjectileInterceptor comp, Vector3 vector1, Vector3 vector2)
	{
		Vector3 normalized = (vector2 - vector1).normalized;
		float num = (vector2 - vector1).MagnitudeHorizontalSquared();
		for (int i = 0; (float)i < num; i++)
		{
			if (CheckIntercept(projectile, comp, vector1 + i * normalized, vector1 + (i + 1) * normalized))
			{
				return vector1 + ((float)i + 0.5f) * normalized;
			}
		}
		return vector2;
	}

	public static bool CheckIntercept(Projectile projectile, CompProjectileInterceptor comp, Vector3 lastExactPos, Vector3 newExactPos)
	{
		Vector3 vector = comp.parent.Position.ToVector3Shifted();
		float num = comp.Props.radius + projectile.def.projectile.SpeedTilesPerTick + 0.1f;
		if ((newExactPos.x - vector.x) * (newExactPos.x - vector.x) + (newExactPos.z - vector.z) * (newExactPos.z - vector.z) > num * num)
		{
			return false;
		}
		if (!CompProjectileInterceptor.InterceptsProjectile(comp.Props, projectile))
		{
			return false;
		}
		if (projectile.Launcher == null && !comp.Props.interceptNonHostileProjectiles)
		{
			return false;
		}
		if (comp.parent.Faction != null)
		{
			if (projectile.Launcher != null && projectile.Launcher.Spawned && !projectile.Launcher.HostileTo(comp.parent.Faction))
			{
				return false;
			}
			if (projectile.Launcher != null && !projectile.Launcher.Spawned && !projectile.Launcher.Faction.HostileTo(comp.parent.Faction))
			{
				return false;
			}
		}
		if (!comp.Props.interceptOutgoingProjectiles && (new Vector2(vector.x, vector.z) - new Vector2(lastExactPos.x, lastExactPos.z)).sqrMagnitude <= comp.Props.radius * comp.Props.radius)
		{
			return false;
		}
		if (!GenGeo.IntersectLineCircleOutline(new Vector2(vector.x, vector.z), comp.Props.radius, new Vector2(lastExactPos.x, lastExactPos.z), new Vector2(newExactPos.x, newExactPos.z)))
		{
			return false;
		}
		return true;
	}
}
