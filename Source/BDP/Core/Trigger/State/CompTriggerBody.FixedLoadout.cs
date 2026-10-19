using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// 触发体首次生成固定芯片的生命周期辅助逻辑。
    /// 该片段只负责创建真实Thing和原子回滚，不改变玩家装卸权限或芯片业务规则。
    /// </summary>
    public sealed partial class CompTriggerBody
    {
        /// <summary>
        /// 按触发体定义首次装入固定芯片。
        /// 该方法只从PostPostMake调用，读档和普通运行时不会再次进入。
        /// </summary>
        private void TryInstallInitialFixedLoadout()
        {
            CompProperties_TriggerBody properties = Props;
            if (properties == null
                || properties.fixedLoadout == null
                || properties.fixedLoadout.Count == 0)
            {
                return;
            }

            long loadoutStart = BdpDiagnostics.BeginTiming();
            long stateReadyStart = BdpDiagnostics.BeginTiming();
            EnsureInternalState();
            EnsureChipContainer();
            EnsureSlots();
            BdpDiagnostics.TraceTiming(
                "固定芯片装载 | stage=state_ready | trigger=" + SafeThingId(parent),
                stateReadyStart);

            TraceLifecycle(
                "fixed_loadout_begin",
                "entryCount=" + properties.fixedLoadout.Count);

            List<Thing> createdChips = new List<Thing>();
            for (int entryIndex = 0; entryIndex < properties.fixedLoadout.Count; entryIndex++)
            {
                TriggerFixedLoadoutEntry entry = properties.fixedLoadout[entryIndex];
                if (entry == null
                    || !Enum.IsDefined(typeof(TriggerSide), entry.side)
                    || entry.slotNumber < 1
                    || entry.chipDef == null)
                {
                    ReportInitialFixedLoadoutFailure(entry, entryIndex, "entry_invalid", null);
                    RollbackInitialFixedLoadout(createdChips);
                    return;
                }

                Thing chip = null;
                try
                {
                    long chipCreationStart = BdpDiagnostics.BeginTiming();
                    if (!string.IsNullOrWhiteSpace(entry.initializationKey))
                    {
                        if (!TriggerFixedLoadoutFactoryRegistry.TryCreate(
                            entry.chipDef,
                            entry.initializationKey,
                            out chip))
                        {
                            ReportInitialFixedLoadoutFailure(entry, entryIndex, "factory_failed", null);
                            RollbackInitialFixedLoadout(createdChips);
                            return;
                        }
                    }
                    else
                    {
                        chip = ThingMaker.MakeThing(entry.chipDef);
                    }
                    BdpDiagnostics.TraceTiming(
                        "固定芯片装载 | stage=chip_created | entry=" + (entryIndex + 1)
                        + " | chipDef=" + entry.chipDef.defName,
                        chipCreationStart);

                    if (chip == null)
                    {
                        ReportInitialFixedLoadoutFailure(entry, entryIndex, "thing_creation_failed", null);
                        RollbackInitialFixedLoadout(createdChips);
                        return;
                    }

                    createdChips.Add(chip);
                    long loadChipStart = BdpDiagnostics.BeginTiming();
                    if (!TriggerLoadoutService.TryLoadChip(
                        BuildLoadoutContext(),
                        entry.side,
                        entry.slotNumber - 1,
                        chip))
                    {
                        ReportInitialFixedLoadoutFailure(entry, entryIndex, "load_rejected", null);
                        RollbackInitialFixedLoadout(createdChips);
                        return;
                    }
                    BdpDiagnostics.TraceTiming(
                        "固定芯片装载 | stage=chip_loaded | entry=" + (entryIndex + 1)
                        + " | side=" + entry.side
                        + " | slot=" + entry.slotNumber,
                        loadChipStart);

                    TraceLifecycle(
                        "fixed_loadout_entry_loaded",
                        "entry=" + (entryIndex + 1)
                        + " | side=" + entry.side
                        + " | slot=" + entry.slotNumber
                        + " | initializationKey=" + (entry.initializationKey ?? "none")
                        + " | chip=" + SafeThingId(chip)
                        + " | chipDef=" + (chip.def != null ? chip.def.defName : "null"));
                }
                catch (Exception exception)
                {
                    ReportInitialFixedLoadoutFailure(entry, entryIndex, "exception", exception);
                    RollbackInitialFixedLoadout(createdChips);
                    return;
                }
            }

            TraceSlotBoundary(
                "fixed_loadout_complete",
                "entryCount=" + properties.fixedLoadout.Count);
            BdpDiagnostics.TraceTiming(
                "固定芯片装载 | stage=total | entryCount=" + properties.fixedLoadout.Count,
                loadoutStart);
        }

        /// <summary>
        /// 回滚本次首次生成批次创建的全部芯片和槽位引用。
        /// 不触碰此前已经存在、且不属于本批次的芯片。
        /// </summary>
        private void RollbackInitialFixedLoadout(IReadOnlyCollection<Thing> createdChips)
        {
            if (createdChips == null || createdChips.Count == 0)
            {
                return;
            }

            HashSet<Thing> createdChipSet = new HashSet<Thing>(createdChips);

            foreach (TriggerSlotState slot in EnumerateRawSlots())
            {
                if (slot == null || slot.LoadedChip == null || !createdChipSet.Contains(slot.LoadedChip))
                {
                    continue;
                }

                slot.SetActive(false);
                slot.ClearBinding();
                slot.SetLoadedChip(null);
            }

            foreach (Thing chip in createdChips)
            {
                if (chip == null || chip.Destroyed)
                {
                    continue;
                }

                if (chip.holdingOwner != null)
                {
                    chip.holdingOwner.Remove(chip);
                }

                chip.Destroy(DestroyMode.Vanish);
            }
        }

        /// <summary>
        /// 记录一次固定装载失败，不向玩家显示消息，也不安排自动重试。
        /// </summary>
        private void ReportInitialFixedLoadoutFailure(
            TriggerFixedLoadoutEntry entry,
            int entryIndex,
            string reason,
            Exception exception)
        {
            string triggerDefName = parent != null && parent.def != null
                ? parent.def.defName
                : "<unknown>";
            string chipDefName = entry != null && entry.chipDef != null
                ? entry.chipDef.defName
                : "<null>";
            string side = entry != null ? entry.side.ToString() : "<null>";
            int slotNumber = entry != null ? entry.slotNumber : 0;
            string exceptionText = exception != null ? exception.GetType().Name + ":" + exception.Message : "none";

            TraceLifecycle(
                "fixed_loadout_failed",
                "entry=" + (entryIndex + 1)
                + " | chipDef=" + chipDefName
                + " | side=" + side
                + " | slot=" + slotNumber
                + " | reason=" + reason
                + " | exception=" + exceptionText);

            BdpDiagnostics.Once(
                "trigger.fixed_loadout.initialization_failed." + triggerDefName,
                "固定芯片首次装载失败。trigger=" + triggerDefName
                + ", entry=" + (entryIndex + 1)
                + ", chipDef=" + chipDefName
                + ", side=" + side
                + ", slotNumber=" + slotNumber
                + ", reason=" + reason
                + ", exception=" + exceptionText);
        }
    }
}
