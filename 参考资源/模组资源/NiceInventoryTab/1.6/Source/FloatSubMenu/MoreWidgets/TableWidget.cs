using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MoreWidgets;

public class TableWidget<T>
{
	public abstract class Column
	{
		protected virtual string Name => null;

		public virtual Comparison<T> Comparer => null;

		public override string ToString()
		{
			return Name;
		}

		public virtual float MinWidth(List<T> list)
		{
			return Text.CalcSize(Name).x + 4f;
		}

		public virtual float MaxWidth(List<T> list)
		{
			return float.MaxValue;
		}

		public virtual void DoHeader(Rect rect)
		{
			Text.Anchor = TextAnchor.LowerCenter;
			Widgets.Label(rect, Name);
			Text.Anchor = TextAnchor.UpperLeft;
		}

		public abstract void DoCell(Rect rect, T item, int row);

		protected float Max(List<T> list, Func<T, float> f)
		{
			if (!list.Any())
			{
				return 0f;
			}
			return list.Max(f);
		}

		protected float Min(List<T> list, Func<T, float> f)
		{
			if (!list.Any())
			{
				return float.MaxValue;
			}
			return list.Min(f);
		}
	}

	protected class SimpleColumn : Column
	{
		private readonly string name;

		private readonly Action<Rect, T> doCell;

		private readonly Func<T, float> minWidth;

		private readonly Func<T, float> maxWidth;

		private readonly Comparison<T> comparer;

		protected override string Name => name;

		public override Comparison<T> Comparer => comparer;

		public SimpleColumn(string name, Action<Rect, T> doCell, Func<T, float> minWidth = null, Func<T, float> maxWidth = null, Comparison<T> comparer = null)
		{
			this.name = name;
			this.doCell = doCell;
			this.minWidth = minWidth ?? ((Func<T, float>)((T _) => 0f));
			this.maxWidth = maxWidth ?? ((Func<T, float>)((T _) => float.MaxValue));
			this.comparer = comparer;
		}

		public override float MinWidth(List<T> list)
		{
			return Mathf.Max(base.MinWidth(list), Max(list, minWidth));
		}

		public override float MaxWidth(List<T> list)
		{
			return Min(list, maxWidth);
		}

		public override void DoCell(Rect rect, T item, int row)
		{
			doCell(rect, item);
		}
	}

	protected class StringColumn : Column
	{
		private static int nextID = 265498191;

		private readonly int ID;

		private readonly string name;

		private readonly Func<T, string> cellText;

		private readonly Func<T, string> tooltip;

		private readonly Action<T> onClick;

		private readonly Comparison<T> comparer;

		protected override string Name => name;

		public override Comparison<T> Comparer => comparer;

		public StringColumn(string name, Func<T, string> cellText, Func<T, string> tooltip = null, Action<T> onClick = null)
		{
			this.name = name;
			this.cellText = cellText;
			this.tooltip = tooltip;
			this.onClick = onClick;
			comparer = ComparerMethod;
			ID = nextID++;
		}

		public override float MinWidth(List<T> list)
		{
			return Mathf.Max(base.MinWidth(list), Max(list, (T x) => Text.CalcSize(cellText(x)).x));
		}

		public override void DoCell(Rect rect, T item, int row)
		{
			string text = cellText(item);
			Text.Anchor = ((!(Text.CalcSize(text).x > rect.width)) ? TextAnchor.MiddleLeft : TextAnchor.UpperLeft);
			Widgets.Label(rect, text);
			Text.Anchor = TextAnchor.UpperLeft;
			if (tooltip != null && tooltip(item) != null)
			{
				TooltipHandler.TipRegion(rect, () => tooltip(item), ID & (row * 1000));
			}
			if (onClick != null && Widgets.ButtonInvisible(rect))
			{
				onClick(item);
			}
		}

		private int ComparerMethod(T x, T y)
		{
			return cellText(x).CompareTo(cellText(y));
		}
	}

	protected const float Margin = 4f;

	protected readonly List<Column> columns = new List<Column>();

	protected readonly List<float> columnWidths = new List<float>();

	protected readonly List<T> items = new List<T>();

	protected readonly List<T> sorted = new List<T>();

	public Action<Rect, T> onRowHover;

	public Action<T> onRowClick;

	public Action<Rect, int> onColumnHover;

	public Action<int> onColumnClick;

	public Action<int> onHeaderClick;

	public bool highlightRow = true;

	public bool highlightColumn;

	public bool highlightHeader = true;

	public bool sameWidthColumns;

	public bool firstColumnLocked;

	private bool sorting;

	private Column sortBy;

	private bool sortReverse;

	protected bool sortDirty = true;

	protected bool itemsDirty = true;

	protected bool colsDirty = true;

	protected Vector2 scroll;

	private Vector2 lastSize;

	private float headerHeight = 30f;

	private float cellHeight = 24f;

	private float totalWidth;

	public int RowCount => sorted.Count;

	public int ColumnCount => columns.Count;

	public IEnumerable<T> Rows => sorted;

	public IEnumerable<Column> Columns => columns;

	public Vector2 ViewSize => new Vector2(totalWidth, (float)items.Count * (cellHeight + 4f) - 4f);

	public void Resort()
	{
		sortDirty = true;
	}

	public void RecaculateWidths()
	{
		colsDirty = true;
	}

	public virtual void OnGUI(Rect rect)
	{
		if (sortDirty)
		{
			Sort();
		}
		if (colsDirty || rect.size != lastSize)
		{
			UpdateColumnWidths(rect);
			Vector2 vector = ViewSize - rect.size;
			vector.y += headerHeight + 4f;
			scroll.x = Mathf.Max(Mathf.Min(scroll.x, vector.x), 0f);
			scroll.y = Mathf.Max(Mathf.Min(scroll.y, vector.y), 0f);
			lastSize = rect.size;
		}
		Widgets.BeginGroup(rect);
		rect = rect.AtZero();
		DoTable(rect);
		Widgets.EndGroup();
	}

	private void DoTable(Rect rect)
	{
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		int num = -1;
		if (highlightColumn || onColumnHover != null || onColumnClick != null)
		{
			Rect rect2 = rect;
			rect2.x -= scroll.x;
			int i = 0;
			for (int count = columns.Count; i < count; i++)
			{
				rect2.width = columnWidths[i];
				if (Mouse.IsOver(rect2))
				{
					if (highlightColumn)
					{
						Widgets.DrawHighlightIfMouseover(rect2);
					}
					onColumnHover?.Invoke(rect2, i);
					num = i;
				}
				rect2.StepX(4f);
			}
		}
		Rect rect3 = rect.TopPartPixels(headerHeight);
		Widgets.BeginGroup(rect3);
		Rect rect4 = rect3.AtZero();
		rect4.x = 0f - scroll.x;
		rect4.width = totalWidth;
		DoHeaders(rect4);
		Widgets.EndGroup();
		rect.yMin += headerHeight + 4f;
		_ = items.Count;
		_ = cellHeight;
		Rect rect5 = new Rect(default(Vector2), ViewSize);
		Widgets.BeginScrollView(rect, ref scroll, rect5);
		Rect visible = new Rect(scroll, rect.size);
		rect3 = rect5.TopPartPixels(cellHeight);
		foreach (T item in sorted)
		{
			if (visible.Overlaps(rect3))
			{
				bool flag = Mouse.IsOver(rect3);
				if (highlightRow && flag)
				{
					Widgets.DrawHighlight(rect3);
				}
				if (onRowHover != null && flag)
				{
					onRowHover(rect3, item);
				}
				DoCells(rect3, item, visible);
				if (onRowClick != null && Widgets.ButtonInvisible(rect3, doMouseoverSound: false))
				{
					onRowClick(item);
				}
			}
			rect3.StepY(4f);
		}
		if (num >= 0 && (int)Event.current.type == 0)
		{
			onColumnClick?.Invoke(num);
		}
		Widgets.EndScrollView();
	}

	private void UpdateColumnWidths(Rect rect)
	{
		columnWidths.Clear();
		columnWidths.AddRange(columns.Select((Column x) => x.MinWidth(sorted)));
		if (sameWidthColumns && columns.Count > 0)
		{
			float value = columnWidths.Max();
			int num = 0;
			for (int count = columnWidths.Count; num < count; num++)
			{
				columnWidths[num] = value;
			}
		}
		totalWidth = rect.width - 18f;
		float num2 = (float)(columnWidths.Count - 1) * 4f;
		float num3 = columnWidths.Sum() + num2;
		if (num3 > totalWidth)
		{
			totalWidth = num3;
		}
		else
		{
			int count2 = columns.Count;
			float num4 = Mathf.Min((totalWidth - num3) / (float)count2, 16f);
			for (int num5 = 0; num5 < count2; num5++)
			{
				columnWidths[num5] = Mathf.Min(columnWidths[num5] + num4, columns[num5].MaxWidth(sorted));
			}
			totalWidth = columnWidths.Sum() + num2;
		}
		colsDirty = false;
	}

	private void DoHeaders(Rect rect)
	{
		int i = 0;
		for (int count = columns.Count; i < count; i++)
		{
			Column column = columns[i];
			rect.width = columnWidths[i];
			HeaderButton(rect, i, onHeaderClick, column.Comparer != null);
			Widgets.BeginGroup(rect);
			column.DoHeader(rect.AtZero());
			Widgets.EndGroup();
			rect.StepX(4f);
		}
	}

	private void DoCells(Rect rect, T item, Rect visible)
	{
		int i = 0;
		for (int count = columns.Count; i < count; i++)
		{
			Column column = columns[i];
			rect.width = columnWidths[i];
			if (visible.Overlaps(rect))
			{
				Widgets.BeginGroup(rect);
				column.DoCell(rect.AtZero(), item, i);
				Widgets.EndGroup();
			}
			rect.StepX(4f);
		}
	}

	public void AddColumn(Column c)
	{
		columns.Add(c);
		colsDirty = true;
	}

	public Column AddColumn(string name, Action<Rect, T> doCell, Func<T, float> minWidth = null, Func<T, float> maxWidth = null, Comparison<T> comparer = null)
	{
		SimpleColumn simpleColumn = new SimpleColumn(name, doCell, minWidth, maxWidth, comparer);
		AddColumn(simpleColumn);
		return simpleColumn;
	}

	public Column AddColumn(string name, Action<Rect, T> doCell, float minWidth, float maxWidth = -1f, Comparison<T> comparer = null)
	{
		return AddColumn(name, doCell, (T _) => minWidth, (maxWidth >= minWidth) ? ((Func<T, float>)((T _) => maxWidth)) : null, comparer);
	}

	public Column AddColumn(string name, Func<T, string> cellText, Func<T, string> tooltip = null, Action<T> onClick = null)
	{
		StringColumn stringColumn = new StringColumn(name, cellText, tooltip, onClick);
		AddColumn(stringColumn);
		return stringColumn;
	}

	public void SetColumn(int i, Column c)
	{
		columns[i] = c;
		colsDirty = true;
	}

	public void RemoveColumn(Column c)
	{
		columns.Remove(c);
		colsDirty = true;
	}

	public void RemoveColumn(int i)
	{
		columns.RemoveAt(i);
		colsDirty = true;
	}

	public void ClearColumns()
	{
		columns.Clear();
		colsDirty = true;
	}

	public void SetItems(IEnumerable<T> items)
	{
		this.items.Clear();
		this.items.AddRange(items);
		itemsDirty = true;
		sortDirty = true;
	}

	protected void HeaderButton(Rect rect, int cur, Action<int> onClick, bool hasSort)
	{
		if (highlightHeader)
		{
			Widgets.DrawHighlightIfMouseover(rect);
		}
		if (Widgets.ButtonInvisible(rect))
		{
			if (hasSort && Event.current.button == 0)
			{
				HeaderClicked(columns[cur], ref sorting, ref sortBy, ref sortReverse);
			}
			else
			{
				onClick?.Invoke(cur);
			}
		}
	}

	protected void HeaderClicked<S>(S cur, ref bool sorting, ref S sortBy, ref bool sortReverse)
	{
		if (sorting && cur.Equals(sortBy))
		{
			if (sortReverse)
			{
				sorting = false;
				sortBy = default(S);
			}
			else
			{
				sortReverse = true;
			}
		}
		else
		{
			sorting = true;
			sortBy = cur;
			sortReverse = false;
		}
		sortDirty = true;
	}

	protected virtual void Sort()
	{
		if (sorting && (!columns.Contains(sortBy) || sortBy?.Comparer == null))
		{
			sorting = false;
			sortReverse = false;
			sortBy = null;
		}
		if (itemsDirty || !sorting)
		{
			sorted.Clear();
			sorted.AddRange(items);
			itemsDirty = false;
		}
		if (sorting)
		{
			Sort(sorted, sortBy.Comparer, sortReverse);
		}
		sortDirty = false;
	}

	protected static void Sort<S>(List<S> list, Comparison<S> cmp, bool reverse)
	{
		if (reverse)
		{
			Comparison<S> normal = cmp;
			cmp = (S x, S y) => -normal(x, y);
		}
		list.Sort(cmp);
	}
}
