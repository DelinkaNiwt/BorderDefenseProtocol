using UnityEngine;
using Verse;

namespace AncotLibrary;

public class Bullet_LongBarrelAdaptive : Projectile_Custom
{
	public new ModExtension_Bullet_LongBarrelAdaptive Props => def.GetModExtension<ModExtension_Bullet_LongBarrelAdaptive>();

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		float num = Props.barrelLength / def.projectile.SpeedTilesPerTick;
		float num2 = Mathf.Clamp01(num / base.StartingTicksToImpact);
		if (!(base.DistanceCoveredFraction < num2))
		{
			base.DrawAt(drawLoc, flip);
		}
	}

	public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
	{
		hitFlags |= ProjectileHitFlags.NonTargetWorld;
		base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
	}
}
