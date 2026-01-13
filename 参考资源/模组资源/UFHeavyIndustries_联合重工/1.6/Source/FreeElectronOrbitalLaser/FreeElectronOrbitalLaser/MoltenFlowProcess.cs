using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FreeElectronOrbitalLaser;

public class MoltenFlowProcess : Thing
{
	private const int LavaCoolInterval = 400;

	private static readonly IntRange LavaBeginCoolingDelay = new IntRange(300000, 360000);

	[Unsaved(false)]
	public int forcePoolSize = -1;

	[Unsaved(false)]
	public int forceCoolDelay = -1;

	public int expandIntervalTicks = 1;

	public int cellsToSpreadPerInterval = 1;

	protected HashSet<IntVec3> openCells = new HashSet<IntVec3>();

	protected HashSet<IntVec3> lavaCells = new HashSet<IntVec3>();

	protected int poolSize;

	private int coolDelay;

	private bool isCooling = false;

	private List<IntVec3> cellsToCool;

	private HashSet<IntVec3> cellsSetForCooling;

	private int coolingCounter = 0;

	private const int CoolingCellsPerTick = 20;

	[Unsaved(false)]
	private Func<IntVec3, float> weightSelector;

	public bool isLinkedToBeam = false;

	protected virtual IntRange PoolSizeRange => new IntRange(450, 600);

	protected virtual bool FireLetter => false;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Collections.Look(ref openCells, "openCells", LookMode.Value);
		Scribe_Collections.Look(ref lavaCells, "lavaCells", LookMode.Value);
		Scribe_Values.Look(ref poolSize, "poolSize", 0);
		Scribe_Values.Look(ref coolDelay, "coolDelay", 0);
		Scribe_Values.Look(ref expandIntervalTicks, "expandIntervalTicks", 1);
		Scribe_Values.Look(ref cellsToSpreadPerInterval, "cellsToSpreadPerInterval", 1);
		Scribe_Values.Look(ref isCooling, "isCooling", defaultValue: false);
		Scribe_Collections.Look(ref cellsToCool, "cellsToCool", LookMode.Value);
		Scribe_Values.Look(ref coolingCounter, "coolingCounter", 0);
		Scribe_Values.Look(ref isLinkedToBeam, "isLinkedToBeam", defaultValue: false);
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		if (!respawningAfterLoad)
		{
			openCells.Add(base.Position);
			if (openCells.Count == 0)
			{
				Destroy();
				return;
			}
			poolSize = ((forcePoolSize > 0) ? forcePoolSize : PoolSizeRange.RandomInRange);
			coolDelay = ((forceCoolDelay > 0) ? forceCoolDelay : LavaBeginCoolingDelay.RandomInRange);
		}
	}

	protected override void Tick()
	{
		base.Tick();
		if (isCooling)
		{
			ProcessCoolingTick();
			return;
		}
		if (this.IsHashIntervalTick(expandIntervalTicks))
		{
			for (int i = 0; i < cellsToSpreadPerInterval; i++)
			{
				if (openCells.Count == 0)
				{
					break;
				}
				SpreadLava();
			}
		}
		if (!isLinkedToBeam && (lavaCells.Count >= poolSize || openCells.Count == 0))
		{
			StartCoolingCalculation();
		}
	}

	public virtual void AddCellDirectly(IntVec3 c)
	{
		if (isCooling || lavaCells.Contains(c) || !c.InBounds(base.Map) || !CanLavaSpreadInto(c))
		{
			return;
		}
		base.Map.terrainGrid.SetTempTerrain(c, TerrainDefOf.LavaShallow);
		lavaCells.Add(c);
		if (openCells.Contains(c))
		{
			openCells.Remove(c);
		}
		IntVec3[] cardinalDirections = GenAdj.CardinalDirections;
		foreach (IntVec3 intVec in cardinalDirections)
		{
			IntVec3 intVec2 = c + intVec;
			if (!openCells.Contains(intVec2) && !lavaCells.Contains(intVec2) && CanLavaSpreadInto(intVec2))
			{
				openCells.Add(intVec2);
			}
		}
	}

	protected void SpreadLava()
	{
		if (openCells.Count == 0)
		{
			return;
		}
		if (!openCells.TryRandomElementByWeight((IntVec3 c) => Mathf.Max(AdjacentLavaCells(c), 1) * 4, out var result))
		{
			result = openCells.RandomElement();
		}
		openCells.Remove(result);
		IntVec3[] cardinalDirections = GenAdj.CardinalDirections;
		foreach (IntVec3 intVec in cardinalDirections)
		{
			IntVec3 intVec2 = result + intVec;
			if (!openCells.Contains(intVec2) && !lavaCells.Contains(intVec2) && CanLavaSpreadInto(intVec2))
			{
				openCells.Add(intVec2);
			}
		}
		base.Map.terrainGrid.SetTempTerrain(result, TerrainDefOf.LavaShallow);
		lavaCells.Add(result);
	}

	protected virtual bool CanLavaSpreadInto(IntVec3 c)
	{
		if (!c.InBounds(base.Map))
		{
			return false;
		}
		Building edifice = c.GetEdifice(base.Map);
		if (edifice != null && !edifice.IsClearableFreeBuilding)
		{
			return false;
		}
		TerrainDef terrain = c.GetTerrain(base.Map);
		return terrain.natural && base.Map.terrainGrid.FoundationAt(c) == null && terrain != TerrainDefOf.LavaDeep;
	}

	private int AdjacentLavaCells(IntVec3 c)
	{
		int num = 0;
		IntVec3[] cardinalDirections = GenAdj.CardinalDirections;
		foreach (IntVec3 intVec in cardinalDirections)
		{
			IntVec3 c2 = c + intVec;
			if (c2.InBounds(base.Map) && c2.GetTerrain(base.Map) == TerrainDefOf.LavaShallow)
			{
				num++;
			}
		}
		return num;
	}

	public void StartCoolingCalculation()
	{
		if (!isCooling)
		{
			isCooling = true;
			cellsToCool = lavaCells.ToList();
			cellsSetForCooling = lavaCells.ToHashSet();
			coolingCounter = 0;
			InitializeWeightSelector();
		}
	}

	private void InitializeWeightSelector()
	{
		if (cellsSetForCooling == null && cellsToCool != null)
		{
			cellsSetForCooling = cellsToCool.ToHashSet();
		}
		weightSelector = delegate(IntVec3 c)
		{
			int num = 0;
			IntVec3[] cardinalDirections = GenAdj.CardinalDirections;
			foreach (IntVec3 intVec in cardinalDirections)
			{
				IntVec3 intVec2 = c + intVec;
				if (intVec2.InBounds(base.Map) && (cellsSetForCooling.Contains(intVec2) || intVec2.GetTerrain(base.Map) == TerrainDefOf.LavaDeep))
				{
					num++;
				}
			}
			return 16 - num * 4;
		};
	}

	private void ProcessCoolingTick()
	{
		if (weightSelector == null)
		{
			InitializeWeightSelector();
		}
		for (int i = 0; i < 20; i++)
		{
			if (cellsToCool.NullOrEmpty())
			{
				isCooling = false;
				Destroy();
				break;
			}
			if (!cellsToCool.TryRandomElementByWeight(weightSelector, out var result))
			{
				result = cellsToCool.RandomElement();
			}
			base.Map.tempTerrain.QueueRemoveTerrain(result, Find.TickManager.TicksGame + coolDelay + 400 * coolingCounter);
			cellsToCool.Remove(result);
			cellsSetForCooling.Remove(result);
			coolingCounter++;
		}
	}
}
