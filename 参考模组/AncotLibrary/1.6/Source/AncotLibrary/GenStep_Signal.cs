using RimWorld;
using Verse;

namespace AncotLibrary;

public abstract class GenStep_Signal : GenStep
{
	public int expandedCell = 10;

	public float? expandedCellPctOfInterestRect;

	public bool triggerUnfogged = false;

	public string singalTag = "AncotSingalTriggered";

	public override void Generate(Map map, GenStepParams parms)
	{
		if (SiteGenStepUtility.TryFindRootToSpawnAroundRectOfInterest(out var rectToDefend, out var singleCellToSpawnNear, map))
		{
			SpawnTrigger(rectToDefend, singleCellToSpawnNear, map, parms);
		}
	}

	private void SpawnTrigger(CellRect rectToDefend, IntVec3 root, Map map, GenStepParams parms)
	{
		string signalTag = singalTag + Find.UniqueIDsManager.GetNextSignalTagID();
		if (expandedCellPctOfInterestRect.HasValue)
		{
			expandedCell = (int)((float)rectToDefend.Size.x * expandedCellPctOfInterestRect).Value;
		}
		CellRect rect = ((!root.IsValid) ? rectToDefend.ExpandedBy(expandedCell) : CellRect.CenteredOn(root, 17));
		SignalAction signalAction = MakeSignalAction(rectToDefend, root, parms);
		signalAction.signalTag = signalTag;
		GenSpawn.Spawn(signalAction, rect.CenterCell, map);
		RectTrigger rectTrigger = MakeRectTrigger();
		rectTrigger.signalTag = signalTag;
		rectTrigger.Rect = rect;
		GenSpawn.Spawn(rectTrigger, rect.CenterCell, map);
		if (this.triggerUnfogged)
		{
			TriggerUnfogged triggerUnfogged = (TriggerUnfogged)ThingMaker.MakeThing(ThingDefOf.TriggerUnfogged);
			triggerUnfogged.signalTag = signalTag;
			GenSpawn.Spawn(triggerUnfogged, rect.CenterCell, map);
		}
	}

	protected abstract SignalAction MakeSignalAction(CellRect rectToDefend, IntVec3 root, GenStepParams parms);

	protected virtual RectTrigger MakeRectTrigger()
	{
		return (RectTrigger)ThingMaker.MakeThing(ThingDefOf.RectTrigger);
	}
}
