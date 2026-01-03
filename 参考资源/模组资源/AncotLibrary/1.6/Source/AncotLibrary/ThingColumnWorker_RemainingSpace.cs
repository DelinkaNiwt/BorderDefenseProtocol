using UnityEngine;
using Verse;

namespace AncotLibrary;

public class ThingColumnWorker_RemainingSpace : ThingColumnWorker
{
	public override void DoCell(Rect rect, Thing thing, ThingTable table)
	{
	}

	public override int GetMinWidth(ThingTable table)
	{
		return 0;
	}

	public override int GetMaxWidth(ThingTable table)
	{
		return 1000000;
	}

	public override int GetOptimalWidth(ThingTable table)
	{
		return GetMaxWidth(table);
	}

	public override int GetMinCellHeight(Thing thing)
	{
		return 0;
	}
}
