using Verse;

namespace AncotLibrary;

public class ModExtension_GeneHediff : DefModExtension
{
	public HediffDef hediff;

	public float severity = 1f;

	public float healthPctThreshold = 0.5f;

	public int cooldownTicks = 60000;
}
