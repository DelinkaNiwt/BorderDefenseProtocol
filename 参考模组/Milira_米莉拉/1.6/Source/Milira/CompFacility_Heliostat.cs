using RimWorld;

namespace Milira;

public class CompFacility_Heliostat : CompFacility
{
	public override bool CanBeActive
	{
		get
		{
			if (parent.Map.roofGrid.Roofed(parent.Position))
			{
				return false;
			}
			return true;
		}
	}
}
