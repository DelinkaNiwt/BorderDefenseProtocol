using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompProperties_WeaponRenderDynamic : CompProperties
{
	public string TexturePath;

	public int totalFrames;

	public int ticksPerFrame;

	public Vector2 DrawSize = Vector2.zero;

	public Vector3 Offset = Vector3.zero;

	public CompProperties_WeaponRenderDynamic()
	{
		compClass = typeof(Comp_WeaponRenderDynamic);
	}
}
