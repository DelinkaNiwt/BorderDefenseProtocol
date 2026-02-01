using System;
using System.Collections.Generic;
using System.Text;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class InterceptorMapComponent : MapComponent
{
	private int nextGridId;

	private readonly List<InterceptorGrid> grids = new List<InterceptorGrid>();

	private readonly List<InterceptorGrid>[] cellGrid;

	private readonly int mapWidth;

	private readonly int mapHeight;

	private readonly int mapTotalCells;

	private int destinationIndex;

	public InterceptorMapComponent(Map map)
		: base(map)
	{
		mapWidth = map.Size.x;
		mapHeight = map.Size.z;
		mapTotalCells = mapWidth * mapHeight;
		cellGrid = new List<InterceptorGrid>[mapTotalCells];
	}

	private int GetCellIndex(Vector3 cell)
	{
		return (int)cell.z * mapWidth + (int)cell.x;
	}

	public int GetCellIndex(IntVec3 cell)
	{
		return cell.z * mapWidth + cell.x;
	}

	public IntVec3 GetCell(int index)
	{
		return new IntVec3(index % mapWidth, 0, index / mapWidth);
	}

	public IEnumerable<IntVec3> GetCoveredCells(CellRect rect)
	{
		if (rect.Area < 1)
		{
			yield break;
		}
		for (int x = rect.minX; x <= rect.maxX; x++)
		{
			for (int z = rect.minZ; z <= rect.maxZ; z++)
			{
				int index = x + z * mapWidth;
				if (index > -1 && index < mapTotalCells && cellGrid[index] != null && cellGrid[index].Count > 0)
				{
					yield return new IntVec3(x, 0, z);
				}
			}
		}
	}

	private void PaintGrid(InterceptorGrid grid, IInterceptorSource source)
	{
		if (grid == null || source == null)
		{
			return;
		}
		foreach (IntVec3 item in ProjectileUtility.GetEffectRadiusCellsAround(source.GetSourceCell(), source.GetGridRadius(), source.GetBaseWidth()))
		{
			if (item.InBounds(map))
			{
				int cellIndex = GetCellIndex(item);
				PaintCell(cellIndex, grid);
				grid.PaintCell(cellIndex, source);
			}
		}
		grid.SortCellSources();
	}

	private void PaintCell(int index, InterceptorGrid grid)
	{
		if (index >= 0 && index < cellGrid.Length)
		{
			if (cellGrid[index] == null)
			{
				cellGrid[index] = new List<InterceptorGrid>(1) { grid };
			}
			else if (!cellGrid[index].Contains(grid))
			{
				cellGrid[index].Add(grid);
			}
		}
	}

	private void UnpaintGrid(InterceptorGrid grid)
	{
		foreach (int cellIndex in grid.CellIndices)
		{
			UnpaintCell(cellIndex, grid);
		}
		grid.ClearIndices();
	}

	private void UnpaintCell(int index, InterceptorGrid grid)
	{
		if (cellGrid[index] != null)
		{
			cellGrid[index].Remove(grid);
		}
	}

	public void RepaintGrid(InterceptorGrid grid)
	{
		UnpaintGrid(grid);
		foreach (IInterceptorSource source in grid.sources)
		{
			PaintGrid(grid, source);
		}
		grid.dirty = false;
	}

	public InterceptorGrid RegisterSource(IInterceptorSource source, InterceptorGrid grid = null)
	{
		if (source == null)
		{
			return null;
		}
		if (grid == null)
		{
			return AddNewGrid(source);
		}
		grid.AddSource(source);
		PaintGrid(grid, source);
		return grid;
	}

	private InterceptorGrid AddNewGrid(IInterceptorSource source)
	{
		InterceptorGrid interceptorGrid = new InterceptorGrid(this, nextGridId++, source);
		grids.Add(interceptorGrid);
		PaintGrid(interceptorGrid, source);
		return interceptorGrid;
	}

	private void RemoveGrid(InterceptorGrid grid)
	{
		UnpaintGrid(grid);
		grid.ClearIndices();
		grid.mapComponent = null;
		grids.Remove(grid);
	}

	public void DeregisterSource(InterceptorGrid grid, IInterceptorSource source = null)
	{
		if (grid == null)
		{
			return;
		}
		if (source != null)
		{
			if (grid.sources.Contains(source))
			{
				grid.sources.Remove(source);
				if (grid.sources.Count < 1)
				{
					RemoveGrid(grid);
				}
				else
				{
					grid.dirty = true;
				}
			}
		}
		else
		{
			RemoveGrid(grid);
		}
	}

	public bool CheckIntercept(Thing thing, Vector3 origin, Vector3 destination)
	{
		destinationIndex = GetCellIndex(destination);
		if (destinationIndex < 0 || destinationIndex >= mapTotalCells)
		{
			return false;
		}
		List<InterceptorGrid> list = cellGrid[destinationIndex];
		if (list != null)
		{
			foreach (InterceptorGrid item in list)
			{
				if (item.TryIntercept(thing, ref origin, ref destination))
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	public bool CheckBombardmentIntercept(float damage, IntVec3 cell)
	{
		destinationIndex = GetCellIndex(cell);
		List<InterceptorGrid> list = cellGrid[destinationIndex];
		if (list != null)
		{
			foreach (InterceptorGrid item in list)
			{
				if (item.TryBombardmentIntercept(damage, cell))
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	public override void MapComponentUpdate()
	{
		if (WorldRendererUtility.DrawingMap && Find.CurrentMap == map)
		{
			Draw();
		}
	}

	private void Draw()
	{
		try
		{
			CellRect currentViewRect = Find.CameraDriver.CurrentViewRect;
			currentViewRect.ClipInsideMap(map);
			currentViewRect = currentViewRect.ExpandedBy(1);
			foreach (InterceptorGrid grid in grids)
			{
				if (grid.dirty)
				{
					RepaintGrid(grid);
				}
				grid.Draw(ref currentViewRect);
			}
		}
		catch (Exception arg)
		{
			Log.Error($"(NCL Defense Grid) Error trying to draw in DefenseGridMapComponent on map {map.ToStringSafe()}: {arg}");
		}
	}

	public override void MapComponentTick()
	{
		foreach (InterceptorGrid grid in grids)
		{
			if (grid.dirty)
			{
				RepaintGrid(grid);
			}
		}
	}

	internal string DebugOutput()
	{
		StringBuilder stringBuilder = new StringBuilder($"(NCL Projectiles) Debug output for InterceptorMapComponent with {grids.Count} active grids:\n");
		foreach (InterceptorGrid grid in grids)
		{
			stringBuilder.AppendLine($"#{grid.id} with {grid.SourceCount} sources covering {grid.CellCount} cells");
		}
		return stringBuilder.ToString();
	}
}
