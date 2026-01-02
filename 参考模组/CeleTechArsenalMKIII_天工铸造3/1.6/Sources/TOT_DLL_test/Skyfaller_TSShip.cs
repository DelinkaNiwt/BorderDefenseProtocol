using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

internal class Skyfaller_TSShip : Skyfaller
{
	private Material cachedShadowMaterial;

	private Material cachedExactShadow = MaterialPool.MatFrom("Things/Skyfaller/TradeShadow", ShaderDatabase.Transparent);

	private Comp_TraderShuttle Ship => innerContainer.Any ? innerContainer[0].TryGetComp<Comp_TraderShuttle>() : null;

	private new Material ShadowMaterial
	{
		get
		{
			if (cachedShadowMaterial == null && !def.skyfaller.shadow.NullOrEmpty())
			{
				cachedShadowMaterial = MaterialPool.MatFrom(def.skyfaller.shadow, ShaderDatabase.Transparent);
			}
			return cachedShadowMaterial;
		}
	}

	private Material ExactShadow => cachedExactShadow;

	private Skyfaller skyfaller => this;

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		Material exactShadow = ExactShadow;
		if (exactShadow != null)
		{
			Vector3 drawPos = DrawPos;
			drawPos.z = this.TrueCenter().z - 0.2f;
			drawPos.y = AltitudeLayer.Shadows.AltitudeFor();
			Color color = exactShadow.color;
			color.a = Mathf.Clamp(1f - (float)skyfaller.ticksToImpact / 150f, 0.2f, 1f);
			exactShadow.color = color;
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(drawPos, base.Rotation.AsQuat, new Vector3(DrawSize.x, 1f, DrawSize.y));
			Graphics.DrawMesh(MeshPool.plane10Back, matrix, exactShadow, 0, null, 0);
		}
	}
}
