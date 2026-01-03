using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Projectile_Fire : Projectile_Explosive
{
	public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_FireGlow");

	public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_ProjectileSmoke_LongLastingGrow");

	public int Fleck_MakeFleckTickMax = 1;

	public IntRange Fleck_MakeFleckNum = new IntRange(2, 2);

	public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);

	public FloatRange Fleck_Scale = new FloatRange(1.3f, 1.5f);

	public FloatRange Fleck_Scale2 = new FloatRange(1.1f, 1.2f);

	public FloatRange Fleck_Speed = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);

	public int Fleck_MakeFleckTick;

	public Vector3 IPPos(float t)
	{
		t = Mathf.Clamp01(t);
		Vector3 vector = origin + (destination - origin).Yto0() * t;
		return vector + 4f * t * (1f - t) * new Vector3(0f, 0f, 1f);
	}

	protected override void DrawAt(Vector3 position, bool flip = false)
	{
		Vector3 vector = IPPos(base.DistanceCoveredFraction - 0.01f);
		position = IPPos(base.DistanceCoveredFraction);
		Quaternion rotation = Quaternion.LookRotation(position - vector);
		if (base.DistanceCoveredFraction > 0.02f)
		{
			Vector3 position2 = position;
			position2.y = AltitudeLayer.Projectile.AltitudeFor();
			Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), position2, rotation, DrawMat, 0);
			Comps_PostDraw();
		}
	}

	protected override void Tick()
	{
		if (intendedTarget.Thing != null)
		{
			destination = intendedTarget.Thing.DrawPos;
		}
		Fleck_MakeFleckTick++;
		if (Fleck_MakeFleckTick >= Fleck_MakeFleckTickMax && base.DistanceCoveredFraction > 0.04f)
		{
			Fleck_MakeFleckTick = 0;
			Map map = base.Map;
			int randomInRange = Fleck_MakeFleckNum.RandomInRange;
			Vector3 vector = IPPos(base.DistanceCoveredFraction);
			Vector3 vector2 = IPPos(base.DistanceCoveredFraction - 0.01f);
			for (int i = 0; i < randomInRange; i++)
			{
				float num = (vector - intendedTarget.CenterVector3).AngleFlat();
				float velocityAngle = Fleck_Angle.RandomInRange + num;
				float randomInRange2 = Fleck_Scale.RandomInRange;
				float randomInRange3 = Fleck_Scale2.RandomInRange;
				float randomInRange4 = Fleck_Speed.RandomInRange;
				float randomInRange5 = Fleck_Speed2.RandomInRange;
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(vector, map, FleckDef, randomInRange2);
				FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(vector, map, FleckDef2, randomInRange3);
				dataStatic.rotation = (vector - vector2).AngleFlat();
				dataStatic2.rotation = (vector - vector2).AngleFlat();
				dataStatic.rotationRate = Fleck_Rotation.RandomInRange;
				dataStatic.velocityAngle = velocityAngle;
				dataStatic.velocitySpeed = randomInRange4;
				dataStatic2.rotationRate = Fleck_Rotation.RandomInRange;
				dataStatic2.velocityAngle = velocityAngle;
				dataStatic2.velocitySpeed = randomInRange5;
				map.flecks.CreateFleck(dataStatic2);
				map.flecks.CreateFleck(dataStatic);
			}
		}
		base.Tick();
	}
}
