using RimWorld;
using Verse;

namespace AncotLibrary;

public class WorldObjectCompProperties_AssignableToPawn_Caravan : WorldObjectCompProperties
{
	public int maxAssignedPawnsCount = 1;

	[MustTranslate]
	public string noAssignablePawnsDesc;

	[MustTranslate]
	public string assignmentGizmoDesc;

	public WorldObjectCompProperties_AssignableToPawn_Caravan()
	{
		compClass = typeof(WorldObjectComp_AssignableToPawn_Caravan);
	}
}
