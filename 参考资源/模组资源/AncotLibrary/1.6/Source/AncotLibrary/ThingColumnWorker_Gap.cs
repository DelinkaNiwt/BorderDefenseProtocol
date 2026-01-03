using UnityEngine;
using Verse;

namespace AncotLibrary;

public class ThingColumnWorker_Gap : ThingColumnWorker
{
	protected virtual int Width => def.gap;

	public override void DoCell(Rect rect, Thing thing, ThingTable table)
	{
	}

	public override int GetMinWidth(ThingTable table)
	{
		return Mathf.Max(base.GetMinWidth(table), Width);
	}

	public override int GetMaxWidth(ThingTable table)
	{
		return Mathf.Min(base.GetMaxWidth(table), Width);
	}

	public override int GetMinCellHeight(Thing thing)
	{
		return 0;
	}
}
