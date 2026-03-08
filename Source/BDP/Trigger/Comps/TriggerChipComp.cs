using System;
using System.Collections.Generic;
using System.Text;
using BDP.Core;
using BDP.FireMode;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 芯片物品上的ThingComp——持有IChipEffect实现实例。
    /// 芯片本身无状态，效果实例由此Comp懒加载创建。
    /// v2.0：添加SpecialDisplayStats，按类型展示芯片完整参数（武器身份代理系统 阶段2）。
    /// </summary>
    public class TriggerChipComp : ThingComp
    {
        private IChipEffect effectInstance;

        public CompProperties_TriggerChip Props => (CompProperties_TriggerChip)props;

        /// <summary>
        /// 获取IChipEffect实例（懒加载）。
        /// 通过Activator.CreateInstance(chipEffectClass)创建，要求无参构造函数。
        /// </summary>
        public IChipEffect GetEffect()
        {
            if (effectInstance != null) return effectInstance;

            if (Props.chipEffectClass == null)
            {
                Log.Error($"[BDP] TriggerChipComp on {parent.def.defName}: chipEffectClass未配置");
                return null;
            }

            try
            {
                effectInstance = (IChipEffect)Activator.CreateInstance(Props.chipEffectClass);
            }
            catch (Exception e)
            {
                Log.Error($"[BDP] 无法实例化IChipEffect {Props.chipEffectClass}: {e.Message}");
            }

            return effectInstance;
        }

        // ═══════════════════════════════════════════
        //  SpecialDisplayStats — Info Card展示
        // ═══════════════════════════════════════════

        /// <summary>
        /// 芯片Info Card的stat条目列表。按类型展示：基础信息 + 武器/Hediff/Ability专属参数。
        /// </summary>
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            // ── 1. 基础信息区块 ──
            foreach (var entry in BaseInfoStats())
                yield return entry;

            // ── 2. 武器芯片专属参数 ──
            var verbCfg = parent.def.GetModExtension<VerbChipConfig>();
            if (verbCfg != null)
            {
                foreach (var entry in WeaponChipStats(verbCfg))
                    yield return entry;
            }

            // ── 3. Hediff芯片专属参数 ──
            var hediffCfg = parent.def.GetModExtension<HediffChipConfig>();
            if (hediffCfg?.hediffDef != null)
            {
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_ChipInfo,
                    "授予效果",
                    hediffCfg.hediffDef.LabelCap,
                    "激活时添加到装备者身上的Hediff效果。\n\n" + hediffCfg.hediffDef.description,
                    2400);
            }

            // ── 4. Ability芯片专属参数 ──
            var abilityCfg = parent.def.GetModExtension<AbilityChipConfig>();
            if (abilityCfg?.abilityDef != null)
            {
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_ChipInfo,
                    "授予技能",
                    abilityCfg.abilityDef.LabelCap,
                    "激活时授予装备者的能力。\n\n" + abilityCfg.abilityDef.description,
                    2400);
            }
        }

        // ═══════════════════════════════════════════
        //  私有辅助方法 — 基础信息
        // ═══════════════════════════════════════════

        /// <summary>
        /// 芯片基础信息条目：类别、成本、预热、后摇、限制等。
        /// </summary>
        private IEnumerable<StatDrawEntry> BaseInfoStats()
        {
            // 类别标签
            if (Props.categories != null && Props.categories.Count > 0)
            {
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_ChipInfo,
                    "芯片类别",
                    string.Join(", ", Props.categories.ToArray()),
                    "芯片的功能分类标签。",
                    2500);
            }

            // 激活成本
            if (Props.activationCost > 0f)
            {
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_ChipInfo,
                    "激活成本",
                    Props.activationCost.ToString("F1") + " Trion",
                    "激活时一次性消耗的Trion量。",
                    2490);
            }

            // 占用成本
            if (Props.allocationCost > 0f)
            {
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_ChipInfo,
                    "占用成本",
                    Props.allocationCost.ToString("F1") + " Trion",
                    "战斗体激活时锁定的Trion容量（不可用于其他芯片）。",
                    2489);
            }

            // 预热时间
            if (Props.activationWarmup > 0)
            {
                float seconds = Props.activationWarmup / 60f;
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_ChipInfo,
                    "激活预热",
                    seconds.ToString("F2") + "秒",
                    "切换到此芯片时的预热等待时间。",
                    2488);
            }

            // 后摇时长
            if (Props.deactivationDelay > 0)
            {
                float seconds = Props.deactivationDelay / 60f;
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_ChipInfo,
                    "关闭后摇",
                    seconds.ToString("F2") + "秒",
                    "关闭此芯片时的后摇延迟时间。",
                    2487);
            }

            // 每日消耗
            if (Props.drainPerDay > 0f)
            {
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_ChipInfo,
                    "持续消耗",
                    Props.drainPerDay.ToString("F1") + " Trion/天",
                    "激活期间每天持续消耗的Trion量。",
                    2486);
            }

            // 最低功率要求
            if (Props.minOutputPower > 0f)
            {
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_ChipInfo,
                    "功率要求",
                    Props.minOutputPower.ToString("F1"),
                    "激活此芯片所需的最低Trion输出功率。",
                    2485);
            }

            // 槽位限制
            if (Props.slotRestriction != ChipSlotRestriction.None)
            {
                string restrictionLabel = Props.slotRestriction == ChipSlotRestriction.SpecialOnly
                    ? "仅特殊槽位"
                    : "仅左右手槽位";
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_ChipInfo,
                    "槽位限制",
                    restrictionLabel,
                    "此芯片只能装载到特定类型的槽位。",
                    2484);
            }

            // 双手占用
            if (Props.isDualHand)
            {
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_ChipInfo,
                    "双手占用",
                    "是",
                    "激活后锁定双手槽位，另一侧不可独立激活/切换。",
                    2483);
            }

            // 互斥标签
            if (Props.exclusionTags != null && Props.exclusionTags.Count > 0)
            {
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_ChipInfo,
                    "互斥标签",
                    string.Join(", ", Props.exclusionTags.ToArray()),
                    "不能与带有相同标签的其他芯片同时激活。",
                    2482);
            }
        }

        // ═══════════════════════════════════════════
        //  私有辅助方法 — 武器芯片参数
        // ═══════════════════════════════════════════

        /// <summary>
        /// 武器芯片专属参数条目：伤害、射程、预热、连射、Trion消耗等。
        /// </summary>
        private IEnumerable<StatDrawEntry> WeaponChipStats(VerbChipConfig cfg)
        {
            // ── 远程武器参数 ──
            if (cfg.primaryVerbProps != null)
            {
                var vp = cfg.primaryVerbProps;

                // 读取FireMode倍率（芯片Thing上可能有CompFireMode）
                var fireMode = parent.TryGetComp<CompFireMode>();
                float dmgMul = fireMode?.Damage ?? 1f;
                float spdMul = fireMode?.Speed ?? 1f;
                float burstMul = fireMode?.Burst ?? 1f;

                // 伤害（含伤害类型名）
                if (vp.defaultProjectile?.projectile != null)
                {
                    var proj = vp.defaultProjectile.projectile;
                    int baseDmg = proj.GetDamageAmount(null);
                    int finalDmg = UnityEngine.Mathf.RoundToInt(baseDmg * dmgMul);
                    string dmgType = proj.damageDef?.LabelCap ?? "";
                    yield return new StatDrawEntry(
                        StatCategoryDefOf.Weapon_Ranged,
                        "伤害",
                        finalDmg + " (" + dmgType + ")",
                        "每次命中造成的伤害。默认射击模式倍率：" + dmgMul.ToString("F2") + "x。",
                        2490);
                }

                // 射程
                yield return new StatDrawEntry(
                    StatCategoryDefOf.Weapon_Ranged,
                    "射程",
                    vp.range.ToString("F1") + "格",
                    "最大有效射程。",
                    2489);

                // 预热时间（应用速度倍率）
                float warmup = vp.warmupTime / spdMul;
                yield return new StatDrawEntry(
                    StatCategoryDefOf.Weapon_Ranged,
                    "预热时间",
                    warmup.ToString("F2") + "秒",
                    "开火前的瞄准预热时间。默认射击模式倍率：" + spdMul.ToString("F2") + "x。",
                    2488);

                // 连射数（burstShotCount > 1时）
                int burstCount = UnityEngine.Mathf.RoundToInt(vp.burstShotCount * burstMul);
                if (burstCount > 1)
                {
                    yield return new StatDrawEntry(
                        StatCategoryDefOf.Weapon_Ranged,
                        "连射数",
                        burstCount.ToString() + "发",
                        "每次射击的连射发数。默认射击模式倍率：" + burstMul.ToString("F2") + "x。",
                        2487);

                    // 连射速率（rpm）
                    if (vp.ticksBetweenBurstShots > 0)
                    {
                        float rpm = 3600f / vp.ticksBetweenBurstShots;
                        yield return new StatDrawEntry(
                            StatCategoryDefOf.Weapon_Ranged,
                            "连射速率",
                            rpm.ToString("F0") + " rpm",
                            "连射期间每分钟射击发数（60秒/连射间隔）。",
                            2486);
                    }
                }

                // Trion消耗/发
                if ((cfg.cost?.trionPerShot ?? 0f) > 0f)
                {
                    yield return new StatDrawEntry(
                        StatCategoryDefOf.Weapon_Ranged,
                        "Trion消耗",
                        (cfg.cost.trionPerShot).ToString("F1") + " /发",
                        "每发射击消耗的Trion量。",
                        2485);
                }

                // 齐射散布
                if ((cfg.ranged?.volleySpreadRadius ?? 0f) > 0f)
                {
                    yield return new StatDrawEntry(
                        StatCategoryDefOf.Weapon_Ranged,
                        "齐射散布",
                        (cfg.ranged.volleySpreadRadius).ToString("F1") + "格",
                        "齐射时每发子弹起点的随机偏移半径。",
                        2484);
                }

                // 引导支持
                if (cfg.ranged?.guided != null)
                {
                    yield return new StatDrawEntry(
                        StatCategoryDefOf.Weapon_Ranged,
                        "变化弹支持",
                        "是 (最多" + (cfg.ranged.guided.maxAnchors) + "锚点)",
                        "支持引导飞行模式，可设置路径锚点。",
                        2483);
                }

                // 穿透力
                if ((cfg.ranged?.passthroughPower ?? 0f) > 0f)
                {
                    yield return new StatDrawEntry(
                        StatCategoryDefOf.Weapon_Ranged,
                        "穿透力",
                        (cfg.ranged.passthroughPower).ToString("F1"),
                        "穿体穿透力初始值，每次穿透后递减。",
                        2482);
                }
            }

            // ── 近战Tool参数 ──
            if (cfg.melee?.tools != null && cfg.melee.tools.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("近战攻击方式：");
                foreach (var tool in cfg.melee.tools)
                {
                    sb.AppendLine("• " + tool.label + ": " + tool.power.ToString("F0") + "dmg / " + tool.cooldownTime.ToString("F1") + "秒");
                }

                yield return new StatDrawEntry(
                    StatCategoryDefOf.Weapon_Melee,
                    "近战攻击",
                    cfg.melee.tools.Count + "种方式",
                    sb.ToString().TrimEnd(),
                    2480);
            }
        }
    }
}
