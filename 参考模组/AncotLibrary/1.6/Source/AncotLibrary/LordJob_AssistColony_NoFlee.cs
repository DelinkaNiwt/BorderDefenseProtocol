using RimWorld;
using Verse;

namespace AncotLibrary;

public class LordJob_AssistColony_NoFlee : LordJob_AssistColony
{
	public override bool AddFleeToil => false;

	public LordJob_AssistColony_NoFlee()
	{
	}

	public LordJob_AssistColony_NoFlee(Faction faction, IntVec3 defendPoint)
		: base(faction, defendPoint)
	{
	}
}
