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
                else if (verb is Verb_BDPDualRanged)
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
                leftHandSecondaryVerb = CreateSingleSideSecondaryVerb(leftCfg, pawn, "Left", SlotSide.LeftHand);

            // 右手副攻击
            if (rightHandAttackVerb != null && rightCfg != null)
                rightHandSecondaryVerb = CreateSingleSideSecondaryVerb(rightCfg, pawn, "Right", SlotSide.RightHand);

            // 双手副攻击：两侧都有副攻击配置时创建Verb_BDPDualVolley
            if (leftHandSecondaryVerb != null && rightHandSecondaryVerb != null && dualAttackVerb != null)
                dualSecondaryVerb = CreateDualSecondaryVerb(leftCfg, rightCfg, pawn);
        }

        /// <summary>
        /// 创建单侧副攻击verb实例。
        /// 创建后设置chipSide以便运行时精确查找芯片。
        /// </summary>
        private Verb CreateSingleSideSecondaryVerb(WeaponChipConfig cfg, Pawn pawn, string sideTag, SlotSide side)
        {
            if (cfg.secondaryVerbProps == null) return null;

            string loadID = $"BDP_Secondary{sideTag}_{parent.ThingID}_{cfg.secondaryVerbProps.label}";
            Verb verb = FindOrCreateVerb(cfg.secondaryVerbProps, pawn, loadID);

            // 设置chipSide以便运行时精确查找芯片
            if (verb is Verb_BDPRangedBase rb)
                rb.chipSide = side;

            return verb;
        }

        /// <summary>
        /// 创建双手副攻击verb实例（Verb_BDPDualVolley）。
        /// 合成两侧参数：range=min, warmup=max, cooldown=max, burstShotCount=1。
        /// chipSide不设置（null）——双侧Verb由子类各自处理。
        /// </summary>
        private Verb CreateDualSecondaryVerb(WeaponChipConfig leftCfg, WeaponChipConfig rightCfg, Pawn pawn)
        {
            var leftSecVp = leftCfg.secondaryVerbProps;
            var rightSecVp = rightCfg.secondaryVerbProps;

            // 合成双手副攻击VerbProperties
            var vp = new VerbProperties
            {
                verbClass = typeof(Verb_BDPDualVolley),
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
                burstShotCount = 1,
                ticksBetweenBurstShots = 0,
                label = "双手齐射",
            };

            string loadID = $"BDP_SecondaryDual_{parent.ThingID}";
            return FindOrCreateVerb(vp, pawn, loadID);
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
        /// 创建单个组合技Verb实例（v10.0新增，v9.0重构）。
        /// 参数取两侧芯片的平均值。
        /// </summary>
        private Verb CreateComboVerb(ComboVerbDef comboDef, bool isSecondary,
            Pawn pawn, ChipSlot leftSlot, ChipSlot rightSlot)
        {
            var leftCfg = leftSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            var rightCfg = rightSlot.loadedChip.def.GetModExtension<WeaponChipConfig>();
            if (leftCfg == null || rightCfg == null) return null;

            // 如果是副攻击但未配置，返回null
            if (isSecondary)
            {
                if (comboDef.secondaryVerbClass != null)
                {
                    return CreateComboVerbFromClass(comboDef, comboDef.secondaryVerbClass,
                        pawn, leftCfg, rightCfg, true);
                }
                return null; // 无副攻击
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
                (leftCfg.GetPrimaryBurstCount() + rightCfg.GetPrimaryBurstCount()) * 0.5f);
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

        // ── 组合技参数读取辅助（从WeaponChipConfig.primaryVerbProps读取） ──

        private static float GetFirstRange(WeaponChipConfig cfg)
        {
            return cfg?.primaryVerbProps?.range ?? 20f;
        }

        private static float GetFirstWarmup(WeaponChipConfig cfg)
        {
            return cfg?.primaryVerbProps?.warmupTime ?? 1f;
        }

        private static float GetFirstCooldown(WeaponChipConfig cfg)
        {
            return cfg?.primaryVerbProps?.defaultCooldownTime ?? 1f;
        }

        private static int GetFirstTicksBetween(WeaponChipConfig cfg)
        {
            return cfg?.primaryVerbProps?.ticksBetweenBurstShots ?? 8;
        }

        private static SoundDef GetFirstSound(WeaponChipConfig cfg)
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
    }
}
