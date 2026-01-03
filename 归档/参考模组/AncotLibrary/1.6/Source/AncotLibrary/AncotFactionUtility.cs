using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AncotLibrary;

public static class AncotFactionUtility
{
	public static Faction FindFaction(FactionDef factionDef)
	{
		return Find.FactionManager.FirstFactionOfDef(factionDef);
	}

	public static Faction FindFactionHostileToFaction(FactionDef factionDef, bool canBeHidden = false, int minTechLevel = 0, int maxTechLevel = 7, List<FactionDef> exceptFactions = null)
	{
		Faction faction = null;
		Faction npcFaction = Find.FactionManager.FirstFactionOfDef(factionDef);
		return Find.FactionManager.AllFactions.Where((Faction x) => !x.defeated && x.HostileTo(npcFaction) && (canBeHidden || !x.Hidden) && (int)x.def.techLevel >= minTechLevel && (int)x.def.techLevel < maxTechLevel && !x.temporary && !exceptFactions.NotNullAndContains(x.def)).RandomElementWithFallback(Find.FactionManager.RandomEnemyFaction());
	}

	public static Faction FindFactionHostileToPlayerAndFaction(FactionDef factionDef, bool canBeHidden = false, int minTechLevel = 0, int maxTechLevel = 7, List<FactionDef> exceptFactions = null)
	{
		Faction faction = null;
		Faction npcFaction = Find.FactionManager.FirstFactionOfDef(factionDef);
		return Find.FactionManager.AllFactions.Where((Faction x) => !x.defeated && x.HostileTo(Faction.OfPlayer) && x.HostileTo(npcFaction) && (canBeHidden || !x.Hidden) && (int)x.def.techLevel >= minTechLevel && (int)x.def.techLevel < maxTechLevel && !x.temporary && !exceptFactions.NotNullAndContains(x.def)).RandomElementWithFallback(Find.FactionManager.RandomEnemyFaction());
	}

	public static Faction FindFactionHostileToBothFaction(FactionDef factionDef1, FactionDef factionDef2, bool canBeHidden = false, int minTechLevel = 0, int maxTechLevel = 7, List<FactionDef> exceptFactions = null)
	{
		Faction faction = null;
		Faction npcFaction1 = Find.FactionManager.FirstFactionOfDef(factionDef1);
		Faction npcFaction2 = Find.FactionManager.FirstFactionOfDef(factionDef2);
		return Find.FactionManager.AllFactions.Where((Faction x) => !x.defeated && x.HostileTo(npcFaction1) && x.HostileTo(npcFaction2) && (canBeHidden || !x.Hidden) && (int)x.def.techLevel >= minTechLevel && (int)x.def.techLevel < maxTechLevel && !x.temporary && !exceptFactions.NotNullAndContains(x.def)).RandomElementWithFallback(Find.FactionManager.RandomEnemyFaction());
	}
}
