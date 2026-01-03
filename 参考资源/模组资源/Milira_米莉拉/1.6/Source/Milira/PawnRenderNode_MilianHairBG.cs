using UnityEngine;
using Verse;

namespace Milira;

public class PawnRenderNode_MilianHairBG : PawnRenderNode_MilianHairBase
{
	public PawnRenderNode_MilianHairBG(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
		: base(pawn, props, tree)
	{
	}

	public override Graphic GraphicFor(Pawn pawn)
	{
		CompMilianHairSwitch compMilianHairSwitch = pawn.TryGetComp<CompMilianHairSwitch>();
		string behindHairPath = compMilianHairSwitch.behindHairPath;
		return GraphicDatabase.Get<Graphic_Multi>(behindHairPath, ShaderDatabase.Cutout, Vector2.one, ColorFor(pawn));
	}
}
