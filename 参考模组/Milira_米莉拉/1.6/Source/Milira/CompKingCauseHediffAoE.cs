using RimWorld;
using Verse;

namespace Milira;

public class CompKingCauseHediffAoE : ThingComp
{
	public CompProperties_KingCauseHediffAoE Props => (CompProperties_KingCauseHediffAoE)props;

	private bool IsPawnAffected(Pawn target)
	{
		Faction faction = Find.FactionManager.FirstFactionOfDef(MiliraDefOf.Milira_Faction);
		if (target.Dead || target.health == null)
		{
			return false;
		}
		if (target.Faction != parent.Faction || parent.Faction != faction)
		{
			return false;
		}
		if (target.RaceProps.IsMechanoid && target != parent)
		{
			return true;
		}
		return false;
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		CauseHediff();
		Log.Message("CauseHediffSpawn");
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		base.PostDestroy(mode, previousMap);
		RemoveHediff(previousMap);
		AllClassAmplification(previousMap);
		Log.Message("RemoveHediff");
	}

	public override void CompTick()
	{
		if (parent.IsHashIntervalTick(Props.timeInterval) && parent.Spawned)
		{
			CauseHediff();
			Log.Message("CauseHediff");
		}
	}

	public void CauseHediff()
	{
		foreach (Pawn item in parent.Map.mapPawns.AllPawnsSpawned)
		{
			if (IsPawnAffected(item))
			{
				HealthUtility.AdjustSeverity(item, Props.hediff, 1f);
			}
		}
	}

	public void RemoveHediff(Map map)
	{
		foreach (Pawn item in map.mapPawns.AllPawnsSpawned)
		{
			if (IsPawnAffected(item))
			{
				Hediff firstHediffOfDef = item.health.hediffSet.GetFirstHediffOfDef(Props.hediff);
				if (firstHediffOfDef != null)
				{
					item.health.RemoveHediff(firstHediffOfDef);
				}
			}
		}
	}

	public void AllClassAmplification(Map map)
	{
		foreach (Pawn item in map.mapPawns.AllPawnsSpawned)
		{
			if (IsPawnAffected(item))
			{
				Hediff firstHediffOfDef = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Pawn);
				Hediff firstHediffOfDef2 = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Knight);
				Hediff firstHediffOfDef3 = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Bishop);
				Hediff firstHediffOfDef4 = item.health.hediffSet.GetFirstHediffOfDef(MiliraDefOf.Milian_ClassHediff_Rook);
				if (firstHediffOfDef != null)
				{
					firstHediffOfDef.Severity = 1f;
				}
				if (firstHediffOfDef2 != null)
				{
					firstHediffOfDef2.Severity = 1f;
				}
				if (firstHediffOfDef3 != null)
				{
					firstHediffOfDef3.Severity = 1f;
				}
				if (firstHediffOfDef4 != null)
				{
					firstHediffOfDef4.Severity = 1f;
				}
			}
		}
	}
}
