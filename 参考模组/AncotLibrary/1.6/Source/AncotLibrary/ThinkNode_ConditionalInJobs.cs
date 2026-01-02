using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class ThinkNode_ConditionalInJobs : ThinkNode_Conditional
{
	public List<JobDef> jobs;

	protected override bool Satisfied(Pawn pawn)
	{
		if (jobs.Contains(pawn.CurJobDef))
		{
			return true;
		}
		return false;
	}
}
