using System.Collections.Generic;
using Verse;

namespace TOT_DLL_test;

public class CompProperties_UAV : CompProperties
{
	public ThingDef turretDef;

	public float angleOffset;

	public bool autoAttack = true;

	public List<PawnRenderNodeProperties> renderNodeProperties;

	public float InterceptorRange = 10f;

	public int STTick = 3;

	public float BobSpeed = 0.0005f;

	public float BobDistance = 0.08f;

	public float Xoffset = 0.2f;

	public float Yoffset = 0.12f;

	public CompProperties_UAV()
	{
		compClass = typeof(Comp_UAV);
	}
}
