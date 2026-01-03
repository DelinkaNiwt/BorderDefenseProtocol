using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompAbilityLaunchProjectileArea : CompAbilityEffect
{
	private Pawn Pawn => parent.pawn;

	public new CompProperties_AbilityLaunchProjectileArea Props => (CompProperties_AbilityLaunchProjectileArea)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		for (int i = 0; i < Props.burstShotCount; i++)
		{
			int maxExclusive = GenRadial.NumCellsInRadius(Props.radius);
			int num = Rand.Range(0, maxExclusive);
			((Projectile)GenSpawn.Spawn(Props.projectileDef, Pawn.Position, Pawn.Map)).Launch(Pawn, Pawn.DrawPos, target.Cell + GenRadial.RadialPattern[num], null, ProjectileHitFlags.IntendedTarget);
		}
		float angleFlat = (target.Cell - Pawn.Position).AngleFlat;
		if (Props.shotStartFleck != null)
		{
			Map mapHeld = Pawn.MapHeld;
			FleckDef shotStartFleck = Props.shotStartFleck;
			Vector3 loc = Pawn.Position.ToVector3Shifted();
			Color fleckColor = Props.fleckColor;
			float rotation = angleFlat + Props.fleckRotation;
			float fleckScale = Props.fleckScale;
			AncotFleckMaker.CustomFleckThrow(mapHeld, shotStartFleck, loc, fleckColor, default(Vector3), fleckScale, 0f, 0f, 0f, rotation);
		}
	}

	public override void DrawEffectPreview(LocalTargetInfo target)
	{
		GenDraw.DrawRadiusRing(target.Cell, Props.radius);
	}

	public override bool AICanTargetNow(LocalTargetInfo target)
	{
		return true;
	}
}
