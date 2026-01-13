using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace TurbojetBackpack;

public class Projectile_ActiveInterceptor : Projectile
{
	private Projectile targetProjectile;

	private Vector3 myExactPosition;

	private bool initialized = false;

	private int ticksFlying = 0;

	private Vector3 originPos;

	private Vector3 controlPoint1;

	private Vector3 controlPoint2;

	private float estimatedTotalTicks = 60f;

	private Vector3 targetLastPos;

	private Vector3 targetFlightDir;

	private static FleckDef _sparkFleckDef;

	private Quaternion bezierRotation = Quaternion.identity;

	private BarrageExtension Ext => def.GetModExtension<BarrageExtension>();

	private static FleckDef SparkFleckDef
	{
		get
		{
			object obj = _sparkFleckDef;
			if (obj == null)
			{
				obj = DefDatabase<FleckDef>.GetNamedSilentFail("Fleck_ADSInterceptSpark") ?? FleckDefOf.ShotFlash;
				_sparkFleckDef = (FleckDef)obj;
			}
			return (FleckDef)obj;
		}
	}

	public override Vector3 ExactPosition => myExactPosition;

	public override Vector3 DrawPos => myExactPosition;

	public override Quaternion ExactRotation
	{
		get
		{
			if (!initialized)
			{
				return base.ExactRotation;
			}
			return bezierRotation;
		}
	}

	public void SetInterceptTarget(Projectile target)
	{
		targetProjectile = target;
		if (target != null)
		{
			targetLastPos = target.DrawPos;
			targetFlightDir = (target.Position - base.Launcher.Position).ToVector3().normalized;
		}
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		myExactPosition = base.Position.ToVector3Shifted();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref targetProjectile, "targetProjectile");
		Scribe_Values.Look(ref myExactPosition, "myExactPosition");
		Scribe_Values.Look(ref initialized, "initialized", defaultValue: false);
		Scribe_Values.Look(ref ticksFlying, "ticksFlying", 0);
		Scribe_Values.Look(ref originPos, "originPos");
		Scribe_Values.Look(ref controlPoint1, "controlPoint1");
		Scribe_Values.Look(ref estimatedTotalTicks, "estimatedTotalTicks", 60f);
		Scribe_Values.Look(ref targetLastPos, "targetLastPos");
		Scribe_Values.Look(ref targetFlightDir, "targetFlightDir");
	}

	private void InitializeBezier()
	{
		originPos = origin;
		float innerRadius = ((Ext != null) ? Ext.curveVarianceMin : 2f);
		float outerRadius = ((Ext != null) ? Ext.curveVarianceMax : 8f);
		controlPoint1 = originPos + Rand.InsideAnnulusVector3(innerRadius, outerRadius);
		if (targetProjectile != null)
		{
			float magnitude = (targetProjectile.DrawPos - originPos).magnitude;
			float num = def.projectile.SpeedTilesPerTick;
			if (num <= 0f)
			{
				num = 1f;
			}
			estimatedTotalTicks = magnitude / num * 1.2f;
		}
		initialized = true;
		if (targetProjectile != null)
		{
			bezierRotation = Quaternion.LookRotation((targetProjectile.DrawPos - originPos).Yto0());
		}
	}

	protected override void Tick()
	{
		if (!initialized)
		{
			InitializeBezier();
		}
		if (targetProjectile == null || targetProjectile.Destroyed)
		{
			Destroy();
			return;
		}
		Vector3 drawPos = targetProjectile.DrawPos;
		if ((drawPos - targetLastPos).sqrMagnitude > 0.0001f)
		{
			targetFlightDir = (drawPos - targetLastPos).normalized;
		}
		targetLastPos = drawPos;
		ticksFlying++;
		float t = Mathf.Clamp01((float)ticksFlying / estimatedTotalTicks);
		controlPoint2 = (originPos + drawPos) * 0.5f + new Vector3(0f, 0f, 2f);
		Vector3 vector = TurbojetBezierUtils.CalculateThreePowerBezierPoint(t, originPos, controlPoint1, controlPoint2, drawPos);
		Vector3 v = TurbojetBezierUtils.CalculateBezierTangent(t, originPos, controlPoint1, controlPoint2, drawPos);
		if (v.sqrMagnitude > 0.001f)
		{
			bezierRotation = Quaternion.LookRotation(v.Yto0());
		}
		float sqrMagnitude = (drawPos - myExactPosition).sqrMagnitude;
		if (sqrMagnitude < 2.25f)
		{
			myExactPosition = drawPos;
		}
		else
		{
			myExactPosition = vector;
		}
		IntVec3 intVec = myExactPosition.ToIntVec3();
		if (intVec != base.Position && intVec.InBounds(base.Map))
		{
			base.Position = intVec;
		}
		DrawComplexTrails(myExactPosition);
		if (sqrMagnitude < 2.25f)
		{
			DoIntercept();
			return;
		}
		ticksToImpact = 200;
		base.Tick();
	}

	private void DrawComplexTrails(Vector3 currentPos)
	{
		if (Ext == null || Ext.tailLayers.NullOrEmpty())
		{
			return;
		}
		Map map = base.Map;
		foreach (TailData tailLayer in Ext.tailLayers)
		{
			if (ticksFlying % Math.Max(1, tailLayer.interval) != 0)
			{
				continue;
			}
			Vector3 vector = currentPos;
			if (tailLayer.offsetZ != 0f)
			{
				vector.z += tailLayer.offsetZ;
			}
			if (tailLayer.fleckDef != null)
			{
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(vector, map, tailLayer.fleckDef, tailLayer.scale);
				if (tailLayer.color.HasValue)
				{
					dataStatic.instanceColor = tailLayer.color.Value;
				}
				map.flecks.CreateFleck(dataStatic);
			}
			else
			{
				if (tailLayer.moteDef == null)
				{
					continue;
				}
				MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(tailLayer.moteDef);
				if (moteThrown != null)
				{
					moteThrown.Scale = tailLayer.scale;
					moteThrown.exactPosition = vector;
					if (tailLayer.color.HasValue)
					{
						moteThrown.instanceColor = tailLayer.color.Value;
					}
					moteThrown.SetVelocity(0f, 0f);
					GenSpawn.Spawn(moteThrown, vector.ToIntVec3(), map);
				}
			}
		}
	}

	private void DoIntercept()
	{
		Map map = base.Map;
		if (map == null)
		{
			return;
		}
		if (targetProjectile == null || targetProjectile.Destroyed)
		{
			Destroy();
			return;
		}
		Vector3 vector = (myExactPosition + targetProjectile.DrawPos) / 2f;
		SoundDef soundDef = def.projectile.soundExplode ?? SoundDefOf.MetalHitImportant;
		soundDef.PlayOneShot(new TargetInfo(vector.ToIntVec3(), map));
		float num = targetFlightDir.AngleFlat();
		int num2 = Rand.Range(20, 35);
		float num3 = 5f;
		for (int i = 0; i < num2; i++)
		{
			float scale = Rand.Range(2.5f, 5.5f);
			FleckCreationData dataStatic = FleckMaker.GetDataStatic(vector, map, SparkFleckDef, scale);
			dataStatic.spawnPosition += Gen.RandomHorizontalVector(0.25f);
			dataStatic.velocityAngle = num + Rand.Range(0f - num3, num3);
			dataStatic.velocitySpeed = Rand.Range(45f, 80f);
			map.flecks.CreateFleck(dataStatic);
		}
		if (!targetProjectile.Destroyed)
		{
			targetProjectile.Destroy();
		}
		Destroy();
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
	}
}
