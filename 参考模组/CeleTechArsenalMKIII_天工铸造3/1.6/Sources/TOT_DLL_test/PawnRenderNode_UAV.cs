using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class PawnRenderNode_UAV : PawnRenderNode
{
	public Comp_UAV turretComp;

	public PawnRenderNode_UAV(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
	}

	public override Graphic GraphicFor(Pawn pawn)
	{
		if (base.Props.texPath != null)
		{
			return GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, ShaderDatabase.CutoutComplex);
		}
		return GraphicDatabase.Get<Graphic_Single>(turretComp.Props.turretDef.graphicData.texPath, ShaderDatabase.CutoutComplex);
	}

	public override Color ColorFor(Pawn pawn)
	{
		Color white = Color.white;
		return apparel.DrawColor;
	}
}
