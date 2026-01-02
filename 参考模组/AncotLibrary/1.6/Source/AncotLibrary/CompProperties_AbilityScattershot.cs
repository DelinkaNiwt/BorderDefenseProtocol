using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityScattershot : CompProperties_AbilityEffect
{
	public ThingDef projectileDef;

	public int burstCount = 5;

	public float scatterAngle = 60f;

	public float range = 20f;

	public int ticksBetweenBurstShots = 5;

	public EffecterDef effecter;

	public JobDef jobDef;

	public FleckDef shotStartFleck;

	public float fleckRotation = 0f;

	public Color fleckColor = new Color(1f, 1f, 1f, 1f);

	public CompProperties_AbilityScattershot()
	{
		compClass = typeof(CompAbilityScattershot);
	}
}
