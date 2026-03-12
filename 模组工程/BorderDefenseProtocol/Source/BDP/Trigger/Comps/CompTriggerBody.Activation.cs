using System.Collections.Generic;
using System.Linq;
using BDP.Core;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody部分类 - 激活/停用模块
    ///
    /// 职责：
    /// - 前置条件检查（CanActivateChip）
    /// - 芯片激活/关闭（ActivateChip, DeactivateChip, DeactivateAll）
    /// - 特殊槽管理（ActivateAllSpecial, DeactivateAllSpecial）
    /// - 战斗体Trion占用（TryAllocateTrionForCombatBody）
    ///
    /// v15.0变更：组合效果管理已移至 CompTriggerBody.ComboSystem.cs
    /// </summary>
    public partial class CompTriggerBody
    {
        // ═══════════════════════════════════════════

        /// <summary>
        /// 检查指定槽位是否可以激活（供UI灰显和ActivateChip前置检查）。
        /// v2.1新增检查：minOutputPower、dualHandLock、exclusionTags。
        /// </summary>
        public bool CanActivateChip(SlotSide side, int slotIndex)
        {
            // v6.0：战斗体未激活时不可激活任何芯片（不变量⑬）
            if (!IsCombatBodyActive) return false;

            var slot = GetSlot(side, slotIndex);
            if (slot?.loadedChip == null) return false;

            // 槽位被禁用（手部/手臂被毁）时不可激活
            if (slot.isDisabled) return false;

            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
            if (chipComp == null) return false;

            var pawn = OwnerPawn;
            if (pawn == null) return false;

            var trion = TrionComp;

            // ── 检查1：激活成本 ──
            if (trion != null && chipComp.Props.activationCost > 0f)
                if (trion.Available < chipComp.Props.activationCost) return false;

            // ── 检查2（v2.1）：最低输出功率 ──
            if (chipComp.Props.minOutputPower > 0f)
                if (pawn.GetStatValue(BDP_DefOf.BDP_TrionOutputPower) < chipComp.Props.minOutputPower)
                    return false;

            // ── 检查3（v2.1）：双手锁定 ──
            // 若有双手芯片激活且不是本槽位，则拒绝
            if (dualHandLockSlot != null && dualHandLockSlot != slot)
                return false;

            // ── 检查4（v2.1）：互斥标签（对称检查） ──
            // exclusionTags同时作为"我的标签"和"我排斥的标签"，交集非空则拒绝
            // 对称性：A∩B = B∩A，只需单向检查
            var myExclusions = chipComp.Props.exclusionTags;
            if (myExclusions != null && myExclusions.Count > 0)
            {
                foreach (var activeSlot in AllActiveSlots())
                {
                    if (activeSlot == slot) continue;
                    var activeExclusions = activeSlot.loadedChip?.TryGetComp<TriggerChipComp>()?.Props.exclusionTags;
                    if (activeExclusions == null) continue;
                    foreach (var tag in myExclusions)
                        if (activeExclusions.Contains(tag)) return false;
                }
            }

            // ── 检查5：效果自身的CanActivate（v4.0：遍历当前形态所有效果） ──
            var effects = chipComp.GetModeEffects(slot.currentModeIndex);
            if (effects == null || effects.Count == 0) return false;
            return effects.All(e => e.CanActivate(pawn, parent));
        }

        // ═══════════════════════════════════════════
        //  激活/关闭
        // ═══════════════════════════════════════════

        /// <summary>
        /// 激活指定槽位（v6.0重写：按侧独立状态机 + 必须前摇 + 后摇）。
        /// 所有激活都必须走前摇（WarmingUp），不再有直接DoActivate路径。
        /// 有旧芯片时：后摇(WindingDown) → 前摇(WarmingUp)。
        /// 无旧芯片时：直接进入前摇(WarmingUp)。
        /// </summary>
        public bool ActivateChip(SlotSide side, int slotIndex)
        {
            var slot = GetSlot(side, slotIndex);
            if (slot?.loadedChip == null) return false;
            if (!CanActivateChip(side, slotIndex)) return false;

            // v2.1：Special侧不走切换逻辑，委托给ActivateAllSpecial
            if (side == SlotSide.Special)
            {
                ActivateAllSpecial();
                return true;
            }

            // 本侧正在切换中，拒绝新请求
            if (IsSideSwitching(side)) return false;

            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();

            // v2.1（T31）：双手芯片——检查两侧都不在切换中
            if (chipComp?.Props.isDualHand == true)
            {
                if (IsSideSwitching(SlotSide.LeftHand) || IsSideSwitching(SlotSide.RightHand))
                    return false;

                var oppositeSide = side == SlotSide.LeftHand ? SlotSide.RightHand : SlotSide.LeftHand;
                var oppositeActive = GetActiveSlot(oppositeSide);
                if (oppositeActive != null)
                    DeactivateSlot(oppositeActive);
                var existingDual = GetActiveSlot(side);
                if (existingDual != null && existingDual != slot)
                    DeactivateSlot(existingDual);
                DoActivate(slot);
                return true;
            }

            // 获取本侧SwitchContext引用
            int now = Find.TickManager.TicksGame;
            var existingActive = GetActiveSlot(side);

            if (existingActive != null && existingActive != slot)
            {
                // 有旧芯片：检查后摇
                var oldChipComp = existingActive.loadedChip?.TryGetComp<TriggerChipComp>();
                int winddown = oldChipComp?.Props.deactivationDelay ?? 0;

                var ctx = new SwitchContext
                {
                    targetSlotIndex = slotIndex,
                };

                if (winddown > 0)
                {
                    // 进入后摇阶段（旧芯片仍isActive=true）
                    ctx.phase = SwitchPhase.WindingDown;
                    ctx.phaseEndTick = now + winddown;
                    ctx.winddownDuration = winddown;
                    ctx.windingDownSlotIndex = existingActive.index;
                }
                else
                {
                    // 无后摇：立即关闭旧芯片，进入前摇
                    DeactivateSlot(existingActive);
                    int warmup = chipComp?.Props.activationWarmup ?? 0;
                    int cooldown = System.Math.Max(Props.switchCooldownTicks, warmup);
                    ctx.phase = SwitchPhase.WarmingUp;
                    ctx.phaseEndTick = now + cooldown;
                    ctx.warmupDuration = cooldown;
                }

                SetSideCtx(side, ctx);
            }
            else
            {
                // 无旧芯片：直接进入前摇
                int warmup = chipComp?.Props.activationWarmup ?? 0;
                int cooldown = System.Math.Max(Props.switchCooldownTicks, warmup);

                var ctx = new SwitchContext
                {
                    phase = SwitchPhase.WarmingUp,
                    phaseEndTick = now + cooldown,
                    warmupDuration = cooldown,
                    targetSlotIndex = slotIndex,
                };

                SetSideCtx(side, ctx);

                // cooldown为0时立即结算
                if (cooldown <= 0)
                {
                    if (CanActivateChip(side, slotIndex))
                        DoActivate(slot);
                    SetSideCtx(side, null);
                }
            }

            return true;
        }

        /// <summary>设置指定侧的SwitchContext。</summary>
        private void SetSideCtx(SlotSide side, SwitchContext ctx)
        {
            if (side == SlotSide.LeftHand) leftSwitchCtx = ctx;
            else if (side == SlotSide.RightHand) rightSwitchCtx = ctx;
        }

        /// <summary>
        /// 在激活上下文中执行action（消除DoActivate/DeactivateSlot中的重复代码）。
        /// 设置ActivatingSide和ActivatingSlot，执行action，然后清理上下文。
        /// </summary>
        private void WithActivatingContext(ChipSlot slot, System.Action action)
        {
            ActivatingSide = slot.side;
            ActivatingSlot = slot;
            try
            {
                action();
            }
            finally
            {
                ActivatingSide = null;
                ActivatingSlot = null;
            }
        }

        /// <summary>
        /// 执行芯片激活（内部方法）。
        /// 消耗激活成本、注册持续消耗、调用effect.Activate、设置isActive标志。
        /// v4.0：支持多效果激活（遍历当前形态的所有效果）。
        /// </summary>
        private void DoActivate(ChipSlot slot)
        {
            var pawn = OwnerPawn;
            if (pawn == null) return;

            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
            var effects = chipComp?.GetModeEffects(slot.currentModeIndex);
            if (effects == null || effects.Count == 0) return;

            // ── 一次性激活成本 ──
            float cost = chipComp.Props.activationCost;
            if (cost > 0f) TrionComp?.Consume(cost);

            // ── v2.1（T32）：统一注册持续消耗 ──
            if (chipComp.Props.drainPerDay > 0f)
                TrionComp?.RegisterDrain($"chip_{slot.side}_{slot.index}", chipComp.Props.drainPerDay);

            // 设置激活上下文并调用所有效果的Activate
            WithActivatingContext(slot, () =>
            {
                foreach (var effect in effects)
                    effect.Activate(pawn, parent);
            });

            slot.isActive = true;

            // ── v2.1（T31）：双手锁定 ──
            if (chipComp.Props.isDualHand)
                dualHandLockSlot = slot;

            // ── v15.0：统一组合效果更新（替代 TryGrantComboAbility） ──
            UpdateComboEffects(pawn);
        }

        /// <summary>
        /// 执行芯片停用（内部方法）。
        /// 注销持续消耗、调用effect.Deactivate、清除isActive标志。
        /// v4.0：支持多效果停用（遍历当前形态的所有效果）。
        /// </summary>
        /// <param name="pawnOverride">显式Pawn引用，优先于OwnerPawn（卸下装备时OwnerPawn为null）。</param>
        private void DeactivateSlot(ChipSlot slot, Pawn pawnOverride = null)
        {
            if (!slot.isActive || slot.loadedChip == null)
            {
                return;
            }

            var pawn = pawnOverride ?? OwnerPawn;
            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
            var effects = chipComp?.GetModeEffects(slot.currentModeIndex);

            // ── v2.1（T32）：统一注销持续消耗 ──
            string drainKey = $"chip_{slot.side}_{slot.index}";
            Log.Message($"[BDP诊断] 注销芯片消耗 - key:{drainKey}");
            TrionComp?.UnregisterDrain(drainKey);

            // 设置激活上下文并调用所有效果的Deactivate
            WithActivatingContext(slot, () =>
            {
                if (effects != null)
                    foreach (var effect in effects)
                        effect.Deactivate(pawn, parent);
            });

            slot.isActive = false;

            // 重置形态索引为默认值（形态0），下次激活时从巨盾模式开始
            slot.currentModeIndex = 0;

            // ── v2.1（T31）：清除双手锁定 ──
            if (dualHandLockSlot == slot)
                dualHandLockSlot = null;

            // ── v15.0：统一组合效果更新（替代 TryRevokeComboAbilities） ──
            UpdateComboEffects(pawn);
        }

        /// <summary>
        /// 关闭指定侧的当前激活芯片（v6.0：支持后摇）。
        /// 有deactivationDelay时进入WindingDown阶段（芯片仍isActive=true），到期才真正关闭。
        /// 无deactivationDelay时立即关闭。
        /// </summary>
        public void DeactivateChip(SlotSide side)
        {
            var active = GetActiveSlot(side);
            if (active == null) return;

            // 该侧已在切换/后摇中，拒绝重复操作
            if (IsSideSwitching(side)) return;

            var chipComp = active.loadedChip?.TryGetComp<TriggerChipComp>();
            int winddown = chipComp?.Props.deactivationDelay ?? 0;

            if (winddown > 0)
            {
                // 进入后摇阶段（芯片仍isActive=true，后摇到期才真正关闭）
                int now = Find.TickManager.TicksGame;
                var ctx = new SwitchContext
                {
                    phase = SwitchPhase.WindingDown,
                    phaseEndTick = now + winddown,
                    winddownDuration = winddown,
                    windingDownSlotIndex = active.index,
                    targetSlotIndex = -1, // 无目标：纯关闭，不接前摇
                };
                SetSideCtx(side, ctx);
            }
            else
            {
                // 无后摇：立即关闭
                DeactivateSlot(active);
            }
        }

        /// <summary>关闭所有激活芯片（卸下触发体时调用）。</summary>
        /// <param name="pawn">显式传入的Pawn引用（卸下装备时OwnerPawn可能已为null）。</param>
        public void DeactivateAll(Pawn pawn = null)
        {
            // [诊断日志] 记录DeactivateAll调用
            Log.Message($"[BDP诊断] DeactivateAll开始 - Pawn:{pawn?.LabelShort ?? OwnerPawn?.LabelShort ?? "null"}");

            int deactivatedCount = 0;
            foreach (var slot in AllSlots())
            {
                if (!slot.isActive) continue;
                deactivatedCount++;
                try
                {
                    DeactivateSlot(slot, pawn);
                }
                catch (System.Exception ex)
                {
                    // 单个slot失败不影响其他slot的关闭
                    Log.Error($"[BDP] DeactivateSlot异常 ({slot}): {ex}");
                    slot.isActive = false; // 强制标记为关闭，防止残留
                }
            }

            Log.Message($"[BDP诊断] DeactivateAll完成 - 共关闭{deactivatedCount}个槽位");

            // 清除按侧Verb数据（v2.0）
            leftHandActiveVerbProps = null; leftHandActiveTools = null;
            rightHandActiveVerbProps = null; rightHandActiveTools = null;

            // v6.0：清除两侧切换上下文
            leftSwitchCtx = null;
            rightSwitchCtx = null;
            // v2.1：清除双手锁定
            dualHandLockSlot = null;
        }

        /// <summary>
        /// 尝试为战斗体占用Trion（v11.1修复：添加原子性保证）。
        /// 由Gene_TrionGland在战斗体激活前调用，用于检查并锁定Trion。
        ///
        /// 流程：
        /// 1. 计算总需求量
        /// 2. 检查是否足够（原子性检查）
        /// 3. 如果足够，设置标志并逐个Allocate；如果不足，返回false
        ///
        /// 返回值：
        /// - true: 所有芯片成功Allocate，可以激活战斗体
        /// - false: Trion不足或TrionComp不存在，不应激活
        ///
        /// 注意：此方法只负责Allocate，不激活特殊槽芯片（由ActivateAllSpecial单独调用）。
        /// </summary>

        // ── 战斗体管理方法已移至 CompTriggerBody.CombatBodySupport.cs ──

        // ═══════════════════════════════════════════
        //  组合效果管理（v15.0已移至 CompTriggerBody.ComboSystem.cs）
        // ═══════════════════════════════════════════

        // ═══════════════════════════════════════════
        //  CompTick — 已移除
        //  原因：装备后的武器CompTick()不被调用。
        //  切换冷却改为懒求值，由UI每帧访问IsSwitching时触发。
        // ═══════════════════════════════════════════

        // ═══════════════════════════════════════════
        //  生命周期
        // ═══════════════════════════════════════════

        // 手部缺失联动已改为由Patch_Pawn_PostApplyDamage直接调用
        // CompTriggerBody.CheckHandIntegrity()，不再依赖BDPEvents事件。

        // ═══════════════════════════════════════════
        //  存档
        // ═══════════════════════════════════════════

        // PostSpawnSetup、Notify_Equipped、Notify_Unequipped、PostDestroy、PostExposeData
        // 已移至 CompTriggerBody.Lifecycle.cs


        // ── Gizmo生成方法已移至 CompTriggerBody.GizmoGeneration.cs ──

        // ═══════════════════════════════════════════
        //  形态切换（v4.0 ChipMode系统）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 切换指定槽位的芯片形态。
        /// 若芯片当前激活，先关闭旧形态效果，切换形态索引，再激活新形态效果。
        /// </summary>
        public bool SwitchChipMode(SlotSide side, int slotIndex, int targetModeIndex)
        {
            var slot = GetSlot(side, slotIndex);
            if (slot?.loadedChip == null) return false;

            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
            if (chipComp == null) return false;

            if (!chipComp.CanSwitchMode(slot, targetModeIndex)) return false;

            // 检查切换成本
            var targetMode = chipComp.Props.modes[targetModeIndex];
            if (targetMode.switchCost > 0f && (TrionComp?.Available ?? 0f) < targetMode.switchCost)
                return false;

            bool wasActive = slot.isActive;

            // 先关闭当前形态效果
            if (wasActive) DeactivateSlot(slot);

            // 消耗切换成本
            if (targetMode.switchCost > 0f)
                TrionComp?.Consume(targetMode.switchCost);

            // 切换形态索引
            slot.currentModeIndex = targetModeIndex;

            // 重新激活新形态效果
            if (wasActive) DoActivate(slot);

            return true;
        }

    }
}
