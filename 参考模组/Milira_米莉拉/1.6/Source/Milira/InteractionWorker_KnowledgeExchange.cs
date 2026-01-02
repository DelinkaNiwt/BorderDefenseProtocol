using RimWorld;
using Verse;

namespace Milira;

public class InteractionWorker_KnowledgeExchange : InteractionWorker
{
	public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
	{
		if (initiator.IsColonist && initiator.def.defName == "Milira_Race")
		{
			return 0.08f;
		}
		return 0f;
	}
}
