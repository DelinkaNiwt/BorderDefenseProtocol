using System.Linq;
using Verse;
using Verse.AI;

namespace Milira;

public class ThinkNode_ConditionalBodyPartAvailable : ThinkNode_Conditional
{
	private string requiredBodypartdefName;

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		ThinkNode_ConditionalBodyPartAvailable thinkNode_ConditionalBodyPartAvailable = (ThinkNode_ConditionalBodyPartAvailable)base.DeepCopy(resolve);
		thinkNode_ConditionalBodyPartAvailable.requiredBodypartdefName = requiredBodypartdefName;
		return thinkNode_ConditionalBodyPartAvailable;
	}

	protected override bool Satisfied(Pawn pawn)
	{
		BodyPartRecord bodyPartRecord = pawn.RaceProps.body.AllParts.FirstOrDefault((BodyPartRecord bpr) => bpr.def.defName == requiredBodypartdefName);
		if (bodyPartRecord != null)
		{
			return pawn.health.hediffSet.GetNotMissingParts().Contains(bodyPartRecord);
		}
		return true;
	}
}
