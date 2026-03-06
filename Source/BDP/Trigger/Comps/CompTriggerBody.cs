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

        // ── 显式实现IVerbOwner接口（v5.1改造：芯片Verb完全脱离VerbTracker） ──
        // VerbTracker.InitVerbs()通过IVerbOwner接口调用，显式实现可正确拦截
        //
        // v5.1改造（根因修复）：
        //   - VerbProperties只返回parent.def.Verbs（占位），不再合并芯片Verb
        //   - 原因：芯片Verb若在VerbTracker.AllVerbs中，会被引擎近战选择池
        //     （Pawn_MeleeVerbs.GetUpdatedAvailableVerbsList）和VerbTracker.GetVerbsCommands
        //     的近战武器路径（FirstOrDefault(IsMeleeAttack)）错误拾取
        //   - hasStandardCommand=false只控制Gizmo生成，不控制近战Verb选择
        //   - 芯片Verb改为在RebuildVerbs中手动创建，不进入VerbTracker.AllVerbs
        //   - Tools始终返回parent.def.tools（触发体"柄"的近战gizmo）

        List<VerbProperties> IVerbOwner.VerbProperties => parent.def.Verbs;

        List<Tool> IVerbOwner.Tools => parent.def.tools;

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

        // ═══════════════════════════════════════════
        //  VerbTracker重建 + 芯片Verb手动创建（v5.1根因修复）
        // ═══════════════════════════════════════════

        // C4修复：Verb.verbTracker是public字段（RimWorld 1.6.4633），无需反射。
        // 原先误以为是internal字段而使用FieldInfo反射访问，实际直接赋值即可。
        // 静态构造函数和fi_verbTracker已移除。

        /// <summary>
        /// 重建触发体VerbTracker、手动创建芯片Verb实例、填充缓存（v5.1）。
        ///
        /// v5.1根因修复：芯片Verb不再通过IVerbOwner.VerbProperties进入VerbTracker。
        /// 原因：VerbTracker.AllVerbs中的近战Verb会被引擎的两条路径错误拾取：
        ///   ① Pawn_MeleeVerbs.GetUpdatedAvailableVerbsList — 近战选择池（IsMeleeAttack即入池）
        ///   ② VerbTracker.GetVerbsCommands Path B — FirstOrDefault(IsMeleeAttack)绑定Y按钮
        /// hasStandardCommand=false只控制Path A（标准Gizmo生成），对①②无效。
        ///
        /// 新流程：
        ///   1. InitVerbsFromZero() — 只重建占位Verb（来自parent.def.Verbs + Tools）
        ///   2. 绑定caster
        ///   3. DualVerbCompositor.ComposeVerbs() — 合成芯片VerbProperties
        ///   4. 手动Activator.CreateInstance创建芯片Verb实例（不进入AllVerbs）
        ///   5. 直接设置verb.verbTracker（public字段，使EquipmentSource正确指向触发体）
        ///   6. 缓存到leftHandAttackVerb/rightHandAttackVerb/dualAttackVerb
        /// </summary>
        public void RebuildVerbs(Pawn pawn)
        {
            if (VerbTracker == null) return;

            // 步骤1：重建VerbTracker（只包含占位Verb + Tool产生的"柄"Verb）
            VerbTracker.InitVerbsFromZero();

            // 步骤2：重新绑定caster（模拟Pawn_EquipmentTracker.Notify_EquipmentAdded的行为）
            if (pawn != null && AllVerbs != null)
            {
                foreach (var verb in AllVerbs)
                    verb.caster = pawn;
            }

            // 步骤3+4+5+6：合成芯片VerbProperties → 手动创建Verb实例 → 缓存
            CreateAndCacheChipVerbs(pawn);

            // 诊断日志
            if (Prefs.DevMode)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[BDP] RebuildVerbs完成 [{parent.LabelShortCap}] pawn={pawn?.LabelShortCap}");
                if (AllVerbs != null)
                {
                    sb.AppendLine($"  VerbTracker.AllVerbs ({AllVerbs.Count}) — 芯片Verb不在此列表中:");
                    for (int i = 0; i < AllVerbs.Count; i++)
                    {
                        var v = AllVerbs[i];
                        sb.AppendLine($"    [{i}] {v.GetType().Name} primary={v.verbProps?.isPrimary} stdCmd={v.verbProps?.hasStandardCommand} melee={v.IsMeleeAttack} label={v.verbProps?.label} caster={v.caster?.LabelShortCap}");
                    }
                }
                sb.AppendLine($"  芯片Verb缓存（手动创建，不在AllVerbs中）:");
                sb.AppendLine($"    left={leftHandAttackVerb?.GetType().Name} right={rightHandAttackVerb?.GetType().Name} dual={dualAttackVerb?.GetType().Name}");
                sb.AppendLine($"    secondaryLeft={leftHandSecondaryVerb?.GetType().Name} secondaryRight={rightHandSecondaryVerb?.GetType().Name} secondaryDual={dualSecondaryVerb?.GetType().Name}");
                sb.AppendLine($"    comboAttack={comboAttackVerb?.GetType().Name} comboSecondary={comboSecondaryVerb?.GetType().Name} comboDef={matchedComboDef?.defName}");
                Log.Message(sb.ToString());
            }
        }

        /// <summary>
        /// 合成芯片VerbProperties，手动创建Verb实例，缓存引用（v5.1）。
        ///
        /// 手动创建的Verb实例不在VerbTracker.AllVerbs中，因此：
        ///   · 不会被Pawn_MeleeVerbs近战选择池拾取
        ///   · 不会被VerbTracker.GetVerbsCommands的Path B拾取
        ///   · 只通过Command_BDPChipAttack gizmo使用
        /// </summary>
        private void CreateAndCacheChipVerbs(Pawn pawn)
        {
            leftHandAttackVerb = null;
            rightHandAttackVerb = null;
            dualAttackVerb = null;
            // v6.1：清空副攻击缓存（v8.0重命名，v9.0统一语义）
            leftHandSecondaryVerb = null;
            rightHandSecondaryVerb = null;
            dualSecondaryVerb = null;
            // v10.0：清空组合技缓存（v9.0重命名）
            comboAttackVerb = null;
            comboSecondaryVerb = null;
            matchedComboDef = null;

            // 合成芯片VerbProperties
            var chipVerbProps = DualVerbCompositor.ComposeVerbs(
                leftHandActiveVerbProps, rightHandActiveVerbProps,
                GetActiveSlot(SlotSide.LeftHand), GetActiveSlot(SlotSide.RightHand));

            if (chipVerbProps == null) return;

            // 为每个芯片VerbProperties手动创建Verb实例（或复用读档反序列化的实例）
            foreach (var vp in chipVerbProps)
            {
                if (vp.verbClass == null) continue;

                string expectedLoadID = $"BDP_Chip_{parent.ThingID}_{chipVerbProps.IndexOf(vp)}";

                // 读档时优先复用已反序列化的Verb实例（Job/Stance的引用指向该实例）
                Verb verb = FindSavedVerb(expectedLoadID);
                if (verb == null)
                {
                    try
                    {
                        verb = (Verb)System.Activator.CreateInstance(vp.verbClass);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[BDP] 创建芯片Verb失败: {vp.verbClass.Name} — {ex}");
                        continue;
                    }
                }

                // 模拟VerbTracker.InitVerb的字段设置
                verb.loadID = expectedLoadID;
                verb.verbProps = vp;
                verb.caster = pawn;
                // verb.tool = null（芯片Verb不基于Tool）
                // verb.maneuver = null

                // 直接设置verbTracker（public字段），使verb.EquipmentSource正确指向触发体
                // （EquipmentSource = (DirectOwner as CompEquippable)?.parent）
                verb.verbTracker = VerbTracker;

                // 按type+label分配到缓存槽位
                var vType = verb.GetType();
                var label = vp.label;

                if (verb is Verb_BDPMelee || verb is Verb_BDPShoot)
                {
                    var side = DualVerbCompositor.ParseSideLabel(label);
                    if (side == SlotSide.LeftHand)
                        leftHandAttackVerb = verb;
                    else if (side == SlotSide.RightHand)
                        rightHandAttackVerb = verb;
                    else
                        dualAttackVerb = verb;
                }
                else if (verb is Verb_BDPDualRanged)
                {
                    dualAttackVerb = verb;
                }
            }

            // v6.1：为支持齐射的芯片创建volley verb
            // v8.0变更：支持primaryVerbProps/secondaryVerbProps配置
            CreateSecondaryVerbs(pawn);

            // v10.0：检测组合技匹配，创建组合技Verb
            CreateComboVerbs(pawn);
        }

        /// <summary>
        /// 为芯片创建副攻击verb实例（v8.0重构）。
        ///
        /// 新逻辑（v8.0）：
        ///   1. 优先使用 secondaryVerbProps 显式配置的副攻击verb
        ///   2. 如果 secondaryVerbProps == null 且 supportsVolley == true，创建齐射verb（向后兼容）
        ///   3. 如果 secondaryVerbProps == null 且 supportsVolley == false，副攻击为null（右键走默认行为）
        ///
        /// 旧逻辑（v6.1）：
        ///   遍历已缓存的burst verb，检查对应芯片的supportsVolley标志，
        ///   为支持齐射的芯片创建Verb_BDPVolley/Verb_BDPDualVolley实例。
        ///
        /// 注意：使用GetActiveOrActivatingSlot而非GetActiveSlot。
        /// 原因：DoActivate中effect.Activate()触发RebuildVerbs时，
        ///       slot.isActive尚未设为true，GetActiveSlot找不到正在激活的芯片。
        /// </summary>
        private void CreateSecondaryVerbs(Pawn pawn)
        {
            var leftSlot = GetActiveOrActivatingSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveOrActivatingSlot(SlotSide.RightHand);
            var leftCfg = leftSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();
            var rightCfg = rightSlot?.loadedChip?.def?.GetModExtension<WeaponChipConfig>();

            // 左手副攻击
            if (leftHandAttackVerb != null && leftCfg != null)
            {
                leftHandSecondaryVerb = CreateSecondaryVerb(leftCfg, leftHandAttackVerb, pawn, "Left");
            }

            // 右手副攻击
            if (rightHandAttackVerb != null && rightCfg != null)
            {
                rightHandSecondaryVerb = CreateSecondaryVerb(rightCfg, rightHandAttackVerb, pawn, "Right");
            }

            // 双手副攻击：两侧都有副攻击时才创建
            if (leftHandSecondaryVerb != null && rightHandSecondaryVerb != null && dualAttackVerb != null)
            {
                dualSecondaryVerb = CreateSecondaryVerb(leftCfg, dualAttackVerb, pawn, "Dual");
            }
        }

        /// <summary>
        /// 创建单个副攻击verb实例（v9.0重构）。
        ///
        /// 逻辑：
        ///   1. 如果 cfg.secondaryVerbProps != null，使用它创建verb
        ///   2. 否则如果 cfg.supportsVolley == true，创建齐射verb（向后兼容）
        ///   3. 否则返回null
        /// </summary>
        private Verb CreateSecondaryVerb(WeaponChipConfig cfg, Verb primaryVerb, Pawn pawn, string side)
        {
            // 优先使用显式配置的secondaryVerbProps
            if (cfg.secondaryVerbProps != null)
            {
                string loadID = $"BDP_Secondary{side}_{parent.ThingID}_{cfg.secondaryVerbProps.label}";
                return FindOrCreateVerb(cfg.secondaryVerbProps, pawn, loadID);
            }

            // 向后兼容：supportsVolley=true时自动创建齐射verb
            if (cfg.supportsVolley)
            {
                return CreateLegacyVolleyVerb(primaryVerb, pawn, side);
            }

            return null;
        }

        /// <summary>
        /// 统一的Verb查找或创建方法（v9.0新增）。
        /// 读档时优先复用已反序列化的Verb实例，否则创建新实例。
        /// </summary>
        private Verb FindOrCreateVerb(VerbProperties vp, Pawn pawn, string loadID)
        {
            if (vp.verbClass == null) return null;

            // 读档时优先复用已反序列化的Verb实例
            Verb verb = FindSavedVerb(loadID);
            if (verb == null)
            {
                try
                {
                    verb = (Verb)System.Activator.CreateInstance(vp.verbClass);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[BDP] 创建Verb失败: {vp.verbClass.Name} — {ex}");
                    return null;
                }
            }

            // 初始化Verb
            verb.loadID = loadID;
            verb.verbProps = vp;
            verb.caster = pawn;
            verb.verbTracker = VerbTracker;

            return verb;
        }

        /// <summary>
        /// 向后兼容方法：基于primaryVerb自动创建齐射verb（v9.0标记为过时）。
        /// </summary>
        [System.Obsolete("Use explicit secondaryVerbProps configuration instead")]
        private Verb CreateLegacyVolleyVerb(Verb primaryVerb, Pawn pawn, string side)
        {
            var srcVp = primaryVerb.verbProps;

            // 确定齐射verb类型
            System.Type volleyVerbClass = side == "Dual" ? typeof(Verb_BDPDualVolley) : typeof(Verb_BDPVolley);

            // 复制VerbProperties，修改verbClass和burstShotCount
            var volleyVp = new VerbProperties
            {
                verbClass = volleyVerbClass,
                isPrimary = srcVp.isPrimary,
                hasStandardCommand = false,
                defaultProjectile = srcVp.defaultProjectile,
                soundCast = srcVp.soundCast,
                muzzleFlashScale = srcVp.muzzleFlashScale,
                ticksBetweenBurstShots = 0,
                range = srcVp.range,
                warmupTime = srcVp.warmupTime,
                defaultCooldownTime = srcVp.defaultCooldownTime,
                burstShotCount = 1, // 引擎只调用一次TryCastShot
                label = "齐射", // 固定label
                meleeDamageDef = srcVp.meleeDamageDef,
                meleeDamageBaseAmount = srcVp.meleeDamageBaseAmount,
            };

            string loadID = $"BDP_LegacyVolley{side}_{parent.ThingID}_{volleyVerbClass.Name}";
            return FindOrCreateVerb(volleyVp, pawn, loadID);
        }

        /// <summary>
        /// 检测组合技匹配，创建组合技Verb实例（v10.0新增，v9.0重构）。
        /// 遍历DefDatabase&lt;ComboVerbDef&gt;，匹配当前左右手激活芯片。
        /// 匹配成功时创建主攻击verb和副攻击verb（如果配置）。
        /// </summary>
        private void CreateComboVerbs(Pawn pawn)
        {
            var leftSlot = GetActiveOrActivatingSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveOrActivatingSlot(SlotSide.RightHand);
            if (leftSlot?.loadedChip == null || rightSlot?.loadedChip == null) return;

            foreach (var comboDef in DefDatabase<ComboVerbDef>.AllDefs)
            {
                if (!comboDef.Matches(leftSlot.loadedChip.def, rightSlot.loadedChip.def))
                    continue;

                // 匹配成功：创建组合技Verb
                matchedComboDef = comboDef;

                // 创建主攻击verb
                comboAttackVerb = CreateComboVerb(comboDef, false, pawn, leftSlot, rightSlot);

                // 创建副攻击verb
                comboSecondaryVerb = CreateComboVerb(comboDef, true, pawn, leftSlot, rightSlot);

                break; // 只匹配第一个
            }
        }

        /// <summary>
        /// 创建单个组合技Verb实例（v10.0新增，v9.0重构）。
        /// 参数取两侧芯片的平均值。
        /// </summary>
        private Verb CreateComboVerb(ComboVerbDef comboDef, bool isSecondary,
            Pawn pawn, ChipSlot leftSlot, ChipSlot rightSlot)
        {
            var leftCfg = leftSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            var rightCfg = rightSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            if (leftCfg == null || rightCfg == null) return null;

            // 如果是副攻击但未配置，检查向后兼容
            if (isSecondary)
            {
                // 优先使用secondaryVerbClass
                if (comboDef.secondaryVerbClass != null)
                {
                    // 创建显式配置的副攻击verb
                    return CreateComboVerbFromClass(comboDef, comboDef.secondaryVerbClass,
                        pawn, leftCfg, rightCfg, true);
                }
                // 向后兼容：supportsVolley=true时自动创建齐射verb
                else if (comboDef.supportsVolley)
                {
                    return CreateComboVerbFromClass(comboDef, typeof(Verb_BDPComboShoot),
                        pawn, leftCfg, rightCfg, true);
                }
                else
                {
                    return null; // 无副攻击
                }
            }

            // 主攻击逻辑：使用primaryVerbClass
            return CreateComboVerbFromClass(comboDef, comboDef.primaryVerbClass,
                pawn, leftCfg, rightCfg, false);
        }

        /// <summary>
        /// 从指定Verb类型创建组合技Verb实例（v9.0新增）。
        /// </summary>
        private Verb CreateComboVerbFromClass(ComboVerbDef comboDef, System.Type verbClass,
            Pawn pawn, WeaponChipConfig leftCfg, WeaponChipConfig rightCfg, bool isSecondary)
        {
            // 计算平均参数
            float avgRange = (GetFirstRange(leftCfg) + GetFirstRange(rightCfg)) * 0.5f;
            float avgWarmup = (GetFirstWarmup(leftCfg) + GetFirstWarmup(rightCfg)) * 0.5f;
            float avgCooldown = (GetFirstCooldown(leftCfg) + GetFirstCooldown(rightCfg)) * 0.5f;
            int avgBurst = UnityEngine.Mathf.RoundToInt(
                (leftCfg.GetFirstBurstCount() + rightCfg.GetFirstBurstCount()) * 0.5f);
            if (avgBurst < 1) avgBurst = 1;
            float avgTrionCost = (leftCfg.trionCostPerShot + rightCfg.trionCostPerShot) * 0.5f;
            float avgAnchorSpread = (leftCfg.anchorSpread + rightCfg.anchorSpread) * 0.5f;
            float avgVolleySpread = (leftCfg.volleySpreadRadius + rightCfg.volleySpreadRadius) * 0.5f;
            int avgTicksBetween = UnityEngine.Mathf.RoundToInt(
                (GetFirstTicksBetween(leftCfg) + GetFirstTicksBetween(rightCfg)) * 0.5f);

            // 构建VerbProperties
            var vp = new VerbProperties
            {
                verbClass = verbClass,
                isPrimary = false,
                hasStandardCommand = false,
                defaultProjectile = comboDef.projectileDef,
                soundCast = GetFirstSound(leftCfg) ?? GetFirstSound(rightCfg),
                muzzleFlashScale = 10f,
                range = avgRange,
                warmupTime = avgWarmup,
                defaultCooldownTime = avgCooldown,
                // 副攻击模式：burstShotCount=1（TryCastShot内循环）
                // 主攻击模式：burstShotCount=avgBurst（引擎burst机制）
                burstShotCount = isSecondary ? 1 : avgBurst,
                ticksBetweenBurstShots = isSecondary ? 0 : avgTicksBetween,
                label = isSecondary ? "组合技副攻击" : comboDef.label,
            };

            string suffix = isSecondary ? "Secondary" : "Attack";
            string loadID = $"BDP_Combo_{parent.ThingID}_{comboDef.defName}_{suffix}";

            // 使用统一的FindOrCreateVerb方法
            Verb verb = FindOrCreateVerb(vp, pawn, loadID);
            if (verb == null) return null;

            // 设置组合技专用字段（如果是Verb_BDPComboShoot）
            if (verb is Verb_BDPComboShoot comboVerb)
            {
                comboVerb.comboDef = comboDef;
                comboVerb.isVolley = isSecondary;
                comboVerb.avgBurstCount = avgBurst;
                comboVerb.avgTrionCost = avgTrionCost;
                comboVerb.avgAnchorSpread = avgAnchorSpread;
                comboVerb.avgVolleySpread = avgVolleySpread;
            }

            return verb;
        }

        // ── 组合技参数读取辅助（从WeaponChipConfig.verbProperties[0]读取） ──

        private static float GetFirstRange(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties != null)
                foreach (var vp in cfg.verbProperties)
                    if (vp.range > 0) return vp.range;
            return 20f;
        }

        private static float GetFirstWarmup(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties != null)
                foreach (var vp in cfg.verbProperties)
                    return vp.warmupTime;
            return 1f;
        }

        private static float GetFirstCooldown(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties != null)
                foreach (var vp in cfg.verbProperties)
                    return vp.defaultCooldownTime;
            return 1f;
        }

        private static int GetFirstTicksBetween(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties != null)
                foreach (var vp in cfg.verbProperties)
                    return vp.ticksBetweenBurstShots;
            return 8;
        }

        private static SoundDef GetFirstSound(WeaponChipConfig cfg)
        {
            if (cfg?.verbProperties != null)
                foreach (var vp in cfg.verbProperties)
                    if (vp.soundCast != null) return vp.soundCast;
            return null;
        }

        /// <summary>
        /// 获取指定侧的激活槽位，包含正在激活中的槽位（ActivatingSlot）。
        /// 原因：DoActivate中slot.isActive在effect.Activate()之后才设为true，
        ///       但RebuildVerbs在effect.Activate()内部调用，此时GetActiveSlot找不到该槽位。
        /// </summary>
        private ChipSlot GetActiveOrActivatingSlot(SlotSide side)
        {
            var active = GetActiveSlot(side);
            if (active != null) return active;
            // 回退：检查正在激活的槽位是否属于该侧
            if (ActivatingSlot != null && ActivatingSlot.side == side)
                return ActivatingSlot;
            return null;
        }

        // ── 读档Verb复用（v8.0 PMS重构） ──

        /// <summary>
        /// 从读档反序列化的Verb列表中查找匹配loadID的实例。
        /// 找到后从列表移除（避免重复匹配）。
        /// </summary>
        private Verb FindSavedVerb(string loadID)
        {
            if (savedChipVerbs == null) return null;
            for (int i = 0; i < savedChipVerbs.Count; i++)
            {
                if (savedChipVerbs[i]?.loadID == loadID)
                {
                    var verb = savedChipVerbs[i];
                    savedChipVerbs.RemoveAt(i);
                    return verb;
                }
            }
            return null;
        }

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
