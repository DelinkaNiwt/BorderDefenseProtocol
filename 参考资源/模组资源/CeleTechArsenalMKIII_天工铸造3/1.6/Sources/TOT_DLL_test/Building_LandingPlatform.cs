using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Building_LandingPlatform : Building
{
	public int landingTick = 0;

	private static Vector3 vec = new Vector3(5f, 0f, 5f);

	private static readonly Material NormalLight = MaterialPool.MatFrom("Things/Buildings/LandingZone_LightNormal", ShaderDatabase.MoteGlow);

	private static readonly Material LandingLight = MaterialPool.MatFrom("Things/Buildings/LandingZone_Light_ShuttleLanding", ShaderDatabase.MoteGlow);

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		Matrix4x4 matrix = default(Matrix4x4);
		Vector3 pos = DrawPos + Altitudes.AltIncVect;
		pos.y = AltitudeLayer.DoorMoveable.AltitudeFor() + 0.1f;
		matrix.SetTRS(pos, Quaternion.identity, vec);
		Graphics.DrawMesh(MeshPool.plane10, matrix, NormalLight, 0);
		if (landingTick > 0 && Rand.Chance(0.98f))
		{
			Graphics.DrawMesh(MeshPool.plane10, matrix, LandingLight, 0);
		}
	}

	protected override void Tick()
	{
		base.Tick();
		if (landingTick > 0)
		{
			landingTick--;
		}
		if (landingTick != 1)
		{
		}
	}
}
