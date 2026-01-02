using Verse;

namespace AncotLibrary;

public class CompRenderSetDirty : ThingComp
{
	public CompProperties_RenderSetDirty Props => (CompProperties_RenderSetDirty)props;

	public override void Notify_Equipped(Pawn pawn)
	{
		pawn.Drawer.renderer.renderTree.SetDirty();
	}

	public override void Notify_Unequipped(Pawn pawn)
	{
		pawn.Drawer.renderer.renderTree.SetDirty();
	}
}
