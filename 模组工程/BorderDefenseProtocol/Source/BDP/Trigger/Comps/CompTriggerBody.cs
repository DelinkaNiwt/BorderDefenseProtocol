using System.Collections.Generic;
using System.Linq;
using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 触发体核心Comp——管理芯片槽位状态机和激活逻辑。
    /// 依赖BDP.Core.CompTrion（通过Pawn.GetComp获取）。
    ///
    /// ⚠️ 关键约束：装备后的武器CompTick()不会被调用。
    ///    Pawn_EquipmentTracker.EquipmentTrackerTick()只调用VerbsTick()，不调用CompTick()。
    ///    因此切换冷却等时间逻辑采用懒求值：在IsSwitching等属性访问时检查并结算。
    ///
    /// v2.0变更：
    ///   - T23：槽位语义从Main/Sub改为Left/Right
    ///   - T24：按侧Verb存储（leftActiveVerbProps/rightActiveVerbProps），由DualVerbCompositor合成
    ///   - §8.3：新增SetSideVerbs/ClearSideVerbs/GetChipSide API
    ///
    /// 不变量：
    ///   ① 每侧激活芯片数 ≤ 1
    ///   ② 已装载芯片数 ≤ 该侧槽位数
    ///   ③ hasRight==false时rightSlots为空
    ///   ④ switchState==Switching时pending!=null
    ///   ⑤ switchState==Idle时pending==null
    ///   ⑥ isActive==true的槽位loadedChip!=null
    ///   ⑦ allowChipManagement==false时loadedChip不可被玩家修改
    /// </summary>
    public class CompTriggerBody : CompEquippable, IVerbOwner
    {
        // ── 槽位数据（v2.0：mainSlots/subSlots → leftSlots/rightSlots） ──
        private List<ChipSlot> leftSlots;
        private List<ChipSlot> rightSlots;

        // ── 切换状态机 ──
        private SwitchState switchState = SwitchState.Idle;
        private SwitchContext pending;

        // ── 调试用：临时覆盖冷却时间 ──
        private bool debugZeroCooldown = false;

        // ── 按侧Verb存储（v2.0 T24：替代单一ActiveVerbProperties/ActiveTools） ──
        // 由WeaponChipEffect通过SetSideVerbs设置，DualVerbCompositor合成最终结果
        private List<VerbProperties> leftActiveVerbProps;
        private List<Tool> leftActiveTools;
        private List<VerbProperties> rightActiveVerbProps;
        private List<Tool> rightActiveTools;

        /// <summary>
        /// 当前正在激活/关闭的槽位侧别（临时上下文）。
        /// 在DoActivate/DeactivateSlot中设置，供WeaponChipEffect等效果类读取自己所在侧。
        /// 调用effect.Activate/Deactivate前设置，调用后清除。
        /// </summary>
        internal SlotSide? ActivatingSide { get; private set; }

        // ── 显式实现IVerbOwner接口（v2.0：改为调用DualVerbCompositor） ──
        // VerbTracker.InitVerbs()通过IVerbOwner接口调用，显式实现可正确拦截
        List<VerbProperties> IVerbOwner.VerbProperties
            => DualVerbCompositor.ComposeVerbs(leftActiveVerbProps, rightActiveVerbProps)
               ?? parent.def.Verbs;

        List<Tool> IVerbOwner.Tools
            => DualVerbCompositor.ComposeTools(leftActiveTools, rightActiveTools)
               ?? parent.def.tools;

        // ── 便利属性 ──
        public CompProperties_TriggerBody Props => (CompProperties_TriggerBody)props;

        // Holder来自CompEquippable：(parent.ParentHolder as Pawn_EquipmentTracker)?.pawn
        private Pawn OwnerPawn => Holder;

        private CompTrion TrionComp => OwnerPawn?.GetComp<CompTrion>();

        // ── 公开属性（v2.0：MainSlots/SubSlots → LeftSlots/RightSlots） ──

        /// <summary>左侧槽位列表（只读，供UI层访问）。懒初始化以兼容CharacterEditor等外部工具。</summary>
        public IReadOnlyList<ChipSlot> LeftSlots { get { EnsureSlotsInitialized(); return leftSlots; } }
        /// <summary>右侧槽位列表（只读，供UI层访问）。懒初始化以兼容CharacterEditor等外部工具。</summary>
        public IReadOnlyList<ChipSlot> RightSlots { get { EnsureSlotsInitialized(); return rightSlots; } }

        /// <summary>
        /// 当前是否处于切换空窗期。
        /// 懒求值：访问时自动结算到期的冷却。
        /// </summary>
        public bool IsSwitching
        {
            get
            {
                TryResolvePendingSwitch();
                return switchState == SwitchState.Switching;
            }
        }

        /// <summary>切换进度（0=刚开始，1=完成），仅IsSwitching时有意义。</summary>
        public float SwitchProgress
        {
            get
            {
                TryResolvePendingSwitch();
                if (pending == null || Props.switchCooldownTicks <= 0) return 1f;
                int remaining = pending.cooldownTick - Find.TickManager.TicksGame;
                return 1f - Mathf.Clamp01((float)remaining / Props.switchCooldownTicks);
            }
        }

        /// <summary>懒求值：检查切换冷却是否到期，到期则结算。</summary>
        private void TryResolvePendingSwitch()
        {
            if (switchState != SwitchState.Switching || pending == null) return;
            if (Find.TickManager.TicksGame < pending.cooldownTick) return;

            // 冷却到期，尝试激活目标芯片
            if (CanActivateChip(pending.side, pending.slotIndex))
                DoActivate(GetSlot(pending.side, pending.slotIndex));

            switchState = SwitchState.Idle;
            pending = null;
        }

        /// <summary>懒初始化槽位列表（兼容CharacterEditor等外部工具）。</summary>
        private void EnsureSlotsInitialized()
        {
            if (leftSlots == null)
                leftSlots = InitSlots(SlotSide.Left, Props.leftSlotCount);
            if (Props.hasRight && rightSlots == null)
                rightSlots = InitSlots(SlotSide.Right, Props.rightSlotCount);
        }

        /// <summary>检查装备者Pawn是否拥有Trion腺体基因。</summary>
        public bool OwnerHasTrionGland()
        {
            var pawn = OwnerPawn;
            return pawn?.genes?.HasActiveGene(BDP_DefOf.BDP_Gene_TrionGland) ?? false;
        }

        // ── 槽位访问 ──

        public ChipSlot GetSlot(SlotSide side, int index)
        {
            var list = side == SlotSide.Left ? leftSlots : rightSlots;
            if (list == null || index < 0 || index >= list.Count) return null;
            return list[index];
        }

        public IEnumerable<ChipSlot> AllSlots()
        {
            if (leftSlots != null) foreach (var s in leftSlots) yield return s;
            if (rightSlots != null) foreach (var s in rightSlots) yield return s;
        }

        public IEnumerable<ChipSlot> AllActiveSlots()
            => AllSlots().Where(s => s.isActive && s.loadedChip != null);

        public bool HasAnyActiveChip()
            => AllSlots().Any(s => s.isActive);

        public ChipSlot GetActiveSlot(SlotSide side)
            => (side == SlotSide.Left ? leftSlots : rightSlots)
               ?.FirstOrDefault(s => s.isActive);

        // ═══════════════════════════════════════════
        //  按侧Verb管理（v2.0 §8.3 新增API）
        // ═══════════════════════════════════════════

        /// <summary>设置指定侧的Verb/Tool数据（供WeaponChipEffect调用）。</summary>
        public void SetSideVerbs(SlotSide side, List<VerbProperties> verbs, List<Tool> tools)
        {
            if (side == SlotSide.Left)
            {
                leftActiveVerbProps = verbs;
                leftActiveTools = tools;
            }
            else
            {
                rightActiveVerbProps = verbs;
                rightActiveTools = tools;
            }
        }

        /// <summary>清除指定侧的Verb/Tool数据（供WeaponChipEffect调用）。</summary>
        public void ClearSideVerbs(SlotSide side)
        {
            if (side == SlotSide.Left)
            {
                leftActiveVerbProps = null;
                leftActiveTools = null;
            }
            else
            {
                rightActiveVerbProps = null;
                rightActiveTools = null;
            }
        }

        /// <summary>查找芯片所在侧别（遍历所有激活槽位）。</summary>
        public SlotSide GetChipSide(Thing chip)
        {
            foreach (var slot in AllActiveSlots())
                if (slot.loadedChip == chip) return slot.side;
            // 未找到时默认左侧
            return SlotSide.Left;
        }

        // ═══════════════════════════════════════════
        //  芯片装载/卸载
        // ═══════════════════════════════════════════

        /// <summary>将芯片物品装入指定槽位（芯片被触发体"吸收"，从持有者库存移除）。</summary>
        public bool LoadChip(SlotSide side, int slotIndex, Thing chip)
        {
            if (!Props.allowChipManagement) return false;
            var slot = GetSlot(side, slotIndex);
            if (slot == null || slot.loadedChip != null) return false;
            if (chip?.TryGetComp<TriggerChipComp>() == null) return false;

            slot.loadedChip = chip;
            chip.holdingOwner?.Remove(chip);
            return true;
        }

        /// <summary>从槽位取出芯片物品（生成到Pawn库存或地面）。</summary>
        public bool UnloadChip(SlotSide side, int slotIndex)
        {
            if (!Props.allowChipManagement) return false;
            var slot = GetSlot(side, slotIndex);
            if (slot?.loadedChip == null) return false;

            if (slot.isActive) DeactivateSlot(slot);

            var chip = slot.loadedChip;
            slot.loadedChip = null;

            var pawn = OwnerPawn;
            if (pawn != null && pawn.inventory != null)
                pawn.inventory.TryAddItemNotForSale(chip);
            else
                GenPlace.TryPlaceThing(chip, parent.Position, parent.Map, ThingPlaceMode.Near);

            return true;
        }

        // ═══════════════════════════════════════════
        //  前置条件检查
        // ═══════════════════════════════════════════

        /// <summary>检查指定槽位是否可以激活（供UI灰显和ActivateChip前置检查）。</summary>
        public bool CanActivateChip(SlotSide side, int slotIndex)
        {
            var slot = GetSlot(side, slotIndex);
            if (slot?.loadedChip == null) return false;

            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
            if (chipComp == null) return false;

            var pawn = OwnerPawn;
            if (pawn == null) return false;

            var trion = TrionComp;
            if (trion != null && chipComp.Props.activationCost > 0f)
                if (trion.Available < chipComp.Props.activationCost) return false;

            var effect = chipComp.GetEffect();
            return effect?.CanActivate(pawn, parent) ?? false;
        }

        // ═══════════════════════════════════════════
        //  激活/关闭
        // ═══════════════════════════════════════════

        /// <summary>激活指定槽位（含切换逻辑）。</summary>
        public bool ActivateChip(SlotSide side, int slotIndex)
        {
            var slot = GetSlot(side, slotIndex);
            if (slot?.loadedChip == null) return false;
            if (!CanActivateChip(side, slotIndex)) return false;
            if (switchState == SwitchState.Switching) return false;

            var existingActive = GetActiveSlot(side);

            if (existingActive != null && existingActive != slot)
            {
                // 切换路径：先关闭旧芯片，进入空窗期
                DeactivateSlot(existingActive);
                int cooldown = debugZeroCooldown ? 0 : Props.switchCooldownTicks;
                switchState = SwitchState.Switching;
                pending = new SwitchContext(side, slotIndex, Find.TickManager.TicksGame + cooldown);
            }
            else
            {
                // 直接激活路径
                DoActivate(slot);
            }

            return true;
        }

        /// <summary>关闭指定侧的当前激活芯片。</summary>
        public void DeactivateChip(SlotSide side)
        {
            var active = GetActiveSlot(side);
            if (active != null) DeactivateSlot(active);
        }

        /// <summary>关闭所有激活芯片（卸下触发体时调用）。</summary>
        public void DeactivateAll()
        {
            foreach (var slot in AllSlots())
                if (slot.isActive) DeactivateSlot(slot);

            // 清除按侧Verb数据（v2.0）
            leftActiveVerbProps = null; leftActiveTools = null;
            rightActiveVerbProps = null; rightActiveTools = null;

            switchState = SwitchState.Idle;
            pending = null;
        }

        private void DoActivate(ChipSlot slot)
        {
            var pawn = OwnerPawn;
            if (pawn == null) return;

            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
            var effect = chipComp?.GetEffect();
            if (effect == null) return;

            float cost = chipComp.Props.activationCost;
            if (cost > 0f) TrionComp?.Consume(cost);

            // 设置激活上下文（供WeaponChipEffect等读取侧别）
            ActivatingSide = slot.side;
            effect.Activate(pawn, parent);
            ActivatingSide = null;

            slot.isActive = true;
        }

        private void DeactivateSlot(ChipSlot slot)
        {
            if (!slot.isActive || slot.loadedChip == null) return;
            var pawn = OwnerPawn;
            var effect = slot.loadedChip.TryGetComp<TriggerChipComp>()?.GetEffect();

            // 设置激活上下文（供WeaponChipEffect等读取侧别）
            ActivatingSide = slot.side;
            effect?.Deactivate(pawn, parent);
            ActivatingSide = null;

            slot.isActive = false;
        }

        // ═══════════════════════════════════════════
        //  CompTick — 已移除
        //  原因：装备后的武器CompTick()不被调用。
        //  切换冷却改为懒求值，由UI每帧访问IsSwitching时触发。
        // ═══════════════════════════════════════════

        // ═══════════════════════════════════════════
        //  生命周期
        // ═══════════════════════════════════════════

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                leftSlots = InitSlots(SlotSide.Left, Props.leftSlotCount);
                rightSlots = Props.hasRight ? InitSlots(SlotSide.Right, Props.rightSlotCount) : null;

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

            if (Props.autoActivateOnEquip)
            {
                foreach (var slot in AllSlots())
                    if (slot.loadedChip != null && !slot.isActive)
                        ActivateChip(slot.side, slot.index);
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            DeactivateAll();
            base.Notify_Unequipped(pawn);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            DeactivateAll();
            base.PostDestroy(mode, previousMap);
        }

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

        // ═══════════════════════════════════════════
        //  存档
        // ═══════════════════════════════════════════

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Collections.Look(ref leftSlots, "leftSlots", LookMode.Deep);
            Scribe_Collections.Look(ref rightSlots, "rightSlots", LookMode.Deep);
            Scribe_Values.Look(ref switchState, "switchState");
            Scribe_Deep.Look(ref pending, "pending");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (leftSlots == null) leftSlots = InitSlots(SlotSide.Left, Props.leftSlotCount);
                if (Props.hasRight && rightSlots == null) rightSlots = InitSlots(SlotSide.Right, Props.rightSlotCount);

                if (switchState == SwitchState.Idle) pending = null;

                // 恢复激活效果（IChipEffect无状态，重新调用Activate()）
                var pawn = OwnerPawn;
                if (pawn != null)
                {
                    foreach (var slot in AllSlots())
                    {
                        if (slot.isActive && slot.loadedChip != null)
                        {
                            var effect = slot.loadedChip.TryGetComp<TriggerChipComp>()?.GetEffect();
                            ActivatingSide = slot.side;
                            effect?.Activate(pawn, parent);
                            ActivatingSide = null;
                        }
                    }
                }
            }
        }

        // ═══════════════════════════════════════════
        //  Gizmo
        // ═══════════════════════════════════════════

        public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra()
        {
            foreach (var g in base.CompGetEquippedGizmosExtra()) yield return g;

            if (!OwnerHasTrionGland()) yield break;

            yield return new Gizmo_TriggerBodyStatus(this);

            if (!DebugSettings.godMode) yield break;
            foreach (var g in GetDebugGizmos()) yield return g;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;
        }

        private IEnumerable<Gizmo> GetDebugGizmos()
        {
            bool hasEmptyLeft = HasEmptySlot(SlotSide.Left);
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 填充左侧",
                defaultDesc = "随机取一个TriggerChip def，装入左侧第一个空槽",
                action = () => FillRandomChip(SlotSide.Left),
                Disabled = !hasEmptyLeft,
                disabledReason = "左侧无空槽"
            };
            bool hasEmptyRight = Props.hasRight && HasEmptySlot(SlotSide.Right);
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 填充右侧",
                defaultDesc = "随机取一个TriggerChip def，装入右侧第一个空槽",
                action = () => FillRandomChip(SlotSide.Right),
                Disabled = !hasEmptyRight,
                disabledReason = "右侧无空槽或无右侧"
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 清空左侧",
                defaultDesc = "关闭并卸载左侧所有芯片（绕过allowChipManagement）",
                action = () => ClearSide(SlotSide.Left)
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 清空右侧",
                defaultDesc = "关闭并卸载右侧所有芯片（绕过allowChipManagement）",
                action = () => ClearSide(SlotSide.Right)
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 状态转储",
                defaultDesc = "Log输出所有槽位状态、激活状态、Trion数据",
                action = DumpState
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 强制关闭",
                defaultDesc = "立即关闭所有激活效果（跳过空窗期）",
                action = DeactivateAll
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 重建Verb",
                defaultDesc = "强制调用pawn.verbTracker.InitVerbsFromZero()",
                action = () => OwnerPawn?.verbTracker?.InitVerbsFromZero()
            };
            yield return new Command_Action
            {
                defaultLabel = debugZeroCooldown ? "[Dev] 空窗=0 (ON)" : "[Dev] 空窗=0 (OFF)",
                defaultDesc = "切换：将switchCooldownTicks临时设为0（快速切换测试）",
                action = () => debugZeroCooldown = !debugZeroCooldown
            };
            yield return new Command_Action
            {
                defaultLabel = "[Dev] 切换锁定",
                defaultDesc = $"切换allowChipManagement（当前: {Props.allowChipManagement}）",
                action = () => Props.allowChipManagement = !Props.allowChipManagement
            };
        }

        private void DumpState()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[BDP] CompTriggerBody [{parent.LabelShortCap}] 状态转储:");
            sb.AppendLine($"  switchState={switchState}, pending={pending?.side}#{pending?.slotIndex}");
            sb.AppendLine($"  debugZeroCooldown={debugZeroCooldown}");
            sb.AppendLine($"  leftVerbProps={leftActiveVerbProps?.Count ?? 0}, rightVerbProps={rightActiveVerbProps?.Count ?? 0}");
            foreach (var slot in AllSlots())
                sb.AppendLine($"  {slot}");
            var trion = TrionComp;
            if (trion != null)
                sb.AppendLine($"  Trion: cur={trion.Cur:F1} max={trion.Max:F1} avail={trion.Available:F1}");
            Log.Message(sb.ToString());
        }

        private bool HasEmptySlot(SlotSide side)
        {
            var slots = side == SlotSide.Left ? leftSlots : rightSlots;
            return slots?.Any(s => s.loadedChip == null) ?? false;
        }

        private void FillRandomChip(SlotSide side)
        {
            var chipDefs = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.comps != null && d.comps.Any(c => c is CompProperties_TriggerChip))
                .ToList();
            if (chipDefs.Count == 0)
            {
                Log.Warning("[BDP] FillRandomChip: 未找到任何TriggerChip ThingDef");
                return;
            }
            var slots = side == SlotSide.Left ? leftSlots : rightSlots;
            var emptySlot = slots?.FirstOrDefault(s => s.loadedChip == null);
            if (emptySlot == null) return;
            LoadChipInternal(side, emptySlot.index, ThingMaker.MakeThing(chipDefs.RandomElement()));
        }

        private void ClearSide(SlotSide side)
        {
            var slots = side == SlotSide.Left ? leftSlots : rightSlots;
            if (slots == null) return;
            foreach (var slot in slots)
            {
                if (slot.isActive) DeactivateSlot(slot);
                slot.loadedChip = null;
            }
        }
    }
}
