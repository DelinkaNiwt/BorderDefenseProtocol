using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AncotLibrary;

public class JobGiver_AISwitchCommandSortie : ThinkNode_JobGiver
{
	public bool swithTo;

	protected override Job TryGiveJob(Pawn pawn)
	{
		CompCommandPivot compCommandPivot = pawn.TryGetComp<CompCommandPivot>();
		CompMechCarrier_Custom compMechCarrier_Custom = pawn.TryGetComp<CompMechCarrier_Custom>();
		List<Pawn> spawnedPawns = compMechCarrier_Custom.spawnedPawns;
		if (compCommandPivot != null && compCommandPivot.sortie != swithTo)
		{
			compCommandPivot.sortie = swithTo;
			for (int i = 0; i < spawnedPawns.Count; i++)
			{
				CompCommandTerminal compCommandTerminal = spawnedPawns[i].TryGetComp<CompCommandTerminal>();
				if (compCommandTerminal != null)
				{
					compCommandTerminal.sortie_Terminal = swithTo;
				}
			}
		}
		return null;
	}
}
