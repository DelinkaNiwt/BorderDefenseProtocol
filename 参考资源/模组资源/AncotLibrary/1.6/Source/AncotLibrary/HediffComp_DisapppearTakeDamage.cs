using Verse;

namespace AncotLibrary;

public class HediffComp_DisapppearTakeDamage : HediffComp
{
	private HediffCompProperties_DisapppearTakeDamage Props => (HediffCompProperties_DisapppearTakeDamage)props;

	public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
	{
		if (Props.damageDefs.Contains(dinfo.Def))
		{
			base.Pawn.health.RemoveHediff(parent);
			if (Props.disapppearEffecter != null)
			{
				Effecter effecter = new Effecter(Props.disapppearEffecter);
				effecter.Trigger(base.Pawn, dinfo.Instigator).Cleanup();
			}
		}
	}
}
