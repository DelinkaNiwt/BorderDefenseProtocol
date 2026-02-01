using RimWorld;
using Verse;

namespace VanillaPsycastsExpanded;

public class StatPart_Group : StatPart_Focus
{
	public override void TransformValue(StatRequest req, ref float val)
	{
		if (ApplyOn(req) && req.Thing.Map != null && req.Thing.Faction != null)
		{
			float num = val;
			int num2 = MeditatingPawnsAround(req.Thing);
			float num3 = ((num2 <= 1) ? 0f : (num2 switch
			{
				2 => 0.06f, 
				3 => 0.2f, 
				4 => 0.45f, 
				_ => 0.8f, 
			}));
			val = num + num3;
		}
	}

	private static int MeditatingPawnsAround(Thing thing)
	{
		return thing.Map.mapPawns.AllHumanlikeSpawned.Count((Pawn p) => p.CurJobDef == JobDefOf.Meditate && p.Position.InHorDistOf(thing.Position, 5f));
	}

	public override string ExplanationPart(StatRequest req)
	{
		if (!ApplyOn(req) || req.Thing.Map == null || req.Thing.Faction == null)
		{
			return "";
		}
		int num = MeditatingPawnsAround(req.Thing);
		TaggedString taggedString = "VPE.GroupFocus".Translate(num - 1) + ": ";
		StatWorker worker = parentStat.Worker;
		float val = ((num <= 1) ? 0f : (num switch
		{
			2 => 0.06f, 
			3 => 0.2f, 
			4 => 0.45f, 
			_ => 0.8f, 
		}));
		return taggedString + worker.ValueToString(val, finalized: true, ToStringNumberSense.Offset);
	}
}
