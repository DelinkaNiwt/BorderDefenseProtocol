using RimWorld;
using Verse;

namespace AncotLibrary;

public class GenStep_Signal_FactionChange : GenStep_Signal
{
	public FactionDef factionOriginal;

	public FactionDef factionTargeted;

	public ThoughtDef thoughtAddToPawn;

	public string dialogText;

	public bool becomePlayer = false;

	public bool draftPawn = false;

	public bool pauseForced = false;

	public override int SeedPart => 432167231;

	protected override SignalAction MakeSignalAction(CellRect rectToDefend, IntVec3 root, GenStepParams parms)
	{
		SignalAction_FactionChange signalAction_FactionChange = (SignalAction_FactionChange)ThingMaker.MakeThing(AncotDefOf.Ancot_SignalAction_FactionChange);
		signalAction_FactionChange.factionOriginalDef = factionOriginal;
		signalAction_FactionChange.factionTargetedDef = factionTargeted;
		signalAction_FactionChange.thoughtAddToPawn = thoughtAddToPawn;
		signalAction_FactionChange.becomePlayer = becomePlayer;
		signalAction_FactionChange.draftPawn = draftPawn;
		signalAction_FactionChange.dialogText = dialogText;
		signalAction_FactionChange.pauseForced = pauseForced;
		return signalAction_FactionChange;
	}
}
