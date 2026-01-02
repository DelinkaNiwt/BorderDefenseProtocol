using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class Projectile_CruiseMissile : Projectile_Explosive
{
	public FleckDef FleckDef = DefDatabase<FleckDef>.GetNamed("CMC_SparkFlash_Blue");

	public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_ProjectileSmoke_LongLasting");

	public int Fleck_MakeFleckTickMax = 1;

	public IntRange Fleck_MakeFleckNum = new IntRange(2, 2);

	public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);

	public FloatRange Fleck_Scale = new FloatRange(1.3f, 1.5f);

	public FloatRange Fleck_Scale2 = new FloatRange(2.1f, 2.3f);

	public FloatRange Fleck_Speed = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);

	public int Fleck_MakeFleckTick;

	private bool flag2 = false;

	private Vector3 Randdd;

	private Vector3 HitLoc;

	private Vector3 position2;

	public Vector3 ExPos;

	private void RandFactor()
	{
		FloatRange floatRange = new FloatRange(-7f, 7f);
		FloatRange floatRange2 = new FloatRange(-7f, 7f);
		Randdd.x = floatRange.RandomInRange;
		Randdd.y = 20f;
		Randdd.z = floatRange2.RandomInRange;
		flag2 = true;
	}

	public Vector3 BPos(float t)
	{
		if (!flag2)
		{
			RandFactor();
		}
		Vector3 vector = origin;
		Vector3 vector2 = origin + new Vector3(0f, 0f, 16f);
		Vector3 vector3 = (destination + origin) / 2f;
		vector3.z += 10f;
		vector2 += Randdd;
		vector3 += Randdd;
		Vector3 vector4 = destination;
		return (1f - t) * (1f - t) * (1f - t) * vector + 3f * t * (1f - t) * (1f - t) * vector2 + 3f * t * t * (1f - t) * vector3 + t * t * t * vector4;
	}

	private void FindNextTarget(Vector3 d)
	{
		IntVec3 center = IntVec3.FromVector3(d);
		IEnumerable<IntVec3> enumerable = GenRadial.RadialCellsAround(center, 7f, useCenter: true);
		foreach (IntVec3 item in enumerable)
		{
			Pawn firstPawn = item.GetFirstPawn(base.Map);
			if (firstPawn != null && (firstPawn.Faction.HostileTo(launcher.Faction) || launcher == null) && !firstPawn.Downed && !firstPawn.Dead)
			{
				intendedTarget = firstPawn;
				return;
			}
		}
		intendedTarget = CellRect.CenteredOn(center, 7).RandomCell;
	}

	protected override void DrawAt(Vector3 position, bool flip = false)
	{
		this.position2 = BPos(base.DistanceCoveredFraction - 0.01f);
		position = BPos(base.DistanceCoveredFraction);
		ExPos = position;
		Quaternion rotation = Quaternion.LookRotation(position - this.position2);
		Vector3 position2 = position;
		position2.y = AltitudeLayer.Projectile.AltitudeFor();
		Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), position2, rotation, DrawMat, 0);
		Comps_PostDraw();
	}

	protected override void Tick()
	{
		if (intendedTarget.Thing != null)
		{
			HitLoc = intendedTarget.Thing.DrawPos;
			if (intendedTarget.Thing is Pawn)
			{
				Pawn pawn = (Pawn)intendedTarget.Thing;
				if ((pawn.Dead || pawn.Downed) && (double)base.DistanceCoveredFraction < 0.6)
				{
					FindNextTarget(HitLoc);
				}
			}
			if (intendedTarget.Thing != null)
			{
				destination = intendedTarget.Thing.DrawPos;
			}
			else
			{
				destination = intendedTarget.CenterVector3;
			}
		}
		Fleck_MakeFleckTick++;
		if (Fleck_MakeFleckTick >= Fleck_MakeFleckTickMax)
		{
			Fleck_MakeFleckTick = 0;
			Map map = base.Map;
			int randomInRange = Fleck_MakeFleckNum.RandomInRange;
			Vector3 vector = BPos(base.DistanceCoveredFraction);
			position2 = BPos(base.DistanceCoveredFraction - 0.01f);
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
				dataStatic.rotation = (vector - position2).AngleFlat();
				dataStatic2.rotation = (vector - position2).AngleFlat();
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

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		base.Impact(hitThing, blockedByShield);
		IntVec3 position = base.Position;
		Map map2 = map;
		double num = def.projectile.explosionRadius;
		DamageDef bomb = DamageDefOf.Bomb;
		Thing instigator = launcher;
		int damageAmount = DamageAmount;
		double num2 = ArmorPenetration;
		ThingDef weapon = equipmentDef;
		ThingDef projectile = def;
		Thing thing = intendedTarget.Thing;
		GasType? postExplosionGasType = null;
		float? num3 = null;
		FloatRange? floatRange = null;
		float radius = (float)num;
		float armorPenetration = (float)num2;
		float? direction = num3;
		FloatRange? affectedAngle = floatRange;
		GenExplosion.DoExplosion(position, map2, radius, bomb, instigator, damageAmount, armorPenetration, null, weapon, projectile, thing, null, 0f, 1, postExplosionGasType, null, 255, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, direction, null, affectedAngle);
		CellRect cellRect = CellRect.CenteredOn(base.Position, 6);
		cellRect.ClipInsideMap(map);
		for (int i = 0; i < 5; i++)
		{
			DomultiExplosion(cellRect.RandomCell, map, 1.9f);
		}
	}

	protected void DomultiExplosion(IntVec3 pos, Map map, float radius)
	{
		GenExplosion.DoExplosion(pos, map, radius, DamageDefOf.Bomb, launcher, 30, ArmorPenetration, null, equipmentDef, def, intendedTarget.Thing);
	}
}
