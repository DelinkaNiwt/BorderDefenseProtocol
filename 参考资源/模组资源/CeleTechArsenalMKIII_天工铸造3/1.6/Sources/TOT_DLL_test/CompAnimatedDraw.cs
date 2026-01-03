using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class CompAnimatedDraw : ThingComp
{
	private Material mat;

	private Mesh animationmesh;

	private CompProperties_AnimatedDraw Props => (CompProperties_AnimatedDraw)props;

	private Mesh Mesh
	{
		get
		{
			if (animationmesh == null)
			{
				animationmesh = MeshPool.GridPlane(Props.DrawSize);
			}
			return animationmesh;
		}
	}

	public override void PostDraw()
	{
		int num = Find.TickManager.TicksGame / Props.ticksPerFrame % Props.totalFrames;
		Vector2 mainTextureScale = new Vector2(1f / (float)Props.totalFrames, 1f);
		Vector2 mainTextureOffset = new Vector2((float)num * mainTextureScale.x, 0f);
		if (mat == null)
		{
			mat = MaterialPool.MatFrom(Props.texturePath, ShaderDatabase.Cutout);
		}
		mat.mainTextureOffset = mainTextureOffset;
		mat.mainTextureScale = mainTextureScale;
		mat.shader = Props.ShaderDef.Shader;
		Graphics.DrawMesh(Mesh, parent.DrawPos + Props.Offset, parent.Rotation.AsQuat, mat, 0);
	}
}
