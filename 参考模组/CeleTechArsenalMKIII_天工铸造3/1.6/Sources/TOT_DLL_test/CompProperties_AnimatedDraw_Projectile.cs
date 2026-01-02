using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompProperties_AnimatedDraw_Projectile : CompProperties
{
	public string texturePath;

	public int totalFrames;

	public int ticksPerFrame;

	public Vector2 DrawSize = Vector2.zero;

	public ShaderTypeDef ShaderDef = ShaderTypeDefOf.Cutout;

	public CompProperties_AnimatedDraw_Projectile()
	{
		compClass = typeof(CompAnimatedDraw_Projectile);
	}
}
