using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace GD3
{
	public class StatPart_Reinforce : StatPart
	{
		public override void TransformValue(StatRequest req, ref float val)
		{
			if (!req.HasThing)
			{
				return;
			}
			ThingWithComps thing = req.Thing as ThingWithComps;
			CompDeckReinforce comp = thing.TryGetComp<CompDeckReinforce>();
			if (comp == null || comp.level == 0)
			{
				return;
			}
			if (!(val <= 0f))
			{
				float a = val * comp.Props.healthGainPerLevel * comp.level;
				a = Mathf.Min(a, 9999999f);
				val += a;
			}
		}

		public override string ExplanationPart(StatRequest req)
		{
			if (req.HasThing && req.Thing.GetStatValue(parentStat) <= 0f)
			{
				return null;
			}
			CompDeckReinforce comp;
			if (req.HasThing && (comp = req.Thing.TryGetComp<CompDeckReinforce>()) != null && comp.level > 0)
			{
				string text = "GD.DeckReinforce".Translate() + ": x" + GenText.ToStringPercent(comp.Props.healthGainPerLevel * comp.level);
				float num = 9999999f;
				if (num < 999999f)
				{
					text += "\n    (" + Translator.Translate("StatsReport_MaxGain") + ": " + GenText.ToStringByStyle(num, parentStat.ToStringStyleUnfinalized, parentStat.toStringNumberSense) + ")";
				}
				return text;
			}
			return null;
		}
	}
}
