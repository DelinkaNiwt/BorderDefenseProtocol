using System.Collections.Generic;
using System.Text;
using Verse;
using RimWorld;

namespace GD3
{
	public class Alert_Exo : Alert_Critical
	{
		private Exostrider dummy = null;

		private Exostrider Dummy
		{
			get
			{
				dummy = null;
				List<Map> maps = Find.Maps;
				for (int i = 0; i < maps.Count; i++)
				{
					if (maps[i].IsPlayerHome)
					{
						continue;
					}
					foreach (Thing thing in maps[i].listerThings.AllThings)
					{
						if (thing.Spawned && thing is Exostrider)
						{
							dummy = (Exostrider)thing;
							break;
						}
					}
				}
				return dummy;
			}
		}

		public Alert_Exo()
		{
			defaultLabel = "ExostriderAlert".Translate();
		}

		public override TaggedString GetExplanation()
		{
			return "ExostriderAlertDesc".Translate();
		}

		public override AlertReport GetReport()
		{
			if (Dummy != null)
            {
				return AlertReport.Active;
            }
			return false;
		}
	}
}
