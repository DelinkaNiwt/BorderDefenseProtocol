using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class CruiseFlyer : Skyfaller
{
	protected bool hoverMode = true;

	protected float hoverRadius = 100f;

	protected float hoverAngle = 0f;

	protected float hoverSpeed = 0.005f;

	protected float currentRotation = 0f;

	protected int bombDropInterval = 200;

	protected int bombDropCounter = 0;

	protected ThingDef bombDef = DefDatabase<ThingDef>.GetNamed("Bullet_FlyingCentipede");

	protected float cruiseProgress;

	protected int cruiseDirection = -1;

	protected float cruiseSpeed = 0.001f;

	protected float cruiseAmplitude;

	protected float cruiseAltitude = 10f;

	protected float waveHeightX = 40f;

	protected float waveHeightZ = 40f;

	protected float waveFrequencyX = 2f;

	protected float waveFrequencyZ = 1.5f;

	protected float shadowSizeFactor = 4f;

	public FleckDef fleckToSpawn = DefDatabase<FleckDef>.GetNamed("Smoke");

	public float fleckSpawnInterval = 1f;

	protected int fleckSpawnCounter;

	public float fleckOffset = 0f;

	public bool autoReverse = true;

	public bool waveMotion = true;

	public bool drawShadow = true;

	protected const float WingOffsetDistance = 4f;

	protected const float WingHeightOffset = 0.5f;

	private ThingDef CentipedeBomb => ThingDef.Named("Bullet_FlyingCentipede");

	private ThingDef LancerBomb => ThingDef.Named("Bullet_FlyingLancer");

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		base.Position = map.Center;
		cruiseAmplitude = (float)map.Size.x * 0.8f;
		if (hoverMode)
		{
			hoverRadius = (float)Mathf.Min(map.Size.x, map.Size.z) * 0.4f;
		}
	}

	protected override void GetDrawPositionAndRotation(ref Vector3 drawLoc, out float extraRotation)
	{
		drawLoc = base.Map.Center.ToVector3Shifted();
		if (hoverMode)
		{
			float offsetX = Mathf.Cos(hoverAngle) * hoverRadius;
			float offsetZ = Mathf.Sin(hoverAngle) * hoverRadius;
			drawLoc += new Vector3(offsetX, cruiseAltitude, offsetZ);
			extraRotation = currentRotation;
			return;
		}
		float horizontalOffset = Mathf.Lerp(cruiseAmplitude, 0f - cruiseAmplitude, cruiseProgress);
		float waveOffsetX = 0f;
		float waveOffsetZ = 0f;
		if (waveMotion)
		{
			waveOffsetX = Mathf.Sin(cruiseProgress * (float)Math.PI * waveFrequencyX) * waveHeightX;
			waveOffsetZ = Mathf.Cos(cruiseProgress * (float)Math.PI * waveFrequencyZ) * waveHeightZ;
		}
		drawLoc += new Vector3(horizontalOffset + waveOffsetX, cruiseAltitude, waveOffsetZ);
		extraRotation = ((cruiseDirection > 0) ? 0f : 180f);
	}

	protected override void HitRoof()
	{
	}

	protected override void Impact()
	{
	}

	protected override void LeaveMap()
	{
	}

	protected override void Tick()
	{
		base.Tick();
		if (hoverMode)
		{
			float oldAngle = hoverAngle;
			hoverAngle += hoverSpeed;
			if (hoverAngle > (float)Math.PI * 2f)
			{
				hoverAngle -= (float)Math.PI * 2f;
			}
			float angleDelta = hoverAngle - oldAngle;
			float tangentX = Mathf.Sin(hoverAngle);
			float tangentZ = Mathf.Cos(hoverAngle);
			currentRotation = Mathf.Atan2(tangentZ, tangentX) * 57.29578f;
			float angleDifference = Mathf.DeltaAngle(currentRotation, currentRotation);
			if (Mathf.Abs(angleDifference) > 90f)
			{
				currentRotation = Mathf.LerpAngle(currentRotation, currentRotation, 0.1f);
			}
		}
		else
		{
			cruiseProgress += cruiseSpeed * (float)Mathf.Abs(cruiseDirection);
			if (cruiseProgress >= 1f || cruiseProgress <= 0f)
			{
				if (autoReverse)
				{
					cruiseDirection *= -1;
					cruiseProgress = Mathf.Clamp01(cruiseProgress);
				}
				else
				{
					cruiseProgress = ((cruiseProgress >= 1f) ? 0f : 1f);
				}
			}
		}
		if (bombDef != null && base.Map != null)
		{
			bombDropCounter++;
			if (bombDropCounter >= bombDropInterval)
			{
				bombDropCounter = 0;
				DropBomb();
			}
		}
		if (fleckToSpawn != null && base.Map != null && fleckSpawnInterval > 0f)
		{
			fleckSpawnCounter++;
			if ((float)fleckSpawnCounter >= fleckSpawnInterval)
			{
				fleckSpawnCounter = 0;
				Vector3 drawPos = GetCurrentDrawPosition();
				FleckMaker.Static(drawPos, base.Map, fleckToSpawn);
				GenerateWingSmoke(drawPos);
			}
		}
	}

	private void DropBomb()
	{
		Vector3 bombPos = GetCurrentDrawPosition();
		IntVec3 targetCell = IntVec3.Invalid;
		IntVec3 cellBelow = bombPos.ToIntVec3();
		if (base.Map.roofGrid.Roofed(cellBelow))
		{
			targetCell = FindNearestOpenCell(cellBelow, base.Map);
			if (!targetCell.IsValid)
			{
				return;
			}
		}
		else
		{
			targetCell = cellBelow;
		}
		ThingDef bombDef = ((Rand.Value < 0.5f) ? CentipedeBomb : LancerBomb);
		Projectile bomb = (Projectile)ThingMaker.MakeThing(bombDef);
		bomb.def = bombDef;
		Vector3 spawnPos = bombPos - new Vector3(0f, 1f, 0f);
		IntVec3 spawnCell = spawnPos.ToIntVec3();
		GenSpawn.Spawn(bomb, spawnCell, base.Map);
		bomb.Launch(this, spawnPos, new LocalTargetInfo(targetCell), new LocalTargetInfo(targetCell), ProjectileHitFlags.All);
		FleckMaker.ThrowSmoke(spawnPos, base.Map, 1f);
		FleckMaker.ThrowLightningGlow(spawnPos, base.Map, 1f);
	}

	private IntVec3 FindNearestOpenCell(IntVec3 center, Map map, int maxRadius = 15)
	{
		if (!map.roofGrid.Roofed(center))
		{
			return center;
		}
		for (int radius = 1; radius <= maxRadius; radius++)
		{
			foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
			{
				if (cell.InBounds(map) && !map.roofGrid.Roofed(cell) && cell.Standable(map))
				{
					return cell;
				}
			}
		}
		return IntVec3.Invalid;
	}

	protected virtual void GenerateWingSmoke(Vector3 centerPos)
	{
		if (fleckToSpawn != null)
		{
			Vector3 forwardDirection = GetForwardDirection();
			Vector3 leftDirection = new Vector3(0f - forwardDirection.z, 0f, forwardDirection.x).normalized;
			Vector3 leftWingPos = centerPos + leftDirection * 4f;
			leftWingPos.y += 0.5f;
			Vector3 rightWingPos = centerPos - leftDirection * 4f;
			rightWingPos.y += 0.5f;
			FleckMaker.Static(leftWingPos, base.Map, fleckToSpawn);
			FleckMaker.Static(rightWingPos, base.Map, fleckToSpawn);
		}
	}

	protected virtual Vector3 GetForwardDirection()
	{
		if (hoverMode)
		{
			float forwardX = 0f - Mathf.Sin(hoverAngle);
			float forwardZ = Mathf.Cos(hoverAngle);
			return new Vector3(forwardX, 0f, forwardZ).normalized;
		}
		return new Vector3(cruiseDirection, 0f, 0f).normalized;
	}

	public Vector3 GetCurrentDrawPosition()
	{
		Vector3 drawLoc = base.Map.Center.ToVector3Shifted();
		GetDrawPositionAndRotation(ref drawLoc, out var _);
		return drawLoc;
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		GetDrawPositionAndRotation(ref drawLoc, out var extraRotation);
		Thing thingForGraphic = GetThingForGraphic();
		Graphic.Draw(drawLoc, thingForGraphic.Rotation, thingForGraphic, extraRotation);
		if (drawShadow && base.ShadowMaterial != null)
		{
			DrawCruiseShadow(drawLoc);
		}
	}

	protected Thing GetThingForGraphic()
	{
		if (def.graphicData != null || !innerContainer.Any)
		{
			return this;
		}
		return innerContainer[0];
	}

	protected void DrawCruiseShadow(Vector3 center)
	{
		Material shadowMaterial = base.ShadowMaterial;
		if (!(shadowMaterial == null))
		{
			Vector3 pos = center;
			pos.y = AltitudeLayer.Shadows.AltitudeFor();
			float shadowSize = shadowSizeFactor * (1f - cruiseAltitude / 100f);
			Skyfaller.DrawDropSpotShadow(shadowSize: new Vector2(shadowSize, shadowSize), center: pos, rot: base.Rotation, material: shadowMaterial, ticksToImpact: 0);
		}
	}

	public void SetHoverMode(bool enabled, float radius = 100f, float speed = 0.01f)
	{
		hoverMode = enabled;
		hoverRadius = radius;
		hoverSpeed = speed;
	}

	public static CruiseFlyer CreateHoverFlyer(ThingDef flyerDef, Map map, float radius = 100f, float speed = 0.01f, float altitude = 15f)
	{
		CruiseFlyer flyer = (CruiseFlyer)ThingMaker.MakeThing(flyerDef);
		flyer.SetHoverMode(enabled: true, radius, speed);
		flyer.cruiseAltitude = altitude;
		GenSpawn.Spawn(flyer, map.Center, map);
		return flyer;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref cruiseProgress, "cruiseProgress", 0f);
		Scribe_Values.Look(ref cruiseDirection, "cruiseDirection", -1);
		Scribe_Values.Look(ref cruiseSpeed, "cruiseSpeed", 0.004f);
		Scribe_Values.Look(ref cruiseAmplitude, "cruiseAmplitude", 0f);
		Scribe_Values.Look(ref cruiseAltitude, "cruiseAltitude", 10f);
		Scribe_Values.Look(ref waveHeightX, "waveHeightX", 40f);
		Scribe_Values.Look(ref waveHeightZ, "waveHeightZ", 40f);
		Scribe_Values.Look(ref waveFrequencyX, "waveFrequencyX", 2f);
		Scribe_Values.Look(ref waveFrequencyZ, "waveFrequencyZ", 1.5f);
		Scribe_Values.Look(ref shadowSizeFactor, "shadowSizeFactor", 1f);
		Scribe_Values.Look(ref autoReverse, "autoReverse", defaultValue: true);
		Scribe_Values.Look(ref waveMotion, "waveMotion", defaultValue: false);
		Scribe_Values.Look(ref drawShadow, "drawShadow", defaultValue: true);
		Scribe_Defs.Look(ref fleckToSpawn, "fleckToSpawn");
		Scribe_Values.Look(ref fleckSpawnInterval, "fleckSpawnInterval", 10f);
		Scribe_Values.Look(ref fleckOffset, "fleckOffset", 0f);
		Scribe_Values.Look(ref bombDropInterval, "bombDropInterval", 200);
		Scribe_Values.Look(ref bombDropCounter, "bombDropCounter", 0);
		Scribe_Defs.Look(ref bombDef, "bombDef");
		Scribe_Values.Look(ref hoverMode, "hoverMode", defaultValue: false);
		Scribe_Values.Look(ref hoverRadius, "hoverRadius", 100f);
		Scribe_Values.Look(ref hoverAngle, "hoverAngle", 0f);
		Scribe_Values.Look(ref hoverSpeed, "hoverSpeed", 0.01f);
		Scribe_Values.Look(ref currentRotation, "currentRotation", 0f);
	}

	public static CruiseFlyer CreateRightToLeftFlyer(ThingDef flyerDef, Thing content, Map map, float speed = 0.004f, float amplitudeFactor = 0.8f, float altitude = 10f, float shadowSize = 1f)
	{
		CruiseFlyer flyer = (CruiseFlyer)ThingMaker.MakeThing(flyerDef);
		if (content != null)
		{
			flyer.innerContainer.TryAdd(content);
		}
		flyer.cruiseSpeed = speed;
		flyer.cruiseDirection = -1;
		flyer.cruiseAmplitude = (float)map.Size.x * amplitudeFactor;
		flyer.cruiseAltitude = altitude;
		flyer.shadowSizeFactor = shadowSize;
		GenSpawn.Spawn(flyer, map.Center, map);
		return flyer;
	}

	public void SetAltitude(float altitude)
	{
		cruiseAltitude = altitude;
	}

	public void SetBombDropInterval(int intervalTicks)
	{
		bombDropInterval = Mathf.Max(10, intervalTicks);
	}

	public void SetBombType(ThingDef newBombDef)
	{
		bombDef = newBombDef;
	}

	public void SetShadowSize(float sizeFactor)
	{
		shadowSizeFactor = sizeFactor;
	}
}
