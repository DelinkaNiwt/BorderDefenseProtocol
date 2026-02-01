using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NCLProjectiles;

public class InterceptorGrid : IEquatable<InterceptorGrid>
{
	public readonly int id;

	public InterceptorMapComponent mapComponent;

	public readonly List<IInterceptorSource> sources = new List<IInterceptorSource>();

	private readonly List<int> cellIndices;

	private readonly Dictionary<int, List<IInterceptorSource>> sourcesByCellIndex = new Dictionary<int, List<IInterceptorSource>>();

	public bool dirty;

	public IEnumerable<int> CellIndices => cellIndices;

	public int CellCount => cellIndices.Count;

	public int SourceCount => sources.Count;

	public InterceptorGrid(InterceptorMapComponent mapComponent, int id, IInterceptorSource source)
	{
		this.mapComponent = mapComponent;
		this.id = id;
		if (source == null)
		{
			cellIndices = new List<int>();
			return;
		}
		AddSource(source);
		cellIndices = new List<int>(ProjectileUtility.GetEffectRadiusCellCount(source.GetRadius(), source.GetBaseWidth()));
	}

	public void ClearIndices()
	{
		cellIndices.Clear();
		foreach (List<IInterceptorSource> value in sourcesByCellIndex.Values)
		{
			value.Clear();
		}
	}

	public void PaintCell(int cellIndex, IInterceptorSource source)
	{
		cellIndices.Add(cellIndex);
		if (!sourcesByCellIndex.ContainsKey(cellIndex))
		{
			sourcesByCellIndex[cellIndex] = new List<IInterceptorSource>();
		}
		if (!sourcesByCellIndex[cellIndex].Contains(source))
		{
			sourcesByCellIndex[cellIndex].Add(source);
		}
	}

	public void SortCellSources()
	{
		foreach (int cellIndex in cellIndices)
		{
			if (sourcesByCellIndex[cellIndex].Count > 1)
			{
				IntVec3 cell = mapComponent.GetCell(cellIndex);
				sourcesByCellIndex[cellIndex].SortBy((IInterceptorSource source) => source.GetSourceCell().DistanceTo(cell));
			}
		}
	}

	public bool Equals(InterceptorGrid other)
	{
		return other != null && mapComponent == other.mapComponent && id == other.id;
	}

	public void AddSource(IInterceptorSource source)
	{
		sources.Add(source);
	}

	public void RemoveSource(IInterceptorSource source)
	{
		sources.Remove(source);
	}

	public bool TryIntercept(Thing thing, ref Vector3 origin, ref Vector3 destination)
	{
		IntVec3 cell = destination.ToIntVec3();
		if (sourcesByCellIndex.TryGetValue(mapComponent.GetCellIndex(cell), out var value))
		{
			if (value.Count > 1)
			{
				foreach (IInterceptorSource item in value)
				{
					if (item.RejectInterception(thing, origin))
					{
						return false;
					}
				}
				{
					foreach (IInterceptorSource item2 in value)
					{
						if (item2.CanIntercept(thing, origin, destination))
						{
							item2.NotifyIntercept(thing);
							return true;
						}
					}
					return false;
				}
			}
			if (value.Count > 0)
			{
				IInterceptorSource interceptorSource = value.First();
				if (interceptorSource.CanIntercept(thing, origin, destination))
				{
					interceptorSource.NotifyIntercept(thing);
					return true;
				}
			}
		}
		return false;
	}

	public bool TryBombardmentIntercept(float damage, IntVec3 cell)
	{
		if (sourcesByCellIndex.TryGetValue(mapComponent.GetCellIndex(cell), out var value))
		{
			foreach (IInterceptorSource item in value)
			{
				if (item.CanInterceptBombardment(mapComponent.map, damage, cell))
				{
					item.NotifyInterceptBombardment(mapComponent.map, damage, cell);
					return true;
				}
			}
			return false;
		}
		return false;
	}

	public void Draw(ref CellRect cameraRect)
	{
		foreach (IInterceptorSource source in sources)
		{
			if (source.ShouldDrawField(ref cameraRect))
			{
				source.DrawField();
			}
		}
	}
}
