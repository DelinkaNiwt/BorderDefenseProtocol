using Verse;

namespace VanillaPsycastsExpanded.Technomancer;

public static class MechUtility
{
	public static bool IsMechAlly(this Pawn mech, Pawn other)
	{
		if (mech.RaceProps.IsMechanoid && MechanitorUtility.IsPlayerOverseerSubject(mech))
		{
			if (other.Faction != mech.Faction)
			{
				if (other.IsColonist)
				{
					return mech.IsColonyMech;
				}
				return false;
			}
			return true;
		}
		return false;
	}
}
