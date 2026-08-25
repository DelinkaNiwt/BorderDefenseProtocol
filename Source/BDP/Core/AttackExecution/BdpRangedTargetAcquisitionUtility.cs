using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>远程模块共用的空地目标与落点目标检索工具。</summary>
    public static class BdpRangedTargetAcquisitionUtility
    {
        /// <summary>允许原版 Targeter（目标选择器）接受空地格。</summary>
        public static void EnsureGroundTargetingAllowed(TargetingParameters parameters)
        {
            if (parameters != null)
            {
                parameters.canTargetLocations = true;
            }
        }

        /// <summary>从指定落点检索半径内最近的合法敌对实体。</summary>
        public static LocalTargetInfo FindNearestAcquirableTarget(
            UnityEngine.Vector3 origin,
            Map map,
            Faction hostileToFaction,
            float radius,
            bool requireLineOfSight,
            Predicate<Thing> extraValidator)
        {
            if (map == null || radius <= 0f)
            {
                return LocalTargetInfo.Invalid;
            }

            var hostile = map.attackTargetsCache.TargetsHostileToFaction(hostileToFaction);
            if (hostile == null || hostile.Count == 0)
            {
                return LocalTargetInfo.Invalid;
            }

            IntVec3 center = IntVec3.FromVector3(origin);
            List<Thing> candidates = new List<Thing>();
            foreach (var attackTarget in hostile)
            {
                if (attackTarget != null && attackTarget.Thing != null)
                {
                    candidates.Add(attackTarget.Thing);
                }
            }
            Thing found = GenClosest.ClosestThing_Global(
                center,
                candidates,
                radius,
                delegate(Thing thing)
                {
                    if (thing == null || thing.Destroyed || thing.Position == IntVec3.Invalid)
                    {
                        return false;
                    }

                    Pawn pawn = thing as Pawn;
                    if (pawn != null && pawn.Downed)
                    {
                        return false;
                    }

                    if (requireLineOfSight && !GenSight.LineOfSight(center, thing.Position, map))
                    {
                        return false;
                    }

                    return extraValidator == null || extraValidator(thing);
                });

            return found != null ? new LocalTargetInfo(found) : LocalTargetInfo.Invalid;
        }
    }
}
