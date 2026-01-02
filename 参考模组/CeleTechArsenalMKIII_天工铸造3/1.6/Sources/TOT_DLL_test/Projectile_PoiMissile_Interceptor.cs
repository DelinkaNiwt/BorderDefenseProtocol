using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

[StaticConstructorOnStartup]
public class Projectile_PoiMissile_Interceptor : Projectile_Explosive
{
	public Vector3 P0;

	public Vector3 P1;

	public Vector3 P2;

	public Vector3 P3;

	private Vector3 lasttargetpos = default(Vector3);

	private bool targetinit = false;

	public Mote_ScaleAndRotate mote;

	public float Randf1;

	public float Randf2;

	public float Randf3;

	private bool Tryinit = false;

	public Vector3 position1;

	public Vector3 position2;

	public Quaternion rotation;

	public float DCFExport;

	public FloatRange Fleck_Angle = new FloatRange(-180f, 180f);

	public FloatRange Fleck_Speed = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Speed2 = new FloatRange(0.1f, 0.2f);

	public FloatRange Fleck_Rotation = new FloatRange(-180f, 180f);

	public FleckDef FleckDef2 = DefDatabase<FleckDef>.GetNamed("CMC_Fleck_ProjectileSmoke_LongLasting");

	private List<Vector3> recentPositions = new List<Vector3>();

	private const int POSITION_HISTORY_COUNT = 5;

	private int lastSmokeTick = -1;

	private const int SMOKE_INTERVAL_TICKS = 2;

	private Vector3 positionTwoFramesAgo;

	private Vector3 previousPosition;

	private float curveHeight;

	private float wobbleFrequency;

	private float wobbleAmplitude;

	private bool initialized;

	public override Vector3 ExactPosition
	{
		get
		{
			_ = position2;
			if (true)
			{
				return position2;
			}
			return base.ExactPosition;
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		recentPositions = new List<Vector3>();
		for (int i = 0; i < 5; i++)
		{
			recentPositions.Add(DrawPos);
		}
	}

	protected override void Tick()
	{
		if (this.DestroyedOrNull())
		{
			return;
		}
		position1 = InterceptMissilePosition(base.DistanceCoveredFraction);
		position2 = InterceptMissilePosition(base.DistanceCoveredFraction - 0.01f);
		rotation = Quaternion.LookRotation(position1 - position2);
		position1.y = AltitudeLayer.Projectile.AltitudeFor();
		position2.y = AltitudeLayer.Projectile.AltitudeFor();
		DCFExport = base.DistanceCoveredFraction;
		if (intendedTarget != null && !intendedTarget.Thing.DestroyedOrNull())
		{
			if (!targetinit)
			{
				lasttargetpos = intendedTarget.Thing.DrawPos;
				targetinit = true;
			}
			else
			{
				destination = intendedTarget.Thing.DrawPos;
				lasttargetpos = destination;
				if (mote.DestroyedOrNull())
				{
					ThingDef cMC_Mote_MissileLocked = CMC_Def.CMC_Mote_MissileLocked;
					if (launcher != null && launcher.Faction != null)
					{
						cMC_Mote_MissileLocked.graphicData.color = launcher.Faction.Color;
					}
					Vector3 vector = new Vector3(0f, 0f, 0f);
					vector.y = AltitudeLayer.PawnRope.AltitudeFor();
					Vector3 vector2 = vector;
					mote = (Mote_ScaleAndRotate)ThingMaker.MakeThing(cMC_Mote_MissileLocked);
					mote.Attach(intendedTarget.Thing, vector2);
					mote.Scale = def.graphicData.drawSize.x * 2f;
					mote.iniscale = def.graphicData.drawSize.x * 2f;
					mote.exactPosition = intendedTarget.Thing.DrawPos + vector2;
					mote.solidTimeOverride = 9999f;
					mote.tickimpact = ticksToImpact + base.TickSpawned;
					mote.tickspawned = base.TickSpawned;
					GenSpawn.Spawn(mote, intendedTarget.Thing.Position, base.Map);
				}
				else
				{
					mote.MaintainMote();
				}
			}
		}
		base.Tick();
	}

	public Vector3 InterceptMissilePosition(float t)
	{
		t = Mathf.Clamp01(t);
		if (!initialized)
		{
			curveHeight = Rand.Range(15f, 30f);
			wobbleFrequency = Rand.Range(2f, 5f);
			wobbleAmplitude = Rand.Range(0.5f, 1.5f);
			initialized = true;
		}
		Vector3 vector = origin;
		Vector3 vector2 = destination;
		Vector3 normalized = (vector2 - vector).normalized;
		float num = Vector3.Distance(vector, vector2);
		Vector3 p = vector + normalized * (num * 0.33f) + Vector3.up * curveHeight * 0.5f;
		Vector3 p2 = vector + normalized * (num * 0.66f) + Vector3.up * curveHeight;
		Vector3 vector3 = CalculateCubicBezier(vector, p, p2, vector2, t);
		float num2 = Mathf.Sin(t * (float)Math.PI * wobbleFrequency) * wobbleAmplitude;
		Vector3 normalized2 = Vector3.Cross(normalized, Vector3.up).normalized;
		return vector3 + normalized2 * num2;
	}

	private Vector3 CalculateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		float num = 1f - t;
		float num2 = t * t;
		float num3 = num * num;
		float num4 = num3 * num;
		float num5 = num2 * t;
		Vector3 vector = num4 * p0;
		vector += 3f * num3 * t * p1;
		vector += 3f * num * num2 * p2;
		return vector + num5 * p3;
	}

	protected override void DrawAt(Vector3 position, bool flip = false)
	{
		Vector3 vector = InterceptMissilePosition(base.DistanceCoveredFraction - 0.01f);
		position = InterceptMissilePosition(base.DistanceCoveredFraction);
		Quaternion quaternion = Quaternion.LookRotation(position - vector);
		Vector3 position2 = position;
		position2.y = AltitudeLayer.Projectile.AltitudeFor();
		Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), position2, quaternion, DrawMat, 0);
		Comps_PostDraw();
	}

	protected override void TickInterval(int delta)
	{
		base.TickInterval(delta);
		if (this.DestroyedOrNull())
		{
			return;
		}
		UpdatePositionHistory(DrawPos);
		if (Find.TickManager.TicksGame - lastSmokeTick >= 2)
		{
			lastSmokeTick = Find.TickManager.TicksGame;
			int num = GenerateSmokeFleckCount(base.DistanceCoveredFraction, DrawPos, previousPosition);
			for (int i = 0; i < num; i++)
			{
				Vector3 smokePosition = GetSmokePosition((float)i / (float)num);
				smokePosition.y = AltitudeLayer.Projectile.AltitudeFor();
				smokePosition += new Vector3(Rand.Range(-0.1f, 0.1f), 0f, Rand.Range(-0.1f, 0.1f));
				float num2 = (smokePosition - intendedTarget.CenterVector3).AngleFlat();
				float velocityAngle = Fleck_Angle.RandomInRange + num2;
				float scale = def.graphicData.drawSize.x / 1.92f;
				float randomInRange = Fleck_Speed2.RandomInRange;
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(smokePosition, base.Map, FleckDef2, scale);
				dataStatic.rotationRate = Fleck_Rotation.RandomInRange;
				dataStatic.velocityAngle = velocityAngle;
				dataStatic.velocitySpeed = randomInRange;
				dataStatic.rotation = Rand.Range(0, 360);
				float a = Mathf.Lerp(0.8f, 0.2f, base.DistanceCoveredFraction);
				dataStatic.instanceColor = new Color(1f, 1f, 1f, a);
				base.Map.flecks.CreateFleck(dataStatic);
			}
		}
	}

	private void UpdatePositionHistory(Vector3 newPosition)
	{
		recentPositions.Insert(0, newPosition);
		if (recentPositions.Count > 5)
		{
			recentPositions.RemoveAt(recentPositions.Count - 1);
		}
	}

	private Vector3 GetSmokePosition(float t)
	{
		if (recentPositions.Count < 2)
		{
			return DrawPos;
		}
		int value = Mathf.FloorToInt(t * (float)(recentPositions.Count - 1));
		value = Mathf.Clamp(value, 0, recentPositions.Count - 4);
		Vector3 vector = recentPositions[value];
		Vector3 vector2 = recentPositions[value + 1];
		Vector3 vector3 = recentPositions[value + 2];
		Vector3 vector4 = recentPositions[value + 3];
		float num = t * (float)(recentPositions.Count - 1) - (float)value;
		return 0.5f * (2f * vector2 + (-vector + vector3) * num + (2f * vector - 5f * vector2 + 4f * vector3 - vector4) * num * num + (-vector + 3f * vector2 - 3f * vector3 + vector4) * num * num * num);
	}

	public int GenerateSmokeFleckCount(float t, Vector3 currentPosition, Vector3 previousPosition)
	{
		float num = Mathf.Lerp(10f, 1f, t * t);
		float num2 = (currentPosition - previousPosition).magnitude / Time.deltaTime;
		float num3 = Mathf.Clamp(num2 / 50f, 0.5f, 2f);
		float num4 = Mathf.Lerp(1f, 0.3f, currentPosition.y / 100f);
		Vector3 normalized = (currentPosition - previousPosition).normalized;
		Vector3 normalized2 = (previousPosition - GetPositionTwoFramesAgo()).normalized;
		float num5 = Vector3.Angle(normalized, normalized2);
		float num6 = 1f + Mathf.Clamp01(num5 / 30f) * 0.5f;
		int b = Mathf.RoundToInt(num * num3 * num4 * num6);
		return Mathf.Max(1, b);
	}

	private Vector3 GetPositionTwoFramesAgo()
	{
		return positionTwoFramesAgo;
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		FleckMaker.ThrowFireGlow(base.Position.ToVector3Shifted(), base.Map, 3.5f);
		FleckMaker.ThrowSmoke(base.Position.ToVector3Shifted(), base.Map, 5.5f);
		FleckMaker.ThrowHeatGlow(base.Position, base.Map, 3.5f);
		TryIntercept();
		Destroy();
	}

	public void TryIntercept()
	{
		IntVec3 center = IntVec3.FromVector3(DrawPos);
		IEnumerable<IntVec3> enumerable = GenRadial.RadialCellsAround(center, 3f, useCenter: true);
		foreach (IntVec3 item in enumerable)
		{
			if (!item.InBounds(base.Map))
			{
				continue;
			}
			foreach (ThingDef projectileDef in DefExtensions.ProjectileDefs)
			{
				Thing firstThing = item.GetFirstThing(base.Map, projectileDef);
				if (firstThing != null && Rand.Chance(0.45f) && firstThing != this)
				{
					FleckMaker.ThrowSmoke(firstThing.DrawPos, base.Map, 3.5f);
					CMC_Def.CMC_Bomb.Spawn(firstThing.DrawPos.ToIntVec3(), base.Map, Rand.Range(1.7f, 3.1f));
					firstThing.Destroy();
				}
			}
		}
	}
}
