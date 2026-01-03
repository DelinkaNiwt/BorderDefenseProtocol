using System.Text;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class StatPart_StuffStaminaFactor : StatPart
{
	public StatDef multiplierStat;

	public override void TransformValue(StatRequest req, ref float val)
	{
		Thing thing = req.Thing;
		if (thing == null || thing.TryGetComp<CompPhysicalShield>()?.AffectedByStuff != false)
		{
			float num = 1f;
			if (req.StuffDef != null)
			{
				num = req.StuffDef.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.MaxHitPoints);
			}
			val += val * (num - 1f) * GetMultiplier(req);
		}
	}

	private float GetMultiplier(StatRequest req)
	{
		if (req.HasThing)
		{
			return req.Thing.GetStatValue(multiplierStat);
		}
		return req.BuildableDef.GetStatValueAbstract(multiplierStat);
	}

	public override string ExplanationPart(StatRequest req)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (req.BuildableDef.MadeFromStuff)
		{
			float statFactorFromList = req.StuffDef.stuffProps.statFactors.GetStatFactorFromList(StatDefOf.MaxHitPoints);
			string text = ((req.StuffDef != null) ? req.StuffDef.label : "None".TranslateSimple());
			string text2 = ((req.StuffDef != null) ? statFactorFromList.ToStringPercent() : "0");
			stringBuilder.AppendLine("StatsReport_Material".Translate() + " (" + text + "): " + text2);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("StatsReport_StuffEffectMultiplier".Translate() + ": " + GetMultiplier(req).ToStringPercent("F0"));
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString().TrimEndNewlines();
	}
}
