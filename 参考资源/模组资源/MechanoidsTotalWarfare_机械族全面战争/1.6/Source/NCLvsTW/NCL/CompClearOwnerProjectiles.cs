using System;
using System.Collections.Generic;
using Verse;

namespace NCL;

public class CompClearOwnerProjectiles : ThingComp
{
	private bool projectilesCleared = false;

	public override void CompTick()
	{
		base.CompTick();
		if (parent.Destroyed && !projectilesCleared)
		{
			ClearOwnerProjectiles();
			projectilesCleared = true;
		}
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		if (!projectilesCleared)
		{
			ClearOwnerProjectiles();
			projectilesCleared = true;
		}
	}

	private void ClearOwnerProjectiles()
	{
		Map map = parent.MapHeld;
		if (map == null)
		{
			return;
		}
		List<Thing> projectiles = map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
		for (int i = projectiles.Count - 1; i >= 0; i--)
		{
			Thing thing = projectiles[i];
			if (thing is Projectile projectile && projectile.Launcher == parent)
			{
				try
				{
					SafeDestroyProjectile(projectile);
				}
				catch (Exception ex)
				{
					Log.Warning($"Failed to safely destroy projectile {projectile}: {ex.Message}");
				}
			}
		}
	}

	private void SafeDestroyProjectile(Projectile projectile)
	{
		if (!projectile.DestroyedOrNull() && projectile.Spawned && projectile.Map != null && !projectile.Destroyed)
		{
			projectile.DeSpawn();
		}
	}
}
