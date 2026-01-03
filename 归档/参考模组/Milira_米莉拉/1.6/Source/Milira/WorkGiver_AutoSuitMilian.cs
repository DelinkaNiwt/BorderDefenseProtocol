using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Milira;

public class WorkGiver_AutoSuitMilian : WorkGiver_Scanner
{
	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return pawn.Map.mapPawns.SpawnedColonyMechs.Where((Pawn p) => MilianUtility.IsMilian(p)).ToList();
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!HasJob(pawn, t, forced))
		{
			return null;
		}
		CompMilianApparelRender compMilianApparelRender = t.TryGetComp<CompMilianApparelRender>();
		if (compMilianApparelRender == null || !compMilianApparelRender.autoSuitUp)
		{
			return null;
		}
		List<Thing> list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);
		if (list.Count == 0)
		{
			return null;
		}
		Pawn milian = (Pawn)t;
		Apparel apparel = null;
		float num = 0f;
		foreach (Thing item in list)
		{
			Apparel apparel2 = (Apparel)item;
			float num2 = ApparelScore(milian, apparel2);
			if (num2 > 60f && !apparel2.IsForbidden(pawn) && apparel2.IsInAnyStorage() && num2 > num)
			{
				apparel = apparel2;
				num = num2;
			}
		}
		if (apparel != null && pawn.CanReserve(apparel, 1, -1, null, forced))
		{
			Job job = JobMaker.MakeJob(MiliraDefOf.Milira_DressMilian, t, apparel);
			job.count = 1;
			return job;
		}
		return null;
	}

	public bool HasJob(Pawn pawn, Thing t, bool forced = false)
	{
		if (!pawn.IsColonist)
		{
			return false;
		}
		Pawn pawn2 = (Pawn)t;
		if (!MilianUtility.IsMilian(pawn2) || !pawn2.Spawned || pawn2.Downed)
		{
			return false;
		}
		if (t.IsForbidden(pawn))
		{
			return false;
		}
		CompMilianApparelRender comp = pawn2.GetComp<CompMilianApparelRender>();
		if (comp == null || !comp.autoSuitUp)
		{
			return false;
		}
		if (!pawn.CanReserve(t, 1, -1, null, forced))
		{
			return false;
		}
		return true;
	}

	public float ApparelScore(Pawn milian, Apparel apparel)
	{
		float num = 0f;
		CompTargetable_Milian compTargetable_Milian = apparel.TryGetComp<CompTargetable_Milian>();
		if (compTargetable_Milian == null || !compTargetable_Milian.TargetableMilianPawnkinds.Contains(milian.kindDef))
		{
			return 0f;
		}
		List<string> tags = apparel.def.apparel.tags;
		Apparel apparel2 = null;
		foreach (Apparel item in milian.apparel.WornApparel)
		{
			if (item.def.apparel.tags.ContainsAny((string t) => tags.Contains(t) && t != "MilianApparel" && !t.StartsWith("Milian_Mechanoid_")))
			{
				apparel2 = item;
				break;
			}
		}
		if (apparel2 == null)
		{
			num += 100f;
		}
		else
		{
			float num2 = apparel2.GetStatValue(StatDefOf.ArmorRating_Sharp) + apparel2.GetStatValue(StatDefOf.ArmorRating_Blunt);
			float num3 = apparel.GetStatValue(StatDefOf.ArmorRating_Sharp) + apparel.GetStatValue(StatDefOf.ArmorRating_Blunt);
			if (num2 < num3)
			{
				num += 50f * (num3 / num2);
			}
			apparel2.TryGetQuality(out var qc);
			apparel.TryGetQuality(out var qc2);
			if ((int)qc < (int)qc2)
			{
				num += 20f;
			}
			float num4 = apparel2.MaxHitPoints;
			float num5 = apparel.MaxHitPoints;
			if (num4 < num5)
			{
				num += 20f * (num5 / num4);
			}
		}
		return num;
	}
}
