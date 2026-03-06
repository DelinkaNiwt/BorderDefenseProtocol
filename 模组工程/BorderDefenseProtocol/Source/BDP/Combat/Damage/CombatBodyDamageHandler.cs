using RimWorld;
using Verse;

namespace BDP.Combat
{
    /// <summary>
    /// 战斗体伤害处理核心入口。
    /// 协调Handler的执行，实现战斗体伤害处理Pipeline。
    ///
    /// Pipeline流程：
    /// 1. TrionCostHandler: 计算并扣除Trion消耗
    /// 2. ShadowHPHandler: 应用伤害到影子HP
    /// 3. WoundHandler: 创建或合并战斗体伤口
    /// 4. CollapseHandler: 检测破裂条件（纯检查，不执行副作用）
    /// 5. 统一执行破裂（Pipeline全部完成后）
    ///
    /// v13.0重构：
    /// - 引入CombatBodyContext，入口查一次Gene/CompTrion，避免各Handler重复查找
    /// - CollapseHandler改为纯检查，不再在Pipeline中途触发解除
    /// - Pipeline末尾统一执行破裂，消除循环调用风险
    ///
    /// 拦截点迁移（2026-03-04）：
    /// - 从 PreApplyDamage 迁移到 FinalizeAndAddInjury
    /// - 直接使用原版计算结果（injury.Severity = 护甲后伤害，injury.Part = 命中部位）
    /// - 避免手动猜测部位和模拟护甲计算
    /// </summary>
    public static class CombatBodyDamageHandler
    {
        /// <summary>
        /// 处理战斗体伤害。
        /// </summary>
        /// <param name="pawn">受伤的Pawn</param>
        /// <param name="injury">原版已构造的伤口对象（包含护甲后伤害和命中部位）</param>
        /// <param name="dinfo">伤害信息（包含DamageDef）</param>
        public static void HandleDamage(Pawn pawn, Hediff_Injury injury, DamageInfo dinfo)
        {
            // 直接使用原版计算结果
            float damage = injury.Severity;          // 护甲后伤害（原版已算好）
            BodyPartRecord hitPart = injury.Part;    // 命中部位（原版已选好）

            // 入口构建上下文，后续各Handler共享
            var ctx = CombatBodyContext.Create(pawn);

            // ═══════════════════════════════════════════
            //  延时破裂阶段无敌检查
            // ═══════════════════════════════════════════
            // 如果战斗体处于Collapsing状态，直接拦截所有伤害
            if (ctx.Runtime != null && ctx.Runtime.State.IsCollapsing)
            {
                Log.Message($"[BDP] 延时破裂阶段无敌，忽略伤害: {pawn.LabelShort} (伤害: {damage:F1})");
                return; // 提前返回，不执行Pipeline
            }

            float trionBefore = ctx.CompTrion?.Cur ?? -1f;
            float shadowHPBefore = (hitPart != null && ctx.ShadowHP != null)
                ? ctx.ShadowHP.GetHP(hitPart) : -1f;

            Log.Message($"[BDP] ── 伤害拦截 ──────────────────────────────────────────");
            Log.Message($"[BDP]   目标: {pawn.LabelShort}  伤害类型: {injury.def.defName}  伤害量: {damage:F1} (护甲后)");
            Log.Message($"[BDP]   受伤部位: {hitPart?.def.defName ?? "无法确定"}");
            Log.Message($"[BDP]   处理前 → Trion: {trionBefore:F1}  部位影子HP: {(shadowHPBefore >= 0 ? shadowHPBefore.ToString("F1") : "N/A")}");

            // Handler 1: Trion消耗
            bool trionSufficient = TrionCostHandler.Handle(ctx, damage);
            float trionAfter = ctx.CompTrion?.Cur ?? -1f;

            // Handler 2: 影子HP
            bool partDestroyed = false;
            bool shadowHPSuccess = true;
            if (hitPart != null)
            {
                shadowHPSuccess = ShadowHPHandler.Handle(ctx, hitPart, damage, out partDestroyed);
            }
            float shadowHPAfter = (hitPart != null && ctx.ShadowHP != null)
                ? ctx.ShadowHP.GetHP(hitPart) : -1f;

            Log.Message($"[BDP]   处理后 → Trion: {trionAfter:F1} (消耗: {trionBefore - trionAfter:F1})  部位影子HP: {(shadowHPAfter >= 0 ? shadowHPAfter.ToString("F1") : "N/A")}");

            // Handler 3 & 4: 根据部位状态选择处理路径
            if (hitPart != null)
            {
                if (partDestroyed)
                {
                    // 路径A：部位破坏 → 跳过伤口，直接应用破坏
                    bool isCritical = IsCriticalPartDestroyed(ctx, hitPart);
                    if (!isCritical)
                    {
                        // 非关键部位破坏：标记部位缺失
                        Log.Message($"[BDP]   部位破坏 → 标记部位缺失（跳过伤口）");
                        ctx.PartDestruction?.Handle(pawn, hitPart);
                    }
                    else
                    {
                        // 关键部位破坏：不标记部位，直接触发破裂
                        Log.Message($"[BDP]   关键部位破坏 → 将触发战斗体破裂（跳过伤口和部位标记）");
                    }
                }
                else
                {
                    // 路径B：部位未破坏 → 添加伤口
                    Log.Message($"[BDP]   添加伤口 → 伤害类型: {dinfo.Def?.defName ?? "null"}, injury.def: {injury.def?.defName ?? "null"}");
                    WoundHandler.Handle(pawn, hitPart, dinfo.Def, damage, dinfo);
                }
            }

            // Handler 5: 破裂检测（纯检查，不执行副作用）
            bool criticalPartDestroyed = partDestroyed && IsCriticalPartDestroyed(ctx, hitPart);
            if (!trionSufficient) Log.Message($"[BDP]   ⚠ Trion不足，触发破裂");
            if (criticalPartDestroyed) Log.Message($"[BDP]   ⚠ 关键部位 {hitPart?.def.defName} 破坏，触发破裂");

            bool shouldCollapse = CollapseHandler.ShouldCollapse(!trionSufficient, criticalPartDestroyed);

            // Pipeline全部完成后，统一执行破裂（消除循环调用风险）
            if (shouldCollapse && ctx.Runtime != null && ctx.Runtime.IsActive)
            {
                // 构建破裂原因
                string collapseReason;
                if (!trionSufficient)
                    collapseReason = $"Trion耗尽 (当前: {trionAfter:F1})";
                else
                    collapseReason = $"关键部位破坏 ({hitPart?.def.defName})";

                Log.Warning($"[BDP] ═══════════════════════════════════════════════════");
                Log.Warning($"[BDP] ⚠ 战斗体触发破裂！");
                Log.Warning($"[BDP] ⚠ 目标: {pawn.LabelShort}");
                Log.Warning($"[BDP] ⚠ 原因: {collapseReason}");
                Log.Warning($"[BDP] ⚠ 进入延时破裂阶段 (90 ticks)");
                Log.Warning($"[BDP] ═══════════════════════════════════════════════════");

                // 转换到Collapsing状态
                ctx.Runtime.State.TransitionToCollapsing(collapseReason);

                // 打断当前动作（避免延时破裂期间继续攻击等动作）
                InterruptCurrentAction(pawn, "进入延时破裂");

                // 添加延时破裂Hediff（管理倒计时）
                pawn.health.AddHediff(Core.BDP_DefOf.BDP_CombatBodyCollapsing);

                // 关键部位破坏时添加视觉Hediff
                if (criticalPartDestroyed && hitPart != null)
                {
                    pawn.health.AddHediff(Core.BDP_DefOf.BDP_CombatBodyPartPending, hitPart);
                    Log.Message($"[BDP]   添加部位待失效标记: {hitPart.def.defName}");
                }
            }

            Log.Message($"[BDP] ────────────────────────────────────────────────────");
        }

        /// <summary>
        /// 关键部位定义：这些部位被破坏时触发战斗体破裂。
        /// </summary>
        private static readonly System.Collections.Generic.HashSet<string> CriticalParts =
            new System.Collections.Generic.HashSet<string>
        {
            "Head",   // 头部
            "Brain",  // 大脑
            "Heart",  // 心脏
            "Neck",   // 脖子
            "Torso"   // 躯干
        };

        /// <summary>
        /// 检查是否为关键部位破坏。
        /// 关键部位：头、大脑、心脏、脖子、躯干。
        /// 使用Context中的ShadowHP避免重复查找Gene。
        /// </summary>
        private static bool IsCriticalPartDestroyed(CombatBodyContext ctx, BodyPartRecord part)
        {
            if (part == null) return false;

            // 检查是否为关键部位
            if (!CriticalParts.Contains(part.def.defName))
                return false;

            // 检查影子HP是否耗尽
            if (ctx.ShadowHP == null)
                return false;

            return ctx.ShadowHP.IsDestroyed(part);
        }

        /// <summary>
        /// 打断Pawn的当前动作。
        /// 用于破裂时强制停止攻击、移动等动作。
        /// </summary>
        /// <param name="pawn">目标Pawn</param>
        /// <param name="reason">打断原因（用于日志）</param>
        private static void InterruptCurrentAction(Pawn pawn, string reason)
        {
            if (pawn == null) return;

            Log.Message($"[BDP]   打断当前动作: {pawn.LabelShort} (原因: {reason})");

            // 1. 结束当前Job
            if (pawn.jobs?.curJob != null)
            {
                pawn.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
            }

            // 2. 取消战斗姿态
            if (pawn.stances != null)
            {
                pawn.stances.CancelBusyStanceSoft();
            }

            // 3. 停止移动
            if (pawn.pather != null)
            {
                pawn.pather.StopDead();
            }
        }
    }
}
