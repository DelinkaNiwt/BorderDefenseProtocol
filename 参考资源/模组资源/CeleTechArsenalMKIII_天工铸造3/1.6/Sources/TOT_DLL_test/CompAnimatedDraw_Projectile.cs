using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompAnimatedDraw_Projectile : ThingComp
{
	private Material mat;

	private int startFrameOffset = -1;

	private CompProperties_AnimatedDraw_Projectile Props => (CompProperties_AnimatedDraw_Projectile)props;

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
			mat = MaterialPool.MatFrom(Props.texturePath, Props.ShaderDef.Shader);
		}
		mat.mainTextureOffset = mainTextureOffset;
		mat.mainTextureScale = mainTextureScale;
		Projectile projectile = parent as Projectile;
		Mesh mesh = MeshPool.GridPlane(Props.DrawSize + new Vector2(0f, (float)(Find.TickManager.TicksGame - projectile.spawnedTick) * 0.048f));
		if (projectile != null)
		{
			Graphics.DrawMesh(mesh, projectile.DrawPos, projectile.ExactRotation, mat, 0);
		}
	}
}
