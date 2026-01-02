using UnityEngine;
using Verse;

namespace Milira;

public class PawnRenderNode_MilianHair : PawnRenderNode_MilianHairBase
{
	public PawnRenderNode_MilianHair(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
	}

	public override Graphic GraphicFor(Pawn pawn)
	{
		CompMilianHairSwitch compMilianHairSwitch = pawn.TryGetComp<CompMilianHairSwitch>();
		string frontHairPath = compMilianHairSwitch.frontHairPath;
		return GraphicDatabase.Get<Graphic_Multi>(frontHairPath, ShaderDatabase.Cutout, Vector2.one, ColorFor(pawn));
	}
}
