using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompProperties_AnimatedDraw : CompProperties
{
	public string texturePath;

	public int totalFrames;

	public int ticksPerFrame;

	public Vector2 DrawSize = Vector2.zero;

	public Vector3 Offset = Vector3.zero;

	public ShaderTypeDef ShaderDef = ShaderTypeDefOf.Cutout;

	public CompProperties_AnimatedDraw()
	{
		compClass = typeof(CompAnimatedDraw);
	}
}
