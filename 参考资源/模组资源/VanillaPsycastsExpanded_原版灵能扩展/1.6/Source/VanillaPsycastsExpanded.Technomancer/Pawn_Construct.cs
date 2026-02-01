using RimWorld.Planet;
using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public class Pawn_Construct : Pawn, IMinHeatGiver, ILoadReferenceable
{
	public bool IsActive
	{
		get
		{
			if (!base.Spawned)
			{
				return this.GetCaravan() != null;
			}
			return true;
		}
	}

	public int MinHeat => 20;
}
