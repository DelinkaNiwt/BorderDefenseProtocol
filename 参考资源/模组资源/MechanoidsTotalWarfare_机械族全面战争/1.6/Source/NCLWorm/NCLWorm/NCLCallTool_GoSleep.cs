using System.Collections.Generic;
using System.Linq;
using Verse;

namespace NCLWorm;

public class NCLCallTool_GoSleep : NCLCallTool
{
	[MustTranslate]
	public string WormInSleep;

	[MustTranslate]
	public string WormOutSleep;

	public override void Action()
	{
		Pawn usedBy = windows.usedBy;
		NCL_Pawn_Worm nCL_Pawn_Worm = (NCL_Pawn_Worm)usedBy.Map.mapPawns.SpawnedColonyMechs.Where((Pawn t) => t.def.defName == "NCL_MechWorm" && t.Faction.IsPlayer).FirstOrDefault();
		if (nCL_Pawn_Worm != null)
		{
			nCL_Pawn_Worm.Sleep = !nCL_Pawn_Worm.Sleep;
		}
		windows.Close();
		string name = WormInSleep;
		if (!nCL_Pawn_Worm.Sleep)
		{
			name = WormOutSleep;
		}
		Find.WindowStack.Add(new Window_NCLcall(usedBy, NCLCall, name));
	}

	public override AcceptanceReport Canuse()
	{
		IEnumerable<Pawn> enumerable = windows.usedBy.Map.mapPawns.SpawnedColonyMechs.Where((Pawn t) => t.def.defName == "NCL_MechWorm" && t.Faction.IsPlayer);
		if (enumerable.EnumerableNullOrEmpty())
		{
			return "NoNCLWormCanSleep".Translate();
		}
		return base.Canuse();
	}

	public override bool NoCanSee()
	{
		return !Find.CurrentMap.mapPawns.AllPawnsSpawned.Any((Pawn x) => x.def.defName == "NCL_MechWorm");
	}
}
