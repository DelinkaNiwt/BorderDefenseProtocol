using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class BuildingZPR : Building
{
	private static readonly Vector3 vec = new Vector3(4.8f, 0f, 7.2f);

	private static readonly Material ScreenTexture = MaterialPool.MatFrom("Things/Buildings/CMC_ZPGenerator_Light", ShaderDatabase.TransparentPostLight);

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		DrawScreen(drawLoc, vec);
	}

	public void DrawScreen(Vector3 drawLoc, Vector3 vec)
	{
		Matrix4x4 matrix = default(Matrix4x4);
		Vector3 pos = DrawPos + Altitudes.AltIncVect + def.graphicData.drawOffset;
		pos.y = AltitudeLayer.Building.AltitudeFor() + 0.15f;
		matrix.SetTRS(pos, Quaternion.identity, vec);
		Graphics.DrawMesh(MeshPool.plane10, matrix, ScreenTexture, 0);
	}
}
