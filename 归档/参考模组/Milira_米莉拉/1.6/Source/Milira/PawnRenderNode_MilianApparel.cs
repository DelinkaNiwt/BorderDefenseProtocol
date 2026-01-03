using RimWorld;
using Verse;

namespace Milira;

public class PawnRenderNode_MilianApparel : PawnRenderNode
{
	public PawnRenderNode_MilianApparel(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
		: base(pawn, props, tree)
	{
		base.apparel = apparel;
	}

	public override Graphic GraphicFor(Pawn pawn)
	{
		if (ApparelGraphicRecordGetter.TryGetGraphicApparel(apparel, BodyTypeDefOf.Female, forStatue: false, out var rec))
		{
			return rec.graphic;
		}
		return null;
	}
}
