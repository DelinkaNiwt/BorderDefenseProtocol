using UnityEngine;
using Verse;

namespace NCL;

public class CompProperties_DualOverlay : CompProperties
{
	public string staticGraphicPath;

	public Vector3 staticOffset = Vector3.zero;

	public float staticRotation = 0f;

	public float staticScale = 1f;

	public AltitudeLayer staticAltitudeLayer = AltitudeLayer.BuildingOnTop;

	public string floatingGraphicPath;

	public Vector3 floatingOffset = Vector3.zero;

	public float floatingRotation = 0f;

	public float floatingScale = 1f;

	public AltitudeLayer floatingAltitudeLayer = AltitudeLayer.MoteOverhead;

	public float floatAmplitude = 0.15f;

	public float floatFrequency = 0.02f;

	public CompProperties_DualOverlay()
	{
		compClass = typeof(CompDualOverlay);
	}
}
