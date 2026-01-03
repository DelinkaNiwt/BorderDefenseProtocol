using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Milira;

public class GenStep_MilianCluster : GenStep
{
	public bool forceNoConditionCauser;

	public int extraRangeToRectOfInterest = 20;

	public bool dormant = true;

	public override int SeedPart => 341174239;

	public override void Generate(Map map, GenStepParams parms)
	{
		MechClusterSketch sketch = MiliraClusterGenerator.GenerateClusterSketch(parms.sitePart.parms.threatPoints, map, dormant, forceNoConditionCauser);
		IntVec3 center = IntVec3.Invalid;
		if (MapGenerator.TryGetVar<CellRect>("RectOfInterest", out var var))
		{
			center = var.ExpandedBy(extraRangeToRectOfInterest).MaxBy((IntVec3 x) => MechClusterUtility.GetClusterPositionScore(x, map, sketch));
		}
		if (!center.IsValid)
		{
			center = MechClusterUtility.FindClusterPosition(map, sketch);
		}
		List<Thing> list = MiliraClusterUtility.SpawnCluster(center, map, sketch, dropInPods: false);
		List<Pawn> list2 = new List<Pawn>();
		foreach (Thing item in list)
		{
			if (item is Pawn)
			{
				list2.Add((Pawn)item);
			}
		}
		if (list2.Any() && dormant)
		{
			GenStep_SleepingMechanoids.SendMechanoidsToSleepImmediately(list2);
		}
	}
}
