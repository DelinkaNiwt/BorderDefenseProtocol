using Verse;

namespace AncotLibrary;

public class HediffComp_EffecterMaintain : HediffComp
{
	public Effecter effecter;

	public int ticks = -1;

	private HediffCompProperties_EffecterMaintain Props => (HediffCompProperties_EffecterMaintain)props;

	public override void CompPostMake()
	{
		ticks = Props.maintainTicks;
		if (Props.effcterDef != null)
		{
			base.Pawn.Map.effecterMaintainer.AddEffecterToMaintain(Props.effcterDef.Spawn(base.Pawn, base.Pawn.Map, Props.scale), base.Pawn, Props.maintainTicks);
		}
	}
}
