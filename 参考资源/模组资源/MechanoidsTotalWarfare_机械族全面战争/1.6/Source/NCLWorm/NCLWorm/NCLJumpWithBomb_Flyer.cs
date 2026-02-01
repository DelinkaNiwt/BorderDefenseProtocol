using RimWorld;
using Verse;

namespace NCLWorm;

public class NCLJumpWithBomb_Flyer : PawnFlyer
{
	protected override void RespawnPawn()
	{
		Map map = base.Map;
		IntVec3 position = base.Position;
		base.RespawnPawn();
		GenExplosion.DoExplosion(position, map, 4f, DamageDefOf.Bomb, base.FlyingPawn, 30, 0.45f);
	}
}
