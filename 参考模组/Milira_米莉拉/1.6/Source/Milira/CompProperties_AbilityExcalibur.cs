using RimWorld;
using UnityEngine;

namespace Milira;

public class CompProperties_AbilityExcalibur : CompProperties_AbilityEffect
{
	public float distance = 4f;

	public float width = 8f;

	public Color color = Color.white;

	public CompProperties_AbilityExcalibur()
	{
		compClass = typeof(CompAbilityEffect_Excalibur);
	}
}
