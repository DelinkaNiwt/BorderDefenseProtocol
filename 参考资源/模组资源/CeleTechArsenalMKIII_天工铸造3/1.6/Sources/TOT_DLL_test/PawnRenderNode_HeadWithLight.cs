using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TOT_DLL_test;

public class PawnRenderNode_HeadWithLight : PawnRenderNode_Apparel
{
	public PawnRenderNode_HeadWithLight(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
		: base(pawn, props, tree, apparel)
	{
		base.apparel = apparel;
		useHeadMesh = true;
	}

	protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
	{
		yield return GraphicDatabase.Get<Graphic_Multi>(base.Props.texPath, base.Props.shaderTypeDef.Shader, Vector2.one, ColorFor(pawn));
	}

	public override GraphicMeshSet MeshSetFor(Pawn pawn)
	{
		if (props.overrideMeshSize.HasValue)
		{
			return MeshPool.GetMeshSetForSize(props.overrideMeshSize.Value.x, props.overrideMeshSize.Value.y);
		}
		return HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn);
	}

	public override Color ColorFor(Pawn pawn)
	{
		return apparel.DrawColor;
	}
}
