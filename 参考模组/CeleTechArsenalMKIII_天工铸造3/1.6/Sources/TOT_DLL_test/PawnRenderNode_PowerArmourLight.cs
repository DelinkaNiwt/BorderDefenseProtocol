using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class PawnRenderNode_PowerArmourLight : PawnRenderNode_Apparel
{
	public PawnRenderNode_PowerArmourLight(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
	}

	public override Graphic GraphicFor(Pawn pawn)
	{
		bool flag = base.Props.texPath != null;
		return GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, ShaderDatabase.CutoutComplex);
	}

	public override Color ColorFor(Pawn pawn)
	{
		Color white = Color.white;
		return apparel.DrawColor;
	}
}
