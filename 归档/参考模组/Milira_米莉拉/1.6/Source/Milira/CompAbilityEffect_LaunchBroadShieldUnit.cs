using RimWorld;
using Verse;

namespace Milira;

public class CompAbilityEffect_LaunchBroadShieldUnit : CompAbilityEffect
{
	public new CompProperties_AbilityLaunchBroadShieldUnit Props => (CompProperties_AbilityLaunchBroadShieldUnit)props;

	public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
	{
		base.Apply(target, dest);
		LaunchProjectile(target);
	}

	private void LaunchProjectile(LocalTargetInfo target)
	{
		Pawn pawn = parent.pawn;
		((Projectile)GenSpawn.Spawn(Props.projectileDef, pawn.Position, pawn.Map)).Launch(pawn, pawn.DrawPos, target, target, ProjectileHitFlags.IntendedTarget);
	}
}
