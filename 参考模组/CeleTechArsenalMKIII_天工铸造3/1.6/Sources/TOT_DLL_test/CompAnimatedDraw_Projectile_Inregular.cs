using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompAnimatedDraw_Projectile_Inregular : ThingComp
{
	private Material mat;

	private Material mat2 = MaterialPool.MatFrom("Things/Projectile/MissileTail", ShaderTypeDefOf.MoteGlow.Shader);

	private int startFrameOffset = -1;

	private CompProperties_AnimatedDraw_Projectile_Inregular Props => (CompProperties_AnimatedDraw_Projectile_Inregular)props;

	public override void PostDraw()
	{
		if (startFrameOffset == -1)
		{
			startFrameOffset = Rand.Range(0, Props.totalFrames);
		}
		int num = (Find.TickManager.TicksGame / Props.ticksPerFrame + startFrameOffset) % Props.totalFrames;
		Vector2 mainTextureScale = new Vector2(1f / (float)Props.totalFrames, 1f);
		Vector2 mainTextureOffset = new Vector2((float)num * mainTextureScale.x, 0f);
		if (mat == null)
		{
			mat = MaterialPool.MatFrom(Props.texturePath, ShaderTypeDefOf.Cutout.Shader);
		}
		mat.mainTextureOffset = mainTextureOffset;
		mat.mainTextureScale = mainTextureScale;
		Mesh mesh = MeshPool.GridPlane(Props.DrawSize);
		if (parent is Projectile_PoiMissile projectile_PoiMissile)
		{
			Graphics.DrawMesh(mesh, projectile_PoiMissile.position1, projectile_PoiMissile.rotation, mat, 0);
			mat2.mainTextureOffset = mainTextureOffset;
			mat2.mainTextureScale = mainTextureScale;
			Mesh mesh2 = MeshPool.GridPlane(Props.DrawSize * 1.45f + new Vector2(0.5f, -2f * projectile_PoiMissile.DCFExport * projectile_PoiMissile.DCFExport + 2f * projectile_PoiMissile.DCFExport + 1.5f));
			Graphics.DrawMesh(mesh2, projectile_PoiMissile.position2 - new Vector3(0f, -1f, 0f), projectile_PoiMissile.rotation, mat2, 0);
		}
		else if (parent is Projectile_PoiMissile_Interceptor projectile_PoiMissile_Interceptor)
		{
			Graphics.DrawMesh(mesh, projectile_PoiMissile_Interceptor.position1, projectile_PoiMissile_Interceptor.rotation, mat, 0);
			mat2.mainTextureOffset = mainTextureOffset;
			mat2.mainTextureScale = mainTextureScale;
			Mesh mesh3 = MeshPool.GridPlane(Props.DrawSize * 1.45f + new Vector2(0.5f, -2f * projectile_PoiMissile_Interceptor.DCFExport * projectile_PoiMissile_Interceptor.DCFExport + 2f * projectile_PoiMissile_Interceptor.DCFExport + 1.5f));
			Graphics.DrawMesh(mesh3, projectile_PoiMissile_Interceptor.position2 - new Vector3(0f, -1f, 0f), projectile_PoiMissile_Interceptor.rotation, mat2, 0);
		}
	}
}
