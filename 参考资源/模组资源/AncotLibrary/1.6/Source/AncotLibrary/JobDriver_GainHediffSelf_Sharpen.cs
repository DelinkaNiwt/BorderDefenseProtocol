using Verse;

namespace AncotLibrary;

public class JobDriver_GainHediffSelf_Sharpen : JobDriver_GainHediffSelf
{
	public override HediffDef HediffDef => AncotDefOf.Ancot_Sharpen;

	public override int WarmupTicks => 60;
}
