using System.Collections.Generic;
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
    /// - 组合能力管理（TryGrantComboAbility, TryRevokeComboAbilities）
    /// - 战斗体Trion占用（TryAllocateTrionForCombatBody）
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

            // ── 检查5：效果自身的CanActivate ──
            var effect = chipComp.GetEffect();
            return effect?.CanActivate(pawn, parent) ?? false;
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
            foreach (var slot in AllSlots())
            {
                if (!slot.isActive) continue;
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
        public bool TryAllocateTrionForCombatBody()
        {

            var trion = TrionComp;
            if (trion == null)
            {
                Log.Error($"[BDP] TryAllocateTrionForCombatBody失败: TrionComp为null");
                return false;
            }

            // 1. 计算总需求量
            float totalCost = 0f;
            var slotsWithCost = new List<(ChipSlot slot, float cost)>();

            foreach (var slot in AllSlots())
            {
                if (slot.loadedChip == null) continue;
                var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                if (chipComp != null && chipComp.Props.allocationCost > 0f)
                {
                    totalCost += chipComp.Props.allocationCost;
                    slotsWithCost.Add((slot, chipComp.Props.allocationCost));
                }
            }

            // 2. 检查是否足够（原子性检查）
            if (trion.Available < totalCost)
            {
                // 改为普通日志，避免红色报错
                return false;
            }

            // 3. 设置战斗体激活标志（必须在Allocate之前设置，因为某些检查依赖此标志）
            IsCombatBodyActive = true;

            // 4. 逐个Allocate（此时已确保足够，理论上不会失败）
            float totalAllocated = 0f;
            int successCount = 0;

            foreach (var (slot, cost) in slotsWithCost)
            {
                bool ok = trion.Allocate(cost);
                if (ok)
                {
                    totalAllocated += cost;
                    successCount++;
                }
                else
                {
                    // 理论上不应该走到这里（因为已经预检查过）
                    Log.Error($"[BDP] Allocate失败（异常情况）: {slot.loadedChip.def.defName} cost={cost:F1} available={trion.Available:F1}");
                }
            }


            // 5. 返回成功
            return true;
        }

        /// <summary>
        /// 开始战斗体激活流程（v2.2重构版 - 已废弃，保留用于兼容性）。
        ///
        /// ⚠️ 此方法已被拆分为TryAllocateTrionForCombatBody + ActivateAllSpecial。
        /// 新代码应使用拆分后的方法以保证原子性。
        ///
        /// 由Gene_TrionGland在战斗体激活时调用。
        /// 流程：设置标志 → 逐个芯片Allocate → 激活特殊槽。
        /// </summary>
        [System.Obsolete("已废弃，请使用TryAllocateTrionForCombatBody + ActivateAllSpecial")]
        public void BeginCombatBodyActivation()
        {
            Log.Message($"[BDP] BeginCombatBodyActivation() 被调用（已废弃）");

            // 1. 设置战斗体激活标志
            IsCombatBodyActive = true;

            // 2. 遍历所有芯片，逐个Allocate（锁定Trion占用）
            var trion = TrionComp;
            float totalAllocated = 0f;
            foreach (var slot in AllSlots())
            {
                if (slot.loadedChip == null) continue;
                var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                if (chipComp != null && chipComp.Props.allocationCost > 0f)
                {
                    bool ok = trion?.Allocate(chipComp.Props.allocationCost) ?? false;
                    if (ok)
                    {
                        totalAllocated += chipComp.Props.allocationCost;
                    }
                    else
                    {
                        Log.Warning($"[BDP] Allocate失败: {slot.loadedChip.def.defName} cost={chipComp.Props.allocationCost} available={trion?.Available ?? 0f:F1}");
                    }
                }
            }

            // 3. 激活特殊槽芯片
            ActivateAllSpecial();

            Log.Message($"[BDP] 战斗体激活完成 (allocated={totalAllocated:F1}, trion={trion?.Cur:F1}/{trion?.Max:F1})");
        }

        /// <summary>
        /// 激活所有特殊槽（全部同时激活）。
        /// 特殊槽不参与切换状态机，不受切换冷却影响（不变量⑨⑫）。
        /// 由战斗体模块在战斗体生成时调用。v2.1新增。
        /// </summary>
        public void ActivateAllSpecial()
        {
            if (specialSlots == null) return;
            foreach (var slot in specialSlots)
            {
                if (slot.loadedChip == null || slot.isActive) continue;
                if (CanActivateChip(SlotSide.Special, slot.index))
                    DoActivate(slot);
            }
        }

        /// <summary>
        /// 关闭所有特殊槽（全部同时关闭）。
        /// 由战斗体模块在战斗体解除时调用。v2.1新增。
        /// </summary>
        public void DeactivateAllSpecial()
        {
            if (specialSlots == null) return;
            foreach (var slot in specialSlots)
                if (slot.isActive) DeactivateSlot(slot, null);
        }

        private void DoActivate(ChipSlot slot)
        {
            var pawn = OwnerPawn;
            if (pawn == null) return;

            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
            var effect = chipComp?.GetEffect();
            if (effect == null) return;

            // ── 一次性激活成本 ──
            float cost = chipComp.Props.activationCost;
            if (cost > 0f) TrionComp?.Consume(cost);

            // ── v2.1（T32）：统一注册持续消耗 ──
            if (chipComp.Props.drainPerDay > 0f)
                TrionComp?.RegisterDrain($"chip_{slot.side}_{slot.index}", chipComp.Props.drainPerDay);

            // 设置激活上下文（供WeaponChipEffect等读取侧别和槽位）
            // C3修复：try/finally保护，防止effect.Activate异常导致上下文残留
            ActivatingSide = slot.side;
            ActivatingSlot = slot;
            try
            {
                effect.Activate(pawn, parent);
            }
            finally
            {
                ActivatingSide = null;
                ActivatingSlot = null;
            }

            slot.isActive = true;

            // ── v2.1（T31）：双手锁定 ──
            if (chipComp.Props.isDualHand)
                dualHandLockSlot = slot;

            // ── v4.0（F1）：组合能力查询 ──
            TryGrantComboAbility(pawn);
        }

        /// <param name="pawnOverride">显式Pawn引用，优先于OwnerPawn（卸下装备时OwnerPawn为null）。</param>
        private void DeactivateSlot(ChipSlot slot, Pawn pawnOverride = null)
        {
            if (!slot.isActive || slot.loadedChip == null) return;
            var pawn = pawnOverride ?? OwnerPawn;
            var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
            var effect = chipComp?.GetEffect();

            // ── v2.1（T32）：统一注销持续消耗 ──
            TrionComp?.UnregisterDrain($"chip_{slot.side}_{slot.index}");

            // 设置激活上下文（供WeaponChipEffect等读取侧别和槽位）
            // C3修复：try/finally保护，防止effect.Deactivate异常导致上下文残留
            ActivatingSide = slot.side;
            ActivatingSlot = slot;
            try
            {
                effect?.Deactivate(pawn, parent);
            }
            finally
            {
                ActivatingSide = null;
                ActivatingSlot = null;
            }

            slot.isActive = false;

            // ── v2.1（T31）：清除双手锁定 ──
            if (dualHandLockSlot == slot)
                dualHandLockSlot = null;

            // ── v4.0（F1）：组合能力移除（芯片关闭后重新检查） ──
            TryRevokeComboAbilities(pawn);
        }

        // ═══════════════════════════════════════════
        //  战斗体管理
        // ═══════════════════════════════════════════

        /// <summary>
        /// 解除战斗体：关闭所有芯片 → 释放全部Trion占用 → 标记未激活。
        /// 释放逻辑基于trion.Allocated（Single Source of Truth），不依赖芯片是否仍在槽位中。
        /// </summary>
        /// <param name="pawnOverride">显式指定Pawn（用于Notify_Unequipped中，此时OwnerPawn可能已为null）</param>
        public void DismissCombatBody(Pawn pawnOverride = null)
        {
            DeactivateAll(pawnOverride);
            // 清除所有槽位的禁用标志（战斗体解除 → 部位恢复 → 槽位可用）
            foreach (var slot in AllSlots())
                slot.isDisabled = false;
            var trion = (pawnOverride ?? OwnerPawn)?.GetComp<CompTrion>();
            if (trion != null) trion.Release(trion.Allocated);
            IsCombatBodyActive = false;
        }

        /// <summary>
        /// 检查当前Trion是否足够生成战斗体（所有已装载芯片的allocationCost总和）。
        /// </summary>
        public bool CanGenerateCombatBody()
        {
            var trion = TrionComp;
            if (trion == null) return false;
            float totalCost = 0f;
            foreach (var slot in AllSlots())
            {
                if (slot.loadedChip == null) continue;
                var chipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                if (chipComp != null)
                    totalCost += chipComp.Props.allocationCost;
            }
            return trion.Available >= totalCost;
        }

        /// <summary>
        /// Trion可用值耗尽回调——自动解除战斗体。
        /// 由CompTrion.OnAvailableDepleted事件触发。
        /// </summary>
        private void OnTrionDepleted()
        {
            if (!IsCombatBodyActive) return;
            DismissCombatBody();
        }

        // ═══════════════════════════════════════════
        //  CompTick — 已移除
        //  原因：装备后的武器CompTick()不被调用。
        //  切换冷却改为懒求值，由UI每帧访问IsSwitching时触发。
        // ═══════════════════════════════════════════

        // ═══════════════════════════════════════════
        //  生命周期
        // ═══════════════════════════════════════════

        // 静态构造函数：订阅部位破坏事件（v12.2新增：手部缺失联动）
        static CompTriggerBody()
        {
            BDPEvents.OnPartDestroyed += OnPartDestroyed;
        }

        /// <summary>
        /// 响应部位破坏事件（静态事件处理器）。
        /// </summary>
        private static void OnPartDestroyed(PartDestroyedEventArgs args)
        {
            if (!args.IsHandPart) return;

            // 找到该Pawn装备的触发体
            CompTriggerBody comp = args.Pawn.equipment?.Primary?.GetComp<CompTriggerBody>();
            if (comp != null)
            {
                comp.OnHandDestroyed(args.HandSide);
            }
        }

        // ═══════════════════════════════════════════
        //  存档
        // ═══════════════════════════════════════════

        // PostSpawnSetup、Notify_Equipped、Notify_Unequipped、PostDestroy、PostExposeData
        // 已移至 CompTriggerBody.Lifecycle.cs

        // ═══════════════════════════════════════════
        //  Gizmo
        // ═══════════════════════════════════════════

        public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra()
        {
            foreach (var g in base.CompGetEquippedGizmosExtra()) yield return g;

            if (!OwnerHasTrionGland()) yield break;

            // ── v5.0新增：芯片攻击Gizmo（仅征召时显示，减少UI噪音） ──
            bool drafted = OwnerPawn?.Drafted == true;
            if (drafted)
            {
                var leftSlot = GetActiveSlot(SlotSide.LeftHand);
                var rightSlot = GetActiveSlot(SlotSide.RightHand);
                var leftChipDef = leftSlot?.loadedChip?.def;
                var rightChipDef = rightSlot?.loadedChip?.def;

                if (leftHandAttackVerb != null && leftChipDef != null)
                {
                    yield return new Command_BDPChipAttack
                    {
                        verb = leftHandAttackVerb,
                        secondaryVerb = leftHandSecondaryVerb, // v9.0：副攻击（可以是齐射或其他模式）
                        attackId = leftChipDef.defName,
                        icon = leftChipDef.uiIcon,
                        defaultLabel = leftChipDef.label,
                    };
                }
                if (rightHandAttackVerb != null && rightChipDef != null)
                {
                    yield return new Command_BDPChipAttack
                    {
                        verb = rightHandAttackVerb,
                        secondaryVerb = rightHandSecondaryVerb, // v9.0：副攻击（可以是齐射或其他模式）
                        attackId = rightChipDef.defName,
                        icon = rightChipDef.uiIcon,
                        defaultLabel = rightChipDef.label,
                    };
                }
                if (dualAttackVerb != null && leftChipDef != null && rightChipDef != null)
                {
                    // 排序保证A+B=B+A
                    var a = leftChipDef.defName;
                    var b = rightChipDef.defName;
                    if (string.Compare(a, b, System.StringComparison.Ordinal) > 0)
                    { var tmp = a; a = b; b = tmp; }

                    yield return new Command_BDPChipAttack
                    {
                        verb = dualAttackVerb,
                        secondaryVerb = dualSecondaryVerb, // v9.0：副攻击（可以是齐射或其他模式）
                        attackId = "dual:" + a + "+" + b,
                        icon = parent.def.uiIcon, // 触发体图标
                        defaultLabel = "双手触发",
                    };
                }

                // v10.0：组合技Gizmo（B+C同时激活时显示）
                if (comboAttackVerb != null && matchedComboDef != null)
                {
                    yield return new Command_BDPChipAttack
                    {
                        verb = comboAttackVerb,
                        secondaryVerb = comboSecondaryVerb, // v9.0：副攻击
                        attackId = "combo:" + matchedComboDef.defName,
                        icon = parent.def.uiIcon, // 暂用触发体图标
                        defaultLabel = matchedComboDef.label ?? "组合技",
                    };
                }
            }

            // v9.0：射击模式Gizmo（始终显示，方便战前配置）
            foreach (var slot in AllActiveSlots())
            {
                var fm = slot.loadedChip?.TryGetComp<CompFireMode>();
                if (fm != null)
                    yield return new Gizmo_FireMode(fm, slot.loadedChip.def.label);
            }

            // v2.1.1：allowChipManagement=false时不显示状态Gizmo
            // 原因：近界/黑触发器玩家无法操作芯片，显示Gizmo无意义
            if (Props.allowChipManagement)
                yield return new Gizmo_TriggerBodyStatus(this);

            if (!DebugSettings.godMode) yield break;
            foreach (var g in GetDebugGizmos()) yield return g;
        }

        // ═══════════════════════════════════════════
        //  组合能力系统（v4.0 F1新增）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 芯片激活后检查是否满足组合条件，满足则授予Ability。
        /// 遍历DefDatabase&lt;ComboAbilityDef&gt;，匹配当前左右手激活芯片。
        /// </summary>
        private void TryGrantComboAbility(Pawn pawn)
        {
            if (pawn?.abilities == null) return;
            var leftSlot = GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveSlot(SlotSide.RightHand);
            if (leftSlot?.loadedChip == null || rightSlot?.loadedChip == null) return;

            foreach (var combo in DefDatabase<ComboAbilityDef>.AllDefs)
            {
                if (grantedCombos.Contains(combo)) continue;
                if (!combo.Matches(leftSlot.loadedChip.def, rightSlot.loadedChip.def)) continue;
                if (combo.abilityDef == null) continue;

                pawn.abilities.GainAbility(combo.abilityDef);
                grantedCombos.Add(combo);
            }
        }

        /// <summary>
        /// 芯片关闭后检查已授予的组合能力是否仍然满足条件，不满足则移除。
        /// </summary>
        private void TryRevokeComboAbilities(Pawn pawn)
        {
            if (pawn?.abilities == null || grantedCombos.Count == 0) return;
            var leftSlot = GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveSlot(SlotSide.RightHand);

            for (int i = grantedCombos.Count - 1; i >= 0; i--)
            {
                var combo = grantedCombos[i];
                bool stillValid = leftSlot?.loadedChip != null && rightSlot?.loadedChip != null
                    && combo.Matches(leftSlot.loadedChip.def, rightSlot.loadedChip.def);
                if (!stillValid)
                {
                    if (combo.abilityDef != null)
                        pawn.abilities.RemoveAbility(combo.abilityDef);
                    grantedCombos.RemoveAt(i);
                }
            }
        }
    }
}
