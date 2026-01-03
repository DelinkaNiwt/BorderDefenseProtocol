using Verse;

namespace AncotLibrary;

public class Command_ToggleShowRange : Command_Toggle
{
	public float range = 0f;

	public IntVec3 Position;

	public override void GizmoUpdateOnMouseover()
	{
		if (range != 0f && Position.IsValid)
		{
			GenDraw.DrawRadiusRing(Position, range);
		}
	}
}
