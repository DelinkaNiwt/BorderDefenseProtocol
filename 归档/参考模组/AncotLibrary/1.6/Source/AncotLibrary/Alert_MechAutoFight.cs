using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class Alert_MechAutoFight : Alert
{
	public static List<Pawn> Targets = new List<Pawn>();

	public Alert_MechAutoFight()
	{
		defaultLabel = "Ancot.Alert_MechAutoFight".Translate();
		requireBiotech = true;
	}

	public override AlertReport GetReport()
	{
		return AlertReport.CulpritsAre(Targets);
	}

	public override TaggedString GetExplanation()
	{
		return "Ancot.Alert_MechAutoFightDescPrefix".Translate() + ":\n" + Targets.Select((Pawn p) => p.LabelCap).ToLineList("  - ") + "\n\n" + "Ancot.Alert_MechAutoFightDesc".Translate();
	}

	public static void ClearCache()
	{
		Targets.Clear();
	}
}
