using System.Collections.Generic;
using BDP.Core.Chips;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.Assembly
{
    /// <summary>
    /// 芯片仓内部容器组件。
    /// 它参考原版基因仓的持物方式，但只保留 BDP 芯片仓需要的最小闭环。
    /// </summary>
    [StaticConstructorOnStartup]
    public sealed class CompChipContainer : ThingComp, IThingHolder
    {
        /// <summary>
        /// 全部弹出按钮图标。
        /// </summary>
        private static readonly Texture2D EjectAllIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/EjectAll");

        /// <summary>
        /// 芯片仓内部真实持物容器。
        /// </summary>
        private ThingOwner<Thing> innerContainer;

        /// <summary>
        /// 芯片仓组件配置。
        /// </summary>
        private CompProperties_ChipContainer Props
        {
            get { return (CompProperties_ChipContainer)props; }
        }

        /// <summary>
        /// 对同程序集内容页签暴露内部容器。
        /// </summary>
        internal ThingOwner<Thing> InnerContainer
        {
            get
            {
                EnsureInnerContainer();
                return innerContainer;
            }
        }

        /// <summary>
        /// 当前芯片仓是否已经达到容量上限。
        /// </summary>
        internal bool Full
        {
            get { return InnerContainer.Count >= ResolveMaxCapacity(); }
        }

        /// <summary>
        /// 当前芯片仓是否还能接收更多芯片。
        /// </summary>
        internal bool CanAcceptMore
        {
            get { return !Full; }
        }

        /// <summary>
        /// 生成后补齐内部容器。
        /// </summary>
        public override void PostPostMake()
        {
            base.PostPostMake();
            EnsureInnerContainer();
        }

        /// <summary>
        /// 返回当前内部容器中可供装配台读取的有效芯片。
        /// </summary>
        internal IReadOnlyList<Thing> GetAvailableChips()
        {
            EnsureInnerContainer();
            List<Thing> chips = new List<Thing>();
            for (int i = 0; i < innerContainer.Count; i++)
            {
                Thing chip = innerContainer[i];
                if (IsUsableChip(chip))
                {
                    chips.Add(chip);
                }
            }

            return chips;
        }

        /// <summary>
        /// 判断指定芯片是否真实位于当前内部容器。
        /// </summary>
        internal bool ContainsChip(Thing chip)
        {
            if (chip == null)
            {
                return false;
            }

            EnsureInnerContainer();
            for (int i = 0; i < innerContainer.Count; i++)
            {
                if (ReferenceEquals(innerContainer[i], chip))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 尝试接收一个有效单芯片物品。
        /// </summary>
        internal bool TryAcceptChip(Thing chip)
        {
            if (!CanAcceptChip(chip) || Full)
            {
                return false;
            }

            EnsureInnerContainer();
            if (ContainsChip(chip))
            {
                return true;
            }

            if (chip.Spawned)
            {
                Map sourceMap = chip.Map;
                IntVec3 sourcePosition = chip.Position;
                chip.DeSpawn();
                if (innerContainer.TryAdd(chip, canMergeWithExistingStacks: false))
                {
                    return true;
                }

                TryRestoreRejectedChip(chip, sourceMap, sourcePosition);
                return false;
            }

            if (chip.holdingOwner != null)
            {
                return chip.holdingOwner.TryTransferToContainer(
                    chip,
                    innerContainer,
                    canMergeWithExistingStacks: false);
            }

            return innerContainer.TryAdd(chip, canMergeWithExistingStacks: false);
        }

        /// <summary>
        /// 尝试从内部容器取出指定芯片。
        /// </summary>
        internal bool TryTakeChip(Thing chip)
        {
            if (!ContainsChip(chip) || !IsUsableChip(chip))
            {
                return false;
            }

            return innerContainer.Remove(chip);
        }

        /// <summary>
        /// 把内部持有的全部芯片弹出到地图。
        /// </summary>
        internal void EjectContents(Map map = null)
        {
            EnsureInnerContainer();
            Map targetMap = map ?? parent?.Map;
            if (targetMap == null || innerContainer.Count == 0)
            {
                return;
            }

            IntVec3 dropCell = ResolveDropCell();
            innerContainer.TryDropAll(dropCell, targetMap, ThingPlaceMode.Near);
        }

        /// <summary>
        /// 返回 RimWorld 直接持有物容器。
        /// </summary>
        public ThingOwner GetDirectlyHeldThings()
        {
            return InnerContainer;
        }

        /// <summary>
        /// 把子持有者追加给 RimWorld 存档与销毁系统。
        /// </summary>
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        /// <summary>
        /// 存档读写内部容器。
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            EnsureInnerContainer();
        }

        /// <summary>
        /// 建筑离图时弹出内部芯片，替换建筑时保留原版替换语义。
        /// </summary>
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode != DestroyMode.WillReplace)
            {
                EjectContents(map);
            }

            base.PostDeSpawn(map, mode);
        }

        /// <summary>
        /// 建筑销毁时清理内部芯片，避免隐藏物品悬空。
        /// </summary>
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (innerContainer != null)
            {
                innerContainer.ClearAndDestroyContents();
            }

            base.PostDestroy(mode, previousMap);
        }

        /// <summary>
        /// 生成芯片仓相关操作按钮。
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (parent?.Faction == Faction.OfPlayer && InnerContainer.Count > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "BDP_Command_ChipContainer_EjectAll".Translate(),
                    defaultDesc = "BDP_Command_ChipContainer_EjectAllDesc".Translate(),
                    icon = EjectAllIcon,
                    action = delegate
                    {
                        EjectContents(parent.Map);
                    }
                };
            }
        }

        /// <summary>
        /// 生成检查面板中的芯片仓容量文本。
        /// </summary>
        public override string CompInspectStringExtra()
        {
            return "BDP_Window_TriggerAssembly_Stored".Translate(
                InnerContainer.Count,
                ResolveMaxCapacity());
        }

        /// <summary>
        /// 确保内部容器存在。
        /// </summary>
        private void EnsureInnerContainer()
        {
            if (innerContainer == null)
            {
                innerContainer = new ThingOwner<Thing>(this);
            }
        }

        /// <summary>
        /// 读取配置容量，并保证至少为 1。
        /// </summary>
        private int ResolveMaxCapacity()
        {
            return Props != null && Props.maxCapacity > 0 ? Props.maxCapacity : 1;
        }

        /// <summary>
        /// 判断物品是否可以放入芯片仓。
        /// </summary>
        private static bool CanAcceptChip(Thing chip)
        {
            return chip != null
                && !chip.Destroyed
                && chip.stackCount == 1
                && IsValidChipDefinition(chip);
        }

        /// <summary>
        /// 判断内部容器中的芯片是否可供装配台读取。
        /// </summary>
        private static bool IsUsableChip(Thing chip)
        {
            return CanAcceptChip(chip);
        }

        /// <summary>
        /// 判断物品定义是否通过 BDP 芯片校验。
        /// </summary>
        private static bool IsValidChipDefinition(Thing thing)
        {
            ChipDefinitionSnapshot snapshot = ChipSnapshotAccess.Read(thing);
            return snapshot != null && snapshot.IsValid;
        }

        /// <summary>
        /// 计算芯片弹出落点。
        /// </summary>
        private IntVec3 ResolveDropCell()
        {
            if (parent != null && parent.def != null && parent.def.hasInteractionCell)
            {
                return parent.InteractionCell;
            }

            return parent != null ? parent.Position : IntVec3.Invalid;
        }

        /// <summary>
        /// 接收失败时尽量把已离图芯片放回原位置，避免吞物品。
        /// </summary>
        private static void TryRestoreRejectedChip(Thing chip, Map sourceMap, IntVec3 sourcePosition)
        {
            if (chip == null || chip.Destroyed || chip.Spawned || chip.holdingOwner != null || sourceMap == null)
            {
                return;
            }

            if (!sourcePosition.IsValid)
            {
                return;
            }

            GenPlace.TryPlaceThing(chip, sourcePosition, sourceMap, ThingPlaceMode.Near);
        }
    }
}
