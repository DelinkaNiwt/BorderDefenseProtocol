using System;
using System.Collections.Generic;
using BDP.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody的Verb系统部分类（阶段3.1）。
    ///
    /// 职责：
    /// - VerbTracker重建和芯片Verb手动创建
    /// - 副攻击Verb创建（齐射/自定义）
    /// - 组合技Verb创建和匹配
    /// - IVerbOwner接口实现
    /// - Verb查找和复用逻辑
    /// </summary>
    public partial class CompTriggerBody
    {
        // ═══════════════════════════════════════════
        //  IVerbOwner接口实现（v5.1改造：芯片Verb完全脱离VerbTracker）
        // ═══════════════════════════════════════════
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

            // v14.0：同步ProxyVerb（自动攻击支持）
            SyncProxyVerb(pawn);

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
            // v15.0：传递VerbChipConfig以便在DualVerbCompositor中设置firingPattern
            var leftSlotForCompose = GetActiveOrActivatingSlot(SlotSide.LeftHand);
            var rightSlotForCompose = GetActiveOrActivatingSlot(SlotSide.RightHand);

            Log.Message($"[BDP调试] CreateAndCacheChipVerbs调用: 左槽={leftSlotForCompose?.loadedChip?.def?.defName ?? "null"} | 右槽={rightSlotForCompose?.loadedChip?.def?.defName ?? "null"} | ActivatingSlot={ActivatingSlot?.loadedChip?.def?.defName ?? "null"}");

            var leftConfig = leftSlotForCompose?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
            var rightConfig = rightSlotForCompose?.loadedChip?.def?.GetModExtension<VerbChipConfig>();

            if (leftConfig != null)
                Log.Message($"[BDP调试] 左槽配置: primaryFiringPattern={leftConfig.primaryFiringPattern}");
            if (rightConfig != null)
                Log.Message($"[BDP调试] 右槽配置: primaryFiringPattern={rightConfig.primaryFiringPattern}");

            var chipVerbProps = DualVerbCompositor.ComposeVerbs(
                leftHandActiveVerbProps, rightHandActiveVerbProps,
                leftSlotForCompose, rightSlotForCompose,
                leftConfig, rightConfig);

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

                // v12.0：为Verb_BDPSingle设置firingPattern
                if (verb is Verb_BDPSingle singleVerb)
                {
                    var side = DualVerbCompositor.ParseSideLabel(label);
                    if (side.HasValue)
                    {
                        var slot = GetActiveOrActivatingSlot(side.Value);
                        var cfg = slot?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
                        if (cfg != null)
                        {
                            singleVerb.firingPattern = cfg.primaryFiringPattern;

                            // 齐射模式必须设置burstShotCount=1
                            if (singleVerb.firingPattern == FiringPattern.Simultaneous)
                            {
                                verb.verbProps.burstShotCount = 1;
                                verb.verbProps.ticksBetweenBurstShots = 0;
                            }
                        }
                    }
                }

                // v12.0：为Verb_BDPDual设置leftFiringPattern和rightFiringPattern
                // v17.0：使用CalculateDualFiringConfig公共方法
                if (verb is Verb_BDPDual dualVerb)
                {
                    var leftSlot = GetActiveOrActivatingSlot(SlotSide.LeftHand);
                    var rightSlot = GetActiveOrActivatingSlot(SlotSide.RightHand);
                    var leftCfg = leftSlot?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
                    var rightCfg = rightSlot?.loadedChip?.def?.GetModExtension<VerbChipConfig>();

                    if (leftCfg != null && rightCfg != null)
                    {
                        var config = CalculateDualFiringConfig(leftCfg, rightCfg, useSecondary: false);

                        dualVerb.leftFiringPattern = config.LeftPattern;
                        dualVerb.rightFiringPattern = config.RightPattern;
                        verb.verbProps.burstShotCount = config.TotalBurstCount;
                    }
                }

                if (verb is Verb_BDPMelee || verb is Verb_BDPRangedBase)
                {
                    var side = DualVerbCompositor.ParseSideLabel(label);
                    if (side == SlotSide.LeftHand)
                    {
                        leftHandAttackVerb = verb;
                        // 设置chipSide以便运行时精确查找芯片
                        if (verb is Verb_BDPRangedBase rbL) rbL.chipSide = SlotSide.LeftHand;
                    }
                    else if (side == SlotSide.RightHand)
                    {
                        rightHandAttackVerb = verb;
                        if (verb is Verb_BDPRangedBase rbR) rbR.chipSide = SlotSide.RightHand;
                    }
                    else
                        dualAttackVerb = verb;
                }
                else if (verb is Verb_BDPDual)
                {
                    dualAttackVerb = verb;
                    // chipSide保持null——双侧Verb由子类各自处理
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
        ///   为支持齐射的芯片创建独立的齐射Verb类实例。
        ///
        /// v15.0变更：发射模式从类级区分改为Def配置级属性（FiringPattern）。
        ///
        /// 注意：使用GetActiveOrActivatingSlot而非GetActiveSlot。
        /// 原因：DoActivate中effect.Activate()触发RebuildVerbs时，
        ///       slot.isActive尚未设为true，GetActiveSlot找不到正在激活的芯片。
        /// </summary>
        private void CreateSecondaryVerbs(Pawn pawn)
        {
            var leftSlot = GetActiveOrActivatingSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveOrActivatingSlot(SlotSide.RightHand);
            var leftCfg = leftSlot?.loadedChip?.def?.GetModExtension<VerbChipConfig>();
            var rightCfg = rightSlot?.loadedChip?.def?.GetModExtension<VerbChipConfig>();

            // 左手副攻击
            if (leftHandAttackVerb != null && leftCfg != null)
                leftHandSecondaryVerb = CreateSingleSideSecondaryVerb(leftCfg, pawn, "Left", SlotSide.LeftHand);

            // 右手副攻击
            if (rightHandAttackVerb != null && rightCfg != null)
                rightHandSecondaryVerb = CreateSingleSideSecondaryVerb(rightCfg, pawn, "Right", SlotSide.RightHand);

            // 双手副攻击：两侧都有副攻击配置时创建Verb_BDPDual
            if (leftHandSecondaryVerb != null && rightHandSecondaryVerb != null && dualAttackVerb != null)
                dualSecondaryVerb = CreateDualSecondaryVerb(leftCfg, rightCfg, pawn);
        }

        /// <summary>
        /// 创建单侧副攻击verb实例。
        /// 创建后设置chipSide以便运行时精确查找芯片。
        /// v12.0：设置firingPattern。
        /// </summary>
        private Verb CreateSingleSideSecondaryVerb(VerbChipConfig cfg, Pawn pawn, string sideTag, SlotSide side)
        {
            if (cfg.secondaryVerbProps == null) return null;

            string loadID = $"BDP_Secondary{sideTag}_{parent.ThingID}_{cfg.secondaryVerbProps.label}";
            Verb verb = FindOrCreateVerb(cfg.secondaryVerbProps, pawn, loadID);

            // 设置chipSide以便运行时精确查找芯片
            if (verb is Verb_BDPRangedBase rb)
                rb.chipSide = side;

            // v12.0：为Verb_BDPSingle设置firingPattern
            if (verb is Verb_BDPSingle singleVerb)
            {
                singleVerb.firingPattern = cfg.secondaryFiringPattern;
                // 齐射模式必须设置burstShotCount=1
                if (singleVerb.firingPattern == FiringPattern.Simultaneous)
                {
                    verb.verbProps.burstShotCount = 1;
                    verb.verbProps.ticksBetweenBurstShots = 0;
                }
            }

            return verb;
        }

        /// <summary>
        /// 创建双手副攻击verb实例（Verb_BDPDual）。
        /// 合成两侧参数：range=min, warmup=max, cooldown=max, burstShotCount根据FiringPattern计算。
        /// chipSide不设置（null）——双侧Verb由子类各自处理。
        /// v12.0：改用Verb_BDPDual，设置leftFiringPattern和rightFiringPattern。
        /// </summary>
        private Verb CreateDualSecondaryVerb(VerbChipConfig leftCfg, VerbChipConfig rightCfg, Pawn pawn)
        {
            var leftSecVp = leftCfg.secondaryVerbProps;
            var rightSecVp = rightCfg.secondaryVerbProps;

            // 计算burstShotCount
            int leftBurst = leftSecVp?.burstShotCount ?? 1;
            int rightBurst = rightSecVp?.burstShotCount ?? 1;
            int leftEffective = leftCfg.secondaryFiringPattern == FiringPattern.Simultaneous ? 1 : leftBurst;
            int rightEffective = rightCfg.secondaryFiringPattern == FiringPattern.Simultaneous ? 1 : rightBurst;

            // 合成双手副攻击VerbProperties
            var vp = new VerbProperties
            {
                verbClass = typeof(Verb_BDPDual),
                isPrimary = false,
                hasStandardCommand = false,
                defaultProjectile = leftSecVp?.defaultProjectile ?? rightSecVp?.defaultProjectile,
                soundCast = leftSecVp?.soundCast ?? rightSecVp?.soundCast,
                muzzleFlashScale = 10f,
                range = Mathf.Min(leftSecVp?.range ?? 20f, rightSecVp?.range ?? 20f),
                warmupTime = Mathf.Max(leftSecVp?.warmupTime ?? 1f, rightSecVp?.warmupTime ?? 1f),
                defaultCooldownTime = Mathf.Max(
                    leftSecVp?.defaultCooldownTime ?? 1f,
                    rightSecVp?.defaultCooldownTime ?? 1f),
                burstShotCount = leftEffective + rightEffective,
                ticksBetweenBurstShots = Mathf.Max(
                    leftSecVp?.ticksBetweenBurstShots ?? 0,
                    rightSecVp?.ticksBetweenBurstShots ?? 0),
            };

            string loadID = $"BDP_DualSecondary_{parent.ThingID}";
            Verb verb = FindOrCreateVerb(vp, pawn, loadID);

            // v12.0：为Verb_BDPDual设置leftFiringPattern和rightFiringPattern
            if (verb is Verb_BDPDual dualVerb)
            {
                dualVerb.leftFiringPattern = leftCfg.secondaryFiringPattern;
                dualVerb.rightFiringPattern = rightCfg.secondaryFiringPattern;
            }

            return verb;
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
        /// 创建单个组合技Verb实例（v10.0新增，v9.0重构，v15.0改为internal）。
        /// 参数取两侧芯片的平均值。
        /// 由 ComboVerbDef.ActivateEffect 通过 CreateComboVerbsForDef 调用。
        /// </summary>
        internal Verb CreateComboVerb(ComboVerbDef comboDef, bool isSecondary,
            Pawn pawn, ChipSlot leftSlot, ChipSlot rightSlot)
        {
            var leftCfg = leftSlot.loadedChip.def.GetModExtension<VerbChipConfig>();
            var rightCfg = rightSlot.loadedChip.def.GetModExtension<VerbChipConfig>();
            if (leftCfg == null || rightCfg == null) return null;

            // v16.0：副攻击逻辑 - 两侧都有副攻击配置时才创建组合技副攻击
            if (isSecondary)
            {
                // 检查两侧芯片是否都配置了副攻击
                bool leftHasSecondary = leftCfg.secondaryVerbProps != null;
                bool rightHasSecondary = rightCfg.secondaryVerbProps != null;

                // 只有两侧都有副攻击时，才创建组合技副攻击
                if (!leftHasSecondary || !rightHasSecondary)
                    return null;

                // 如果ComboVerbDef配置了secondaryVerbClass，使用它；否则使用primaryVerbClass
                Type verbClass = comboDef.secondaryVerbClass ?? comboDef.primaryVerbClass;
                return CreateComboVerbFromClass(comboDef, verbClass,
                    pawn, leftSlot, rightSlot, leftCfg, rightCfg, true);
            }

            // 主攻击逻辑：使用primaryVerbClass
            return CreateComboVerbFromClass(comboDef, comboDef.primaryVerbClass,
                pawn, leftSlot, rightSlot, leftCfg, rightCfg, false);
        }

        /// <summary>
        /// 从指定Verb类型创建组合技Verb实例（v9.0新增）。
        /// </summary>
        private Verb CreateComboVerbFromClass(ComboVerbDef comboDef, System.Type verbClass,
            Pawn pawn, ChipSlot leftSlot, ChipSlot rightSlot, VerbChipConfig leftCfg, VerbChipConfig rightCfg, bool isSecondary)
        {
            // v17.0：使用CalculateDualFiringConfig计算发射模式和burstShotCount
            var firingConfig = CalculateDualFiringConfig(leftCfg, rightCfg, isSecondary);

            // 计算平均参数
            float avgRange = (GetFirstRange(leftCfg) + GetFirstRange(rightCfg)) * 0.5f;
            float avgWarmup = (GetFirstWarmup(leftCfg) + GetFirstWarmup(rightCfg)) * 0.5f;
            float avgCooldown = (GetFirstCooldown(leftCfg) + GetFirstCooldown(rightCfg)) * 0.5f;
            int avgBurst = UnityEngine.Mathf.RoundToInt(
                (leftCfg.GetPrimaryBurstCount() + rightCfg.GetPrimaryBurstCount()) * 0.5f);
            if (avgBurst < 1) avgBurst = 1;

            // 获取使用消耗（统一层）
            float leftCost = ChipUsageCostHelper.GetUsageCost(leftSlot.loadedChip);
            float rightCost = ChipUsageCostHelper.GetUsageCost(rightSlot.loadedChip);
            float avgTrionCost = (leftCost + rightCost) * 0.5f;

            float avgAnchorSpread = ((leftCfg.ranged?.guided?.anchorSpread ?? 0.3f) + (rightCfg.ranged?.guided?.anchorSpread ?? 0.3f)) * 0.5f;
            float avgVolleySpread = ((leftCfg.ranged?.volleySpreadRadius ?? 0f) + (rightCfg.ranged?.volleySpreadRadius ?? 0f)) * 0.5f;
            int avgTicksBetween = UnityEngine.Mathf.RoundToInt(
                (GetFirstTicksBetween(leftCfg) + GetFirstTicksBetween(rightCfg)) * 0.5f);

            // 构建VerbProperties
            // v17.0：burstShotCount使用CalculateDualFiringConfig的结果，而不是硬编码
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
                burstShotCount = firingConfig.TotalBurstCount,  // 使用计算出的burst数
                ticksBetweenBurstShots = firingConfig.IsBothSimultaneous ? 0 : avgTicksBetween,
                label = isSecondary ? "组合技副攻击" : comboDef.label,
            };

            string suffix = isSecondary ? "Secondary" : "Attack";
            string loadID = $"BDP_Combo_{parent.ThingID}_{comboDef.defName}_{suffix}";

            // 使用统一的FindOrCreateVerb方法
            Verb verb = FindOrCreateVerb(vp, pawn, loadID);
            if (verb == null) return null;

            // v12.0：设置组合技专用字段（Verb_BDPCombo）
            // v17.0：使用CalculateDualFiringConfig公共方法
            if (verb is Verb_BDPCombo comboVerb)
            {
                comboVerb.comboDef = comboDef;

                var config = CalculateDualFiringConfig(leftCfg, rightCfg, isSecondary);

                // 组合技：双侧齐射→齐射，其他→逐发
                comboVerb.firingPattern = config.IsBothSimultaneous
                    ? FiringPattern.Simultaneous
                    : FiringPattern.Sequential;

                comboVerb.avgBurstCount = avgBurst;
                comboVerb.avgTrionCost = avgTrionCost;
                comboVerb.avgAnchorSpread = avgAnchorSpread;
                comboVerb.avgVolleySpread = avgVolleySpread;
            }

            return verb;
        }

        // ── 双侧发射模式配置（v17.0提取公共逻辑） ──

        /// <summary>
        /// 双侧发射配置结果（v17.0新增）。
        /// </summary>
        private struct DualFiringConfig
        {
            public FiringPattern LeftPattern;
            public FiringPattern RightPattern;
            public int TotalBurstCount;
            public bool IsBothSimultaneous;
        }

        /// <summary>
        /// 为双侧Verb配置发射模式和burstShotCount（v17.0提取公共逻辑）。
        /// 适用于Verb_BDPDual和Verb_BDPCombo。
        /// </summary>
        /// <param name="leftCfg">左侧芯片配置</param>
        /// <param name="rightCfg">右侧芯片配置</param>
        /// <param name="useSecondary">是否使用副攻击模式</param>
        /// <returns>配置结果：(leftPattern, rightPattern, totalBurstCount, isBothSimultaneous)</returns>
        private static DualFiringConfig CalculateDualFiringConfig(
            VerbChipConfig leftCfg,
            VerbChipConfig rightCfg,
            bool useSecondary = false)
        {
            // 1. 读取发射模式
            FiringPattern leftPattern = useSecondary
                ? leftCfg?.secondaryFiringPattern ?? FiringPattern.Sequential
                : leftCfg?.primaryFiringPattern ?? FiringPattern.Sequential;

            FiringPattern rightPattern = useSecondary
                ? rightCfg?.secondaryFiringPattern ?? FiringPattern.Sequential
                : rightCfg?.primaryFiringPattern ?? FiringPattern.Sequential;

            // 2. 判断是否双侧齐射
            bool isBothSimultaneous = (leftPattern == FiringPattern.Simultaneous &&
                                       rightPattern == FiringPattern.Simultaneous);

            // 3. 计算有效burst数
            int leftBurst = useSecondary
                ? (leftCfg?.secondaryVerbProps?.burstShotCount ?? 1)
                : leftCfg?.GetPrimaryBurstCount() ?? 1;
            int rightBurst = useSecondary
                ? (rightCfg?.secondaryVerbProps?.burstShotCount ?? 1)
                : rightCfg?.GetPrimaryBurstCount() ?? 1;

            int leftEffective = leftPattern == FiringPattern.Simultaneous ? 1 : leftBurst;
            int rightEffective = rightPattern == FiringPattern.Simultaneous ? 1 : rightBurst;

            // 4. 计算总burst数
            int totalBurstCount = isBothSimultaneous ? 1 : (leftEffective + rightEffective);

            return new DualFiringConfig
            {
                LeftPattern = leftPattern,
                RightPattern = rightPattern,
                TotalBurstCount = totalBurstCount,
                IsBothSimultaneous = isBothSimultaneous
            };
        }

        // ── 组合技参数读取辅助（从VerbChipConfig.primaryVerbProps读取） ──

        private static float GetFirstRange(VerbChipConfig cfg)
        {
            return cfg?.primaryVerbProps?.range ?? 20f;
        }

        private static float GetFirstWarmup(VerbChipConfig cfg)
        {
            return cfg?.primaryVerbProps?.warmupTime ?? 1f;
        }

        private static float GetFirstCooldown(VerbChipConfig cfg)
        {
            return cfg?.primaryVerbProps?.defaultCooldownTime ?? 1f;
        }

        private static int GetFirstTicksBetween(VerbChipConfig cfg)
        {
            return cfg?.primaryVerbProps?.ticksBetweenBurstShots ?? 8;
        }

        private static SoundDef GetFirstSound(VerbChipConfig cfg)
        {
            return cfg?.primaryVerbProps?.soundCast;
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

        // ── v14.0新增：ProxyVerb自动攻击支持 ──

        /// <summary>
        /// 同步ProxyVerb（v14.0自动攻击）。
        /// 在RebuildVerbs末尾调用，根据当前主攻击芯片Verb创建或更新ProxyVerb。
        /// 无远程芯片时proxyVerb=null，引擎回退到"柄"近战。
        /// </summary>
        private void SyncProxyVerb(Pawn pawn)
        {
            var primary = GetPrimaryChipVerb();
            // 无武器芯片或仅近战芯片 → 清空ProxyVerb
            if (primary == null || primary.IsMeleeAttack)
            {
                proxyVerb = null;
                return;
            }

            // 创建或复用ProxyVerb实例
            if (proxyVerb == null)
            {
                proxyVerb = new Verb_BDPProxy();
                proxyVerb.verbTracker = VerbTracker;
                proxyVerb.loadID = $"BDP_Proxy_{parent.ThingID}";
            }

            // 绑定caster和同步verbProps
            proxyVerb.caster = pawn;
            proxyVerb.SyncFrom(primary, this);
        }

        /// <summary>
        /// 驱动芯片Verb的VerbTick（v14.0自动攻击）。
        /// 由Patch_VerbTracker_VerbsTick调用，防止double-tick。
        /// 原因：芯片Verb不在VerbTracker.AllVerbs中，引擎不会自动tick，
        /// 导致burst连射第2发起卡住（ticksToNextBurstShot不递减）。
        /// </summary>
        internal void TickChipVerbs()
        {
            int curTick = Find.TickManager.TicksGame;
            if (curTick == lastChipVerbTickedTick) return; // 防double-tick
            lastChipVerbTickedTick = curTick;

            leftHandAttackVerb?.VerbTick();
            rightHandAttackVerb?.VerbTick();
            dualAttackVerb?.VerbTick();
            // 副攻击和组合技Verb也需要tick（warmup/cooldown计时）
            leftHandSecondaryVerb?.VerbTick();
            rightHandSecondaryVerb?.VerbTick();
            dualSecondaryVerb?.VerbTick();
            comboAttackVerb?.VerbTick();
            comboSecondaryVerb?.VerbTick();
        }
    }
}
