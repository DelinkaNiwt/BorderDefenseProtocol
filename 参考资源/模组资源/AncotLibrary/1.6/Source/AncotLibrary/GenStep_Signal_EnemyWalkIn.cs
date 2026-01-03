using RimWorld;
using Verse;

namespace AncotLibrary;

public class GenStep_Signal_EnemyWalkIn : GenStep_Signal
{
	public FactionDef factionDef;

	public Faction faction;

	public float points_WalkIn = 1000f;

	public bool canTimeoutOrFlee_WalkIn = false;

	public override int SeedPart => 432167231;

	protected override SignalAction MakeSignalAction(CellRect rectToDefend, IntVec3 root, GenStepParams parms)
	{
		SignalAction_EnemyWalkIn signalAction_EnemyWalkIn = (SignalAction_EnemyWalkIn)ThingMaker.MakeThing(AncotDefOf.Ancot_SignalAction_EnemyWalkIn);
		signalAction_EnemyWalkIn.enemyFaction = faction ?? Find.FactionManager.FirstFactionOfDef(factionDef);
		signalAction_EnemyWalkIn.parms = parms;
		signalAction_EnemyWalkIn.points_WalkIn = points_WalkIn;
		signalAction_EnemyWalkIn.canTimeoutOrFlee_WalkIn = canTimeoutOrFlee_WalkIn;
		return signalAction_EnemyWalkIn;
	}
}
