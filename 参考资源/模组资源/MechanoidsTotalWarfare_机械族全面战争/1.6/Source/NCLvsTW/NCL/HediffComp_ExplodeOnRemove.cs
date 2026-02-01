using Verse;

namespace NCL;

public class HediffComp_ExplodeOnRemove : HediffComp
{
	public HediffCompProperties_ExplodeOnRemove Props => (HediffCompProperties_ExplodeOnRemove)props;

	public override void CompPostPostRemoved()
	{
		base.CompPostPostRemoved();
		TryDoExplosion();
	}

	private void TryDoExplosion()
	{
		Pawn pawn = parent.pawn;
		if (pawn != null && !pawn.Destroyed && pawn.Map != null && pawn.Spawned && pawn.Position.IsValid)
		{
			GenExplosion.DoExplosion(pawn.Position, pawn.Map, Props.explosionRadius, Props.damageDef, pawn, Props.damageAmount);
		}
	}
}
