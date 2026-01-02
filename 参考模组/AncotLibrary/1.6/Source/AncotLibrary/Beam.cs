using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class Beam : Projectile_Custom
{
	private Mote mote;

	public virtual float EndOffsetFactor => 3f;

	public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
	{
		base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
		Vector3 vector = destination + Vector3.up * def.Altitude;
		Vector3 offsetA = (vector - launcher.Position.ToVector3Shifted()).Yto0().normalized * def.projectile.beamStartOffset;
		Vector3 offsetB = (vector - launcher.Position.ToVector3Shifted()).Yto0().normalized * EndOffsetFactor;
		if (def.projectile.beamMoteDef != null)
		{
			mote = MoteMaker.MakeInteractionOverlay(def.projectile.beamMoteDef, launcher, this, offsetA, offsetB);
		}
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		base.Impact(hitThing, blockedByShield);
		if (mote != null)
		{
			mote.solidTimeOverride = 0.01f;
		}
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		if (mote != null)
		{
			mote.solidTimeOverride = 0.01f;
		}
		base.Destroy(mode);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref mote, "mote");
	}
}
