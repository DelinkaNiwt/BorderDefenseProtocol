using UnityEngine;
using Verse;

namespace NCL;

public class CompProperties_AuraEffect : CompProperties
{
	public int spawnIntervalTicks = 10;

	public FloatRange spawnRadius = new FloatRange(0.5f, 1.2f);

	public Vector3 offset = Vector3.zero;

	public FleckDef fleckDef;

	public int fleckCount = 1;

	public ThingDef moteDef;

	public Color particleColor = Color.white;

	public float scale = 1f;

	public EffecterDef effecterDef;

	public int effecterTriggerInterval = 30;

	public CompProperties_AuraEffect()
	{
		compClass = typeof(CompAuraEffect);
	}
}
