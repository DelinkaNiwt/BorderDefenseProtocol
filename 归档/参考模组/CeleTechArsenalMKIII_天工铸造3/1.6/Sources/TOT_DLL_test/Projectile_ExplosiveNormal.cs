using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Projectile_ExplosiveNormal : Projectile_Explosive
{
	private int tickcount;

	public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_SparkFlash_Blue_Small");

	public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_SparkFlash_Blue_LongLasting_Small");

	public int Fleck_MakeFleckTickMax = 1;

	public IntRange Fleck_MakeFleckNum = new IntRange(2, 2);

	public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);

	public FloatRange Fleck_Scale = new FloatRange(2.2f, 2.3f);

	public FloatRange Fleck_Speed = new FloatRange(5f, 7f);

	public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);

	public int Fleck_MakeFleckTick;

	public override bool AnimalsFleeImpact => true;

	private Vector3 CurretPos(float t)
	{
		return origin + (destination - origin) * t;
	}

	protected override void DrawAt(Vector3 position, bool flip = false)
	{
		Vector3 vector = CurretPos(base.DistanceCoveredFraction - 0.01f);
		position = CurretPos(base.DistanceCoveredFraction);
		Quaternion rotation = Quaternion.LookRotation(position - vector);
		if (tickcount >= 2)
		{
			Vector3 position2 = position;
			position2.y = AltitudeLayer.Projectile.AltitudeFor();
			Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), position2, rotation, DrawMat, 0);
			Comps_PostDraw();
		}
	}

	protected override void Tick()
	{
		tickcount++;
		Fleck_MakeFleckTick++;
		if (Fleck_MakeFleckTick >= Fleck_MakeFleckTickMax && tickcount >= 8)
		{
			Fleck_MakeFleckTick = 0;
			Map map = base.Map;
			int randomInRange = Fleck_MakeFleckNum.RandomInRange;
			Vector3 vector = CurretPos(base.DistanceCoveredFraction - 0.01f);
			Vector3 vector2 = CurretPos(base.DistanceCoveredFraction - 0.02f);
			for (int i = 0; i < randomInRange; i++)
			{
				float num = (vector - intendedTarget.CenterVector3).AngleFlat();
				float velocityAngle = Fleck_Angle.RandomInRange + num;
				float randomInRange2 = Fleck_Scale.RandomInRange;
				float randomInRange3 = Fleck_Speed.RandomInRange;
				float randomInRange4 = Fleck_Speed2.RandomInRange;
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(vector, map, FleckDef, randomInRange2);
				FleckCreationData dataStatic2 = FleckMaker.GetDataStatic(vector2, map, FleckDef2, randomInRange2);
				dataStatic.rotation = (vector - vector2).AngleFlat();
				dataStatic.velocityAngle = velocityAngle;
				dataStatic.velocitySpeed = randomInRange3;
				dataStatic2.rotation = (vector - vector2).AngleFlat();
				dataStatic2.velocityAngle = velocityAngle;
				dataStatic2.velocitySpeed = randomInRange4;
				map.flecks.CreateFleck(dataStatic2);
				map.flecks.CreateFleck(dataStatic);
			}
		}
		base.Tick();
	}
}
