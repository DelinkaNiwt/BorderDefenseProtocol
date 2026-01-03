using RimWorld;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Apparel_PersonalTerminal : Apparel
{
	private static bool IsHashIntervalTick(Thing t, int interval)
	{
		return t.HashOffsetTicks() % interval == 0;
	}

	protected override void Tick()
	{
		base.Tick();
		if (IsHashIntervalTick(this, 5900) && base.Wearer != null && !base.Wearer.Dead && ModLister.IdeologyInstalled)
		{
			HediffDef named = DefDatabase<HediffDef>.GetNamed("NeuralSupercharge");
			Hediff hediff = HediffMaker.MakeHediff(named, base.Wearer);
			hediff.TryGetComp<HediffComp_Disappears>().ticksToDisappear = 6000;
			base.Wearer.health.AddHediff(hediff);
		}
	}
}
