using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompProperties_AnimatedDraw_Projectile_Inregular : CompProperties
{
	public string texturePath;

	public int totalFrames;

	public int ticksPerFrame;

	public Vector2 DrawSize = Vector2.zero;

	public CompProperties_AnimatedDraw_Projectile_Inregular()
	{
		compClass = typeof(CompAnimatedDraw_Projectile_Inregular);
	}
}
