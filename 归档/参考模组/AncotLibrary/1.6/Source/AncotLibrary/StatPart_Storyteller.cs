using System.Text;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class StatPart_Storyteller : StatPart
{
	public StorytellerDef storytellerDef;

	public float offset = 0f;

	public float factor = 1f;

	public bool Active => Find.Storyteller.def == storytellerDef;

	public override void TransformValue(StatRequest req, ref float val)
	{
		if (Active)
		{
			val = val * factor + offset;
		}
	}

	public override string ExplanationPart(StatRequest req)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (Active)
		{
			if (factor != 1f)
			{
				stringBuilder.AppendLine("Ancot.StatsReport_StorytellerFactor".Translate() + " (" + storytellerDef.LabelCap + "): " + factor.ToStringPercent("F1"));
			}
			if (offset != 0f)
			{
				stringBuilder.AppendLine("Ancot.StatsReport_StorytellerOffset".Translate() + " (" + storytellerDef.LabelCap + "): " + offset.ToStringByStyle(parentStat.toStringStyle));
			}
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString().TrimEndNewlines();
	}
}
