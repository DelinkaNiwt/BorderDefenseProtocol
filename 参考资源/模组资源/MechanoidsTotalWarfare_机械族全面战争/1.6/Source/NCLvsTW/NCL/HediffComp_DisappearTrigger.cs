using Verse;

namespace NCL;

public class HediffComp_DisappearTrigger : HediffComp
{
	public HediffCompProperties_DisappearTrigger Props => (HediffCompProperties_DisappearTrigger)props;

	public override void CompPostPostRemoved()
	{
		base.CompPostPostRemoved();
		if ((!Props.onlyIfFullyHealed || !(parent.Severity > 0f)) && parent.pawn != null && parent.pawn.Spawned && !parent.pawn.Dead)
		{
			parent.pawn.health.AddHediff(Props.hediffToGive);
		}
	}
}
