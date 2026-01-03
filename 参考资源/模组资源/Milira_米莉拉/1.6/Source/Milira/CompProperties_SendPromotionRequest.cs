using Verse;

namespace Milira;

public class CompProperties_SendPromotionRequest : CompProperties
{
	public HediffDef promotionHediffType;

	public CompProperties_SendPromotionRequest()
	{
		compClass = typeof(CompSendPromotionRequest);
	}
}
