using System.Collections.Generic;
using BDP.Core.Trigger;
using Verse;
using VerseThing = Verse.Thing;

namespace BDP.Content.Assembly.ChipManufacturing.Migration
{
    /// <summary>通过 Trigger 正式命令口安全清除槽位中的非法芯片引用。</summary>
    public sealed class TriggerInvalidChipEvacuationAdapter
    {
        /// <summary>只定位目标当前所属触发体，不改变任何槽位状态。</summary>
        public bool TryFindLoadedChipOwner(VerseThing target, out Pawn ownerPawn)
        {
            ownerPawn = null;
            if (target == null)
            {
                return false;
            }

            foreach (Pawn pawn in EnumeratePawns())
            {
                ITriggerLoadoutReader reader = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
                if (reader == null)
                {
                    continue;
                }

                foreach (ITriggerSlotState slot in reader.GetAllSlots())
                {
                    if (IsSameThing(slot?.LoadedChip, target))
                    {
                        ownerPawn = pawn;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>若目标正装载于任一触发体，则销毁槽位物品并返回所属 Pawn。</summary>
        public bool TryDestroyLoadedChip(VerseThing target, out Pawn ownerPawn)
        {
            ownerPawn = null;
            if (target == null)
            {
                return false;
            }

            foreach (Pawn pawn in EnumeratePawns())
            {
                ITriggerLoadoutReader reader = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
                ITriggerLoadoutCommands commands = TriggerSurfaceAccess.ResolveLoadoutCommands(pawn);
                if (reader == null || commands == null)
                {
                    continue;
                }

                foreach (ITriggerSlotState slot in reader.GetAllSlots())
                {
                    if (!IsSameThing(slot?.LoadedChip, target))
                    {
                        continue;
                    }

                    TriggerSide side = slot.IsBindingMirror
                        ? slot.BindingRootSide
                        : slot.Side;
                    int index = slot.IsBindingMirror
                        ? slot.BindingRootIndex
                        : slot.Index;
                    if (commands.TryDestroyLoadedChip(side, index, target.ThingID))
                    {
                        ownerPawn = pawn;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>稳定枚举地图 Pawn 和世界 Pawn，不依赖当前选中地图。</summary>
        private static IEnumerable<Pawn> EnumeratePawns()
        {
            HashSet<Pawn> seen = new HashSet<Pawn>();
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                {
                    if (seen.Add(pawn))
                    {
                        yield return pawn;
                    }
                }
            }

            if (Find.WorldPawns == null)
            {
                yield break;
            }

            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                if (seen.Add(pawn))
                {
                    yield return pawn;
                }
            }
        }

        /// <summary>优先比较对象引用，必要时比较稳定 ThingID。</summary>
        private static bool IsSameThing(VerseThing left, VerseThing right)
        {
            return ReferenceEquals(left, right)
                || (left != null
                    && right != null
                    && !left.ThingID.NullOrEmpty()
                    && left.ThingID == right.ThingID);
        }
    }
}
