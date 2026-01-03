using System.Linq;
using Verse;

namespace Milira;

public class CompGainHediffSpawn : ThingComp
{
	private CompProperties_GainHediffSpawn Props => (CompProperties_GainHediffSpawn)props;

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (parent is Pawn { Dead: false } pawn)
		{
			Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
			BodyPartRecord bodyPartRecord = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def.defName == "Milian_Brain");
			if (firstHediffOfDef == null && bodyPartRecord != null)
			{
				firstHediffOfDef = pawn.health.AddHediff(Props.hediffDef, bodyPartRecord);
				firstHediffOfDef.Severity = 0.1f;
			}
		}
	}
}
