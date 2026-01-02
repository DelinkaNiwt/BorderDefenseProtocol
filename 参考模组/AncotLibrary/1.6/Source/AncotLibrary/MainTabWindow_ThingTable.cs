using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncotLibrary;

public abstract class MainTabWindow_ThingTable : MainTabWindow
{
	private ThingTable table;

	protected virtual float ExtraBottomSpace => 53f;

	protected virtual float ExtraTopSpace => 0f;

	protected abstract ThingTableDef ThingTableDef { get; }

	protected override float Margin => 6f;

	public override Vector2 RequestedTabSize
	{
		get
		{
			if (table == null)
			{
				return Vector2.zero;
			}
			return new Vector2(table.Size.x + Margin * 2f, table.Size.y + ExtraBottomSpace + ExtraTopSpace + Margin * 2f);
		}
	}

	protected abstract IEnumerable<Thing> Things { get; }

	public override void PostOpen()
	{
		base.PostOpen();
		if (table == null)
		{
			table = CreateTable();
		}
		SetDirty();
	}

	public override void DoWindowContents(Rect rect)
	{
		table.PawnTableOnGUI(new Vector2(rect.x, rect.y + ExtraTopSpace));
	}

	public void Notify_PawnsChanged()
	{
		SetDirty();
	}

	public override void Notify_ResolutionChanged()
	{
		table = CreateTable();
		base.Notify_ResolutionChanged();
	}

	private ThingTable CreateTable()
	{
		return (ThingTable)Activator.CreateInstance(ThingTableDef.workerClass, ThingTableDef, (Func<IEnumerable<Thing>>)(() => Things), UI.screenWidth - (int)(Margin * 2f), (int)((float)(UI.screenHeight - 35) - ExtraBottomSpace - ExtraTopSpace - Margin * 2f));
	}

	protected void SetDirty()
	{
		table.SetDirty();
		SetInitialSizeAndPosition();
	}
}
