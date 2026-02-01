using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MoreWidgets;

public class Table2DWidget<TRow, TColumn> : TableWidget<TRow>
{
	private class ItemColumn : Column
	{
		private readonly Table2DWidget<TRow, TColumn> table;

		public readonly TColumn col;

		protected override string Name => table.columnLabel(col);

		public override Comparison<TRow> Comparer
		{
			get
			{
				if (table.rowComparer != null)
				{
					return CompareFunc;
				}
				return null;
			}
		}

		public ItemColumn(Table2DWidget<TRow, TColumn> table, TColumn col)
		{
			this.table = table;
			this.col = col;
		}

		public override bool Equals(object obj)
		{
			if (obj is ItemColumn itemColumn && (object)col == (object)itemColumn.col)
			{
				return table == itemColumn.table;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return col.GetHashCode() ^ table.GetHashCode();
		}

		public override float MinWidth(List<TRow> list)
		{
			return Mathf.Max(base.MinWidth(list), ((IEnumerable<TRow>)list).Max((Func<TRow, float>)MinCellWidth));
		}

		public override float MaxWidth(List<TRow> list)
		{
			return Mathf.Min(base.MaxWidth(list), ((IEnumerable<TRow>)list).Max((Func<TRow, float>)MaxCellWidth));
		}

		public override void DoCell(Rect rect, TRow row, int _)
		{
			table.doCell?.Invoke(rect, row, col);
		}

		private int CompareFunc(TRow x, TRow y)
		{
			return table.rowComparer(x, y, col);
		}

		private float MinCellWidth(TRow row)
		{
			return table.cellMinWidth?.Invoke(row, col) ?? 0f;
		}

		private float MaxCellWidth(TRow row)
		{
			return table.cellMaxWidth?.Invoke(row, col) ?? float.MaxValue;
		}
	}

	public Func<TRow, string> rowLabel;

	public Func<TColumn, string> columnLabel;

	public Action<Rect, TRow, TColumn> doCell;

	public Func<TRow, TColumn, float> cellMinWidth;

	public Func<TRow, TColumn, float> cellMaxWidth;

	public Func<TRow, TRow, TColumn, int> rowComparer;

	public Func<TColumn, TColumn, TRow, int> columnComparer;

	public new Action<TColumn> onHeaderClick;

	public new Action<TColumn> onColumnClick;

	public new Action<Rect, TColumn> onColumnHover;

	public Action<TRow> onRowLabelClick;

	public new bool highlightColumn;

	private readonly List<TColumn> columnItems = new List<TColumn>();

	private readonly List<TColumn> columnsSorted = new List<TColumn>();

	private bool sortingCol;

	private TRow sortColBy;

	private bool sortColReverse;

	public Table2DWidget()
	{
		firstColumnLocked = true;
	}

	public void SetRowItems(IEnumerable<TRow> rows)
	{
		SetItems(rows);
	}

	public void SetColumnItems(IEnumerable<TColumn> columns)
	{
		columnItems.Clear();
		columnItems.AddRange(columns);
		colsDirty = true;
		sortDirty = true;
	}

	public override void OnGUI(Rect rect)
	{
		if (base.onHeaderClick == null && onHeaderClick != null)
		{
			base.onHeaderClick = delegate(int col)
			{
				onHeaderClick?.Invoke(columnItems[col]);
			};
		}
		if (base.onHeaderClick == null && onHeaderClick != null)
		{
			base.onHeaderClick = delegate(int col)
			{
				onHeaderClick?.Invoke(columnItems[col]);
			};
		}
		if (base.onColumnHover == null && (highlightColumn || onColumnHover != null))
		{
			base.onColumnHover = OnColumnHover;
		}
		base.OnGUI(rect);
	}

	protected override void Sort()
	{
		bool flag = sortingCol && columnComparer != null;
		if (colsDirty || !flag)
		{
			columnsSorted.Clear();
			columnsSorted.AddRange(columnItems);
		}
		if (flag)
		{
			TableWidget<TRow>.Sort(columnsSorted, (TColumn a, TColumn b) => columnComparer(a, b, sortColBy), sortColReverse);
		}
		if (colsDirty || flag)
		{
			UpdateColumns();
		}
		base.Sort();
	}

	private void UpdateColumns()
	{
		ClearColumns();
		AddColumn("", rowLabel, null, OnRowLabelClick);
		foreach (TColumn item in columnsSorted)
		{
			AddColumn(new ItemColumn(this, item));
		}
	}

	private void OnColumnHover(Rect rect, int col)
	{
		if (col != 0)
		{
			onColumnHover?.Invoke(rect, columnItems[col - 1]);
			if (highlightColumn)
			{
				Widgets.DrawHighlight(rect);
			}
		}
	}

	private void OnRowLabelClick(TRow row)
	{
		if (columnComparer != null && Event.current.button == 0)
		{
			HeaderClicked(row, ref sortingCol, ref sortColBy, ref sortColReverse);
		}
		else
		{
			onRowLabelClick?.Invoke(row);
		}
	}
}
