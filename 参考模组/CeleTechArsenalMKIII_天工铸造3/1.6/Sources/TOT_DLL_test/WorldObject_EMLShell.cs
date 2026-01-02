using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class WorldObject_EMLShell : WorldObject
{
	private const float TravelSpeed = 0.0004f;

	private bool arrived;

	public IntVec3 destinationCell = IntVec3.Invalid;

	public int destinationTile = -1;

	private int initialTile = -1;

	public ThingDef Projectile;

	public Thing railgun;

	public int spread = 1;

	private float traveledPct;

	private Vector3 Start
	{
		get
		{
			_ = base.Tile;
			if (true)
			{
				return Find.WorldGrid.GetTileCenter(base.Tile);
			}
			return Find.WorldGrid.GetTileCenter(-1);
		}
	}

	private Vector3 End
	{
		get
		{
			_ = GameComponent_CeleTech.Instance.ASEA_observedMap.Map.Tile;
			if (true)
			{
				return Find.WorldGrid.GetTileCenter(GameComponent_CeleTech.Instance.ASEA_observedMap.Map.Tile);
			}
			return Find.WorldGrid.GetTileCenter(-1);
		}
	}

	public override Vector3 DrawPos => Vector3.Slerp(Start, End, traveledPct);

	private float TraveledPctStepPerTick
	{
		get
		{
			Vector3 start = Start;
			Vector3 end = End;
			if (start == end)
			{
				return 1f;
			}
			float num = GenMath.SphericalDistance(start.normalized, end.normalized);
			if (num == 0f)
			{
				return 1f;
			}
			return 0.0004f / num;
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref destinationTile, "destinationTile", 0);
		Scribe_Values.Look(ref destinationCell, "destinationCell");
		Scribe_Values.Look(ref arrived, "arrived", defaultValue: false);
		Scribe_Values.Look(ref initialTile, "initialTile", 0);
		Scribe_Values.Look(ref traveledPct, "traveledPct", 0f);
		Scribe_Defs.Look(ref Projectile, "Projectile");
	}

	public override void PostAdd()
	{
		base.PostAdd();
	}

	protected override void Tick()
	{
		base.Tick();
		traveledPct += TraveledPctStepPerTick;
		if (traveledPct >= 1f)
		{
			traveledPct = 1f;
			Arrived();
		}
	}

	private void Arrived()
	{
		if (!arrived)
		{
			arrived = true;
			Map map = GameComponent_CeleTech.Instance.ASEA_observedMap.Map;
			if (map != null)
			{
				IntVec3 loc = new IntVec3(CellRect.WholeMap(map).Width / 2, 0, CellRect.WholeMap(map).maxZ);
				Projectile projectile = (Projectile)GenSpawn.Spawn(Projectile, loc, map);
				CellFinder.TryFindRandomCellNear(destinationCell, map, spread, null, out var result);
				projectile.Launch(railgun, result, result, ProjectileHitFlags.IntendedTarget);
			}
			Find.WorldObjects.Remove(this);
		}
	}
}
