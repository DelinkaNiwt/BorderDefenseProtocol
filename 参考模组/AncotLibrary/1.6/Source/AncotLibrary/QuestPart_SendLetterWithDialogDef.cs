using RimWorld;
using Verse;

namespace AncotLibrary;

public class QuestPart_SendLetterWithDialogDef : QuestPart
{
	public TaggedString label;

	public TaggedString text;

	public LetterDef letterDef;

	public DialogDef dialogDef;

	public string inSignal;

	public string outSignal;

	public string optionSignalA;

	public string optionSignalB;

	public string optionSignalC;

	public string optionSignalD;

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
			DialogLetter_DialogDef dialogLetter_DialogDef = (DialogLetter_DialogDef)LetterMaker.MakeLetter(letterDef);
			dialogLetter_DialogDef.Label = label;
			dialogLetter_DialogDef.Text = text;
			dialogLetter_DialogDef.dialogDef = dialogDef;
			dialogLetter_DialogDef.outSignal = outSignal;
			dialogLetter_DialogDef.quest = quest;
			dialogLetter_DialogDef.optionSignalA = optionSignalA;
			dialogLetter_DialogDef.optionSignalB = optionSignalB;
			dialogLetter_DialogDef.optionSignalC = optionSignalC;
			dialogLetter_DialogDef.optionSignalD = optionSignalD;
			if (limitTicks != -1)
			{
				dialogLetter_DialogDef.StartTimeout(limitTicks);
			}
			Find.LetterStack.ReceiveLetter(dialogLetter_DialogDef);
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
		Scribe_Defs.Look(ref dialogDef, "dialogDef");
		Scribe_References.Look(ref faction, "faction");
		Scribe_Values.Look(ref points, "points", 0f);
		Scribe_Values.Look(ref limitTicks, "limitTicks", -1);
		Scribe_Values.Look(ref optionSignalA, "optionSignalA");
		Scribe_Values.Look(ref optionSignalB, "optionSignalB");
		Scribe_Values.Look(ref optionSignalC, "optionSignalC");
		Scribe_Values.Look(ref optionSignalD, "optionSignalD");
		Scribe_Defs.Look(ref incidentDef, "incidentDef");
		Scribe_References.Look(ref map, "map");
		Scribe_Values.Look(ref incidentDelayTicks, "incidentDelayTicks", 0);
	}
}
