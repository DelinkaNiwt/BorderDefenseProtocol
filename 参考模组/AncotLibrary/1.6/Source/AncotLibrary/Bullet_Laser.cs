using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class Bullet_Laser : Bullet
{
	private static readonly Material BeamMat = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

	private static readonly Material BeamEndMat = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, MapMaterialRenderQueues.OrbitalBeam);

	private static readonly MaterialPropertyBlock MatPropertyBlock = new MaterialPropertyBlock();

	public ModExtension_BulletLaser Props => def.GetModExtension<ModExtension_BulletLaser>();

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		float num = Props.barrelLength / def.projectile.SpeedTilesPerTick;
		float num2 = Mathf.Clamp01(num / base.StartingTicksToImpact);
		if (!(base.DistanceCoveredFraction < num2))
		{
			base.DrawAt(drawLoc, flip);
			Vector3 vector = origin;
			float num3 = Mathf.Max((DrawPos - origin).magnitude + 0.8f, 0f);
			Vector3 normalized = (destination - origin).normalized;
			Vector3 vector2 = normalized * 1.5f;
			Vector3 pos = vector + normalized * num3 * 0.5f + vector2;
			Vector3 vector3 = normalized * ((1f - num2) * num) + vector2;
			pos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
			Color value = new Color(0.906f, 0.18f, 0.18f);
			value.a *= 1f / base.DistanceCoveredFraction;
			MatPropertyBlock.SetColor(ShaderPropertyIDs.Color, value);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(pos, Quaternion.Euler(0f, normalized.AngleFlat(), 0f), new Vector3(0.4f, 1f, num3));
			Graphics.DrawMesh(MeshPool.plane10, matrix, BeamMat, 0, null, 0, MatPropertyBlock);
			Vector3 pos2 = vector + vector3;
			pos2.y = AltitudeLayer.MetaOverlays.AltitudeFor();
			Matrix4x4 matrix2 = default(Matrix4x4);
			matrix2.SetTRS(pos2, Quaternion.Euler(0f, normalized.AngleFlat(), 0f), new Vector3(0.4f, 1f, 0.2f));
			Graphics.DrawMesh(MeshPool.plane10, matrix2, BeamEndMat, 0, null, 0, MatPropertyBlock);
		}
	}

	public static void CustomFleckThrow(Map map, FleckDef fleckDef, Vector3 loc, Color color, Vector3 offset, float scale = 1f, float rotationRate = 0f, float velocityAngle = 0f, float velocitySpeed = 0f, float rotation = 0f)
	{
		if (loc.ToIntVec3().ShouldSpawnMotesAt(map))
		{
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + offset, map, fleckDef, scale);
			dataStatic.rotationRate = rotationRate;
			dataStatic.velocityAngle = velocityAngle;
			dataStatic.velocitySpeed = velocitySpeed;
			dataStatic.instanceColor = color;
			dataStatic.scale = scale;
			dataStatic.rotation = rotation;
			map.flecks.CreateFleck(dataStatic);
		}
	}
}
