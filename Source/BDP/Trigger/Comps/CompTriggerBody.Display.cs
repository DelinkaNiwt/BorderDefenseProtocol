using System.Collections.Generic;
using System.Text;
using BDP.Core;
using BDP.FireMode;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// CompTriggerBody部分类 - 信息展示模块（武器身份代理系统 阶段1）
    ///
    /// 职责：
    /// - 通过ThingComp虚方法override，将激活芯片的武器信息投射到触发体的原版UI通道
    /// - TransformLabel：动态标签（装备栏、检视面板标题）
    /// - CompInspectStringExtra：左下角检视面板
    /// - CompTipStringExtra：悬停Tooltip
    /// - SpecialDisplayStats：Info Card的stat条目
    ///
    /// 设计原则：
    /// - 纯新增override，不修改任何现有方法
    /// - 无激活芯片时返回null/原值，向后兼容
    /// - 直接从slot.loadedChip.def.GetModExtension读取，不走ActivatingSlot上下文
    /// </summary>
    public partial class CompTriggerBody
    {
        // ═══════════════════════════════════════════
        //  TransformLabel — 动态标签
        // ═══════════════════════════════════════════

        /// <summary>
        /// 根据激活芯片动态修改触发体显示名称。
        /// 无芯片→原标签，单侧→[芯片名]，双侧同→[芯片名×2]，双侧异→[左/右]。
        /// </summary>
        public override string TransformLabel(string label)
        {
            // 获取左右手激活槽位
            var leftSlot = GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveSlot(SlotSide.RightHand);

            // 无激活芯片 → 原标签
            if (leftSlot == null && rightSlot == null)
                return label;

            string leftLabel = leftSlot?.loadedChip?.LabelNoCount;
            string rightLabel = rightSlot?.loadedChip?.LabelNoCount;

            // 单侧激活
            if (leftSlot != null && rightSlot == null)
                return label + " [" + leftLabel + "]";
            if (leftSlot == null && rightSlot != null)
                return label + " [" + rightLabel + "]";

            // 双侧激活：同芯片def → ×2，异芯片 → 左/右
            if (leftSlot.loadedChip.def == rightSlot.loadedChip.def)
                return label + " [" + leftLabel + "×2]";

            return label + " [" + leftLabel + "/" + rightLabel + "]";
        }

        // ═══════════════════════════════════════════
        //  CompInspectStringExtra — 检视面板
        // ═══════════════════════════════════════════

        /// <summary>
        /// 左下角检视面板文本。显示各侧芯片名称和核心武器参数。
        /// </summary>
        public override string CompInspectStringExtra()
        {
            if (!HasAnyActiveChip()) return null;

            var sb = new StringBuilder();

            // 左手侧
            AppendSideStatus(sb, SlotSide.LeftHand, "左手");
            // 右手侧
            AppendSideStatus(sb, SlotSide.RightHand, "右手");
            // 特殊槽
            AppendSpecialStatus(sb);

            // 移除末尾换行
            if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
                sb.Length -= 1;

            return sb.Length > 0 ? sb.ToString() : null;
        }

        /// <summary>
        /// 向StringBuilder追加指定侧的芯片状态行。
        /// 格式：左手: 星尘(远程武器) 12dmg×5 射程40
        /// </summary>
        private void AppendSideStatus(StringBuilder sb, SlotSide side, string sideLabel)
        {
            var slot = GetActiveSlot(side);
            if (slot == null) return;

            var chip = slot.loadedChip;
            sb.Append(sideLabel).Append(": ").Append(chip.LabelNoCount);

            // 显示芯片主类别
            var chipProps = chip.TryGetComp<TriggerChipComp>()?.Props;
            if (chipProps != null && chipProps.primaryCategory != ChipPrimaryCategory.Unspecified)
            {
                sb.Append("(").Append(chipProps.GetPrimaryCategoryLabel()).Append(")");
            }

            // 读取武器配置
            var verbCfg = chip.def.GetModExtension<VerbChipConfig>();
            if (verbCfg?.primaryVerbProps != null)
            {
                var vp = verbCfg.primaryVerbProps;

                // 读取FireMode倍率
                var fireMode = chip.TryGetComp<CompFireMode>();
                float dmgMul = fireMode?.Damage ?? 1f;
                float burstMul = fireMode?.Burst ?? 1f;

                // 伤害
                string dmgStr = FormatDamageString(vp, dmgMul);
                if (dmgStr != null)
                    sb.Append(" ").Append(dmgStr);

                // 连射数（应用FireMode倍率后取整）
                int burstCount = UnityEngine.Mathf.RoundToInt(vp.burstShotCount * burstMul);
                if (burstCount > 1)
                    sb.Append("×").Append(burstCount);

                // 射程
                sb.Append(" 射程").Append(vp.range.ToString("F0"));
            }

            sb.AppendLine();
        }

        /// <summary>
        /// 追加特殊槽激活芯片的简要信息。
        /// </summary>
        private void AppendSpecialStatus(StringBuilder sb)
        {
            if (specialSlots == null) return;

            bool hasAny = false;
            foreach (var slot in specialSlots)
            {
                if (!slot.isActive || slot.loadedChip == null) continue;
                if (!hasAny)
                {
                    sb.Append("特殊: ");
                    hasAny = true;
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(slot.loadedChip.LabelNoCount);
            }
            if (hasAny) sb.AppendLine();
        }

        // ═══════════════════════════════════════════
        //  CompTipStringExtra — 悬停提示
        // ═══════════════════════════════════════════

        /// <summary>
        /// 悬停Tooltip文本。简洁格式：左手: 星尘 | 右手: 陨石
        /// </summary>
        public override string CompTipStringExtra()
        {
            var leftSlot = GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveSlot(SlotSide.RightHand);

            if (leftSlot == null && rightSlot == null)
                return null;

            var sb = new StringBuilder();

            if (leftSlot != null)
                sb.Append("左手: ").Append(leftSlot.loadedChip.LabelNoCount);

            if (leftSlot != null && rightSlot != null)
                sb.Append(" | ");

            if (rightSlot != null)
                sb.Append("右手: ").Append(rightSlot.loadedChip.LabelNoCount);

            return sb.ToString();
        }

        // ═══════════════════════════════════════════
        //  SpecialDisplayStats — Info Card stat条目
        // ═══════════════════════════════════════════

        /// <summary>
        /// Info Card的stat条目列表。按组输出：触发器配置（芯片概览+芯片属性）、武器参数。
        /// </summary>
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            // ── 1. 触发器配置区块 ──
            foreach (var entry in TriggerConfigEntries())
                yield return entry;

            // ── 2. 各激活芯片的武器参数 ──
            foreach (var entry in ActiveChipWeaponStats())
                yield return entry;
        }

        /// <summary>
        /// 触发器配置区块：芯片名称概览 + 芯片属性对比（两阶段对齐）。
        /// </summary>
        private IEnumerable<StatDrawEntry> TriggerConfigEntries()
        {
            var leftSlot  = GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveSlot(SlotSide.RightHand);
            var leftChip  = leftSlot?.loadedChip;
            var rightChip = rightSlot?.loadedChip;
            var leftProps  = leftChip?.TryGetComp<TriggerChipComp>()?.Props;
            var rightProps = rightChip?.TryGetComp<TriggerChipComp>()?.Props;

            // ── 芯片名称（概览行，始终显示） ──
            string nameValue = FormatSidedValue(
                leftChip?.LabelNoCount,
                rightChip?.LabelNoCount, "");
            if (string.IsNullOrEmpty(nameValue)) nameValue = "无激活芯片";

            // 构建tooltip：基础说明 + 各侧芯片描述
            var configDesc = new StringBuilder("触发体当前激活的芯片配置。");
            if (leftChip != null && !string.IsNullOrEmpty(leftChip.def.description))
                configDesc.Append("\n\n左手 [").Append(leftChip.LabelNoCount).Append("]:\n").Append(leftChip.def.description);
            if (rightChip != null && !string.IsNullOrEmpty(rightChip.def.description))
                configDesc.Append("\n\n右手 [").Append(rightChip.LabelNoCount).Append("]:\n").Append(rightChip.def.description);

            yield return new StatDrawEntry(
                BDP_StatCategoryDefOf.BDP_TriggerConfig,
                "当前配置",
                nameValue,
                configDesc.ToString(),
                5000);

            // ── 芯片主类别 ──
            if (leftProps != null || rightProps != null)
            {
                string leftCategory = (leftProps != null && leftProps.primaryCategory != ChipPrimaryCategory.Unspecified)
                    ? leftProps.GetPrimaryCategoryLabel()
                    : null;
                string rightCategory = (rightProps != null && rightProps.primaryCategory != ChipPrimaryCategory.Unspecified)
                    ? rightProps.GetPrimaryCategoryLabel()
                    : null;

                if (leftCategory != null || rightCategory != null)
                {
                    yield return new StatDrawEntry(
                        BDP_StatCategoryDefOf.BDP_TriggerConfig,
                        "芯片类别",
                        FormatSidedValue(leftCategory, rightCategory, ""),
                        "芯片的主要功能类别。",
                        4999);
                }
            }

            // 无芯片时不输出属性对比
            if (leftProps == null && rightProps == null) yield break;

            // ── 收集芯片属性对比条目（两阶段对齐） ──
            var cfgStats = new System.Collections.Generic.List<(
                string label, string lv, string rv, string suffix, string desc, int pri)>();

            // 激活成本
            {
                string lv = (leftProps  != null && leftProps.activationCost  > 0f) ? leftProps.activationCost.ToString("F1")  : null;
                string rv = (rightProps != null && rightProps.activationCost > 0f) ? rightProps.activationCost.ToString("F1") : null;
                if (lv != null || rv != null)
                    cfgStats.Add(("激活成本", lv, rv, " Trion", "激活时一次性消耗的Trion量。", 4989));
            }

            // 占用成本
            {
                string lv = (leftProps  != null && leftProps.allocationCost  > 0f) ? leftProps.allocationCost.ToString("F1")  : null;
                string rv = (rightProps != null && rightProps.allocationCost > 0f) ? rightProps.allocationCost.ToString("F1") : null;
                if (lv != null || rv != null)
                    cfgStats.Add(("占用成本", lv, rv, " Trion", "战斗体激活时锁定的Trion容量。", 4988));
            }

            // 激活预热
            {
                string lv = (leftProps  != null && leftProps.activationWarmup  > 0) ? (leftProps.activationWarmup  / 60f).ToString("F2") : null;
                string rv = (rightProps != null && rightProps.activationWarmup > 0) ? (rightProps.activationWarmup / 60f).ToString("F2") : null;
                if (lv != null || rv != null)
                    cfgStats.Add(("激活预热", lv, rv, "秒", "切换到此芯片时的预热等待时间。", 4987));
            }

            // 关闭后摇
            {
                string lv = (leftProps  != null && leftProps.deactivationDelay  > 0) ? (leftProps.deactivationDelay  / 60f).ToString("F2") : null;
                string rv = (rightProps != null && rightProps.deactivationDelay > 0) ? (rightProps.deactivationDelay / 60f).ToString("F2") : null;
                if (lv != null || rv != null)
                    cfgStats.Add(("关闭后摇", lv, rv, "秒", "关闭此芯片时的后摇延迟时间。", 4986));
            }

            // 持续消耗
            {
                string lv = (leftProps  != null && leftProps.drainPerDay  > 0f) ? leftProps.drainPerDay.ToString("F1")  : null;
                string rv = (rightProps != null && rightProps.drainPerDay > 0f) ? rightProps.drainPerDay.ToString("F1") : null;
                if (lv != null || rv != null)
                    cfgStats.Add(("持续消耗", lv, rv, " Trion/秒", "激活期间每秒持续消耗的Trion量。", 4985));
            }

            // 功率要求
            {
                string lv = (leftProps  != null && leftProps.minOutputPower  > 0f) ? leftProps.minOutputPower.ToString("F1")  : null;
                string rv = (rightProps != null && rightProps.minOutputPower > 0f) ? rightProps.minOutputPower.ToString("F1") : null;
                if (lv != null || rv != null)
                    cfgStats.Add(("功率要求", lv, rv, "", "激活所需的最低Trion输出功率。", 4984));
            }

            // 计算左侧最长显示长度
            int maxLeftLen = 0;
            foreach (var s in cfgStats)
            {
                string leftDisplay = !string.IsNullOrEmpty(s.lv) ? s.lv + s.suffix : "[N/A]";
                if (leftDisplay.Length > maxLeftLen) maxLeftLen = leftDisplay.Length;
            }

            // 格式化并输出
            foreach (var s in cfgStats)
                yield return new StatDrawEntry(
                    BDP_StatCategoryDefOf.BDP_TriggerConfig,
                    s.label,
                    FormatSidedValue(s.lv, s.rv, s.suffix, maxLeftLen),
                    s.desc, s.pri);
        }

        /// <summary>追加一组槽位的装载信息到report文本。</summary>
        private void AppendSlotListInfo(StringBuilder sb, List<ChipSlot> slots, string sideLabel)
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                sb.Append(sideLabel);
                if (slots.Count > 1) sb.Append("#").Append(i + 1);
                sb.Append(": ");

                if (slot.isDisabled)
                    sb.AppendLine("已禁用");
                else if (slot.loadedChip == null)
                    sb.AppendLine("空");
                else
                    sb.Append(slot.loadedChip.LabelNoCount)
                      .Append(slot.isActive ? " (激活)" : " (待机)")
                      .AppendLine();
            }
        }

        /// <summary>
        /// 生成武器芯片的详细参数条目。
        /// 两阶段处理：先收集所有原始值，计算最长左侧长度，再统一格式化输出，
        /// 使所有条目的 &lt;-|-&gt; 分隔符尽量对齐。
        /// </summary>
        private IEnumerable<StatDrawEntry> ActiveChipWeaponStats()
        {
            var leftSlot  = GetActiveSlot(SlotSide.LeftHand);
            var rightSlot = GetActiveSlot(SlotSide.RightHand);
            var leftChip  = leftSlot?.loadedChip;
            var rightChip = rightSlot?.loadedChip;
            var leftCfg   = leftChip?.def.GetModExtension<VerbChipConfig>();
            var rightCfg  = rightChip?.def.GetModExtension<VerbChipConfig>();

            // ── 远程武器参数（两阶段：收集 → 对齐格式化） ──
            if (leftCfg?.primaryVerbProps != null || rightCfg?.primaryVerbProps != null)
            {
                var leftVP  = leftCfg?.primaryVerbProps;
                var rightVP = rightCfg?.primaryVerbProps;
                var leftFM  = leftChip?.TryGetComp<CompFireMode>();
                var rightFM = rightChip?.TryGetComp<CompFireMode>();

                // 用具名元组收集原始值，suffix只附加到有值的一侧
                var ranged = new System.Collections.Generic.List<(
                    StatCategoryDef cat, string label,
                    string lv, string rv, string suffix,
                    string desc, int pri)>();

                // 伤害（伤害类型内嵌到值字符串，避免后置拼接破坏对齐）
                if (TriggerBodyDisplayConfig.ShowDamage)
                {
                    string lv = null, rv = null;
                    if (leftVP?.defaultProjectile?.projectile != null)
                    {
                        var proj = leftVP.defaultProjectile.projectile;
                        int dmg = UnityEngine.Mathf.RoundToInt(proj.GetDamageAmount(null) * (leftFM?.Damage ?? 1f));
                        string t = proj.damageDef?.LabelCap ?? "";
                        lv = dmg + (t.Length > 0 ? " (" + t + ")" : "");
                    }
                    if (rightVP?.defaultProjectile?.projectile != null)
                    {
                        var proj = rightVP.defaultProjectile.projectile;
                        int dmg = UnityEngine.Mathf.RoundToInt(proj.GetDamageAmount(null) * (rightFM?.Damage ?? 1f));
                        string t = proj.damageDef?.LabelCap ?? "";
                        rv = dmg + (t.Length > 0 ? " (" + t + ")" : "");
                    }
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "伤害", lv, rv, "",
                            "每次命中造成的伤害（已应用射击模式倍率）。", 2490));
                }

                // 射程
                if (TriggerBodyDisplayConfig.ShowRange)
                {
                    string lv = leftVP  != null ? leftVP.range.ToString("F1")  : null;
                    string rv = rightVP != null ? rightVP.range.ToString("F1") : null;
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "射程", lv, rv, "格",
                            "最大有效射程。", 2489));
                }

                // 预热时间（不受FireMode影响）
                if (TriggerBodyDisplayConfig.ShowWarmup)
                {
                    string lv = leftVP  != null ? leftVP.warmupTime.ToString("F2")  : null;
                    string rv = rightVP != null ? rightVP.warmupTime.ToString("F2") : null;
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "预热时间", lv, rv, "秒",
                            "开火前的瞄准预热时间。", 2488));
                }

                // 子弹速度（受Speed轴影响）
                if (TriggerBodyDisplayConfig.ShowProjectileSpeed)
                {
                    string lv = null, rv = null;
                    if (leftVP?.defaultProjectile?.projectile != null)
                        lv = (leftVP.defaultProjectile.projectile.speed * (leftFM?.Speed ?? 1f)).ToString("F1");
                    if (rightVP?.defaultProjectile?.projectile != null)
                        rv = (rightVP.defaultProjectile.projectile.speed * (rightFM?.Speed ?? 1f)).ToString("F1");
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "子弹速度", lv, rv, "",
                            "子弹飞行速度（已应用射击模式倍率）。", 2487));
                }

                // 连射次数
                if (TriggerBodyDisplayConfig.ShowBurstCount)
                {
                    string lv = null, rv = null;
                    if (leftVP  != null) { int b = UnityEngine.Mathf.RoundToInt(leftVP.burstShotCount  * (leftFM?.Burst  ?? 1f)); if (b > 1) lv = b.ToString(); }
                    if (rightVP != null) { int b = UnityEngine.Mathf.RoundToInt(rightVP.burstShotCount * (rightFM?.Burst ?? 1f)); if (b > 1) rv = b.ToString(); }
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "连射次数", lv, rv, "发",
                            "每次射击的连射发数（已应用射击模式倍率）。", 2486));
                }

                // 连射速率
                if (TriggerBodyDisplayConfig.ShowBurstRate)
                {
                    string lv = null, rv = null;
                    if (leftVP  != null && leftVP.ticksBetweenBurstShots  > 0) lv = (3600f / leftVP.ticksBetweenBurstShots).ToString("F0");
                    if (rightVP != null && rightVP.ticksBetweenBurstShots > 0) rv = (3600f / rightVP.ticksBetweenBurstShots).ToString("F0");
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "连射速率", lv, rv, "rpm",
                            "连射期间每分钟射击发数。", 2485));
                }

                // Trion消耗（统一层）
                if (TriggerBodyDisplayConfig.ShowTrionCost)
                {
                    float leftCost = leftSlot?.loadedChip != null ? ChipUsageCostHelper.GetUsageCost(leftSlot.loadedChip) : 0f;
                    float rightCost = rightSlot?.loadedChip != null ? ChipUsageCostHelper.GetUsageCost(rightSlot.loadedChip) : 0f;
                    string lv = leftCost > 0f ? leftCost.ToString("F1") : null;
                    string rv = rightCost > 0f ? rightCost.ToString("F1") : null;
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "Trion消耗", lv, rv, "/发",
                            "每发射击消耗的Trion量。", 2484));
                }

                // 齐射散布
                if (TriggerBodyDisplayConfig.ShowVolleySpread)
                {
                    string lv = (leftCfg  != null && (leftCfg.ranged?.volleySpreadRadius ?? 0f)  > 0f) ? (leftCfg.ranged.volleySpreadRadius).ToString("F1")  : null;
                    string rv = (rightCfg != null && (rightCfg.ranged?.volleySpreadRadius ?? 0f) > 0f) ? (rightCfg.ranged.volleySpreadRadius).ToString("F1") : null;
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "齐射散布", lv, rv, "格",
                            "齐射时每发子弹起点的随机偏移半径。", 2483));
                }

                // 变化弹支持
                if (TriggerBodyDisplayConfig.ShowGuidedSupport)
                {
                    string lv = (leftCfg  != null && leftCfg.ranged?.guided != null)  ? "是(" + (leftCfg.ranged.guided.maxAnchors)  + "锚点)" : null;
                    string rv = (rightCfg != null && rightCfg.ranged?.guided != null) ? "是(" + (rightCfg.ranged.guided.maxAnchors) + "锚点)" : null;
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "变化弹支持", lv, rv, "",
                            "支持引导飞行模式，可设置路径锚点。", 2482));
                }

                // 穿透力
                if (TriggerBodyDisplayConfig.ShowPassthroughPower)
                {
                    string lv = (leftCfg  != null && (leftCfg.ranged?.passthroughPower ?? 0f)  > 0f) ? (leftCfg.ranged.passthroughPower).ToString("F1")  : null;
                    string rv = (rightCfg != null && (rightCfg.ranged?.passthroughPower ?? 0f) > 0f) ? (rightCfg.ranged.passthroughPower).ToString("F1") : null;
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "穿透力", lv, rv, "",
                            "穿体穿透力初始值，每次穿透后递减。", 2481));
                }

                // 冷却时间
                if (TriggerBodyDisplayConfig.ShowCooldown)
                {
                    string lv = leftVP  != null ? leftVP.defaultCooldownTime.ToString("F2")  : null;
                    string rv = rightVP != null ? rightVP.defaultCooldownTime.ToString("F2") : null;
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "冷却时间", lv, rv, "秒",
                            "每次射击后的冷却等待时间。", 2480));
                }

                // 护甲穿透
                if (TriggerBodyDisplayConfig.ShowArmorPenetration)
                {
                    string lv = null, rv = null;
                    if (leftVP?.defaultProjectile?.projectile != null)
                        lv = leftVP.defaultProjectile.projectile.GetArmorPenetration(null).ToStringPercent();
                    if (rightVP?.defaultProjectile?.projectile != null)
                        rv = rightVP.defaultProjectile.projectile.GetArmorPenetration(null).ToStringPercent();
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "护甲穿透", lv, rv, "",
                            "子弹对护甲的穿透率。", 2479));
                }

                // 抑止能力
                if (TriggerBodyDisplayConfig.ShowStoppingPower)
                {
                    string lv = null, rv = null;
                    if (leftVP?.defaultProjectile?.projectile != null)
                    {
                        float sp = leftVP.defaultProjectile.projectile.stoppingPower;
                        if (sp > 0f) lv = sp.ToString("F2");
                    }
                    if (rightVP?.defaultProjectile?.projectile != null)
                    {
                        float sp = rightVP.defaultProjectile.projectile.stoppingPower;
                        if (sp > 0f) rv = sp.ToString("F2");
                    }
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "抑止能力", lv, rv, "",
                            "命中时打断目标行动的能力值。", 2478));
                }

                // 精度（近/短/中/远）
                if (TriggerBodyDisplayConfig.ShowAccuracy)
                {
                    string lv, rv;
                    lv = leftVP  != null ? leftVP.accuracyTouch.ToStringPercent()  : null;
                    rv = rightVP != null ? rightVP.accuracyTouch.ToStringPercent() : null;
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "精度(近)", lv, rv, "",
                            "近距离命中率。", 2477));

                    lv = leftVP  != null ? leftVP.accuracyShort.ToStringPercent()  : null;
                    rv = rightVP != null ? rightVP.accuracyShort.ToStringPercent() : null;
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "精度(短)", lv, rv, "",
                            "短距离命中率。", 2476));

                    lv = leftVP  != null ? leftVP.accuracyMedium.ToStringPercent()  : null;
                    rv = rightVP != null ? rightVP.accuracyMedium.ToStringPercent() : null;
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "精度(中)", lv, rv, "",
                            "中距离命中率。", 2475));

                    lv = leftVP  != null ? leftVP.accuracyLong.ToStringPercent()  : null;
                    rv = rightVP != null ? rightVP.accuracyLong.ToStringPercent() : null;
                    if (lv != null || rv != null)
                        ranged.Add((StatCategoryDefOf.Weapon_Ranged, "精度(远)", lv, rv, "",
                            "远距离命中率。", 2474));
                }

                // 阶段2：计算左侧最长显示长度，用于对齐分隔符
                int maxLeftLen = 0;
                foreach (var s in ranged)
                {
                    string leftDisplay = !string.IsNullOrEmpty(s.lv) ? s.lv + s.suffix : "[N/A]";
                    if (leftDisplay.Length > maxLeftLen) maxLeftLen = leftDisplay.Length;
                }

                // 阶段3：格式化并输出
                foreach (var s in ranged)
                    yield return new StatDrawEntry(s.cat, s.label,
                        FormatSidedValue(s.lv, s.rv, s.suffix, maxLeftLen),
                        s.desc, s.pri);
            }

            // ── 近战Tool参数（分侧显示） ──
            if (TriggerBodyDisplayConfig.ShowMeleeTools)
            {
                if (leftCfg?.melee?.tools != null)
                {
                    foreach (var tool in leftCfg.melee.tools)
                    {
                        yield return new StatDrawEntry(
                            StatCategoryDefOf.Weapon_Melee,
                            "左手 " + tool.label,
                            tool.power.ToString("F0") + "dmg / " + tool.cooldownTime.ToString("F1") + "秒",
                            "近战攻击方式：" + tool.label + "。",
                            2480);
                    }
                }

                if (rightCfg?.melee?.tools != null)
                {
                    foreach (var tool in rightCfg.melee.tools)
                    {
                        yield return new StatDrawEntry(
                            StatCategoryDefOf.Weapon_Melee,
                            "右手 " + tool.label,
                            tool.power.ToString("F0") + "dmg / " + tool.cooldownTime.ToString("F1") + "秒",
                            "近战攻击方式：" + tool.label + "。",
                            2479);
                    }
                }
            }

            // ── 非武器芯片（Hediff/Ability，遍历所有槽位） ──
            foreach (var slot in AllActiveSlots())
            {
                var chip = slot.loadedChip;
                string prefix = GetSideLabel(slot.side);

                // Hediff芯片
                if (TriggerBodyDisplayConfig.ShowHediffChips)
                {
                    var hediffCfg = chip.def.GetModExtension<HediffChipConfig>();
                    if (hediffCfg?.hediffDef != null)
                    {
                        yield return new StatDrawEntry(
                            StatCategoryDefOf.Weapon_Ranged,
                            prefix + " 效果",
                            hediffCfg.hediffDef.LabelCap,
                            "激活时授予的Hediff效果：" + hediffCfg.hediffDef.description,
                            2470);
                    }
                }

                // Ability芯片
                if (TriggerBodyDisplayConfig.ShowAbilityChips)
                {
                    var abilityCfg = chip.def.GetModExtension<AbilityChipConfig>();
                    if (abilityCfg?.abilityDef != null)
                    {
                        yield return new StatDrawEntry(
                            StatCategoryDefOf.Weapon_Ranged,
                            prefix + " 技能",
                            abilityCfg.abilityDef.LabelCap,
                            "激活时授予的能力：" + abilityCfg.abilityDef.description,
                            2470);
                    }
                }
            }
        }

        // ═══════════════════════════════════════════
        //  Stat管线桥接（阶段3）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 聚合所有激活芯片的stat加算修正。
        /// RimWorld的StatWorker会调用此方法，将返回值加到最终stat值上。
        /// </summary>
        public override float GetStatOffset(StatDef stat)
        {
            float total = 0f;

            foreach (var slot in AllActiveSlots())
            {
                // 读取芯片配置的StatModifier
                var cfg = slot.loadedChip?.def.GetModExtension<ChipStatConfig>();
                if (cfg?.equippedStatOffsets != null)
                {
                    foreach (var modifier in cfg.equippedStatOffsets)
                    {
                        if (modifier.stat == stat)
                            total += modifier.value;
                    }
                }

                // v4.0：读取当前形态的StatModifier
                var chipComp = slot.loadedChip?.TryGetComp<TriggerChipComp>();
                var mode = chipComp?.GetCurrentMode(slot);
                if (mode?.statOffsets != null)
                {
                    foreach (var modifier in mode.statOffsets)
                    {
                        if (modifier.stat == stat)
                            total += modifier.value;
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// 聚合所有激活芯片的stat乘算修正。
        /// RimWorld的StatWorker会调用此方法，将返回值乘到最终stat值上。
        /// v4.0：支持形态StatModifier。
        /// </summary>
        public override float GetStatFactor(StatDef stat)
        {
            float total = 1f;

            foreach (var slot in AllActiveSlots())
            {
                // 读取芯片配置的StatModifier
                var cfg = slot.loadedChip?.def.GetModExtension<ChipStatConfig>();
                if (cfg?.equippedStatFactors != null)
                {
                    foreach (var modifier in cfg.equippedStatFactors)
                    {
                        if (modifier.stat == stat)
                            total *= modifier.value;
                    }
                }

                // v4.0：读取当前形态的StatModifier
                var chipComp = slot.loadedChip?.TryGetComp<TriggerChipComp>();
                var mode = chipComp?.GetCurrentMode(slot);
                if (mode?.statFactors != null)
                {
                    foreach (var modifier in mode.statFactors)
                    {
                        if (modifier.stat == stat)
                            total *= modifier.value;
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// 为stat解释文本添加各芯片的贡献明细。
        /// 在stat面板中点击stat时，会显示此方法返回的文本。
        /// </summary>
        public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace)
        {
            // 收集所有有贡献的芯片
            var contributors = new List<(string chipLabel, float offset, float factor)>();

            foreach (var slot in AllActiveSlots())
            {
                var chip = slot.loadedChip;
                var cfg = chip?.def.GetModExtension<ChipStatConfig>();
                if (cfg == null) continue;

                float offset = 0f;
                float factor = 1f;

                if (cfg.equippedStatOffsets != null)
                {
                    foreach (var modifier in cfg.equippedStatOffsets)
                    {
                        if (modifier.stat == stat)
                            offset += modifier.value;
                    }
                }

                if (cfg.equippedStatFactors != null)
                {
                    foreach (var modifier in cfg.equippedStatFactors)
                    {
                        if (modifier.stat == stat)
                            factor *= modifier.value;
                    }
                }

                // 只记录有实际贡献的芯片
                if (offset != 0f || factor != 1f)
                {
                    string sideLabel = GetSideLabel(slot.side);
                    contributors.Add((sideLabel + ": " + chip.LabelNoCount, offset, factor));
                }
            }

            // 输出贡献明细
            if (contributors.Count > 0)
            {
                sb.AppendLine();
                sb.Append(whitespace).AppendLine("激活芯片修正:");

                foreach (var (label, offset, factor) in contributors)
                {
                    sb.Append(whitespace).Append("  ").Append(label);

                    if (offset != 0f)
                        sb.Append(" ").Append(offset.ToStringWithSign("F2"));

                    if (factor != 1f)
                        sb.Append(" ×").Append(factor.ToString("F2"));

                    sb.AppendLine();
                }
            }
        }

        // ═══════════════════════════════════════════
        //  公开查询API（阶段4 - 第三方兼容）
        // ═══════════════════════════════════════════

        /// <summary>
        /// 获取当前激活芯片的合成VerbProperties列表（展示/查询用）。
        /// 返回所有激活武器芯片的primaryVerbProps。
        /// </summary>
        public List<VerbProperties> GetActiveWeaponVerbProperties()
        {
            var result = new List<VerbProperties>();

            foreach (var slot in AllActiveSlots())
            {
                var verbCfg = slot.loadedChip?.def.GetModExtension<VerbChipConfig>();
                if (verbCfg?.primaryVerbProps != null)
                    result.Add(verbCfg.primaryVerbProps);
            }

            return result;
        }

        /// <summary>
        /// 获取当前武器配置的估算DPS（每秒伤害）。
        /// 简化计算：(伤害 × 连射数) / (预热 + 连射间隔总时长)。
        /// 不考虑命中率、护甲等因素。
        /// </summary>
        public float GetActiveWeaponDPS()
        {
            float totalDPS = 0f;

            foreach (var slot in AllActiveSlots())
            {
                var chip = slot.loadedChip;
                var verbCfg = chip?.def.GetModExtension<VerbChipConfig>();
                if (verbCfg?.primaryVerbProps == null) continue;

                var vp = verbCfg.primaryVerbProps;
                if (vp.defaultProjectile?.projectile == null) continue;

                // 读取FireMode倍率
                var fireMode = chip.TryGetComp<CompFireMode>();
                float dmgMul = fireMode?.Damage ?? 1f;
                float spdMul = fireMode?.Speed ?? 1f;
                float burstMul = fireMode?.Burst ?? 1f;

                // 计算单次射击的总伤害
                int baseDmg = vp.defaultProjectile.projectile.GetDamageAmount(null);
                int finalDmg = UnityEngine.Mathf.RoundToInt(baseDmg * dmgMul);
                int burstCount = UnityEngine.Mathf.RoundToInt(vp.burstShotCount * burstMul);
                float totalDamage = finalDmg * burstCount;

                // 计算射击周期（预热 + 连射时长）
                float warmup = vp.warmupTime / spdMul;
                float burstDuration = (burstCount - 1) * vp.ticksBetweenBurstShots / 60f;
                float cycleDuration = warmup + burstDuration;

                if (cycleDuration > 0f)
                    totalDPS += totalDamage / cycleDuration;
            }

            return totalDPS;
        }

        /// <summary>
        /// 获取当前有效射程（取各侧最大值）。
        /// </summary>
        public float GetActiveWeaponRange()
        {
            float maxRange = 0f;

            foreach (var slot in AllActiveSlots())
            {
                var verbCfg = slot.loadedChip?.def.GetModExtension<VerbChipConfig>();
                if (verbCfg?.primaryVerbProps != null)
                {
                    if (verbCfg.primaryVerbProps.range > maxRange)
                        maxRange = verbCfg.primaryVerbProps.range;
                }
            }

            return maxRange;
        }

        // ═══════════════════════════════════════════
        //  私有辅助方法
        // ═══════════════════════════════════════════

        /// <summary>返回本地化的侧别标签名。</summary>
        private string GetSideLabel(SlotSide side)
        {
            switch (side)
            {
                case SlotSide.LeftHand:  return "左手";
                case SlotSide.RightHand: return "右手";
                case SlotSide.Special:   return "特殊";
                default:                 return side.ToString();
            }
        }

        /// <summary>
        /// 格式化伤害字符串：如 "12dmg"。
        /// 读取defaultProjectile.projectile.GetDamageAmount，应用FireMode倍率。
        /// </summary>
        private string FormatDamageString(VerbProperties vp, float dmgMultiplier)
        {
            if (vp?.defaultProjectile?.projectile == null) return null;
            int baseDmg = vp.defaultProjectile.projectile.GetDamageAmount(null);
            int finalDmg = UnityEngine.Mathf.RoundToInt(baseDmg * dmgMultiplier);
            return finalDmg + "dmg";
        }

        /// <summary>
        /// 格式化左右手合并显示的值。
        /// 无值的一侧显示 [N/A]，始终保持双侧对称。
        /// padToLength：将左侧内容填充到指定字符数，使分隔符尽量对齐。
        /// </summary>
        private string FormatSidedValue(string leftValue, string rightValue, string suffix = "", int padToLength = 0)
        {
            bool hasLeft  = !string.IsNullOrEmpty(leftValue);
            bool hasRight = !string.IsNullOrEmpty(rightValue);

            if (!hasLeft && !hasRight) return string.Empty;

            string left  = hasLeft  ? leftValue  + suffix : "[N/A]";
            string right = hasRight ? rightValue + suffix : "[N/A]";

            // 用空格填充左侧，使分隔符对齐
            if (padToLength > left.Length)
                left += new string(' ', padToLength - left.Length);

            return left + "   <-|->   " + right;
        }
    }
}
