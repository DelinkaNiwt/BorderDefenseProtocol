using Verse;

namespace Milira;

public class CompProperties_SwitchResonate : CompProperties
{
	public int resonateLevel = 1;

	public int checkInterval = 180;

	public bool displayGizmoWhileUndrafted = false;

	public string gizmoIconPath_Pawn = "Milira/Effect/Promotion/Promotion_Pawn";

	public string gizmoIconPath_Knight = "Milira/Effect/Promotion/Promotion_Knight";

	public string gizmoIconPath_Bishop = "Milira/Effect/Promotion/Promotion_Bishop";

	public string gizmoIconPath_Rook = "Milira/Effect/Promotion/Promotion_Rook";

	public string gizmoIconPath_King = "Milira/Effect/Promotion/Promotion_King";

	public CompProperties_SwitchResonate()
	{
		compClass = typeof(CompSwitchResonate);
	}
}
