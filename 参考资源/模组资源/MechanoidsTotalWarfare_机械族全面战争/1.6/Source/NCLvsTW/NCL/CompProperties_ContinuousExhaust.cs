using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CompProperties_ContinuousExhaust : CompProperties
{
	public List<FleckDef> fleckTypes = new List<FleckDef>
	{
		FleckDefOf.Smoke,
		FleckDefOf.FireGlow,
		FleckDefOf.MicroSparks
	};

	public FloatRange fleckScale = new FloatRange(0.3f, 0.7f);

	public FloatRange smokeScale = new FloatRange(0.8f, 1.5f);

	public FloatRange fleckAngleRange = new FloatRange(0f, 360f);

	public FloatRange smokeAngleRange = new FloatRange(-30f, 30f);

	public FloatRange fleckSpeedRange = new FloatRange(0.05f, 0.15f);

	public FloatRange smokeSpeedRange = new FloatRange(0.01f, 0.03f);

	public FloatRange fleckRotationRange = new FloatRange(0f, 360f);

	public FloatRange fleckRotationRate = new FloatRange(-30f, 30f);

	public int fleckInterval = 1;

	public Vector3 offsetDirection = Vector3.back;

	public float offsetDistance = 0.5f;

	public CompProperties_ContinuousExhaust()
	{
		compClass = typeof(CompContinuousExhaust);
	}
}
