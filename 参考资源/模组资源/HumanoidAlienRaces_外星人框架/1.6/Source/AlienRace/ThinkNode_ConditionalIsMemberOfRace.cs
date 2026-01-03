using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;
using Verse.AI;

namespace AlienRace;

[UsedImplicitly]
public class ThinkNode_ConditionalIsMemberOfRace : ThinkNode_Conditional
{
	public List<ThingDef> races;

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		ThinkNode_ConditionalIsMemberOfRace obj = (ThinkNode_ConditionalIsMemberOfRace)base.DeepCopy(resolve);
		obj.races = new List<ThingDef>(races);
		return obj;
	}

	protected override bool Satisfied(Pawn pawn)
	{
		return races.Contains(pawn.def);
	}
}
