using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class CompProperties_AbilityLaunchProjectileCustom : CompProperties_AbilityEffect
{
	public ThingDef projectileDef;

	public FleckDef shotStartFleck;

	public float fleckRotation = 0f;

	public float fleckScale = 1f;

	public Color fleckColor = new Color(1f, 1f, 1f, 1f);

	public CompProperties_AbilityLaunchProjectileCustom()
	{
		compClass = typeof(CompAbilityEffect_LaunchProjectileCustom);
	}
}
