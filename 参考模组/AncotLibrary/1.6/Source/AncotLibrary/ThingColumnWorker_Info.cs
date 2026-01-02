using UnityEngine;
using Verse;

namespace AncotLibrary;

[StaticConstructorOnStartup]
public class ThingColumnWorker_Info : ThingColumnWorker
{
	public override void DoCell(Rect rect, Thing thing, ThingTable table)
	{
		Widgets.InfoCardButtonCentered(rect, thing);
	}

	public override int GetMinWidth(ThingTable table)
	{
		return 24;
	}

	public override int GetMaxWidth(ThingTable table)
	{
		return 24;
	}
}
