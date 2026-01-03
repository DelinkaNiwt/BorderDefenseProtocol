using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityLaunchProjectileArea : CompProperties_AbilityEffect
{
	public ThingDef projectileDef;

	public float radius;

	public int burstShotCount = 1;

	public FleckDef shotStartFleck;

	public float fleckRotation = 0f;

	public float fleckScale = 1f;

	public Color fleckColor = new Color(1f, 1f, 1f, 1f);

	public CompProperties_AbilityLaunchProjectileArea()
	{
		compClass = typeof(CompAbilityLaunchProjectileArea);
	}
}
