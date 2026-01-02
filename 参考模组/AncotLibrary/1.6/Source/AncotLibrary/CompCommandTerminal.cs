using Verse;

namespace AncotLibrary;

public class CompCommandTerminal : ThingComp
{
	public bool sortie_Terminal;

	public Pawn pivot;

	private Pawn pawn => parent as Pawn;

	public CompMechCarrier_Custom compMechCarrier_Custom => pawn.TryGetComp<CompMechCarrier_Custom>();

	public CompProperties_CommandTerminal Props => (CompProperties_CommandTerminal)props;

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref sortie_Terminal, "sortie_Terminal", defaultValue: false);
		Scribe_References.Look(ref pivot, "pivot", saveDestroyedThings: true);
	}
}
