using RimWorld;
using Verse;

namespace Milira;

public class GenStep_BombardmentArea : GenStep
{
	public override int SeedPart => 341121587;

	public override void Generate(Map map, GenStepParams parms)
	{
		MechClusterSketch sketch = MiliraClusterGenerator.GenerateClusterSketch(parms.sitePart.parms.threatPoints, map, startDormant: false);
		IntVec3 loc = IntVec3.Invalid;
		if (MapGenerator.TryGetVar<CellRect>("RectOfInterest", out var var))
		{
			loc = var.CenterCell;
		}
		if (!loc.IsValid)
		{
			Log.Message("!center.IsValid");
			loc = MiliraClusterUtility.FindClusterPosition(map, sketch);
		}
		Bombardment bombardment = (Bombardment)GenSpawn.Spawn(ThingDefOf.Bombardment, loc, map);
		bombardment.duration = 540;
		bombardment.instigator = null;
		bombardment.StartStrike();
	}
}
