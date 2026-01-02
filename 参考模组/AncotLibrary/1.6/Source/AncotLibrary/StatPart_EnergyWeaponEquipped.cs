using System.Text;
using RimWorld;
using Verse;

namespace AncotLibrary;

public class StatPart_EnergyWeaponEquipped : StatPart
{
	public StatDef pawnStat;

	public StatDef correctionStat;

	public override void TransformValue(StatRequest req, ref float val)
	{
		float num = 1f;
		PawnOwner(req, out var pawn);
		if (pawn != null)
		{
			num = pawn.GetStatValue(pawnStat);
		}
		val += val * (num - 1f) * GetMultiplier(req, correctionStat);
	}

	private float GetMultiplier(StatRequest req, StatDef statDef)
	{
		if (req.HasThing)
		{
			return req.Thing.GetStatValue(statDef);
		}
		return req.BuildableDef.GetStatValueAbstract(statDef);
	}

	public override string ExplanationPart(StatRequest req)
	{
		PawnOwner(req, out var pawn);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Ancot.StatsReport_CorrectionFactor".Translate() + ": " + GetMultiplier(req, correctionStat).ToStringPercent("F0"));
		stringBuilder.AppendLine();
		if (pawn != null)
		{
			float statValue = pawn.GetStatValue(pawnStat);
			stringBuilder.AppendLine("Ancot.StatsReport_StatEquipped".Translate(pawnStat.LabelCap, statValue.ToStringByStyle(pawnStat.toStringStyle)));
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString().TrimEndNewlines();
	}

	public void PawnOwner(StatRequest req, out Pawn pawn)
	{
		pawn = (((ThingWithComps)req.Thing)?.ParentHolder as Pawn_EquipmentTracker)?.pawn;
	}
}
