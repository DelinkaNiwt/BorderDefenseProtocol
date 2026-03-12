using System.Collections.Generic;
using BDP.Core;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody生命周期钩子（partial class）
    /// </summary>
    public partial class CompTriggerBody
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            // B5修复：只在槽位尚未初始化时才初始化。
            // 原因：触发体从装备卸下掉到地面时，GenSpawn.Spawn调用PostSpawnSetup(respawningAfterLoad: false)，
            // 若无条件重新初始化会清空已装载的芯片。加 leftHandSlots == null 守卫避免覆盖。
            if (!respawningAfterLoad && leftHandSlots == null)
            {
                leftHandSlots = InitSlots(SlotSide.LeftHand, Props.leftHandSlotCount);
                rightHandSlots = Props.hasRightHand ? InitSlots(SlotSide.RightHand, Props.rightHandSlotCount) : null;
                // v2.1：初始化特殊槽
                specialSlots = Props.specialSlotCount > 0
                             ? InitSlots(SlotSide.Special, Props.specialSlotCount)
                             : null;

                if (Props.preloadedChips != null)
                {
                    foreach (var cfg in Props.preloadedChips)
                    {
                        if (cfg.chipDef == null) continue;
                        var chip = ThingMaker.MakeThing(cfg.chipDef);
                        LoadChipInternal(cfg.side, cfg.slotIndex, chip);
                    }
                }
            }
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            EnsureSlotsInitialized();

            // v6.0：注册Trion耗尽事件
            var trion = TrionComp;
            if (trion != null) trion.OnAvailableDepleted += OnTrionDepleted;

            // v1.8：装备时同步预占用值
            SyncReservedAllocationToTrion();

            if (Props.autoActivateOnEquip)
            {
                foreach (var slot in AllSlots())
                    if (slot.loadedChip != null && !slot.isActive)
                        ActivateChip(slot.side, slot.index);
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            // v2.2修复：如果战斗体激活中，必须在这里直接释放Trion
            // 原因：此时触发体即将从pawn.equipment.Primary移除，Core层无法通过反射找到它
            // 所以必须在这里（触发体还存在时）完成Trion释放
            if (IsCombatBodyActive)
            {
                Log.Message($"[BDP] 触发体被卸下，战斗体激活中，直接释放Trion: {pawn?.Name}");

                // 1. 直接调用DismissCombatBody释放Trion（使用传入的pawn参数）
                DismissCombatBody(pawn);

                // 2. 通知Core层解除战斗体（换装、恢复Hediff等，但不再释放Trion）
                BDPEvents.TriggerDeactivateRequest(pawn);
            }

            // v6.0：注销Trion耗尽事件（pawn参数即卸下前的持有者）
            var trion = pawn?.GetComp<CompTrion>();
            if (trion != null)
            {
                trion.OnAvailableDepleted -= OnTrionDepleted;
                // v1.8：卸下时清零预占用值
                trion.UpdateReservedAllocation(0f);
            }

            DeactivateAll(pawn);
            base.Notify_Unequipped(pawn);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            DeactivateAll();
            base.PostDestroy(mode, previousMap);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Collections.Look(ref leftHandSlots, "leftHandSlots", LookMode.Deep);
            Scribe_Collections.Look(ref rightHandSlots, "rightHandSlots", LookMode.Deep);
            // v2.1：序列化特殊槽
            Scribe_Collections.Look(ref specialSlots, "specialSlots", LookMode.Deep);
            // v6.0：按侧独立切换上下文（旧存档中不存在，Scribe_Deep返回null即Idle）
            Scribe_Deep.Look(ref leftSwitchCtx, "leftSwitchCtx");
            Scribe_Deep.Look(ref rightSwitchCtx, "rightSwitchCtx");
            // dualHandLockSlot是运行时引用，不序列化，读档后由激活恢复逻辑重建

            // v3.1：序列化IsCombatBodyActive（战斗体状态需跨存档保持）
            bool isCombatBodyActive = IsCombatBodyActive;
            Scribe_Values.Look(ref isCombatBodyActive, "isCombatBodyActive");
            IsCombatBodyActive = isCombatBodyActive;

            // v8.0 PMS重构：序列化芯片Verb，使读档时Job/Stance能解析Verb引用
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                savedChipVerbs = new List<Verb>();
                if (leftHandAttackVerb != null) savedChipVerbs.Add(leftHandAttackVerb);
                if (rightHandAttackVerb != null) savedChipVerbs.Add(rightHandAttackVerb);
                if (dualAttackVerb != null) savedChipVerbs.Add(dualAttackVerb);
                if (leftHandSecondaryVerb != null) savedChipVerbs.Add(leftHandSecondaryVerb);
                if (rightHandSecondaryVerb != null) savedChipVerbs.Add(rightHandSecondaryVerb);
                if (dualSecondaryVerb != null) savedChipVerbs.Add(dualSecondaryVerb);
                // v10.0：组合技Verb（v9.0重命名）
                if (comboAttackVerb != null) savedChipVerbs.Add(comboAttackVerb);
                if (comboSecondaryVerb != null) savedChipVerbs.Add(comboSecondaryVerb);
            }
            Scribe_Collections.Look(ref savedChipVerbs, "chipVerbs", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (leftHandSlots == null) leftHandSlots = InitSlots(SlotSide.LeftHand, Props.leftHandSlotCount);
                if (Props.hasRightHand && rightHandSlots == null) rightHandSlots = InitSlots(SlotSide.RightHand, Props.rightHandSlotCount);
                // v2.1：特殊槽读档恢复
                if (Props.specialSlotCount > 0 && specialSlots == null)
                    specialSlots = InitSlots(SlotSide.Special, Props.specialSlotCount);

                // 恢复激活效果（IChipEffect无状态，重新调用Activate()）
                var pawn = OwnerPawn;
                if (pawn != null)
                {
                    // v6.0：读档后重新注册Trion耗尽事件
                    var trion = pawn.GetComp<CompTrion>();
                    if (trion != null) trion.OnAvailableDepleted += OnTrionDepleted;

                    foreach (var slot in AllSlots())
                    {
                        if (slot.isActive && slot.loadedChip != null)
                        {
                            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                            var effects = chipComp?.GetModeEffects(slot.currentModeIndex);
                            // C3修复：try/finally保护读档恢复路径
                            ActivatingSide = slot.side;
                            ActivatingSlot = slot;
                            try
                            {
                                if (effects != null)
                                    foreach (var effect in effects)
                                        effect.Activate(pawn, parent);
                            }
                            finally
                            {
                                ActivatingSide = null;
                                ActivatingSlot = null;
                            }

                            // v2.1：读档后重建dualHandLockSlot
                            if (chipComp?.Props.isDualHand == true)
                                dualHandLockSlot = slot;
                        }
                    }
                }
            }
        }

        // ── 辅助方法 ──

        private static List<ChipSlot> InitSlots(SlotSide side, int count)
        {
            var list = new List<ChipSlot>(count);
            for (int i = 0; i < count; i++) list.Add(new ChipSlot(i, side));
            return list;
        }

        /// <summary>内部装载（不检查allowChipManagement，用于预装和读档恢复）。</summary>
        private void LoadChipInternal(SlotSide side, int slotIndex, Thing chip)
        {
            var slot = GetSlot(side, slotIndex);
            if (slot == null || slot.loadedChip != null) return;
            slot.loadedChip = chip;
        }
    }
}
