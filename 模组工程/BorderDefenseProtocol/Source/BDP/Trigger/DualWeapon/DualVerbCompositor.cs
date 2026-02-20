using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 双武器系统组合器——纯静态工具类（v2.0 §8.4）。
    /// 根据左右两侧的Verb/Tool数据合成最终结果，供IVerbOwner接口返回。
    ///
    /// 组合规则：
    ///   仅一侧有值 → 直接返回该侧
    ///   近战+近战 → 生成 Verb_BDPDualMelee
    ///   远程+远程 → 生成 Verb_BDPDualRanged
    ///   近战+远程 → 远程VerbProperties + 近战Tools（利用原生距离选择）
    ///
    /// 设计约束：无状态纯函数，不持有任何运行时数据。
    /// </summary>
    public static class DualVerbCompositor
    {
        /// <summary>合成两侧VerbProperties为最终结果。双侧均null时返回null（回退到ThingDef.Verbs）。</summary>
        public static List<VerbProperties> ComposeVerbs(
            List<VerbProperties> leftVerbs,
            List<VerbProperties> rightVerbs)
        {
            if (leftVerbs == null && rightVerbs == null) return null;
            if (leftVerbs == null) return rightVerbs;
            if (rightVerbs == null) return leftVerbs;

            // 双侧都有武器 → 判断组合类型
            bool leftMelee = IsMeleeOnly(leftVerbs);
            bool rightMelee = IsMeleeOnly(rightVerbs);

            if (leftMelee && rightMelee)
                return ComposeDualMelee(leftVerbs, rightVerbs);
            if (!leftMelee && !rightMelee)
                return ComposeDualRanged(leftVerbs, rightVerbs);

            // 近战+远程混合（T28）→ 只返回远程侧VerbProperties
            // 近战能力由ComposeTools提供（通过Tools）
            return leftMelee ? rightVerbs : leftVerbs;
        }

        /// <summary>合成两侧Tools为最终结果。双侧均null时返回null（回退到ThingDef.tools）。</summary>
        public static List<Tool> ComposeTools(
            List<Tool> leftTools,
            List<Tool> rightTools)
        {
            if (leftTools == null && rightTools == null) return null;
            if (leftTools == null) return rightTools;
            if (rightTools == null) return leftTools;

            // 双侧都有 → 合并列表
            // 近战+近战：由Verb_BDPDualMelee内部处理，此处返回合并列表
            // 远程+远程：返回合并列表（远程武器的近战Tools通常是弱枪托）
            // 近战+远程：理想情况下只返回近战侧Tools（排除远程武器的枪托）
            //   ⚠️ 当前简化实现：合并全部，后续可细化
            return leftTools.Concat(rightTools).ToList();
        }

        /// <summary>判断VerbProperties列表是否全部为近战攻击。</summary>
        public static bool IsMeleeOnly(List<VerbProperties> verbs)
            => verbs != null && verbs.Count > 0 && verbs.All(v => v.IsMeleeAttack);

        // ── 近战+近战合成 ──

        private static List<VerbProperties> ComposeDualMelee(
            List<VerbProperties> leftVerbs,
            List<VerbProperties> rightVerbs)
        {
            // 取两侧最大冷却时间
            float leftCooldown = leftVerbs.Max(v => v.defaultCooldownTime);
            float rightCooldown = rightVerbs.Max(v => v.defaultCooldownTime);

            return new List<VerbProperties>
            {
                new VerbProperties
                {
                    verbClass = typeof(Verb_BDPDualMelee),
                    isPrimary = true,
                    // 冷却时间 = max(左侧冷却, 右侧冷却)
                    defaultCooldownTime = Mathf.Max(leftCooldown, rightCooldown),
                }
            };
        }

        // ── 远程+远程合成 ──

        private static List<VerbProperties> ComposeDualRanged(
            List<VerbProperties> leftVerbs,
            List<VerbProperties> rightVerbs)
        {
            // 取两侧参数的极值
            float leftRange = leftVerbs.Max(v => v.range);
            float rightRange = rightVerbs.Max(v => v.range);
            float leftWarmup = leftVerbs.Max(v => v.warmupTime);
            float rightWarmup = rightVerbs.Max(v => v.warmupTime);
            float leftCooldown = leftVerbs.Max(v => v.defaultCooldownTime);
            float rightCooldown = rightVerbs.Max(v => v.defaultCooldownTime);
            int leftBurst = leftVerbs.Max(v => v.burstShotCount);
            int rightBurst = rightVerbs.Max(v => v.burstShotCount);

            return new List<VerbProperties>
            {
                new VerbProperties
                {
                    verbClass = typeof(Verb_BDPDualRanged),
                    isPrimary = true,
                    range = Mathf.Min(leftRange, rightRange),
                    warmupTime = Mathf.Max(leftWarmup, rightWarmup),
                    defaultCooldownTime = Mathf.Max(leftCooldown, rightCooldown),
                    // burstShotCount = 左连射数 + 右连射数
                    burstShotCount = leftBurst + rightBurst,
                }
            };
        }
    }
}
