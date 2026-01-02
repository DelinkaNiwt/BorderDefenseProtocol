using System.Collections.Generic;
using FCI_Milira;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AncotLibrary;

public class SignalAction_EnemyWalkIn : SignalAction
{
	public Faction enemyFaction;

	public GenStepParams parms;

	public float points_WalkIn;

	public bool canTimeoutOrFlee_WalkIn;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref enemyFaction, "enemyFaction");
		Scribe_Values.Look(ref points_WalkIn, "points_WalkIn", 0f);
		Scribe_Values.Look(ref canTimeoutOrFlee_WalkIn, "canTimeoutOrFlee_WalkIn", defaultValue: false);
		Scribe_Values.Look(ref parms, "parms");
	}

	protected override void DoAction(SignalArgs args)
	{
		if (enemyFaction == null)
		{
			return;
		}
		Map map = base.Map;
		if (map == null)
		{
			return;
		}
		List<Pawn> list = new List<Pawn>();
		IntVec3 root = AreaFinder.FindNearEdgeCell(map, null);
		foreach (Pawn item in AncotPawnGenUtility.GeneratePawnGroup(parms, map, enemyFaction, points_WalkIn))
		{
			GenSpawn.Spawn(item, CellFinder.RandomSpawnCellForPawnNear(root, map), map);
			list.Add(item);
		}
		LordMaker.MakeNewLord(enemyFaction, new LordJob_AssaultColony(enemyFaction, canKidnap: true, canTimeoutOrFlee_WalkIn, sappers: false, useAvoidGridSmart: false, canSteal: false), map, list);
	}
}
