using Verse;

namespace NCL;

public class HediffCompProperties_EffectAura : HediffCompProperties
{
	public FleckDef EffectFleckDef;

	public SoundDef soundDef;

	public float minDistance = 5f;

	public float maxDistance = 6f;

	public int EffectInterval = 120;

	public int EffectsPerBurst = 3;

	public HediffCompProperties_EffectAura()
	{
		compClass = typeof(HediffComp_EffectAura);
	}
}
