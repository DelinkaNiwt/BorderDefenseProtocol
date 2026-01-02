using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Projectile_Grenade : Projectile_Explosive
{
	private int ticksToDetonation;

	public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_ProjectileSmoke_LongLastingGrow");

	public int Fleck_MakeFleckTickMax = 1;

	public IntRange Fleck_MakeFleckNum = new IntRange(2, 2);

	public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);

	public FloatRange Fleck_Scale2 = new FloatRange(0.77f, 0.83f);

	public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);

	public int Fleck_MakeFleckTick;

	public Vector3 IPPos(float t)
	{
		t = Mathf.Clamp01(t);
		return origin + (destination - origin).Yto0() * t;
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
				float randomInRange2 = Fleck_Scale2.RandomInRange;
				float randomInRange3 = Fleck_Speed2.RandomInRange;
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(vector, map, FleckDef2, randomInRange2);
				dataStatic.rotation = (vector - vector2).AngleFlat();
				dataStatic.rotationRate = Fleck_Rotation.RandomInRange;
				dataStatic.velocityAngle = velocityAngle;
				dataStatic.velocitySpeed = randomInRange3;
				map.flecks.CreateFleck(dataStatic);
			}
		}
		base.Tick();
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (blockedByShield || def.projectile.explosionDelay == 0)
		{
			Explode();
			return;
		}
		landed = true;
		ticksToDetonation = 60;
		GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, def.projectile.damageDef, launcher.Faction, launcher);
	}
}
