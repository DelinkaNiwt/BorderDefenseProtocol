using NCL;
using Verse;

public class PawnRenderNode_TurretPawnOnly : PawnRenderNode
{
	public CompTurretGunPawnOnly turretComp;

	public PawnRenderNode_TurretPawnOnly(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
	}

	public override Graphic GraphicFor(Pawn pawn)
	{
		if (turretComp?.Props?.turretDef?.graphicData?.texPath == null)
		{
			return base.GraphicFor(pawn);
		}
		return GraphicDatabase.Get<Graphic_Single>(turretComp.Props.turretDef.graphicData.texPath, ShaderDatabase.Cutout);
	}
}
