using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 复制原版 TargetingParameters（目标参数）的中性工具。
    /// 原版 Verb.targetParams（Verb 目标参数）通常由 VerbProperties 共享，
    /// 目标阶段必须在副本上叠加运行时能力，不能直接改动它。
    /// </summary>
    internal static class TargetingParametersCopyUtility
    {
        /// <summary>
        /// 复制完整目标参数，并独立复制可变的派系列表。
        /// </summary>
        /// <param name="source">原版或宿主提供的目标参数。</param>
        /// <returns>可供当前一次目标交互使用的参数副本。</returns>
        internal static TargetingParameters Clone(TargetingParameters source)
        {
            if (source == null)
            {
                return new TargetingParameters();
            }

            return new TargetingParameters
            {
                canTargetLocations = source.canTargetLocations,
                canTargetSelf = source.canTargetSelf,
                canTargetPawns = source.canTargetPawns,
                canTargetFires = source.canTargetFires,
                canTargetBuildings = source.canTargetBuildings,
                canTargetItems = source.canTargetItems,
                canTargetAnimals = source.canTargetAnimals,
                canTargetHumans = source.canTargetHumans,
                canTargetMechs = source.canTargetMechs,
                canTargetPlants = source.canTargetPlants,
                canTargetSubhumans = source.canTargetSubhumans,
                canTargetEntities = source.canTargetEntities,
                onlyTargetFactions = source.onlyTargetFactions != null
                    ? new List<Faction>(source.onlyTargetFactions)
                    : null,
                validator = source.validator,
                onlyTargetFlammables = source.onlyTargetFlammables,
                targetSpecificThing = source.targetSpecificThing,
                mustBeSelectable = source.mustBeSelectable,
                neverTargetDoors = source.neverTargetDoors,
                neverTargetIncapacitated = source.neverTargetIncapacitated,
                neverTargetHostileFaction = source.neverTargetHostileFaction,
                onlyTargetSameIdeo = source.onlyTargetSameIdeo,
                onlyTargetThingsAffectingRegions = source.onlyTargetThingsAffectingRegions,
                onlyTargetDamagedThings = source.onlyTargetDamagedThings,
                mapObjectTargetsMustBeAutoAttackable = source.mapObjectTargetsMustBeAutoAttackable,
                onlyTargetIncapacitatedPawns = source.onlyTargetIncapacitatedPawns,
                onlyTargetColonistsOrPrisoners = source.onlyTargetColonistsOrPrisoners,
                onlyTargetColonistsOrPrisonersOrSlaves = source.onlyTargetColonistsOrPrisonersOrSlaves,
                onlyTargetColonistsOrPrisonersOrSlavesAllowMinorMentalBreaks = source.onlyTargetColonistsOrPrisonersOrSlavesAllowMinorMentalBreaks,
                onlyTargetControlledPawns = source.onlyTargetControlledPawns,
                onlyTargetColonists = source.onlyTargetColonists,
                onlyTargetPrisonersOfColony = source.onlyTargetPrisonersOfColony,
                onlyTargetPsychicSensitive = source.onlyTargetPsychicSensitive,
                onlyTargetAnimaTrees = source.onlyTargetAnimaTrees,
                canTargetBloodfeeders = source.canTargetBloodfeeders,
                onlyRepairableMechs = source.onlyRepairableMechs,
                thingCategory = source.thingCategory,
                onlyTargetDoors = source.onlyTargetDoors,
                canTargetCorpses = source.canTargetCorpses,
                onlyTargetCorpses = source.onlyTargetCorpses,
                mapBoundsContractedBy = source.mapBoundsContractedBy
            };
        }
    }
}
