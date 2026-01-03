using Verse;

namespace AncotLibrary;

public class HediffCompProperties_RestoreProjectileInterceptor : HediffCompProperties
{
	public float restoreThreshold = 0.1f;

	public float restorePct = 0.2f;

	public float severityThreshold = 1f;

	public float severityCost = 0.99f;

	public EffecterDef effecter;

	public HediffCompProperties_RestoreProjectileInterceptor()
	{
		compClass = typeof(HediffComp_RestoreProjectileInterceptor);
	}
}
