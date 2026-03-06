using BDP.Core;
using BDP.Combat.Snapshot;
using BDP.Trigger;
using RimWorld;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体协调器。
    /// 负责编排战斗体激活/解除的完整流程。
    ///
    /// 设计目的:
    /// - 分离Gene_TrionGland的协调职责
    /// - 集中管理激活/解除的复杂流程
    /// - 提高代码可测试性和可维护性
    ///
    /// 职责:
    /// - 编排激活流程(前置检查→Trion占用→状态转换→激活芯片→注册消耗)
    /// - 编排解除流程(清理状态→解除触发器→注销消耗→恢复快照→更新状态)
    /// - 提供清晰的错误处理和日志记录
    /// </summary>
    public class CombatBodyOrchestrator
    {
        // ═══════════════════════════════════════════
        //  激活流程
        // ═══════════════════════════════════════════

        /// <summary>
        /// 尝试激活战斗体。
        /// 编排完整的激活流程,保证原子性。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        /// <param name="runtime">战斗体运行时聚合体</param>
        /// <returns>true=激活成功,false=激活失败</returns>
        public bool TryActivate(Pawn pawn, CombatBodyRuntime runtime)
        {
            // 阶段0: 前置条件检查
            if (!ValidateActivation(pawn, runtime, out string reason))
            {
                Messages.Message($"{pawn.Name}: {reason}", MessageTypeDefOf.RejectInput);
                return false;
            }

            // 入口查一次ICombatBodySupport，后续方法共享
            var support = FindCombatBodySupport(pawn);

            // 阶段1: Trion占用
            if (!AllocateTrion(pawn, support, out float allocateAmount))
            {
                // 修改：Trion不足时使用普通信息提示，而非红色报错
                Messages.Message($"{pawn.Name} Trion不足，无法激活战斗体", MessageTypeDefOf.CautionInput);
                return false;
            }

            // 阶段2: 状态转换
            ApplyTransformation(pawn, runtime.Snapshot);

            // 阶段3: 初始化影子HP系统
            InitializeShadowHP(pawn, runtime);

            // 阶段4: 激活芯片
            ActivateChips(support);

            // 阶段5: 注册消耗
            RegisterMaintenance(pawn, runtime);

            // 更新状态
            runtime.State.TransitionToActive(allocateAmount);

            Messages.Message($"{pawn.Name} 激活战斗体", MessageTypeDefOf.PositiveEvent);
            return true;
        }

        // ═══════════════════════════════════════════
        //  解除流程
        // ═══════════════════════════════════════════

        /// <summary>
        /// 解除战斗体。
        /// 编排完整的解除流程。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        /// <param name="runtime">战斗体运行时聚合体</param>
        /// <param name="isEmergency">是否为紧急脱离</param>
        public void Deactivate(Pawn pawn, CombatBodyRuntime runtime, bool isEmergency)
        {
            // 紧急脱离时执行传送
            if (isEmergency && HasEmergencyEscapeChip(pawn, out ChipSlot chipSlot))
            {
                IntVec3 origin = pawn.Position;  // 记录起点
                IntVec3 destination = EmergencyEscapeRouter.FindEscapeDestination(pawn, pawn.Map);

                if (destination.IsValid && destination != pawn.Position)
                {
                    // 播放入口特效
                    EmergencyEscapeEffects.PlayEntryEffects(origin, pawn.Map);

                    // 执行传送
                    pawn.Position = destination;
                    pawn.Notify_Teleported(false, true);

                    // 播放出口特效
                    EmergencyEscapeEffects.PlayExitEffects(destination, pawn.Map);

                    // 销毁芯片（一次性使用）
                    if (chipSlot != null && chipSlot.loadedChip != null)
                    {
                        chipSlot.loadedChip.Destroy(DestroyMode.Vanish);
                        chipSlot.loadedChip = null;
                        chipSlot.isActive = false;

                        Messages.Message(
                            $"紧急脱离芯片已销毁",
                            MessageTypeDefOf.NeutralEvent);
                    }

                    // 解除征召状态（仅紧急脱离时）
                    if (pawn.drafter != null)
                    {
                        pawn.drafter.Drafted = false;
                    }

                    Messages.Message(
                        $"{pawn.Name} 紧急脱离至安全位置",
                        new TargetInfo(destination, pawn.Map),
                        MessageTypeDefOf.PositiveEvent);
                }
            }

            // 清理战斗体状态（包括影子HP）
            CleanupCombatBodyState(pawn, runtime, runtime.Snapshot);

            // 解除触发器系统
            ReleaseTriggerSystem(pawn, isEmergency);

            // 注销消耗
            UnregisterMaintenance(pawn);

            // 恢复快照
            runtime.Snapshot.RestoreAll();

            // 被动破裂时添加枯竭Hediff
            if (isEmergency)
            {
                var compTrion = pawn.GetComp<CompTrion>();
                if (compTrion != null)
                {
                    compTrion.ForceDeplete();
                }
                pawn.health.AddHediff(BDP_DefOf.BDP_Exhaustion);
            }

            // 更新状态
            int cooldownTicks = isEmergency ? 60000 : 0;

            // 根据当前状态选择转换方法
            if (runtime.State.CurrentState == CombatBodyState.OuterState.Active)
            {
                // 从Active直接转换到Cooldown（正常解除）
                runtime.State.TransitionToCooldown(cooldownTicks);
            }
            else if (runtime.State.CurrentState == CombatBodyState.OuterState.Collapsing)
            {
                // 从Collapsing转换到Cooldown（延时破裂完成）
                runtime.State.TransitionToCooldownFromCollapsing(cooldownTicks);
            }
            else
            {
                Log.Warning($"[BDP] 解除战斗体时状态异常: 当前状态={runtime.State.CurrentState}，跳过状态转换");
            }

            string msg = isEmergency ? "紧急脱离战斗体" : "解除战斗体";
            Messages.Message($"{pawn.Name} {msg}", MessageTypeDefOf.PositiveEvent);
        }

        // ═══════════════════════════════════════════
        //  私有辅助方法 - 通用
        // ═══════════════════════════════════════════

        /// <summary>
        /// 从Pawn主武器查找ICombatBodySupport实现。
        /// 消除AllocateTrion/ActivateChips/ReleaseTriggerSystem三处重复查找。
        /// </summary>
        private static ICombatBodySupport FindCombatBodySupport(Pawn pawn)
        {
            var allComps = pawn?.equipment?.Primary?.AllComps;
            if (allComps == null) return null;
            foreach (var comp in allComps)
            {
                if (comp is ICombatBodySupport s) return s;
            }
            return null;
        }

        // ═══════════════════════════════════════════
        //  私有辅助方法 - 激活流程
        // ═══════════════════════════════════════════

        /// <summary>
        /// 验证激活前置条件。
        /// </summary>
        private bool ValidateActivation(Pawn pawn, CombatBodyRuntime runtime, out string reason)
        {
            reason = "";

            // 检查runtime对象
            if (runtime == null)
            {
                reason = "运行时对象为null";
                return false;
            }

            // 检查冷却状态
            if (runtime.State != null && !runtime.State.CanActivate())
            {
                int remainingTicks = runtime.State.GetCooldownRemaining();
                float remainingDays = remainingTicks / 60000f;
                reason = $"冷却中 (剩余{remainingDays:F1}天)";
                return false;
            }

            // 检查CompTrion
            var compTrion = pawn.GetComp<CompTrion>();
            if (compTrion == null)
            {
                reason = "缺少Trion组件";
                return false;
            }

            // 静态可否决事件检查
            var checkArgs = new CanActivateCombatBodyEventArgs { Pawn = pawn };
            BDPEvents.TriggerCanActivateQuery(checkArgs);
            if (checkArgs.Vetoed)
            {
                reason = checkArgs.BlockReason;
                return false;
            }

            // 检查Trion总量
            float allocateAmount = compTrion.ReservedAllocation;
            if (compTrion.Cur < allocateAmount)
            {
                reason = $"Trion不足 (需要{allocateAmount:F1}, 当前{compTrion.Cur:F1})";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 占用Trion。
        /// </summary>
        private bool AllocateTrion(Pawn pawn, ICombatBodySupport support, out float allocateAmount)
        {
            allocateAmount = 0f;

            if (support == null)
            {
                Log.Error($"[BDP] AllocateTrion失败: 未找到ICombatBodySupport接口实现");
                return false;
            }

            bool allocated = support.TryAllocateForCombatBody();
            if (!allocated)
            {
                return false;
            }

            var compTrion = pawn.GetComp<CompTrion>();
            allocateAmount = compTrion?.Allocated ?? 0f;
            return true;
        }

        /// <summary>
        /// 应用状态转换(快照并换装)。
        /// </summary>
        private void ApplyTransformation(Pawn pawn, CombatBodySnapshot snapshot)
        {
            snapshot.SnapshotAll();
            snapshot.ApplyTransformation();
            snapshot.RemoveAllHediffsExceptExcluded();
            pawn.health.AddHediff(BDP_DefOf.BDP_CombatBodyActive);

            // 强制征召
            if (pawn.drafter == null)
                pawn.drafter = new Pawn_DraftController(pawn);
            pawn.drafter.Drafted = true;
        }

        /// <summary>
        /// 初始化影子HP系统。
        /// </summary>
        private void InitializeShadowHP(Pawn pawn, CombatBodyRuntime runtime)
        {
            if (runtime.ShadowHP == null)
            {
                Log.Error($"[BDP] InitializeShadowHP: {pawn.LabelShort} 的ShadowHPTracker为null，无法初始化");
                return;
            }
            runtime.ShadowHP.InitializeFromSnapshot(pawn);
            Log.Message($"[BDP] 影子HP系统初始化完成");
        }

        /// <summary>
        /// 激活芯片。
        /// </summary>
        private void ActivateChips(ICombatBodySupport support)
        {
            if (support == null)
            {
                Log.Warning($"[BDP] ActivateChips失败: 未找到ICombatBodySupport接口实现");
                return;
            }

            support.ActivateSpecialSlots();
            Log.Message($"[BDP] 已激活特殊槽芯片");
        }

        /// <summary>
        /// 注册维持消耗。
        /// </summary>
        private void RegisterMaintenance(Pawn pawn, CombatBodyRuntime runtime)
        {
            var compTrion = pawn.GetComp<CompTrion>();
            if (compTrion == null) return;

            // 从XML配置获取维持消耗
            var ext = runtime.GeneDef?.GetModExtension<GeneExtension_CombatBody>();
            float combatBodyDrain = ext?.maintenanceDrainPerDay ?? 0f;

            if (combatBodyDrain > 0f)
            {
                compTrion.RegisterDrain("CombatBody", combatBodyDrain);
            }

            compTrion.SetFrozen(true);
        }

        // ═══════════════════════════════════════════
        //  私有辅助方法 - 解除流程
        // ═══════════════════════════════════════════

        /// <summary>
        /// 清理战斗体状态。
        /// </summary>
        private void CleanupCombatBodyState(Pawn pawn, CombatBodyRuntime runtime, CombatBodySnapshot snapshot)
        {
            snapshot.RemoveAllHediffsExceptExcluded();

            // 清理影子HP
            runtime.ShadowHP?.Clear();

            // 清理部位破坏记录并恢复部位
            runtime.PartDestruction?.Clear(pawn);

            // 清理战斗体伤口（新增）
            WoundHandler.Clear(pawn);
        }

        /// <summary>
        /// 解除触发器系统。
        /// </summary>
        private void ReleaseTriggerSystem(Pawn pawn, bool isEmergency)
        {
            var support = FindCombatBodySupport(pawn);

            if (support != null)
            {
                support.ReleaseFromCombatBody();
            }
            else
            {
                // 触发体不在（被卸下/销毁），验证Trion确实已释放
                var compTrion = pawn.GetComp<CompTrion>();
                if (compTrion != null && compTrion.Allocated > 0f)
                {
                    Log.Error($"[BDP] 触发体不在但Trion未释放！allocated={compTrion.Allocated:F1}（流程异常）");
                }
            }
        }

        /// <summary>
        /// 注销维持消耗。
        /// </summary>
        private void UnregisterMaintenance(Pawn pawn)
        {
            var compTrion = pawn.GetComp<CompTrion>();
            if (compTrion == null) return;

            compTrion.UnregisterDrain("CombatBody");
            compTrion.SetFrozen(false);
        }

        /// <summary>
        /// 检查Pawn是否装备了紧急脱离芯片。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        /// <param name="chipSlot">输出参数：找到的芯片槽位</param>
        /// <returns>是否装备了紧急脱离芯片</returns>
        private bool HasEmergencyEscapeChip(Pawn pawn, out ChipSlot chipSlot)
        {
            chipSlot = null;

            var support = FindCombatBodySupport(pawn);
            if (support == null) return false;

            // 将ICombatBodySupport转换为CompTriggerBody以访问SpecialSlots
            var triggerComp = support as CompTriggerBody;
            if (triggerComp == null) return false;

            // 检查特殊槽位是否有紧急脱离芯片
            var specialSlots = triggerComp.SpecialSlots;
            if (specialSlots == null) return false;

            foreach (var slot in specialSlots)
            {
                if (slot.loadedChip?.def.defName == "BDP_Chip_EmergencyEscape" && slot.isActive)
                {
                    chipSlot = slot;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查Pawn是否装备了紧急脱离芯片（重载方法，不返回槽位）。
        /// </summary>
        private bool HasEmergencyEscapeChip(Pawn pawn)
        {
            return HasEmergencyEscapeChip(pawn, out _);
        }
    }
}
