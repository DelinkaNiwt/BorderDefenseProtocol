using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class StraightLineCruiseFlyerRighttoLeft : CruiseFlyer
{
	protected float startZ;

	protected float horizontalProgress;

	protected const float WestRotation = 0f;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		hoverMode = false;
		waveMotion = false;
		autoReverse = false;
		float safeMinZ = Mathf.Max(10f, (float)map.Size.z * 0.1f);
		float safeMaxZ = Mathf.Min((float)map.Size.z - 10f, (float)map.Size.z * 0.9f);
		startZ = Rand.Range(safeMinZ, safeMaxZ);
		horizontalProgress = 0f;
	}

	protected override void GetDrawPositionAndRotation(ref Vector3 drawLoc, out float extraRotation)
	{
		float mapCenterX = base.Map.Center.ToVector3Shifted().x;
		float mapWidth = base.Map.Size.x;
		float currentX = Mathf.Lerp(mapCenterX + mapWidth / 2f, mapCenterX - mapWidth / 2f, horizontalProgress);
		float safeZ = Mathf.Clamp(startZ, 0f, base.Map.Size.z);
		drawLoc = new Vector3(currentX, cruiseAltitude, safeZ);
		extraRotation = 0f;
	}

	protected override void Tick()
	{
		base.Tick();
		horizontalProgress += cruiseSpeed;
		if (horizontalProgress >= 1f)
		{
			Destroy();
		}
	}

	protected override Vector3 GetForwardDirection()
	{
		return new Vector3(-1f, 0f, 0f).normalized;
	}

	protected override void GenerateWingSmoke(Vector3 centerPos)
	{
		if (fleckToSpawn != null)
		{
			Vector3 forward = GetForwardDirection();
			Vector3 right = new Vector3(forward.z, 0f, 0f - forward.x).normalized;
			Vector3 leftWingPos = centerPos - right * 4f;
			leftWingPos.y += 0.5f;
			Vector3 rightWingPos = centerPos + right * 4f;
			rightWingPos.y += 0.5f;
			FleckMaker.Static(leftWingPos, base.Map, fleckToSpawn);
			FleckMaker.Static(rightWingPos, base.Map, fleckToSpawn);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref startZ, "startZ", 0f);
		Scribe_Values.Look(ref horizontalProgress, "horizontalProgress", 0f);
	}

	public static StraightLineCruiseFlyerRighttoLeft Create(ThingDef flyerDef, Map map, float speed = 0.005f, float altitude = 10f)
	{
		StraightLineCruiseFlyerRighttoLeft flyer = (StraightLineCruiseFlyerRighttoLeft)ThingMaker.MakeThing(flyerDef);
		flyer.cruiseSpeed = speed;
		flyer.cruiseAltitude = altitude;
		IntVec3 spawnPos = map.Center;
		if (!spawnPos.Walkable(map))
		{
			spawnPos = CellFinder.RandomClosewalkCellNear(map.Center, map, 10);
		}
		GenSpawn.Spawn(flyer, spawnPos, map);
		return flyer;
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

	private void DropBomb()
	{
		Vector3 bombPos = GetCurrentDrawPosition();
		Projectile bomb = (Projectile)ThingMaker.MakeThing(bombDef);
		bomb.def = bombDef;
		Vector3 spawnPos = bombPos - new Vector3(0f, 1f, 0f);
		IntVec3 spawnCell = spawnPos.ToIntVec3();
		if (!spawnCell.InBounds(base.Map))
		{
			spawnCell = spawnCell.ClampInsideMap(base.Map);
		}
		GenSpawn.Spawn(bomb, spawnCell, base.Map);
		IntVec3 targetCell = spawnCell + new IntVec3(0, 0, -1);
		if (!targetCell.InBounds(base.Map))
		{
			targetCell = targetCell.ClampInsideMap(base.Map);
		}
		bomb.Launch(this, spawnPos, new LocalTargetInfo(targetCell), new LocalTargetInfo(targetCell), ProjectileHitFlags.All);
		if (spawnCell.InBounds(base.Map))
		{
			FleckMaker.ThrowSmoke(spawnPos, base.Map, 1f);
			FleckMaker.ThrowLightningGlow(spawnPos, base.Map, 1f);
		}
	}
}
