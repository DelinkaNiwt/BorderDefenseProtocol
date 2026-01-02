using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public class ThingTable
{
	private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.2f);

	private ThingTableDef def;

	private Func<IEnumerable<Thing>> ThingsGetter;

	private int minTableWidth;

	private int maxTableWidth;

	private int minTableHeight;

	private int maxTableHeight;

	private Vector2 fixedSize;

	private bool hasFixedSize;

	private bool dirty;

	private List<bool> columnAtMaxWidth = new List<bool>();

	private List<bool> columnAtOptimalWidth = new List<bool>();

	private Vector2 scrollPosition;

	private ThingColumnDef sortByColumn;

	private bool sortDescending;

	private Vector2 cachedSize;

	private List<Thing> cachedThings = new List<Thing>();

	private List<float> cachedColumnWidths = new List<float>();

	private List<float> cachedRowHeights = new List<float>();

	private List<LookTargets> cachedLookTargets = new List<LookTargets>();

	private List<ThingColumnDef> columns = new List<ThingColumnDef>();

	private float cachedHeaderHeight;

	private float cachedHeightNoScrollbar;

	public List<ThingColumnDef> Columns
	{
		get
		{
			columns.Clear();
			foreach (ThingColumnDef column in def.columns)
			{
				if (column.Worker.VisibleCurrently)
				{
					columns.Add(column);
				}
			}
			return columns;
		}
	}

	public ThingColumnDef SortingBy => sortByColumn;

	public bool SortingDescending => SortingBy != null && sortDescending;

	public Vector2 Size
	{
		get
		{
			RecacheIfDirty();
			return cachedSize;
		}
	}

	public float HeightNoScrollbar
	{
		get
		{
			RecacheIfDirty();
			return cachedHeightNoScrollbar;
		}
	}

	public float HeaderHeight
	{
		get
		{
			RecacheIfDirty();
			return cachedHeaderHeight;
		}
	}

	public List<Thing> PawnsListForReading
	{
		get
		{
			RecacheIfDirty();
			return cachedThings;
		}
	}

	public ThingTable(ThingTableDef def, Func<IEnumerable<Thing>> ThingsGetter, int uiWidth, int uiHeight)
	{
		this.def = def;
		this.ThingsGetter = ThingsGetter;
		SetMinMaxSize(def.minWidth, uiWidth, 0, uiHeight);
		SetDirty();
	}

	public void PawnTableOnGUI(Vector2 position)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)Event.current.type == 8)
		{
			return;
		}
		RecacheIfDirty();
		float num = cachedSize.x - 16f;
		List<ThingColumnDef> list = Columns;
		int num2 = 0;
		for (int i = 0; i < list.Count; i++)
		{
			int num3 = ((i != list.Count - 1) ? ((int)cachedColumnWidths[i]) : ((int)(num - (float)num2)));
			Rect rect = new Rect((int)position.x + num2, (int)position.y, num3, (int)cachedHeaderHeight);
			list[i].Worker.DoHeader(rect, this);
			num2 += num3;
		}
		Rect outRect = new Rect((int)position.x, (int)position.y + (int)cachedHeaderHeight, (int)cachedSize.x, (int)cachedSize.y - (int)cachedHeaderHeight);
		Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, (int)cachedHeightNoScrollbar - (int)cachedHeaderHeight);
		Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
		num2 = 0;
		int num4;
		for (int j = 0; j < list.Count; j++)
		{
			num4 = 0;
			ThingColumnDef thingColumnDef = list[j];
			int num5 = ((j != list.Count - 1) ? ((int)cachedColumnWidths[j]) : ((int)(num - (float)num2)));
			for (int k = 0; k < cachedThings.Count; k++)
			{
				GUI.color = BorderColor;
				Widgets.DrawLineHorizontal(num2, num4, num5);
				GUI.color = Color.white;
				Rect rect2 = new Rect(num2, num4, num5, (int)cachedRowHeights[k]);
				Thing thing = cachedThings[k];
				bool flag = false;
				if (thingColumnDef.groupable)
				{
					int num6 = k;
					for (int l = k + 1; l < cachedThings.Count && list[j].Worker.CanGroupWith(cachedThings[k], cachedThings[l]); l++)
					{
						rect2.yMax += (int)cachedRowHeights[l];
						num6 = l;
						flag = true;
					}
					k = num6;
				}
				if ((float)num4 - scrollPosition.y + (float)(int)cachedRowHeights[k] >= 0f && (float)num4 - scrollPosition.y <= outRect.height)
				{
					list[j].Worker.DoCell(rect2, thing, this);
					if (thingColumnDef.groupable && flag)
					{
						GUI.color = BorderColor;
						Widgets.DrawLineVertical(rect2.xMin, rect2.yMin, rect2.height);
						Widgets.DrawLineVertical(rect2.xMax, rect2.yMin, rect2.height);
						GUI.color = Color.white;
					}
				}
				GUI.color = Color.white;
				num4 += (int)rect2.height;
			}
			num2 += num5;
		}
		num4 = 0;
		for (int m = 0; m < cachedThings.Count; m++)
		{
			Rect rect3 = new Rect(0f, num4, viewRect.width, (int)cachedRowHeights[m]);
			if (Find.Selector.IsSelected(cachedThings[m]))
			{
				Widgets.DrawHighlight(rect3, 0.6f);
			}
			if (Mouse.IsOver(rect3))
			{
				Widgets.DrawHighlight(rect3);
			}
			num4 += (int)cachedRowHeights[m];
		}
		Widgets.EndScrollView();
	}

	public void SetDirty()
	{
		dirty = true;
	}

	public void SetMinMaxSize(int minTableWidth, int maxTableWidth, int minTableHeight, int maxTableHeight)
	{
		this.minTableWidth = minTableWidth;
		this.maxTableWidth = maxTableWidth;
		this.minTableHeight = minTableHeight;
		this.maxTableHeight = maxTableHeight;
		hasFixedSize = false;
		SetDirty();
	}

	public void SetFixedSize(Vector2 size)
	{
		fixedSize = size;
		hasFixedSize = true;
		SetDirty();
	}

	public void SortBy(ThingColumnDef column, bool descending)
	{
		sortByColumn = column;
		sortDescending = descending;
		SetDirty();
	}

	private void RecacheIfDirty()
	{
		if (dirty)
		{
			dirty = false;
			RecacheColumns();
			RecacheThings();
			RecacheRowHeights();
			cachedHeaderHeight = CalculateHeaderHeight();
			cachedHeightNoScrollbar = CalculateTotalRequiredHeight();
			RecacheSize();
			RecacheColumnWidths();
			RecacheLookTargets();
		}
	}

	private void RecacheColumns()
	{
		foreach (ThingColumnDef column in def.columns)
		{
			column.Worker.Recache();
		}
	}

	private void RecacheThings()
	{
		cachedThings.Clear();
		cachedThings.AddRange(ThingsGetter());
		cachedThings = LabelSortFunction(cachedThings).ToList();
		if (sortByColumn != null)
		{
			if (sortDescending)
			{
				cachedThings.SortStable(sortByColumn.Worker.Compare);
			}
			else
			{
				cachedThings.SortStable((Thing a, Thing b) => sortByColumn.Worker.Compare(b, a));
			}
		}
		cachedThings = PrimarySortFunction(cachedThings).ToList();
	}

	protected virtual IEnumerable<Thing> LabelSortFunction(IEnumerable<Thing> input)
	{
		return input.OrderBy((Thing p) => p.Label);
	}

	protected virtual IEnumerable<Thing> PrimarySortFunction(IEnumerable<Thing> input)
	{
		return input;
	}

	private void RecacheColumnWidths()
	{
		float num = cachedSize.x - 16f;
		float minWidthsSum = 0f;
		RecacheColumnWidths_StartWithMinWidths(out minWidthsSum);
		if (minWidthsSum == num)
		{
			return;
		}
		if (minWidthsSum > num)
		{
			SubtractProportionally(minWidthsSum - num, minWidthsSum);
			return;
		}
		RecacheColumnWidths_DistributeUntilOptimal(num, ref minWidthsSum, out var noMoreFreeSpace);
		if (!noMoreFreeSpace)
		{
			RecacheColumnWidths_DistributeAboveOptimal(num, ref minWidthsSum);
		}
	}

	private void RecacheColumnWidths_StartWithMinWidths(out float minWidthsSum)
	{
		minWidthsSum = 0f;
		cachedColumnWidths.Clear();
		List<ThingColumnDef> list = Columns;
		for (int i = 0; i < list.Count; i++)
		{
			float minWidth = GetMinWidth(list[i]);
			cachedColumnWidths.Add(minWidth);
			minWidthsSum += minWidth;
		}
	}

	private void RecacheColumnWidths_DistributeUntilOptimal(float totalAvailableSpaceForColumns, ref float usedWidth, out bool noMoreFreeSpace)
	{
		columnAtOptimalWidth.Clear();
		List<ThingColumnDef> list = Columns;
		for (int i = 0; i < list.Count; i++)
		{
			columnAtOptimalWidth.Add(cachedColumnWidths[i] >= GetOptimalWidth(list[i]));
		}
		int num = 0;
		bool flag;
		bool flag2;
		do
		{
			num++;
			if (num < 10000)
			{
				float num2 = float.MinValue;
				for (int j = 0; j < list.Count; j++)
				{
					if (!columnAtOptimalWidth[j])
					{
						num2 = Mathf.Max(num2, list[j].widthPriority);
					}
				}
				float num3 = 0f;
				for (int k = 0; k < cachedColumnWidths.Count; k++)
				{
					if (!columnAtOptimalWidth[k] && (float)list[k].widthPriority == num2)
					{
						num3 += GetOptimalWidth(list[k]);
					}
				}
				float num4 = totalAvailableSpaceForColumns - usedWidth;
				flag = false;
				flag2 = false;
				for (int l = 0; l < cachedColumnWidths.Count; l++)
				{
					if (columnAtOptimalWidth[l])
					{
						continue;
					}
					if ((float)list[l].widthPriority != num2)
					{
						flag = true;
						continue;
					}
					float num5 = num4 * GetOptimalWidth(list[l]) / num3;
					float num6 = GetOptimalWidth(list[l]) - cachedColumnWidths[l];
					if (num5 >= num6)
					{
						num5 = num6;
						columnAtOptimalWidth[l] = true;
						flag2 = true;
					}
					else
					{
						flag = true;
					}
					if (num5 > 0f)
					{
						cachedColumnWidths[l] += num5;
						usedWidth += num5;
					}
				}
				if (usedWidth >= totalAvailableSpaceForColumns - 0.1f)
				{
					noMoreFreeSpace = true;
					break;
				}
				continue;
			}
			Log.Error("Too many iterations.");
			break;
		}
		while (flag && flag2);
		noMoreFreeSpace = false;
	}

	private void RecacheColumnWidths_DistributeAboveOptimal(float totalAvailableSpaceForColumns, ref float usedWidth)
	{
		columnAtMaxWidth.Clear();
		List<ThingColumnDef> list = Columns;
		for (int i = 0; i < list.Count; i++)
		{
			columnAtMaxWidth.Add(cachedColumnWidths[i] >= GetMaxWidth(list[i]));
		}
		int num = 0;
		bool flag;
		do
		{
			num++;
			if (num < 10000)
			{
				float num2 = 0f;
				for (int j = 0; j < list.Count; j++)
				{
					if (!columnAtMaxWidth[j])
					{
						num2 += Mathf.Max(GetOptimalWidth(list[j]), 1f);
					}
				}
				float num3 = totalAvailableSpaceForColumns - usedWidth;
				flag = false;
				for (int k = 0; k < list.Count; k++)
				{
					if (!columnAtMaxWidth[k])
					{
						float num4 = num3 * Mathf.Max(GetOptimalWidth(list[k]), 1f) / num2;
						float num5 = GetMaxWidth(list[k]) - cachedColumnWidths[k];
						if (num4 >= num5)
						{
							num4 = num5;
							columnAtMaxWidth[k] = true;
						}
						else
						{
							flag = true;
						}
						if (num4 > 0f)
						{
							cachedColumnWidths[k] += num4;
							usedWidth += num4;
						}
					}
				}
				if (usedWidth >= totalAvailableSpaceForColumns - 0.1f)
				{
					return;
				}
				continue;
			}
			Log.Error("Too many iterations.");
			return;
		}
		while (flag);
		DistributeRemainingWidthProportionallyAboveMax(totalAvailableSpaceForColumns - usedWidth);
	}

	private void RecacheRowHeights()
	{
		cachedRowHeights.Clear();
		for (int i = 0; i < cachedThings.Count; i++)
		{
			cachedRowHeights.Add(CalculateRowHeight(cachedThings[i]));
		}
	}

	private void RecacheSize()
	{
		if (hasFixedSize)
		{
			cachedSize = fixedSize;
			return;
		}
		float num = 0f;
		List<ThingColumnDef> list = Columns;
		for (int i = 0; i < list.Count; i++)
		{
			if (!list[i].ignoreWhenCalculatingOptimalTableSize)
			{
				num += GetOptimalWidth(list[i]);
			}
		}
		float a = Mathf.Clamp(num + 16f, minTableWidth, maxTableWidth);
		float a2 = Mathf.Clamp(cachedHeightNoScrollbar, minTableHeight, maxTableHeight);
		a = Mathf.Min(a, UI.screenWidth);
		a2 = Mathf.Min(a2, UI.screenHeight);
		cachedSize = new Vector2(a, a2);
	}

	private void RecacheLookTargets()
	{
		cachedLookTargets.Clear();
		cachedLookTargets.AddRange(cachedThings.Select((Thing p) => new LookTargets(p)));
	}

	private void SubtractProportionally(float toSubtract, float totalUsedWidth)
	{
		for (int i = 0; i < cachedColumnWidths.Count; i++)
		{
			cachedColumnWidths[i] -= toSubtract * cachedColumnWidths[i] / totalUsedWidth;
		}
	}

	private void DistributeRemainingWidthProportionallyAboveMax(float toDistribute)
	{
		float num = 0f;
		List<ThingColumnDef> list = Columns;
		for (int i = 0; i < list.Count; i++)
		{
			num += Mathf.Max(GetOptimalWidth(list[i]), 1f);
		}
		for (int j = 0; j < list.Count; j++)
		{
			cachedColumnWidths[j] += toDistribute * Mathf.Max(GetOptimalWidth(list[j]), 1f) / num;
		}
	}

	private float GetOptimalWidth(ThingColumnDef column)
	{
		return Mathf.Max(column.Worker.GetOptimalWidth(this), 0f);
	}

	private float GetMinWidth(ThingColumnDef column)
	{
		return Mathf.Max(column.Worker.GetMinWidth(this), 0f);
	}

	private float GetMaxWidth(ThingColumnDef column)
	{
		return Mathf.Max(column.Worker.GetMaxWidth(this), 0f);
	}

	private float CalculateRowHeight(Thing thing)
	{
		float num = 0f;
		List<ThingColumnDef> list = Columns;
		for (int i = 0; i < list.Count; i++)
		{
			num = Mathf.Max(num, list[i].Worker.GetMinCellHeight(thing));
		}
		return num;
	}

	private float CalculateHeaderHeight()
	{
		float num = 0f;
		List<ThingColumnDef> list = Columns;
		for (int i = 0; i < list.Count; i++)
		{
			num = Mathf.Max(num, list[i].Worker.GetMinHeaderHeight(this));
		}
		return num;
	}

	private float CalculateTotalRequiredHeight()
	{
		float num = CalculateHeaderHeight();
		for (int i = 0; i < cachedThings.Count; i++)
		{
			num += CalculateRowHeight(cachedThings[i]);
		}
		return num;
	}
}
