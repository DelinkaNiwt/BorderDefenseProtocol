using Verse;

namespace AncotLibrary;

public class PawnRenderNode_AlternateWeapon : PawnRenderNode
{
	public HediffComp_AlternateWeapon compAlternateWeapon;

	public PawnRenderNode_AlternateWeapon(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
	}

	public override Graphic GraphicFor(Pawn pawn)
	{
		if (compAlternateWeapon == null)
		{
			compAlternateWeapon = hediff.TryGetComp<HediffComp_AlternateWeapon>();
		}
		if (compAlternateWeapon != null && !compAlternateWeapon.innerContainer.NullOrEmpty())
		{
			return compAlternateWeapon.innerContainer[0].Graphic;
		}
		return null;
	}
}
