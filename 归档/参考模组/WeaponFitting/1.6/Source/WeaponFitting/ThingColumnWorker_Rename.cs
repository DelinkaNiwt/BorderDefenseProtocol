using AncotLibrary;
using UnityEngine;
using Verse;

namespace WeaponFitting;

public class ThingColumnWorker_Rename : ThingColumnWorker
{
	public override void DoCell(Rect rect, Thing thing, ThingTable table)
	{
		WF_Utility.DrawRenameButton(rect, thing);
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
