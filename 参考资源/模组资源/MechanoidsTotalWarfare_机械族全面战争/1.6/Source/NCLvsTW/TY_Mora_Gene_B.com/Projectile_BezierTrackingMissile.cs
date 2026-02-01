using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TY_Mora_Gene_B.com;

[StaticConstructorOnStartup]
public class Projectile_BezierTrackingMissile : Projectile_Explosive
{
	public enum MissileTrajectoryType
	{
		BezierCurve,
		Parabolic,
		Sinusoidal,
		Spiral,
		Linear,
		SmoothStep,
		Lemniscate,
		Random
	}

	private static readonly Material tailMaterial = MaterialPool.MatFrom("Things/Mote/Smoke", ShaderDatabase.MoteGlow, new Color(1f, 0.7f, 0.2f));

	private static readonly Material shadowMaterial = MaterialPool.MatFrom("Things/Mote/Smoke", ShaderDatabase.Transparent);

	private float maxTrackingRadius = 30f;

	private float maxTurnAngle = 45f;

	private float turnRatePerTick = 1.2f;

	private bool canSwitchTargets = true;

	private float targetSwitchChance = 0.1f;

	private float minTargetSwitchDistance = 5f;

	private float agilePhaseStart = 0.3f;

	private float agilePhaseEnd = 0.8f;

	private float heightMultiplier = 1f;

	private MissileTrajectoryType trajectoryType = MissileTrajectoryType.BezierCurve;

	private float searchRadius = 30f;

	private float trajectoryAmplitude = 1f;

	private float trajectoryFrequency = 1f;

	private float spiralRadius = 3f;

	private float spiralTightness = 0.5f;

	private float tailWidth = 1.2f;

	private float tailLength = 3f;

	private float smokeChance = 0.7f;

	private int flecksPerBurst = 3;

	private int fleckInterval = 2;

	private Vector3 p0;

	private Vector3 p1;

	private Vector3 p2;

	private Vector3 p3;

	private float randOffset1;

	private float randOffset2;

	private float heightOffset;

	private bool curveInitialized = false;

	private Vector3 lastTargetPos = Vector3.zero;

	private bool targetAcquired = false;

	private int losTargetCountdown = 0;

	private float currentTurnAngle = 0f;

	private Vector2 tailDrawSize;

	private int fleckEmitTick = 0;

	private IntRange fleckCountRange;

	private FloatRange fleckAngleRange = new FloatRange(-180f, 180f);

	private FloatRange fleckSpeedRange = new FloatRange(0.05f, 0.15f);

	private FloatRange fleckRotationRange = new FloatRange(-30f, 30f);

	private Vector3 previousPosition;

	private bool reachedApex = false;

	private float lastTurnDirection = 0f;

	private float currentVelocity = 0f;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		LoadConfigFromComp();
		randOffset1 = Rand.Range(-0.15f, 0.15f);
		randOffset2 = Rand.Range(-0.1f, 0.1f);
		heightOffset = Rand.Range(25f, 40f) * heightMultiplier;
		tailDrawSize = new Vector2(tailWidth, tailLength);
		fleckCountRange = new IntRange(Mathf.Max(1, flecksPerBurst - 1), flecksPerBurst + 1);
		fleckAngleRange = new FloatRange(-180f, 180f);
		fleckSpeedRange = new FloatRange(0.05f, 0.15f);
		fleckRotationRange = new FloatRange(-30f, 30f);
		previousPosition = ExactPosition;
		currentVelocity = def.projectile.speed;
	}

	private void LoadConfigFromComp()
	{
		CompBezierMissile comp = this.TryGetComp<CompBezierMissile>();
		if (comp != null)
		{
			maxTrackingRadius = comp.Props.maxTrackingRadius;
			maxTurnAngle = comp.Props.maxTurnAngle;
			turnRatePerTick = comp.Props.turnRatePerTick;
			canSwitchTargets = comp.Props.canSwitchTargets;
			targetSwitchChance = comp.Props.targetSwitchChance;
			minTargetSwitchDistance = comp.Props.minTargetSwitchDistance;
			agilePhaseStart = comp.Props.agilePhaseStart;
			agilePhaseEnd = comp.Props.agilePhaseEnd;
			heightMultiplier = comp.Props.heightMultiplier;
			trajectoryType = comp.Props.trajectoryType;
			searchRadius = comp.Props.searchRadius;
			trajectoryAmplitude = comp.Props.trajectoryAmplitude;
			trajectoryFrequency = comp.Props.trajectoryFrequency;
			spiralRadius = comp.Props.spiralRadius;
			spiralTightness = comp.Props.spiralTightness;
			tailWidth = comp.Props.tailWidth;
			tailLength = comp.Props.tailLength;
			smokeChance = comp.Props.smokeChance;
			flecksPerBurst = comp.Props.flecksPerBurst;
			fleckInterval = comp.Props.fleckInterval;
		}
		if (trajectoryType == MissileTrajectoryType.Random)
		{
			trajectoryType = (MissileTrajectoryType)Rand.Range(0, Enum.GetValues(typeof(MissileTrajectoryType)).Length - 1);
		}
	}

	private Vector3 CalculateTrajectoryPoint(float t)
	{
		t = Mathf.Clamp01(t);
		if (!curveInitialized)
		{
			InitializeBezierCurve();
		}
		Vector3 result;
		try
		{
			result = trajectoryType switch
			{
				MissileTrajectoryType.BezierCurve => CalculateBezierPoint(t), 
				MissileTrajectoryType.Parabolic => CalculateParabolicPoint(t), 
				MissileTrajectoryType.Sinusoidal => CalculateSinusoidalPoint(t), 
				MissileTrajectoryType.Spiral => CalculateSpiralPoint(t), 
				MissileTrajectoryType.Linear => CalculateLinearPoint(t), 
				MissileTrajectoryType.SmoothStep => CalculateSmoothStepPoint(t), 
				MissileTrajectoryType.Lemniscate => CalculateLemniscatePoint(t), 
				_ => CalculateBezierPoint(t), 
			};
		}
		catch (Exception ex)
		{
			if (Prefs.DevMode)
			{
				Log.Error("轨迹计算出错: " + ex.Message + ", 使用简单线性轨迹");
			}
			result = Vector3.Lerp(origin, destination, t);
		}
		if (float.IsNaN(result.x) || float.IsNaN(result.y) || float.IsNaN(result.z) || float.IsInfinity(result.x) || float.IsInfinity(result.y) || float.IsInfinity(result.z))
		{
			result = Vector3.Lerp(origin, destination, t);
		}
		return result;
	}

	private Vector3 CalculateBezierPoint(float t)
	{
		float oneMinusT = 1f - t;
		float oneMinusTSq = oneMinusT * oneMinusT;
		float oneMinusTCube = oneMinusTSq * oneMinusT;
		float tSq = t * t;
		float tCube = tSq * t;
		return oneMinusTCube * p0 + 3f * oneMinusTSq * t * p1 + 3f * oneMinusT * tSq * p2 + tCube * p3;
	}

	private Vector3 CalculateParabolicPoint(float t)
	{
		Vector3 start = origin;
		Vector3 end = destination;
		Vector3 direct = end - start;
		float height = Vector3.Distance(start, end) * heightMultiplier * 0.5f;
		float parabolicHeight = 4f * height * t * (1f - t);
		Vector3 position = Vector3.Lerp(start, end, t);
		position.y += parabolicHeight;
		return position;
	}

	private Vector3 CalculateSinusoidalPoint(float t)
	{
		Vector3 start = origin;
		Vector3 end = destination;
		Vector3 direct = (end - start).normalized;
		float distance = Vector3.Distance(start, end);
		Vector3 position = Vector3.Lerp(start, end, t);
		Vector3 orthogonal = Vector3.Cross(direct, Vector3.up).normalized;
		float sinValue = Mathf.Sin(t * trajectoryFrequency * (float)Math.PI * 2f) * trajectoryAmplitude;
		position += orthogonal * sinValue;
		float parabolicHeight = 4f * (distance * heightMultiplier * 0.25f) * t * (1f - t);
		position.y += parabolicHeight;
		return position;
	}

	private Vector3 CalculateSpiralPoint(float t)
	{
		Vector3 start = origin;
		Vector3 end = destination;
		Vector3 direct = (end - start).normalized;
		float distance = Vector3.Distance(start, end);
		Vector3 position = Vector3.Lerp(start, end, t);
		Vector3 orthogonal1 = Vector3.Cross(direct, Vector3.up).normalized;
		Vector3 orthogonal2 = Vector3.Cross(direct, orthogonal1).normalized;
		float angle = t * spiralTightness * (float)Math.PI * 2f * 3f;
		float radius = spiralRadius * (1f - t);
		position += orthogonal1 * Mathf.Cos(angle) * radius;
		position += orthogonal2 * Mathf.Sin(angle) * radius;
		float parabolicHeight = 4f * (distance * heightMultiplier * 0.25f) * t * (1f - t);
		position.y += parabolicHeight;
		return position;
	}

	private Vector3 CalculateLinearPoint(float t)
	{
		return Vector3.Lerp(origin, destination, t);
	}

	private Vector3 CalculateSmoothStepPoint(float t)
	{
		float smoothT = Mathf.SmoothStep(0f, 1f, t);
		Vector3 position = Vector3.Lerp(origin, destination, smoothT);
		float distance = Vector3.Distance(origin, destination);
		float parabolicHeight = 4f * (distance * heightMultiplier * 0.25f) * t * (1f - t);
		position.y += parabolicHeight;
		return position;
	}

	private Vector3 CalculateLemniscatePoint(float t)
	{
		Vector3 start = origin;
		Vector3 end = destination;
		Vector3 direct = (end - start).normalized;
		float distance = Vector3.Distance(start, end);
		Vector3 position = Vector3.Lerp(start, end, t);
		Vector3 orthogonal1 = Vector3.Cross(direct, Vector3.up).normalized;
		Vector3 orthogonal2 = Vector3.Cross(direct, orthogonal1).normalized;
		float angle = t * trajectoryFrequency * (float)Math.PI * 4f;
		float a = trajectoryAmplitude * (1f - t * 0.8f);
		float denom = 1f + Mathf.Pow(Mathf.Sin(angle), 2f);
		float x = a * Mathf.Cos(angle) / denom;
		float y = a * Mathf.Sin(angle) * Mathf.Cos(angle) / denom;
		position += orthogonal1 * x;
		position += orthogonal2 * y;
		float parabolicHeight = 4f * (distance * heightMultiplier * 0.25f) * t * (1f - t);
		position.y += parabolicHeight;
		return position;
	}

	private Vector3 BezierPoint(float t)
	{
		return CalculateTrajectoryPoint(t);
	}

	private void InitializeBezierCurve()
	{
		p0 = origin;
		p3 = destination;
		p1 = origin + (destination - origin) * (0.3f + randOffset1);
		p2 = origin + (destination - origin) * (0.7f + randOffset2) + new Vector3(0f, heightOffset, 0f);
		curveInitialized = true;
	}

	private void RecalculateCurve()
	{
		if (!targetAcquired || intendedTarget.Thing == null || intendedTarget.Thing.Destroyed)
		{
			return;
		}
		Vector3 currentPos = ExactPosition;
		Vector3 targetPos = intendedTarget.Thing.DrawPos;
		float currentFraction = base.DistanceCoveredFraction;
		Vector3 predictedTargetPos = targetPos;
		if (intendedTarget.Thing is Pawn { Dead: false, Downed: false, pather: not null } pawn && pawn.pather.Moving)
		{
			Vector3 targetVelocity = targetPos - lastTargetPos;
			predictedTargetPos = targetPos + targetVelocity * 3f;
		}
		destination = predictedTargetPos;
		p3 = predictedTargetPos;
		lastTargetPos = targetPos;
		float flightPhase = base.DistanceCoveredFraction;
		Vector3 currentDirection = ((flightPhase > 0.01f) ? (ExactPosition - previousPosition).normalized : (destination - origin).normalized);
		Vector3 directionToTarget = (predictedTargetPos - currentPos).normalized;
		float angleDiff = Vector3.Angle(currentDirection, directionToTarget);
		float agileMultiplier = 1f;
		if (flightPhase > agilePhaseStart && flightPhase < agilePhaseEnd)
		{
			agileMultiplier = 1.5f;
		}
		else if (flightPhase >= agilePhaseEnd)
		{
			agileMultiplier = 2f;
		}
		float maxAllowedTurn = Mathf.Min(maxTurnAngle, turnRatePerTick * agileMultiplier);
		float turnAngle = Mathf.Min(angleDiff, maxAllowedTurn);
		float turnDirection = Mathf.Sign(Vector3.Cross(currentDirection, directionToTarget).y);
		if (lastTurnDirection != 0f && Mathf.Sign(lastTurnDirection) != Mathf.Sign(turnDirection) && angleDiff > 10f)
		{
			turnAngle *= 0.5f;
		}
		lastTurnDirection = turnDirection;
		Quaternion rotation = Quaternion.AngleAxis(turnAngle * turnDirection, Vector3.up);
		Vector3 adjustedDirection = rotation * currentDirection;
		float remainingDistance = Vector3.Distance(currentPos, predictedTargetPos);
		if (trajectoryType == MissileTrajectoryType.BezierCurve)
		{
			if (flightPhase > 0.5f || reachedApex)
			{
				reachedApex = true;
				p1 = currentPos + adjustedDirection * (remainingDistance * 0.3f);
				p2 = currentPos + adjustedDirection * (remainingDistance * 0.6f) + new Vector3(0f, Mathf.Max(2f, heightOffset * (1f - currentFraction) * 0.5f), 0f);
			}
			else
			{
				Vector3 flightDirection = (predictedTargetPos - origin).normalized;
				float distance = Vector3.Distance(origin, predictedTargetPos);
				p1 = origin + flightDirection * (distance * (0.3f + randOffset1));
				p2 = origin + flightDirection * (distance * (0.7f + randOffset2)) + new Vector3(0f, heightOffset, 0f);
			}
		}
		else if (flightPhase > 0.7f)
		{
			trajectoryType = MissileTrajectoryType.Linear;
			p0 = currentPos;
			p3 = predictedTargetPos;
			p1 = Vector3.Lerp(p0, p3, 0.33f);
			p2 = Vector3.Lerp(p0, p3, 0.66f);
		}
	}

	private void FindNewTarget()
	{
		if (!canSwitchTargets || !(base.DistanceCoveredFraction > agilePhaseStart) || !(base.DistanceCoveredFraction < agilePhaseEnd) || !Rand.Chance(targetSwitchChance))
		{
			return;
		}
		IntVec3 searchCenter = IntVec3.FromVector3(ExactPosition);
		IEnumerable<Pawn> potentialTargets = from p in GenRadial.RadialCellsAround(searchCenter, searchRadius, useCenter: true)
			where p.InBounds(base.Map)
			let pawn = p.GetFirstPawn(base.Map)
			where pawn != null && !pawn.Dead && !pawn.Downed && pawn.Faction != null && (launcher == null || pawn.Faction.HostileTo(launcher.Faction)) && Vector3.Distance(ExactPosition, pawn.DrawPos) >= minTargetSwitchDistance
			orderby Vector3.Distance(ExactPosition, pawn.DrawPos)
			select pawn;
		Pawn newTarget = potentialTargets.FirstOrDefault();
		if (newTarget != null)
		{
			intendedTarget = newTarget;
			targetAcquired = true;
			lastTargetPos = newTarget.DrawPos;
			if (Prefs.DevMode)
			{
				Log.Message($"导弹锁定新目标: {newTarget.Label}, 距离: {Vector3.Distance(ExactPosition, newTarget.DrawPos):F1}");
			}
		}
	}

	private void CheckTargetStatus()
	{
		if (intendedTarget.Thing != null && !intendedTarget.Thing.Destroyed)
		{
			if (!targetAcquired)
			{
				targetAcquired = true;
				lastTargetPos = intendedTarget.Thing.DrawPos;
			}
			float targetDistance = Vector3.Distance(ExactPosition, intendedTarget.Thing.DrawPos);
			if (targetDistance > searchRadius)
			{
				losTargetCountdown++;
				if (losTargetCountdown > 5)
				{
					if (Prefs.DevMode)
					{
						Log.Message($"导弹丢失目标 - 目标超出跟踪范围 ({targetDistance:F1} > {searchRadius:F1})");
					}
					targetAcquired = false;
					losTargetCountdown = 0;
				}
			}
			else
			{
				losTargetCountdown = 0;
				RecalculateCurve();
			}
		}
		else if (targetAcquired)
		{
			targetAcquired = false;
			FindNewTarget();
		}
	}

	protected override void Tick()
	{
		try
		{
			if (base.Destroyed || base.Map == null)
			{
				return;
			}
			Vector3 currentExactPosition = ExactPosition;
			IntVec3 currentPosition = base.Position;
			if (base.DistanceCoveredFraction < 0.95f)
			{
				CheckTargetStatus();
				if (!targetAcquired)
				{
					FindNewTarget();
				}
			}
			if (!curveInitialized)
			{
				InitializeBezierCurve();
			}
			previousPosition = ExactPosition;
			try
			{
				Vector3 newPosition = BezierPoint(base.DistanceCoveredFraction + 0.01f);
				if (!newPosition.InBounds(base.Map) || float.IsNaN(newPosition.x) || float.IsNaN(newPosition.y) || float.IsNaN(newPosition.z))
				{
					Impact(null);
					return;
				}
				base.Tick();
			}
			catch (Exception ex)
			{
				if (Prefs.DevMode)
				{
					Log.Warning("导弹基类Tick失败: " + ex.Message + "，使用备用更新逻辑");
				}
				Vector3 newExactPos = BezierPoint(base.DistanceCoveredFraction + 0.01f);
				IntVec3 newCell = newExactPos.ToIntVec3();
				if (!newCell.InBounds(base.Map))
				{
					Impact(null);
					return;
				}
				base.Position = newCell;
				destination = origin;
				origin = newExactPos;
				ticksToImpact = 1;
			}
			if (!base.Destroyed && base.Map != null)
			{
				EmitFlecks();
			}
			if (!reachedApex && base.DistanceCoveredFraction > 0.5f)
			{
				reachedApex = true;
			}
			if (targetAcquired && intendedTarget.Thing != null && !intendedTarget.Thing.Destroyed)
			{
				float distToTarget = Vector3.Distance(ExactPosition, intendedTarget.Thing.DrawPos);
				float hitRadius = 1.5f;
				if (base.DistanceCoveredFraction > 0.9f)
				{
					hitRadius = 2.5f;
				}
				if (distToTarget < hitRadius)
				{
					try
					{
						if (intendedTarget.Cell.InBounds(base.Map))
						{
							base.Position = intendedTarget.Cell;
							ImpactSomething();
						}
						else
						{
							Impact(null);
						}
					}
					catch (Exception ex2)
					{
						if (Prefs.DevMode)
						{
							Log.Error("导弹命中目标时出错: " + ex2.Message);
						}
						try
						{
							Impact(null);
						}
						catch
						{
							Destroy();
						}
					}
				}
			}
			if (!base.Destroyed && !base.Position.InBounds(base.Map))
			{
				Impact(null);
			}
		}
		catch (Exception ex3)
		{
			if (Prefs.DevMode)
			{
				Log.Error("导弹Tick时发生未处理的异常: " + ex3.Message + "\n" + ex3.StackTrace);
			}
			try
			{
				if (!base.Destroyed && base.Map != null)
				{
					Impact(null);
				}
				else
				{
					Destroy();
				}
			}
			catch
			{
				Destroy();
			}
		}
	}

	private void EmitFlecks()
	{
		if (base.Map == null || base.Destroyed)
		{
			return;
		}
		fleckEmitTick++;
		if (fleckEmitTick < fleckInterval)
		{
			return;
		}
		fleckEmitTick = 0;
		Vector3 position = BezierPoint(base.DistanceCoveredFraction);
		position.y = def.Altitude;
		_ = fleckCountRange;
		if (false)
		{
			fleckCountRange = new IntRange(Mathf.Max(1, flecksPerBurst - 1), flecksPerBurst + 1);
		}
		int fleckCount = fleckCountRange.RandomInRange;
		for (int i = 0; i < fleckCount; i++)
		{
			FleckDef fleckDef = ((base.DistanceCoveredFraction < 0.7f) ? FleckDefOf.FireGlow : FleckDefOf.MicroSparks);
			if (fleckDef == null)
			{
				continue;
			}
			_ = fleckAngleRange;
			if (false)
			{
				fleckAngleRange = new FloatRange(-180f, 180f);
			}
			_ = fleckSpeedRange;
			if (false)
			{
				fleckSpeedRange = new FloatRange(0.05f, 0.15f);
			}
			_ = fleckRotationRange;
			if (false)
			{
				fleckRotationRange = new FloatRange(-30f, 30f);
			}
			float angle = (position - destination).AngleFlat() + fleckAngleRange.RandomInRange;
			float speed = fleckSpeedRange.RandomInRange;
			float rotation = fleckRotationRange.RandomInRange;
			if (def?.graphicData == null)
			{
				continue;
			}
			float scale = def.graphicData.drawSize.x * 0.4f;
			try
			{
				FleckCreationData data = FleckMaker.GetDataStatic(position, base.Map, fleckDef, scale);
				data.velocityAngle = angle;
				data.velocitySpeed = speed;
				data.rotationRate = rotation;
				base.Map.flecks.CreateFleck(data);
				if (Rand.Chance(smokeChance))
				{
					FleckMaker.ThrowSmoke(position, base.Map, scale * 1.2f);
				}
			}
			catch (Exception ex)
			{
				if (Prefs.DevMode)
				{
					Log.Error("导弹粒子效果生成错误: " + ex.Message);
				}
			}
		}
	}

	protected override void DrawAt(Vector3 drawPos, bool flip = false)
	{
		Vector3 actualPos = BezierPoint(base.DistanceCoveredFraction);
		Vector3 prevPos = ((base.DistanceCoveredFraction > 0.01f) ? BezierPoint(base.DistanceCoveredFraction - 0.01f) : previousPosition);
		Quaternion rotation = Quaternion.LookRotation(actualPos - prevPos);
		Vector3 shadowPos = new Vector3(actualPos.x, 0f, actualPos.z);
		float shadowSize = def.graphicData.drawSize.x * 0.7f;
		Graphics.DrawMesh(MeshPool.GridPlane(new Vector2(shadowSize, shadowSize)), shadowPos, Quaternion.identity, shadowMaterial, 0);
		actualPos.y = def.Altitude;
		if (base.DistanceCoveredFraction > 0.1f && base.DistanceCoveredFraction < 0.95f)
		{
			Graphics.DrawMesh(MeshPool.GridPlane(tailDrawSize), actualPos, rotation, tailMaterial, 0);
		}
		Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), actualPos, rotation, DrawMat, 0);
		Comps_PostDraw();
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		try
		{
			Map map = base.Map;
			IntVec3 position = base.Position;
			if (map == null)
			{
				Destroy();
				return;
			}
			base.Impact(hitThing, blockedByShield);
			if (map != null && map.regionAndRoomUpdater.Enabled)
			{
				FleckMaker.Static(position.ToVector3Shifted(), map, FleckDefOf.ExplosionFlash, 12f);
				for (int i = 0; i < 6; i++)
				{
					Vector3 loc = position.ToVector3Shifted() + new Vector3(Rand.Range(-1f, 1f), 0f, Rand.Range(-1f, 1f));
					FleckMaker.ThrowSmoke(loc, map, Rand.Range(1.5f, 2.5f));
				}
				for (int j = 0; j < 6; j++)
				{
					Vector3 loc2 = position.ToVector3Shifted() + new Vector3(Rand.Range(-1f, 1f), 0f, Rand.Range(-1f, 1f));
					FleckMaker.ThrowMicroSparks(loc2, map);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error("导弹爆炸时出错: " + ex.Message);
			try
			{
				Destroy();
			}
			catch
			{
				if (this != null && !base.Destroyed)
				{
					Destroy();
				}
			}
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref maxTrackingRadius, "maxTrackingRadius", 30f);
		Scribe_Values.Look(ref maxTurnAngle, "maxTurnAngle", 45f);
		Scribe_Values.Look(ref turnRatePerTick, "turnRatePerTick", 1.2f);
		Scribe_Values.Look(ref canSwitchTargets, "canSwitchTargets", defaultValue: true);
		Scribe_Values.Look(ref targetSwitchChance, "targetSwitchChance", 0.1f);
		Scribe_Values.Look(ref minTargetSwitchDistance, "minTargetSwitchDistance", 5f);
		Scribe_Values.Look(ref agilePhaseStart, "agilePhaseStart", 0.3f);
		Scribe_Values.Look(ref agilePhaseEnd, "agilePhaseEnd", 0.8f);
		Scribe_Values.Look(ref heightMultiplier, "heightMultiplier", 1f);
		Scribe_Values.Look(ref tailWidth, "tailWidth", 1.2f);
		Scribe_Values.Look(ref tailLength, "tailLength", 3f);
		Scribe_Values.Look(ref smokeChance, "smokeChance", 0.7f);
		Scribe_Values.Look(ref flecksPerBurst, "flecksPerBurst", 3);
		Scribe_Values.Look(ref fleckInterval, "fleckInterval", 2);
		Scribe_Values.Look(ref trajectoryType, "trajectoryType", MissileTrajectoryType.BezierCurve);
		Scribe_Values.Look(ref searchRadius, "searchRadius", 30f);
		Scribe_Values.Look(ref trajectoryAmplitude, "trajectoryAmplitude", 1f);
		Scribe_Values.Look(ref trajectoryFrequency, "trajectoryFrequency", 1f);
		Scribe_Values.Look(ref spiralRadius, "spiralRadius", 3f);
		Scribe_Values.Look(ref spiralTightness, "spiralTightness", 0.5f);
		Scribe_Values.Look(ref curveInitialized, "curveInitialized", defaultValue: false);
		Scribe_Values.Look(ref targetAcquired, "targetAcquired", defaultValue: false);
		Scribe_Values.Look(ref losTargetCountdown, "losTargetCountdown", 0);
		Scribe_Values.Look(ref reachedApex, "reachedApex", defaultValue: false);
		Scribe_Values.Look(ref lastTurnDirection, "lastTurnDirection", 0f);
		if (Scribe.mode == LoadSaveMode.Saving && curveInitialized)
		{
			Vector3 p0Value = p0;
			Vector3 p1Value = p1;
			Vector3 p2Value = p2;
			Vector3 p3Value = p3;
			Scribe_Values.Look(ref p0Value, "p0");
			Scribe_Values.Look(ref p1Value, "p1");
			Scribe_Values.Look(ref p2Value, "p2");
			Scribe_Values.Look(ref p3Value, "p3");
		}
		if (Scribe.mode == LoadSaveMode.LoadingVars)
		{
			Vector3 p0Value2 = Vector3.zero;
			Vector3 p1Value2 = Vector3.zero;
			Vector3 p2Value2 = Vector3.zero;
			Vector3 p3Value2 = Vector3.zero;
			Scribe_Values.Look(ref p0Value2, "p0");
			Scribe_Values.Look(ref p1Value2, "p1");
			Scribe_Values.Look(ref p2Value2, "p2");
			Scribe_Values.Look(ref p3Value2, "p3");
			if (curveInitialized)
			{
				p0 = p0Value2;
				p1 = p1Value2;
				p2 = p2Value2;
				p3 = p3Value2;
			}
		}
	}
}
