using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Collections.Generic;
using System.Linq;
using Verse.AI.Group;

namespace GD3
{
	public class MissionComponent : WorldComponent
	{
		public MissionComponent(World world) : base(world)
		{
		}

		public int EndingProgress => 20;

		public bool ShouldPayTax
		{
			get
			{
				if (Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire) == null)
				{
					return false;
				}
				if (Faction.OfPlayer.HostileTo(Find.FactionManager.FirstFactionOfDef(FactionDefOf.Empire)))
				{
					return false;
				}
				return true;
			}
		}

		public bool Advanced
        {
            get
            {
				return Find.ResearchManager.GetProgress(GDDefOf.GD3_Intelligence) == GDDefOf.GD3_Intelligence.baseCost;
			}
        }

		public bool FirewallShouldChange
        {
            get
            {
				List<Map> list = Find.Maps;
				for (int j = 0; j < list.Count; j++)
				{
					Map map = list[j];
					if (map != null)
					{
						List<Building> buildings = map.listerBuildings.allBuildingsColonist;
						if (buildings.Find((Building b) => b.def == GDDefOf.GD_CommunicationStation) != null)
						{
							return true;
						}
					}
				}
				return false;
			}
        }

		public MechanoidScriptDef Script
        {
            get
            {
				List<MechanoidScriptDef> trees = DefDatabase<MechanoidScriptDef>.AllDefs.ToList().FindAll((MechanoidScriptDef m) => !m.Ended && m.ID != -1);
				if (trees.Count == 0)
                {
					return null;
                }
                else
                {
					trees.SortBy((MechanoidScriptDef m) => m.ID);
					return trees[0];
				}
			}
        }

		public static List<QuestScriptDef> MechhiveQuests = new List<QuestScriptDef>
        {
			GDDefOf.GD_Quest_FixTurret,
			GDDefOf.GD_Quest_HelpMech,
			GDDefOf.GD_Quest_ExploreBase,
			GDDefOf.GD_Quest_BuildCluster
        };

		public int lastMechhiveQuestTick = -1;

        public override void FinalizeInit(bool fromload)
        {
            base.FinalizeInit(fromload);
			List<MechanoidScriptDef> list = DefDatabase<MechanoidScriptDef>.AllDefs.ToList();
			for (int j = 0; j < list.Count; j++)
            {
				MechanoidScriptDef def = list[j];
				if (def.priceKind == "None" && !script_Allowed.Contains(def.ID))
                {
					script_Allowed.Add(def.ID);
                }
            }
			if (GDUtility.MissionComponent.savingMechs == null)
			{
				GDUtility.MissionComponent.savingMechs = new List<Pawn>();
			}
		}

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
			if (Find.TickManager.TicksGame % 12000 == 0 && Find.AnyPlayerHomeMap != null)
			{
				Map map = Find.AnyPlayerHomeMap;
				if (!GDUtility.QuestExist(GDDefOf.GD_Quest_SendCorpse) && map.listerThings.AnyThingWithDef(GDDefOf.GD_MechCorpse))
				{
					GDUtility.TrySpawnQuest(GDDefOf.GD_Quest_SendCorpse, map);
				}
				if (!GDUtility.QuestExist(GDDefOf.GD_Quest_BlackApocriton) && progress >= EndingProgress && !GDUtility.MissionComponent.scriptEnded)
				{
					GDUtility.TrySpawnQuest(GDDefOf.GD_Quest_BlackApocriton, map);
				}
				IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.GiveQuest, Find.World);
				if (Find.TickManager.TicksGame - lastMechhiveQuestTick > GDSettings.mechhiveInterval * 60000)
                {
					if (GDSettings.DeveloperMode)
                    {
						Log.Warning("Trying generating mechhive quests");
                    }
					if (TryGetQuest(parms.points, parms.target, out QuestScriptDef result))
                    {
						GDUtility.TrySpawnQuest(result, map);
						lastMechhiveQuestTick = Find.TickManager.TicksGame;
					}
				}
			}
			if (Find.TickManager.TicksGame - lastChangeTick >= 120000)
            {
				lastChangeTick = Find.TickManager.TicksGame + interval.RandomInRange;
				ChangeFirewallRandom();
            }
        }

		private bool TryGetQuest(float points, IIncidentTarget target, out QuestScriptDef chosen)
		{
			return MechhiveQuests.Where((QuestScriptDef x) => x.IsRootRandomSelected && x.CanRun(points, target)).TryRandomElementByWeight((QuestScriptDef x) => NaturalRandomQuestChooser.GetNaturalRandomSelectionWeight(x, points, target.StoryState), out chosen);
		}

		public void ChangeFirewallRandom()
        {
			if (!FirewallShouldChange)
            {
				return;
            }
			Random random = new Random();
			int i = random.Next(0, 100);
			if (i >= 85 && i < 95)
			{
				if (firewallLevel != FirewallLevel.Unstable)
                {
					Messages.Message("GD.FirewallToUnstable".Translate(), MessageTypeDefOf.NeutralEvent);
                }
				firewallLevel = FirewallLevel.Unstable;
				if (GDSettings.pauseWhenUnstable)
                {
					Find.TickManager.Pause();
                }
			}
			else if (i >= 95)
			{
				if (firewallLevel != FirewallLevel.Alert)
				{
					Messages.Message("GD.FirewallToAlert".Translate(), MessageTypeDefOf.NegativeEvent);
				}
				firewallLevel = FirewallLevel.Alert;
				Find.TickManager.Pause();
			}
            else
            {
				if (firewallLevel != FirewallLevel.Stable)
				{
					Messages.Message("GD.FirewallToStable".Translate(), MessageTypeDefOf.PositiveEvent);
				}
				firewallLevel = FirewallLevel.Stable;
			}
		}

		public void BlackMechRelationOffset(int i)
        {
			if (factionRelationLock)
            {
				return;
            }
			relation += i;
			if (relation >= 0)
            {
				relation = 0;
            }
			if (relation <= -100)
            {
				Faction faction = Find.FactionManager.FirstFactionOfDef(GDDefOf.BlackMechanoid);
				faction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false);
				factionRelationLock = true;
				Find.LetterStack.ReceiveLetter("GD.BlackHostileTitle".Translate(), "GD.BlackHostileDesc".Translate(), LetterDefOf.ThreatSmall);
				Map map = Find.CurrentMap;
				if (map != null)
                {
					List<Pawn> list = map.mapPawns.AllPawnsSpawned.ToList().FindAll((Pawn p) => p.Faction?.def == GDDefOf.BlackMechanoid);
					for (int j = 0; j < list.Count; j++)
                    {
						Pawn p = list[j];
						CompBlackFaction comp = p.TryGetComp<CompBlackFaction>();
						if (comp != null && !comp.inMission || comp == null)
                        {
							continue;
                        }
						if (!RCellFinder.TryFindTravelDestFrom(p.Position, map, out IntVec3 travelDest))
						{
							Log.Warning(string.Concat("Failed to do traveler incident from ", p.Position, ": Couldn't find anywhere for the traveler to go."));
							continue;
						}
						LordJob_TravelAndExit lordJob = new LordJob_TravelAndExit(travelDest);
						Lord lord = LordMaker.MakeNewLord(faction, lordJob, map);
						lord.AddPawn(p);
					}
                }
			}
		}

		public bool IsSavingMech(Pawn pawn)
        {
			for (int i = 0; i < savingMechs.Count; i++)
			{
				if (savingMechs[i] == pawn)
                {
					return true;
                }
            }
			return false;
        }

		public override void ExposeData()
		{
			Scribe_Values.Look<bool>(ref blackMechDiscoverd, "blackMechDiscoverd", false, false);
			Scribe_Values.Look<bool>(ref factionRelationLock, "factionRelationLock", false, false);
			Scribe_Values.Look<bool>(ref keyGained, "keyGained", false, false);
			Scribe_Values.Look<bool>(ref militorSpawned, "militorSpawned", false, false);
			Scribe_Values.Look<bool>(ref apocritonDead, "apocritonDead", false, false);
			Scribe_Values.Look<bool>(ref scriptEnded, "scriptEnded", false, false);
			Scribe_Values.Look<int>(ref relation, "relation", 0, false);
			Scribe_Values.Look<int>(ref lastChangeTick, "lastChangeTick", 0, false);
			Scribe_Values.Look<int>(ref intelligencePrimary, "intelligencePrimary", 0, false);
			Scribe_Values.Look<int>(ref intelligenceAdvanced, "intelligenceAdvanced", 0, false);
			Scribe_Values.Look<int>(ref progress, "progress", 0, false);
			Scribe_Values.Look<int>(ref lastMechhiveQuestTick, "lastMechhiveQuestTick", -1, false);
			Scribe_Values.Look<FirewallLevel>(ref firewallLevel, "firewallLevel", FirewallLevel.Stable, false);
			Scribe_Collections.Look<string, bool>(ref BranchDict, "BranchDict", LookMode.Value, LookMode.Value, ref tmpStrings, ref tmpBool, false);
			Scribe_Collections.Look<int>(ref script_Finished, "script_Finished", LookMode.Value, Array.Empty<object>());
			Scribe_Collections.Look<int>(ref script_Allowed, "script_Allowed", LookMode.Value, Array.Empty<object>());
			Scribe_Collections.Look<Pawn>(ref cachedPawns, "cachedPawns", LookMode.Deep, Array.Empty<object>());
			Scribe_Collections.Look<Pawn>(ref savingMechs, "savingMechs", LookMode.Reference, Array.Empty<object>());
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (savingMechs == null)
				{
					savingMechs = new List<Pawn>();
				}
			}
		}

		public List<int> script_Finished = new List<int>();

		public List<int> script_Allowed = new List<int>();

		public bool blackMechDiscoverd = false;

		public bool factionRelationLock = false;

		public bool keyGained = false;

		public bool militorSpawned = false;

		public bool apocritonDead = false;

		public bool scriptEnded = false;

		public int relation = 0;

		public int intelligencePrimary;

		public int intelligenceAdvanced;

		public int lastChangeTick;

		private IntRange interval = new IntRange(-10000, 10000);

		public Dictionary<string, bool> BranchDict = new Dictionary<string, bool>();

		private List<string> tmpStrings = new List<string>();

		private List<bool> tmpBool = new List<bool>();

		public List<Pawn> cachedPawns = new List<Pawn>();

		public List<Pawn> savingMechs = new List<Pawn>();

		public int progress;

		public enum FirewallLevel
		{
			Stable,
			Unstable,
			Alert
		}

		public FirewallLevel firewallLevel = FirewallLevel.Stable;
	}
}
