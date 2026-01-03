using RimWorld;
using Verse;

namespace AncotLibrary;

public class QuestPart_SendDialogLetter : QuestPart
{
	public TaggedString label;

	public TaggedString text;

	public LetterDef letterDef;

	public string inSignal;

	public string outSignal;

	public IncidentDef incidentDef;

	public Faction faction;

	public Map map;

	public int incidentDelayTicks;

	public float points;

	public int limitTicks = -1;

	public override void Notify_QuestSignalReceived(Signal signal)
	{
		base.Notify_QuestSignalReceived(signal);
		if (signal.tag == inSignal)
		{
			DialogLetter_Base dialogLetter_Base = (DialogLetter_Base)LetterMaker.MakeLetter(letterDef);
			dialogLetter_Base.Label = label;
			dialogLetter_Base.Text = text;
			dialogLetter_Base.outSignal = outSignal;
			if (limitTicks != -1)
			{
				dialogLetter_Base.StartTimeout(limitTicks);
			}
			Find.LetterStack.ReceiveLetter(dialogLetter_Base);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref inSignal, "inSignal");
		Scribe_Values.Look(ref outSignal, "outSignal");
		Scribe_Values.Look(ref label, "label");
		Scribe_Values.Look(ref text, "text");
		Scribe_Defs.Look(ref letterDef, "letterDef");
		Scribe_References.Look(ref faction, "faction");
		Scribe_Values.Look(ref points, "points", 0f);
		Scribe_Values.Look(ref limitTicks, "limitTicks", -1);
	}
}
