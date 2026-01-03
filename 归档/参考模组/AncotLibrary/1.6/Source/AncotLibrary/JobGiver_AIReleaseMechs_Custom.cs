using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_AIReleaseMechs_Custom : ThinkNode_JobGiver
{
	public JobDef jobDef;

	protected override Job TryGiveJob(Pawn pawn)
	{
		CompMechCarrier_Custom compMechCarrier_Custom = pawn.TryGetComp<CompMechCarrier_Custom>();
		if (compMechCarrier_Custom != null && (bool)compMechCarrier_Custom.CanSpawn)
		{
			return JobMaker.MakeJob(jobDef);
		}
		return null;
	}
}
