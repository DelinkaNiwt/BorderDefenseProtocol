using RimWorld;
using UnityEngine;
using Verse;

namespace NCL;

public class StraightLineCruiseFlyerUptoDown : CruiseFlyer
{
	protected float startX;

	protected float verticalProgress;

	protected const float SouthRotation = 270f;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		hoverMode = false;
		waveMotion = false;
		autoReverse = false;
		float safeMinX = Mathf.Max(10f, (float)map.Size.x * 0.1f);
		float safeMaxX = Mathf.Min((float)map.Size.x - 10f, (float)map.Size.x * 0.9f);
		startX = Rand.Range(safeMinX, safeMaxX);
		verticalProgress = 0f;
	}

	protected override void GetDrawPositionAndRotation(ref Vector3 drawLoc, out float extraRotation)
	{
		float mapCenterZ = base.Map.Center.ToVector3Shifted().z;
		float mapHeight = base.Map.Size.z;
		float currentZ = Mathf.Lerp(mapCenterZ + mapHeight / 2f, mapCenterZ - mapHeight / 2f, verticalProgress);
		float safeX = Mathf.Clamp(startX, 0f, base.Map.Size.x);
		drawLoc = new Vector3(safeX, cruiseAltitude, currentZ);
		extraRotation = 270f;
	}

	protected override void Tick()
	{
		base.Tick();
		verticalProgress += cruiseSpeed;
		if (verticalProgress >= 1f)
		{
			Destroy();
		}
	}

	protected override Vector3 GetForwardDirection()
	{
		return new Vector3(0f, 0f, -1f).normalized;
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
		Scribe_Values.Look(ref startX, "startX", 0f);
		Scribe_Values.Look(ref verticalProgress, "verticalProgress", 0f);
	}

	public static StraightLineCruiseFlyerUptoDown Create(ThingDef flyerDef, Map map, float speed = 0.005f, float altitude = 10f)
	{
		StraightLineCruiseFlyerUptoDown flyer = (StraightLineCruiseFlyerUptoDown)ThingMaker.MakeThing(flyerDef);
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
