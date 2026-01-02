using RimWorld;
using Verse;

namespace AncotLibrary;

public class StatPart_StatCurve : StatPart_Curve
{
	public StatDef stat;

	protected override bool AppliesTo(StatRequest req)
	{
		return true;
	}

	protected override float CurveXGetter(StatRequest req)
	{
		return req.Thing.GetStatValue(stat);
	}

	protected override string ExplanationLabel(StatRequest req)
	{
		return "Ancot.StatsReport_StatCurve".Translate(stat.LabelCap, req.Thing.GetStatValue(stat).ToStringByStyle(stat.toStringStyle));
	}
}
