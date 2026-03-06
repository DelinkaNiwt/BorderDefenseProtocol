using System.Collections.Generic;
using System.Linq;
// System.Reflection已移除：Verb.verbTracker是public字段，无需反射（C4修复）
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
    ///   - T24：按侧Verb存储（leftHandActiveVerbProps/rightHandActiveVerbProps），由DualVerbCompositor合成
    ///   - §8.3：新增SetSideVerbs/ClearSideVerbs/GetChipSide API
    ///
    /// v5.0变更（6.2.1 Gizmo架构重设计）：
    ///   - IVerbOwner始终返回占位Verb + 芯片Verb（芯片Verb设hasStandardCommand=false）
    ///   - IVerbOwner.Tools始终返回parent.def.tools（移除近战抑制逻辑）
    ///   - 新增Verb引用缓存（leftHandAttackVerb/rightHandAttackVerb/dualAttackVerb）
    ///   - RebuildVerbs从WeaponChipEffect搬入，统一管理VerbTracker重建+缓存填充
    ///   - CompGetEquippedGizmosExtra通过Command_BDPChipAttack生成芯片攻击Gizmo
    ///
    /// v5.1变更（根因修复：芯片Verb脱离VerbTracker）：
    ///   - IVerbOwner.VerbProperties不再合并芯片Verb，只返回parent.def.Verbs
    ///   - 芯片Verb改为在RebuildVerbs中手动创建（Activator.CreateInstance + 直接设置verbTracker）
    ///   - 手动创建的Verb不在VerbTracker.AllVerbs中，彻底隔离于：
    ///     ① Pawn_MeleeVerbs近战选择池（IsMeleeAttack即入池，hasStandardCommand无效）
    ///     ② VerbTracker.GetVerbsCommands Path B（FirstOrDefault(IsMeleeAttack)绑定Y按钮）
    ///   - 芯片Verb只通过Command_BDPChipAttack gizmo使用
    ///
    /// 不变量：
    ///   ① 每侧激活芯片数 ≤ 1（左右手槽）；特殊槽无此限制（全部激活或全部关闭）
    ///   ② 已装载芯片数 ≤ 该侧槽位数
    ///   ③ hasRightHand==false时rightHandSlots为空
    ///   ④ leftSwitchCtx/rightSwitchCtx非null时phase为WindingDown或WarmingUp
    ///   ⑤ leftSwitchCtx/rightSwitchCtx为null时该侧处于Idle
    ///   ⑥ isActive==true的槽位loadedChip!=null
    ///   ⑦ allowChipManagement==false时loadedChip不可被玩家修改
    ///   ⑧ dualHandLockSlot!=null时，另一侧不可激活新芯片（v2.1）
    ///   ⑨ specialSlots全部同时激活/关闭，不参与切换状态机（v2.1）
    ///   ⑩ specialSlotCount==0时specialSlots为null（v2.1）
    ///   ⑪ 特殊槽芯片的激活/关闭必须全部同时进行，不可单独操作（v2.1.1）
    ///   ⑫ activationWarmup对特殊槽芯片无效（战斗体生成时立即激活）（v2.1.1）
    ///   ⑬ IsCombatBodyActive==false时不可激活任何芯片（v6.0）
    ///   ⑭ WindingDown阶段旧芯片仍isActive=true，后摇到期才Deactivate（v6.0）
    ///
    /// v11.0变更（战斗体系统重构）：
    ///   - 实现ICombatBodySupport接口，消除反射调用
    ///   - 提供类型安全的战斗体支持API
    /// </summary>
    public partial class CompTriggerBody : CompEquippable, IVerbOwner, ICombatBodySupport
    {
        // ── 字段声明已移至 CompTriggerBody.Fields.cs ──

        /// <summary>
        /// 从当前操作槽位读取指定类型的DefModExtension，回退到遍历所有激活槽位。
        /// 统一替代各Effect类中重复的GetConfig模式（Fix-6）。
        /// </summary>
        public T GetChipExtension<T>() where T : DefModExtension
        {
            // 优先从ActivatingSlot读取（激活/关闭上下文）
            if (ActivatingSlot?.loadedChip != null)
            {
                var cfg = ActivatingSlot.loadedChip.def.GetModExtension<T>();
                if (cfg != null) return cfg;
            }
            // 回退：遍历所有激活槽位（兼容读档恢复等边界情况）
            foreach (var slot in AllActiveSlots())
            {
                var cfg = slot.loadedChip?.def?.GetModExtension<T>();
                if (cfg != null) return cfg;
            }
            return null;
        }

        // ── IVerbOwner接口实现已移至 CompTriggerBody.VerbSystem.cs ──


        // ── 便利属性 ──
        public CompProperties_TriggerBody Props => (CompProperties_TriggerBody)props;

        // ── 公开属性（v2.0：MainSlots/SubSlots → LeftHandSlots/RightHandSlots） ──

        /// <summary>左手槽列表（只读，供UI层访问）。懒初始化以兼容CharacterEditor等外部工具。</summary>
        public IReadOnlyList<ChipSlot> LeftHandSlots { get { EnsureSlotsInitialized(); return leftHandSlots; } }
        /// <summary>右手槽列表（只读，供UI层访问）。懒初始化以兼容CharacterEditor等外部工具。</summary>
        public IReadOnlyList<ChipSlot> RightHandSlots { get { EnsureSlotsInitialized(); return rightHandSlots; } }
        /// <summary>特殊槽列表（只读，供UI层访问）。v2.1新增。</summary>
        public IReadOnlyList<ChipSlot> SpecialSlots { get { EnsureSlotsInitialized(); return specialSlots; } }

        // ── 切换状态机方法已移至 CompTriggerBody.SwitchStateMachine.cs ──

        // ── 槽位管理方法已移至 CompTriggerBody.SlotManagement.cs ──

        /// <summary>检查装备者Pawn是否拥有Trion腺体基因。</summary>
        public bool OwnerHasTrionGland()
        {
            var pawn = OwnerPawn;
            return pawn?.genes?.HasActiveGene(BDP_DefOf.BDP_Gene_TrionGland) ?? false;
        }


        // ── Verb系统方法已移至 CompTriggerBody.VerbSystem.cs ──


        // ═══════════════════════════════════════════
        //  预占用值同步（v1.8）
        // ═══════════════════════════════════════════

        // ── Trion预占用同步方法已移至 CompTriggerBody.SlotManagement.cs ──

        // ═══════════════════════════════════════════
        //  前置条件检查
        // ── 激活/停用方法已移至 CompTriggerBody.Activation.cs ──

        // ═══════════════════════════════════════════
        //  ICombatBodySupport接口实现（v11.0战斗体系统重构）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 实现ICombatBodySupport.TryAllocateForCombatBody。
        /// 委托给现有的TryAllocateTrionForCombatBody方法。
        /// </summary>
        bool ICombatBodySupport.TryAllocateForCombatBody()
        {
            return TryAllocateTrionForCombatBody();
        }

        /// <summary>
        /// 实现ICombatBodySupport.ReleaseFromCombatBody。
        /// 委托给DismissCombatBody方法，执行完整的解除逻辑。
        /// </summary>
        void ICombatBodySupport.ReleaseFromCombatBody()
        {
            DismissCombatBody();
        }

        /// <summary>
        /// 实现ICombatBodySupport.ActivateSpecialSlots。
        /// 委托给现有的ActivateAllSpecial方法。
        /// </summary>
        void ICombatBodySupport.ActivateSpecialSlots()
        {
            ActivateAllSpecial();
        }

        /// <summary>
        /// 实现ICombatBodySupport.DeactivateSpecialSlots。
        /// 关闭所有特殊槽芯片。
        /// </summary>
        void ICombatBodySupport.DeactivateSpecialSlots()
        {
            if (specialSlots == null) return;
            foreach (var slot in specialSlots)
            {
                if (slot.loadedChip != null && slot.isActive)
                {
                    DeactivateSlot(slot);
                }
            }
        }

        // ═══════════════════════════════════════════
        //  手部缺失联动（v12.2新增）
        // ═══════════════════════════════════════════

        // ── 手部破坏联动方法已移至 CompTriggerBody.HandDestruction.cs ──

        // ── 调试/开发工具方法已提取到 CompTriggerBody.Debug.cs（Fix-8：partial class） ──
    }
}
