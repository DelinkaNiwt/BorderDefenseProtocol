using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaPsycastsExpanded;

public class StatPart_NearbyWealth : StatPart_Focus
{
	public override void TransformValue(StatRequest req, ref float val)
	{
		if (ApplyOn(req) && req.Thing.Map != null)
		{
			float num = Mathf.Max(req.Thing.Map.wealthWatcher.WealthTotal, 1000f);
			if (!(num <= 0f))
			{
				float num2 = Mathf.Min(GenRadialCached.WealthAround(req.Thing.Position, req.Thing.Map, 6f, useCenter: true), num);
				val += num2 / num;
			}
		}
	}

	public override string ExplanationPart(StatRequest req)
	{
		if (!ApplyOn(req) || req.Thing.Map == null)
		{
			return string.Empty;
		}
		float num = Mathf.Max(req.Thing.Map.wealthWatcher.WealthTotal, 1000f);
		float num2 = Mathf.Min(GenRadialCached.WealthAround(req.Thing.Position, req.Thing.Map, 6f, useCenter: true), num);
		return "VPE.WealthNearby".Translate(num2.ToStringMoney(), num.ToStringMoney()) + ": " + parentStat.Worker.ValueToString(num2 / num, finalized: true, ToStringNumberSense.Offset);
	}
}
