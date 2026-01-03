using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompAbilityEffect_LaunchProjectileCustom : CompAbilityEffect
{
	public new CompProperties_AbilityLaunchProjectileCustom Props => (CompProperties_AbilityLaunchProjectileCustom)props;

	private Pawn Caster => parent.pawn;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		LaunchProjectile(target);
	}

	private void LaunchProjectile(LocalTargetInfo target)
	{
		if (Props.projectileDef != null)
		{
			Pawn pawn = parent.pawn;
			((Projectile)GenSpawn.Spawn(Props.projectileDef, pawn.Position, pawn.Map)).Launch(pawn, pawn.DrawPos, target, target, ProjectileHitFlags.IntendedTarget, parent.verb.preventFriendlyFire);
		}
		float angleFlat = (target.Cell - Caster.Position).AngleFlat;
		if (Props.shotStartFleck != null)
		{
			Map mapHeld = Caster.MapHeld;
			FleckDef shotStartFleck = Props.shotStartFleck;
			Vector3 loc = Caster.Position.ToVector3Shifted();
			Color fleckColor = Props.fleckColor;
			float rotation = angleFlat + Props.fleckRotation;
			float fleckScale = Props.fleckScale;
			AncotFleckMaker.CustomFleckThrow(mapHeld, shotStartFleck, loc, fleckColor, default(Vector3), fleckScale, 0f, 0f, 0f, rotation);
		}
	}

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}
}
