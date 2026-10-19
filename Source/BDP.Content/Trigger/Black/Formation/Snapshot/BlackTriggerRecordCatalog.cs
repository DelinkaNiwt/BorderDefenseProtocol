using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>原版与 DLC 生涯记录白名单及玩家显示分组。</summary>
    public static class BlackTriggerRecordCatalog
    {
        private static readonly Dictionary<string, string> Groups = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "DamageDealt", "BDP_BlackTrigger_RecordGroup_Combat" }, { "TimeDrafted", "BDP_BlackTrigger_RecordGroup_Combat" }, { "ShotsFired", "BDP_BlackTrigger_RecordGroup_Combat" },
            { "Headshots", "BDP_BlackTrigger_RecordGroup_Combat" }, { "KillsAnimals", "BDP_BlackTrigger_RecordGroup_Combat" }, { "KillsHumanlikes", "BDP_BlackTrigger_RecordGroup_Combat" },
            { "KillsMechanoids", "BDP_BlackTrigger_RecordGroup_Combat" }, { "KillsEntities", "BDP_BlackTrigger_RecordGroup_Combat" }, { "PawnsDownedAnimals", "BDP_BlackTrigger_RecordGroup_Combat" },
            { "PawnsDownedHumanlikes", "BDP_BlackTrigger_RecordGroup_Combat" }, { "PawnsDownedMechanoids", "BDP_BlackTrigger_RecordGroup_Combat" }, { "PawnsDownedEntities", "BDP_BlackTrigger_RecordGroup_Combat" },
            { "TimeHunting", "BDP_BlackTrigger_RecordGroup_Combat" }, { "PeopleCaptured", "BDP_BlackTrigger_RecordGroup_Combat" },
            { "OperationsPerformed", "BDP_BlackTrigger_RecordGroup_Medicine" }, { "TimesTendedOther", "BDP_BlackTrigger_RecordGroup_Medicine" }, { "TimeTreatingAndFeeding", "BDP_BlackTrigger_RecordGroup_Medicine" },
            { "FiresExtinguished", "BDP_BlackTrigger_RecordGroup_Medicine" }, { "TimeFirefighting", "BDP_BlackTrigger_RecordGroup_Medicine" },
            { "PrisonersChatted", "BDP_BlackTrigger_RecordGroup_Social" }, { "PrisonersRecruited", "BDP_BlackTrigger_RecordGroup_Social" }, { "TimeWardening", "BDP_BlackTrigger_RecordGroup_Social" },
            { "AnimalsTamed", "BDP_BlackTrigger_RecordGroup_Social" }, { "TimeHandlingAnimals", "BDP_BlackTrigger_RecordGroup_Social" },
            { "AnimalsSlaughtered", "BDP_BlackTrigger_RecordGroup_Production" }, { "MealsCooked", "BDP_BlackTrigger_RecordGroup_Production" }, { "CellsMined", "BDP_BlackTrigger_RecordGroup_Production" },
            { "TimeMining", "BDP_BlackTrigger_RecordGroup_Production" }, { "PlantsSown", "BDP_BlackTrigger_RecordGroup_Production" }, { "PlantsHarvested", "BDP_BlackTrigger_RecordGroup_Production" },
            { "TimeSowingAndHarvesting", "BDP_BlackTrigger_RecordGroup_Production" }, { "ThingsConstructed", "BDP_BlackTrigger_RecordGroup_Production" }, { "TimeConstructing", "BDP_BlackTrigger_RecordGroup_Production" },
            { "ThingsCrafted", "BDP_BlackTrigger_RecordGroup_Production" }, { "ThingsDeconstructed", "BDP_BlackTrigger_RecordGroup_Production" }, { "ThingsInstalled", "BDP_BlackTrigger_RecordGroup_Production" },
            { "ThingsUninstalled", "BDP_BlackTrigger_RecordGroup_Production" }, { "ThingsRepaired", "BDP_BlackTrigger_RecordGroup_Production" }, { "TimeRepairing", "BDP_BlackTrigger_RecordGroup_Production" },
            { "ThingsHauled", "BDP_BlackTrigger_RecordGroup_Production" }, { "TimeHauling", "BDP_BlackTrigger_RecordGroup_Production" }, { "MessesCleaned", "BDP_BlackTrigger_RecordGroup_Production" }, { "TimeCleaning", "BDP_BlackTrigger_RecordGroup_Production" },
            { "ResearchPointsResearched", "BDP_BlackTrigger_RecordGroup_Research" }, { "TimeResearching", "BDP_BlackTrigger_RecordGroup_Research" }, { "ArtifactsActivated", "BDP_BlackTrigger_RecordGroup_Research" }, { "ContainersOpened", "BDP_BlackTrigger_RecordGroup_Research" },
            { "DamageTaken", "BDP_BlackTrigger_RecordGroup_Adversity" }, { "TimeDowned", "BDP_BlackTrigger_RecordGroup_Adversity" }, { "TimeInBedForMedicalReasons", "BDP_BlackTrigger_RecordGroup_Adversity" },
            { "TimesTendedTo", "BDP_BlackTrigger_RecordGroup_Adversity" }, { "OperationsReceived", "BDP_BlackTrigger_RecordGroup_Adversity" }, { "TimesOnFire", "BDP_BlackTrigger_RecordGroup_Adversity" },
            { "TimeOnFire", "BDP_BlackTrigger_RecordGroup_Adversity" }, { "TimesInMentalState", "BDP_BlackTrigger_RecordGroup_Adversity" }, { "TimeInMentalState", "BDP_BlackTrigger_RecordGroup_Adversity" },
            { "BodiesStripped", "BDP_BlackTrigger_RecordGroup_Experience" }, { "CorpsesBuried", "BDP_BlackTrigger_RecordGroup_Experience" }, { "TimeAsColonistOrColonyAnimal", "BDP_BlackTrigger_RecordGroup_Experience" },
            { "TimeAsPrisoner", "BDP_BlackTrigger_RecordGroup_Experience" }, { "TimeAsQuestLodger", "BDP_BlackTrigger_RecordGroup_Experience" }, { "TimeAsChildInColony", "BDP_BlackTrigger_RecordGroup_Experience" }
        };

        /// <summary>返回已确认的记录白名单。</summary>
        public static IEnumerable<string> AllowedNames { get { return Groups.Keys; } }

        /// <summary>判断记录是否属于原版／DLC 白名单。</summary>
        public static bool IsAllowed(RecordDef record)
        {
            return record != null && Groups.ContainsKey(record.defName);
        }

        /// <summary>返回记录的玩家显示分组。</summary>
        public static string ResolveGroup(RecordDef record)
        {
            string group;
            return record != null && Groups.TryGetValue(record.defName, out group) ? group : "BDP_BlackTrigger_RecordGroup_Other";
        }

        /// <summary>返回当前运行时实际存在的白名单记录。</summary>
        public static List<RecordDef> ResolveAvailable()
        {
            return DefDatabase<RecordDef>.AllDefsListForReading
                .Where(IsAllowed)
                .OrderBy(value => value.defName, StringComparer.Ordinal)
                .ToList();
        }
    }
}
