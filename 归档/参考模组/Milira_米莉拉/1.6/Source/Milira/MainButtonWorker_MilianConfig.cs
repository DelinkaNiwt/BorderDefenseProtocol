using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Milira;

public class MainButtonWorker_MilianConfig : MainButtonWorker_ToggleTab
{
	public override bool Disabled
	{
		get
		{
			if (base.Disabled)
			{
				return true;
			}
			Map currentMap = Find.CurrentMap;
			if (currentMap != null)
			{
				List<Pawn> spawnedColonyMechs = currentMap.mapPawns.SpawnedColonyMechs;
				for (int i = 0; i < spawnedColonyMechs.Count; i++)
				{
					if (MilianUtility.IsMilian(spawnedColonyMechs[i]))
					{
						return false;
					}
				}
			}
			return true;
		}
	}

	public override bool Visible => !Disabled && (int)MiliraRaceSettings.TabAvailable_MilianConfig > 0;
}
