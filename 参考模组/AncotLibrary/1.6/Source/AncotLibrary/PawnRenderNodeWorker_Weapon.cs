using Verse;

namespace AncotLibrary;

public class PawnRenderNodeWorker_Weapon : PawnRenderNodeWorker
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!base.CanDrawNow(node, parms))
		{
			return false;
		}
		return AncotLibrarySettings.weaponExtraRender_Draw;
	}

	protected override Graphic GetGraphic(PawnRenderNode node, PawnDrawParms parms)
	{
		PawnRenderNodeProperties_Weapon pawnRenderNodeProperties_Weapon = node.Props as PawnRenderNodeProperties_Weapon;
		if (!PawnRenderUtility.CarryWeaponOpenly(parms.pawn) && pawnRenderNodeProperties_Weapon.texPath_Undrafted != null)
		{
			return node.Graphics[0];
		}
		return node.Graphics[1];
	}
}
