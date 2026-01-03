using Verse;

namespace AncotLibrary;

public class CompProperties_ApplyHediffArea : CompProperties
{
	public float radius = 2f;

	public float severity = 1f;

	public HediffDef hediff;

	public bool applyOnAlly = true;

	public bool applyOnAllyOnly = false;

	public bool applyOnMech = true;

	public bool ignoreCaster = false;

	public EffecterDef effecter;

	public int intervalTick = 60;

	public CompProperties_ApplyHediffArea()
	{
		compClass = typeof(CompApplyHediffArea);
	}
}
