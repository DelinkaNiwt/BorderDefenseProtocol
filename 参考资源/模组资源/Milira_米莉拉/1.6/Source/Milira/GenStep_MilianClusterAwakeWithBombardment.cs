using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Milira;

public class GenStep_MilianClusterAwakeWithBombardment : GenStep
{
	public bool forceNoConditionCauser;

	public int extraRangeToRectOfInterest = 20;

	private IntVec3 nextBombardmentCell = IntVec3.Invalid;

	public static readonly SimpleCurve BombardmentDistanceChanceFactor = new SimpleCurve
	{
		new CurvePoint(0f, 0f),
		new CurvePoint(0.1f, 0f),
		new CurvePoint(0.4f, 0.9f),
		new CurvePoint(0.6f, 0.9f),
		new CurvePoint(1f, 0.2f)
	};

	public override int SeedPart => 341174079;

	public override void Generate(Map map, GenStepParams parms)
	{
		MechClusterSketch sketch = MiliraClusterGenerator.GenerateClusterSketch(parms.sitePart.parms.threatPoints, map, startDormant: false, forceNoConditionCauser);
		IntVec3 center = IntVec3.Invalid;
		if (MapGenerator.TryGetVar<CellRect>("RectOfInterest", out var var))
		{
			center = var.ExpandedBy(extraRangeToRectOfInterest).MaxBy((IntVec3 x) => MechClusterUtility.GetClusterPositionScore(x, map, sketch));
		}
		if (!center.IsValid)
		{
			center = MiliraClusterUtility.FindClusterPosition(map, sketch);
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
		for (int num = 0; num < 40; num++)
		{
			GetBombardmentnCell(map, center, 22f);
			SkyfallerMaker.SpawnSkyfaller(MiliraDefOf.Milira_ChurchBombardmentI, nextBombardmentCell, map);
		}
		for (int num2 = 0; num2 < 40; num2++)
		{
			GetBombardmentnCell(map, center, 22f);
			SkyfallerMaker.SpawnSkyfaller(MiliraDefOf.Milira_ChurchBombardmentII, nextBombardmentCell, map);
		}
		for (int num3 = 0; num3 < 40; num3++)
		{
			GetBombardmentnCell(map, center, 22f);
			SkyfallerMaker.SpawnSkyfaller(MiliraDefOf.Milira_ChurchBombardmentIII, nextBombardmentCell, map);
		}
	}

	private void GetBombardmentnCell(Map map, IntVec3 center, float areaRadius)
	{
		nextBombardmentCell = (from x in GenRadial.RadialCellsAround(center, areaRadius, useCenter: true)
			where x.InBounds(map)
			select x).RandomElementByWeight((IntVec3 x) => BombardmentDistanceChanceFactor.Evaluate(x.DistanceTo(center) / areaRadius));
	}
}
