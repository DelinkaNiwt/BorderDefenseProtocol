using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace AlienRace;

[UsedImplicitly]
public class AlienPawnRenderNode_Swaddle : PawnRenderNode_Swaddle
{
	public AlienPawnRenderNode_Swaddle(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
	}

	public override Graphic GraphicFor(Pawn pawn)
	{
		AlienRenderTreePatches.PawnRenderResolveData pawnRenderData = AlienRenderTreePatches.RegenerateResolveData(pawn);
		if (pawnRenderData.alienProps != null)
		{
			return GraphicDatabase.Get<Graphic_Multi>(pawnRenderData.alienProps.alienRace.graphicPaths.swaddle.GetPath(pawn, ref pawnRenderData.sharedIndex, pawn.HashOffset()), ShaderFor(pawn), Vector2.one, ColorFor(pawn));
		}
		return base.GraphicFor(pawn);
	}
}
