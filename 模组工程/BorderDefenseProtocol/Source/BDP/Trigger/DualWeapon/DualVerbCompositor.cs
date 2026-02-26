using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 双武器系统组合器——纯静态工具类（v2.0 §8.4，v4.0重构）。
    /// 根据左右两侧的Verb/Tool数据合成最终结果，供IVerbOwner接口返回。
    ///
    /// v4.0变更（F1 Gizmo架构重设计）：
    ///   · ComposeVerbs返回包含所有独立Verb + 双武器Verb的完整列表
    ///   · 双武器Verb设置isPrimary=true作为默认攻击
    ///   · 相同芯片只保留1个独立Verb
    ///   · 新增ChipSlot参数用于判断芯片是否相同
    ///
    /// v5.0变更（6.2.1 Gizmo架构重设计）：
    ///   · 所有芯片Verb统一设hasStandardCommand=false，脱离标准Gizmo生成路径
    ///   · Gizmo改由CompGetEquippedGizmosExtra通过Command_BDPChipAttack自定义生成
    ///   · Verb_BDPDualMelee重命名为Verb_BDPMelee（单侧+双侧共用）
    ///
    /// 组合规则：
    ///   仅一侧有值 → 该侧Verb(isPrimary=true, label=侧别标签)
    ///   近战+近战 → [左Verb, 右Verb(如不同芯片), DualMeleeVerb(isPrimary=true)]
    ///   远程+远程 → [左Verb, 右Verb(如不同芯片), DualRangedVerb(isPrimary=true)]
    ///   近战+远程 → [近战Verb(label=近战侧), 远程Verb(label=远程侧, isPrimary=true)]
    ///
    /// 设计约束：无状态纯函数，不持有任何运行时数据。
    /// </summary>
    public static class DualVerbCompositor
    {
        // ── 侧别标签常量（编码在VerbProperties.label中，供独立Verb识别自己属于哪一侧） ──
        public const string SideLabel_LeftHand = "BDP_LeftHand";
        public const string SideLabel_RightHand = "BDP_RightHand";

        /// <summary>从VerbProperties.label解析侧别。非BDP标签返回null。</summary>
        public static SlotSide? ParseSideLabel(string label)
        {
            if (label == SideLabel_LeftHand) return SlotSide.LeftHand;
            if (label == SideLabel_RightHand) return SlotSide.RightHand;
            return null;
        }

        /// <summary>
        /// 合成两侧VerbProperties为最终结果（v4.0重构）。
        /// 返回包含所有独立Verb + 双武器Verb的完整列表。
        /// 双侧均null时返回null（回退到ThingDef.Verbs）。
        /// </summary>
        /// <param name="leftSlot">左手槽（用于判断芯片是否相同和读取芯片label）。</param>
        /// <param name="rightSlot">右手槽（用于判断芯片是否相同和读取芯片label）。</param>
        public static List<VerbProperties> ComposeVerbs(
            List<VerbProperties> leftVerbs,
            List<VerbProperties> rightVerbs,
            ChipSlot leftSlot = null,
            ChipSlot rightSlot = null)
        {
            if (leftVerbs == null && rightVerbs == null) return null;

            // 仅一侧有值 → 该侧Verb(isPrimary=true, 带侧别label)
            // Bug1修复：EnsurePrimary不设label → ParseSideLabel返回null → Verb被错误分配到dualAttackVerb
            //          → 单芯片场景下无攻击Gizmo。改为TagSideAndEnsurePrimary，显式标记侧别。
            if (leftVerbs == null)
                return TagSideAndEnsurePrimary(rightVerbs, SideLabel_RightHand);
            if (rightVerbs == null)
                return TagSideAndEnsurePrimary(leftVerbs, SideLabel_LeftHand);

            // 双侧都有武器 → 判断组合类型
            bool leftMelee = IsMeleeOnly(leftVerbs);
            bool rightMelee = IsMeleeOnly(rightVerbs);
            bool sameChip = AreSameChip(leftSlot, rightSlot);

            if (leftMelee && rightMelee)
                return ComposeDualMelee(leftVerbs, rightVerbs, sameChip);
            if (!leftMelee && !rightMelee)
                return ComposeDualRanged(leftVerbs, rightVerbs, sameChip);

            // 近战+远程混合 → 两个独立Verb，远程isPrimary=true
            return ComposeMixed(leftVerbs, rightVerbs, leftMelee);
        }

        /// <summary>合成两侧Tools为最终结果。双侧均null时返回null（回退到ThingDef.tools）。</summary>
        public static List<Tool> ComposeTools(
            List<Tool> leftTools,
            List<Tool> rightTools)
        {
            if (leftTools == null && rightTools == null) return null;
            if (leftTools == null) return rightTools;
            if (rightTools == null) return leftTools;
            return leftTools.Concat(rightTools).ToList();
        }

        /// <summary>判断VerbProperties列表是否全部为近战攻击。</summary>
        public static bool IsMeleeOnly(List<VerbProperties> verbs)
            => verbs != null && verbs.Count > 0 && verbs.All(v => v.IsMeleeAttack);

        // ═══════════════════════════════════════════
        //  内部方法
        // ═══════════════════════════════════════════

        /// <summary>
        /// Bug1修复：替代原EnsurePrimary，为单侧Verb列表标记侧别label并设isPrimary。
        /// 原因：无label的Verb在CreateAndCacheChipVerbs中被ParseSideLabel(null)→null
        ///       → 错误分配到dualAttackVerb → 单芯片场景无攻击Gizmo。
        /// </summary>
        private static List<VerbProperties> TagSideAndEnsurePrimary(
            List<VerbProperties> verbs, string sideLabel)
        {
            if (verbs == null || verbs.Count == 0) return verbs;
            var result = new List<VerbProperties>(verbs.Count);
            for (int i = 0; i < verbs.Count; i++)
            {
                var copy = CopyVerbProps(verbs[i]);
                copy.label = sideLabel;
                copy.hasStandardCommand = false;
                if (i == 0) copy.isPrimary = true;
                result.Add(copy);
            }
            return result;
        }

        /// <summary>判断两个槽位是否装载了相同defName的芯片。</summary>
        private static bool AreSameChip(ChipSlot left, ChipSlot right)
        {
            if (left?.loadedChip == null || right?.loadedChip == null) return false;
            return left.loadedChip.def == right.loadedChip.def;
        }

        // ── 近战+近战合成：独立近战Verb + DualMelee Verb ──
        // 独立近战Verb也使用Verb_BDPMelee（单侧模式），通过label标识侧别，
        // 避免标准Verb_MeleeAttackDamage使用触发体ThingDef作为weapon的问题。
        // v6.0变更：设置burstShotCount和ticksBetweenBurstShots，支持引擎burst机制。

        private static List<VerbProperties> ComposeDualMelee(
            List<VerbProperties> leftVerbs,
            List<VerbProperties> rightVerbs,
            bool sameChip)
        {
            float leftCooldown = leftVerbs.Max(v => v.defaultCooldownTime);
            float rightCooldown = rightVerbs.Max(v => v.defaultCooldownTime);

            DamageDef damageDef = leftVerbs[0].meleeDamageDef ?? rightVerbs[0].meleeDamageDef;
            int leftDmg = leftVerbs.Max(v => v.meleeDamageBaseAmount);
            int rightDmg = rightVerbs.Max(v => v.meleeDamageBaseAmount);
            int combinedDmg = System.Math.Max(leftDmg, rightDmg);

            // v6.0：从VerbProperties读取burst参数（由SynthesizeMeleeVerbProps设置）
            int leftBurst = leftVerbs.Max(v => v.burstShotCount);
            int rightBurst = rightVerbs.Max(v => v.burstShotCount);
            int leftInterval = leftVerbs.Max(v => v.ticksBetweenBurstShots);
            int rightInterval = rightVerbs.Max(v => v.ticksBetweenBurstShots);

            var result = new List<VerbProperties>();

            // 左侧独立近战Verb（单侧模式，通过label标识）
            result.Add(new VerbProperties
            {
                verbClass = typeof(Verb_BDPMelee),
                isPrimary = false,
                hasStandardCommand = false,
                defaultCooldownTime = leftCooldown,
                meleeDamageDef = leftVerbs[0].meleeDamageDef ?? damageDef,
                meleeDamageBaseAmount = leftDmg,
                label = SideLabel_LeftHand,
                // v6.0：单侧burst参数
                burstShotCount = leftBurst,
                ticksBetweenBurstShots = leftInterval,
            });

            // 相同芯片时不重复添加右侧独立Verb
            if (!sameChip)
            {
                result.Add(new VerbProperties
                {
                    verbClass = typeof(Verb_BDPMelee),
                    isPrimary = false,
                    hasStandardCommand = false,
                    defaultCooldownTime = rightCooldown,
                    meleeDamageDef = rightVerbs[0].meleeDamageDef ?? damageDef,
                    meleeDamageBaseAmount = rightDmg,
                    label = SideLabel_RightHand,
                    // v6.0：单侧burst参数
                    burstShotCount = rightBurst,
                    ticksBetweenBurstShots = rightInterval,
                });
            }

            // 双武器合成Verb（isPrimary=true，默认攻击，无侧别label=双侧模式）
            result.Add(new VerbProperties
            {
                verbClass = typeof(Verb_BDPMelee),
                isPrimary = true,
                hasStandardCommand = false,
                defaultCooldownTime = Mathf.Max(leftCooldown, rightCooldown),
                meleeDamageDef = damageDef,
                meleeDamageBaseAmount = combinedDmg,
                // v6.0：合成burst参数（总击数=左+右，间隔取max作为默认值，实际会被per-hit覆盖）
                burstShotCount = leftBurst + rightBurst,
                ticksBetweenBurstShots = System.Math.Max(leftInterval, rightInterval),
            });

            return result;
        }

        // ── 远程+远程合成 ──

        private static List<VerbProperties> ComposeDualRanged(
            List<VerbProperties> leftVerbs,
            List<VerbProperties> rightVerbs,
            bool sameChip)
        {
            var result = new List<VerbProperties>();

            // 添加左侧独立Verb（hasStandardCommand=false脱离标准路径，label标识侧别）
            foreach (var v in leftVerbs)
            {
                var copy = CopyVerbProps(v);
                copy.isPrimary = false;
                copy.hasStandardCommand = false;
                copy.label = SideLabel_LeftHand;
                result.Add(copy);
            }

            // 相同芯片只保留1个独立Verb
            if (!sameChip)
            {
                foreach (var v in rightVerbs)
                {
                    var copy = CopyVerbProps(v);
                    copy.isPrimary = false;
                    copy.hasStandardCommand = false;
                    copy.label = SideLabel_RightHand;
                    result.Add(copy);
                }
            }

            // 取两侧参数的极值
            float leftRange = leftVerbs.Max(v => v.range);
            float rightRange = rightVerbs.Max(v => v.range);
            float leftWarmup = leftVerbs.Max(v => v.warmupTime);
            float rightWarmup = rightVerbs.Max(v => v.warmupTime);
            float leftCooldown = leftVerbs.Max(v => v.defaultCooldownTime);
            float rightCooldown = rightVerbs.Max(v => v.defaultCooldownTime);
            int leftBurst = leftVerbs.Max(v => v.burstShotCount);
            int rightBurst = rightVerbs.Max(v => v.burstShotCount);
            var primaryVerb = leftVerbs[0];

            // 双武器合成Verb（isPrimary=true，默认攻击）
            result.Add(new VerbProperties
            {
                verbClass = typeof(Verb_BDPDualRanged),
                isPrimary = true,
                hasStandardCommand = false,
                defaultProjectile = primaryVerb.defaultProjectile,
                soundCast = primaryVerb.soundCast,
                muzzleFlashScale = Mathf.Max(
                    leftVerbs.Max(v => v.muzzleFlashScale),
                    rightVerbs.Max(v => v.muzzleFlashScale)),
                ticksBetweenBurstShots = System.Math.Max(
                    leftVerbs.Max(v => v.ticksBetweenBurstShots),
                    rightVerbs.Max(v => v.ticksBetweenBurstShots)),
                range = Mathf.Min(leftRange, rightRange),
                warmupTime = Mathf.Max(leftWarmup, rightWarmup),
                defaultCooldownTime = Mathf.Max(leftCooldown, rightCooldown),
                burstShotCount = leftBurst + rightBurst,
            });

            return result;
        }

        // ── 近战+远程混合 ──
        // Bug2修复：原版未设label → 两个Verb都被ParseSideLabel(null)→null
        //          → 互相覆盖dualAttackVerb，近战Verb丢失。
        //          现在根据leftIsMelee确定各侧label。

        private static List<VerbProperties> ComposeMixed(
            List<VerbProperties> leftVerbs,
            List<VerbProperties> rightVerbs,
            bool leftIsMelee)
        {
            var meleeVerbs = leftIsMelee ? leftVerbs : rightVerbs;
            var rangedVerbs = leftIsMelee ? rightVerbs : leftVerbs;
            // Bug2修复：根据实际侧别分配label
            string meleeLabel = leftIsMelee ? SideLabel_LeftHand : SideLabel_RightHand;
            string rangedLabel = leftIsMelee ? SideLabel_RightHand : SideLabel_LeftHand;

            var result = new List<VerbProperties>();

            // 近战侧独立Verb（isPrimary=false，带侧别label）
            foreach (var v in meleeVerbs)
            {
                var copy = CopyVerbProps(v);
                copy.isPrimary = false;
                copy.hasStandardCommand = false;
                copy.label = meleeLabel;
                result.Add(copy);
            }

            // 远程侧独立Verb（isPrimary=true，默认攻击，带侧别label）
            foreach (var v in rangedVerbs)
            {
                var copy = CopyVerbProps(v);
                copy.isPrimary = true;
                copy.hasStandardCommand = false;
                copy.label = rangedLabel;
                result.Add(copy);
            }

            return result;
        }

        // ── MemberwiseClone缓存（Fix-7：替代手动字段拷贝，不会遗漏新增字段） ──
        private static readonly System.Func<VerbProperties, VerbProperties> CloneVerbProps;

        static DualVerbCompositor()
        {
            var mi = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            CloneVerbProps = vp => (VerbProperties)mi.Invoke(vp, null);
        }

        /// <summary>
        /// 浅拷贝VerbProperties（MemberwiseClone，不会遗漏新增字段）。
        /// VerbProperties的引用类型字段（defaultProjectile, soundCast等）本身是Def引用，共享安全。
        /// </summary>
        private static VerbProperties CopyVerbProps(VerbProperties src) => CloneVerbProps(src);
    }
}