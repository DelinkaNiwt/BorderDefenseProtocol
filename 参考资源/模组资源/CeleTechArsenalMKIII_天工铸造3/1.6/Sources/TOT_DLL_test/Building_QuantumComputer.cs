using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Building_QuantumComputer : Building
{
	private static readonly Vector3 vector = new Vector3(2.46875f, 0f, 2.46875f);

	private static readonly Material QCGlowTexture = MaterialPool.MatFrom("Things/Buildings/CMC_Beacon_Light", ShaderDatabase.MoteGlow);

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		DrawScreen();
	}

	public void DrawScreen()
	{
		Matrix4x4 matrix = default(Matrix4x4);
		Vector3 pos = DrawPos + Altitudes.AltIncVect + def.graphicData.drawOffset;
		pos.y = AltitudeLayer.BuildingBelowTop.AltitudeFor() + 0.1f;
		matrix.SetTRS(pos, Quaternion.identity, vector);
		Graphics.DrawMesh(MeshPool.plane10, matrix, QCGlowTexture, 0);
	}
}
