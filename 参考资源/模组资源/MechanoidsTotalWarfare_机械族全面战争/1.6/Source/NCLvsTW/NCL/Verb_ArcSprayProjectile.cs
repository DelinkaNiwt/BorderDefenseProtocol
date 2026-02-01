using Verse;

namespace NCL;

public class Verb_ArcSprayProjectile : Verb_ArcSpray
{
	protected override void HitCell(IntVec3 cell)
	{
		base.HitCell(cell);
		((Projectile)GenSpawn.Spawn(verbProps.defaultProjectile, caster.Position, caster.Map)).Launch(caster, caster.DrawPos, cell, cell, ProjectileHitFlags.All);
	}
}
