using RimWorld;
using Verse;

namespace Milira;

public class SitePartWorker_ChurchBombardment : SitePartWorker
{
	public override void PostMapGenerate(Map map)
	{
		IntVec3 intVec = IntVec3.Invalid;
		if (MapGenerator.TryGetVar<CellRect>("RectOfInterest", out var var))
		{
			intVec = var.RandomCell;
		}
		if (intVec.IsValid)
		{
		}
	}
}
