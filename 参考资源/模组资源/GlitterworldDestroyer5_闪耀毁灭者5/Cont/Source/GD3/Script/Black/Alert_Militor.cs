using System.Collections.Generic;
using System.Text;
using Verse;
using RimWorld;

namespace GD3
{
    public class Alert_Militor : Alert
    {
		private Pawn militor = null;

		private Pawn Militor
		{
			get
			{
				militor = null;
				List<Map> maps = Find.Maps;
				for (int i = 0; i < maps.Count; i++)
				{
					if (maps[i].IsPlayerHome)
					{
						continue;
					}
					foreach (Pawn pawn in maps[i].mapPawns.AllPawns)
					{
						if ((pawn.Spawned || pawn.BrieflyDespawned()) && pawn.def.defName == "Mech_Militor" && pawn.TryGetComp<CompMilitor>() != null && pawn.TryGetComp<CompMilitor>().active)
						{
							militor = pawn;
							break;
						}
					}
				}
				return militor;
			}
		}

		public Alert_Militor()
		{
			defaultLabel = "MilitorAlert".Translate();
			defaultPriority = AlertPriority.Critical;
		}

		public override TaggedString GetExplanation()
		{
			return "MilitorAlertDesc".Translate();
		}

		public override AlertReport GetReport()
		{
			if (Find.AnyPlayerHomeMap == null)
			{
				return false;
			}
			return AlertReport.CulpritIs(Militor);
		}
	}
}
