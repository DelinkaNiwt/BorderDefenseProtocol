using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using VerseThing = Verse.Thing;

namespace BDP.Content.Assembly.ChipManufacturing.Migration
{
    /// <summary>以先安置遗留物、再销毁原物的事务顺序完成替换。</summary>
    public sealed class InvalidChipPlacementService
    {
        /// <summary>替换普通地图或容器物品；任何落点失败都保留原物。</summary>
        public bool Replace(VerseThing invalidItem, VerseThing remnant)
        {
            if (invalidItem == null || remnant == null || invalidItem.Destroyed)
            {
                return false;
            }

            InterruptRelatedPawnJobs(invalidItem);
            if (invalidItem.Spawned)
            {
                return ReplaceSpawned(invalidItem, remnant);
            }

            IThingHolder parent = invalidItem.ParentHolder;
            ThingOwner originalOwner = parent?.GetDirectlyHeldThings();
            Map rootMap = ThingOwnerUtility.GetRootMap(parent);
            if (rootMap != null)
            {
                IntVec3 rootPosition = ThingOwnerUtility.GetRootPosition(parent);
                return ReplaceHeldNearMap(
                    invalidItem,
                    remnant,
                    originalOwner,
                    rootPosition,
                    rootMap);
            }

            Pawn rootPawn = FindRootPawn(invalidItem);
            ThingOwner targetOwner = rootPawn?.inventory?.GetDirectlyHeldThings()
                ?? originalOwner;
            return ReplaceInsideOwner(
                invalidItem,
                remnant,
                originalOwner,
                targetOwner);
        }

        /// <summary>先为触发体所有者安置遗留物，再提交正式槽位销毁命令。</summary>
        public bool ReplaceForTriggerOwner(
            Pawn pawn,
            VerseThing remnant,
            Func<bool> commitOriginalRemoval)
        {
            if (pawn == null || remnant == null || commitOriginalRemoval == null)
            {
                return false;
            }

            if (pawn.Spawned)
            {
                PlacedRemnant placed;
                if (!TryPlaceOnMap(remnant, pawn.Position, pawn.Map, out placed))
                {
                    return false;
                }

                try
                {
                    if (TryCommit(commitOriginalRemoval))
                    {
                        return true;
                    }
                }
                catch
                {
                    RollbackPlacedRemnant(placed);
                    throw;
                }

                RollbackPlacedRemnant(placed);
                return false;
            }

            ThingOwner inventory = pawn.inventory?.GetDirectlyHeldThings();
            if (inventory == null || !inventory.TryAdd(remnant, false))
            {
                return false;
            }

            try
            {
                if (TryCommit(commitOriginalRemoval))
                {
                    return true;
                }
            }
            catch
            {
                RollbackPlacedRemnant(new PlacedRemnant
                {
                    Item = remnant,
                    AddedCount = 1,
                    Owner = inventory
                });
                throw;
            }

            RollbackPlacedRemnant(new PlacedRemnant
            {
                Item = remnant,
                AddedCount = 1,
                Owner = inventory
            });
            return false;
        }

        /// <summary>在地图上预放置遗留物，成功后才销毁原生成物。</summary>
        private static bool ReplaceSpawned(VerseThing invalidItem, VerseThing remnant)
        {
            PlacedRemnant placed;
            if (!TryPlaceOnMap(remnant, invalidItem.Position, invalidItem.Map, out placed))
            {
                return false;
            }

            try
            {
                invalidItem.Destroy(DestroyMode.Vanish);
                return true;
            }
            catch
            {
                RollbackPlacedRemnant(placed);
                throw;
            }
        }

        /// <summary>在地图宿主附近预放遗留物，成功后才移除容器内原物。</summary>
        private static bool ReplaceHeldNearMap(
            VerseThing invalidItem,
            VerseThing remnant,
            ThingOwner originalOwner,
            IntVec3 position,
            Map map)
        {
            PlacedRemnant placed;
            if (!TryPlaceOnMap(remnant, position, map, out placed))
            {
                return false;
            }

            if (originalOwner != null && !originalOwner.Remove(invalidItem))
            {
                RollbackPlacedRemnant(placed);
                return false;
            }

            try
            {
                invalidItem.Destroy(DestroyMode.Vanish);
                return true;
            }
            catch
            {
                RestoreOriginalOwner(originalOwner, invalidItem);
                RollbackPlacedRemnant(placed);
                throw;
            }
        }

        /// <summary>在离地图容器中替换；目标拒收时把原物放回原容器。</summary>
        private static bool ReplaceInsideOwner(
            VerseThing invalidItem,
            VerseThing remnant,
            ThingOwner originalOwner,
            ThingOwner targetOwner)
        {
            if (originalOwner == null
                || targetOwner == null
                || !originalOwner.Remove(invalidItem))
            {
                return false;
            }

            if (!targetOwner.TryAdd(remnant, false))
            {
                RestoreOriginalOwner(originalOwner, invalidItem);
                return false;
            }

            try
            {
                invalidItem.Destroy(DestroyMode.Vanish);
                return true;
            }
            catch
            {
                targetOwner.Remove(remnant);
                RestoreOriginalOwner(originalOwner, invalidItem);
                if (!remnant.Destroyed)
                {
                    remnant.Destroy(DestroyMode.Vanish);
                }

                throw;
            }
        }

        /// <summary>通过原版落点器预放一个遗留物，并记录实际合并目标以便回滚。</summary>
        private static bool TryPlaceOnMap(
            VerseThing remnant,
            IntVec3 position,
            Map map,
            out PlacedRemnant placed)
        {
            placed = null;
            if (map == null)
            {
                return false;
            }

            VerseThing placedItem = null;
            int addedCount = 0;
            bool succeeded = GenPlace.TryPlaceThing(
                remnant,
                position,
                map,
                ThingPlaceMode.Near,
                (item, count) =>
                {
                    placedItem = item;
                    addedCount += count;
                });
            if (!succeeded)
            {
                return false;
            }

            placed = new PlacedRemnant
            {
                Item = placedItem ?? remnant,
                AddedCount = addedCount > 0 ? addedCount : 1
            };
            return true;
        }

        /// <summary>执行触发体原物移除提交；异常交给上层，同时允许 finally 式回滚。</summary>
        private static bool TryCommit(Func<bool> commitOriginalRemoval)
        {
            return commitOriginalRemoval();
        }

        /// <summary>撤销已经放入地图或容器的一份遗留物。</summary>
        private static void RollbackPlacedRemnant(PlacedRemnant placed)
        {
            if (placed?.Item == null || placed.Item.Destroyed)
            {
                return;
            }

            if (placed.Owner != null)
            {
                placed.Owner.Remove(placed.Item);
                placed.Item.Destroy(DestroyMode.Vanish);
                return;
            }

            if (placed.Item.stackCount > placed.AddedCount)
            {
                placed.Item.stackCount -= placed.AddedCount;
            }
            else
            {
                placed.Item.Destroy(DestroyMode.Vanish);
            }
        }

        /// <summary>把尚未销毁的原物放回原始容器。</summary>
        private static bool RestoreOriginalOwner(
            ThingOwner originalOwner,
            VerseThing invalidItem)
        {
            return originalOwner != null
                && invalidItem != null
                && !invalidItem.Destroyed
                && originalOwner.TryAdd(invalidItem, false);
        }

        /// <summary>在销毁半成品前结束所有直接引用或正在携带它的 Pawn 工作。</summary>
        private static void InterruptRelatedPawnJobs(VerseThing invalidItem)
        {
            HashSet<Pawn> pawns = new HashSet<Pawn>();
            foreach (Map map in Find.Maps)
            {
                pawns.UnionWith(map.mapPawns.AllPawns);
            }

            if (Find.WorldPawns != null)
            {
                pawns.UnionWith(Find.WorldPawns.AllPawnsAliveOrDead);
            }

            foreach (Pawn pawn in pawns)
            {
                if (pawn?.jobs == null)
                {
                    continue;
                }

                List<Job> jobs = new List<Job>(pawn.jobs.AllJobs());
                for (int index = 0; index < jobs.Count; index++)
                {
                    Job job = jobs[index];
                    bool carriesInvalidItem = ReferenceEquals(
                        pawn.carryTracker?.CarriedThing,
                        invalidItem);
                    if (job != null
                        && (job.AnyTargetIs(invalidItem)
                            || (carriesInvalidItem && ReferenceEquals(job, pawn.CurJob))))
                    {
                        pawn.jobs.EndCurrentOrQueuedJob(
                            job,
                            JobCondition.Incompletable,
                            true,
                            false);
                    }
                }
            }
        }

        /// <summary>沿 ParentHolder 链寻找最外层 Pawn。</summary>
        private static Pawn FindRootPawn(VerseThing thing)
        {
            IThingHolder holder = thing?.ParentHolder;
            Pawn pawn = null;
            while (holder != null)
            {
                Pawn current = holder as Pawn;
                if (current != null)
                {
                    pawn = current;
                }

                holder = holder.ParentHolder;
            }

            return pawn;
        }

        /// <summary>一份已预放遗留物的回滚信息。</summary>
        private sealed class PlacedRemnant
        {
            /// <summary>地图上的结果堆或容器中的独立物品。</summary>
            public VerseThing Item { get; set; }

            /// <summary>本次实际增加的数量。</summary>
            public int AddedCount { get; set; }

            /// <summary>离地图放置时的目标容器。</summary>
            public ThingOwner Owner { get; set; }
        }
    }
}
