using System;
using System.Collections.Generic;
using BDP.Core.AttackExecution;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Content.HitPartPolicy
{
    /// <summary>
    /// 命中部位策略模式。
    /// </summary>
    public enum HitPartSelectionMode
    {
        /// <summary>不主动解析命中部位。</summary>
        None,

        /// <summary>优先从要害候选中选择。</summary>
        PreferVital,

        /// <summary>优先从非要害候选中选择。</summary>
        AvoidVital,

        /// <summary>按原版未缺失部位的覆盖率和命中倍率随机选择。</summary>
        VanillaRandom,

        /// <summary>按身体部位定义和完整身体树出现序号精确选择。</summary>
        ExactDefOccurrence
    }

    /// <summary>
    /// 统一的命中部位候选分类与选择器。
    /// 要害分类只存在于 Content，Core 不依赖这套业务语义。
    /// </summary>
    public static class HitPartPolicyResolver
    {
        private const int ConsciousnessSource = 1;
        private const int BloodPumpingSource = 2;
        private const int BreathingSource = 4;
        private const int BloodFiltrationSource = 8;
        private const int MetabolismSource = 16;

        /// <summary>
        /// 按中性策略配置解析一个当前有效的身体部位。
        /// 返回 false 或 null 表示让调用方继续使用原版命中流程。
        /// </summary>
        public static bool TryResolve(
            Pawn attacker,
            Thing target,
            DamageDef damageDef,
            BodyPartGroupDef bodyPartGroup,
            HitPartPolicyConfig policy,
            out BodyPartRecord hitPart)
        {
            hitPart = null;
            if (!(target is Pawn pawn) || policy == null)
            {
                return false;
            }

            HitPartSelectionMode mode;
            if (!Enum.TryParse(policy.PolicyKey, true, out mode))
            {
                return false;
            }

            if (mode == HitPartSelectionMode.None)
            {
                return false;
            }

            float chance = NormalizeChance(policy.Chance);
            if (chance <= 0f || (chance < 1f && Rand.Value > chance))
            {
                return false;
            }

            List<HitPartCandidate> available = CollectAvailableParts(pawn);
            if (available.Count == 0)
            {
                return false;
            }

            if (mode == HitPartSelectionMode.VanillaRandom)
            {
                hitPart = SelectVanillaRandomCandidate(available, damageDef);
                return hitPart != null;
            }

            List<HitPartCandidate> vital = new List<HitPartCandidate>();
            List<HitPartCandidate> nonVital = new List<HitPartCandidate>();
            for (int index = 0; index < available.Count; index++)
            {
                HitPartCandidate candidate = available[index];
                if (candidate.IsVital)
                {
                    vital.Add(candidate);
                }
                else
                {
                    nonVital.Add(candidate);
                }
            }

            if (mode == HitPartSelectionMode.AvoidVital)
            {
                // 只排除要害候选；剩余部位按原版覆盖率和部位命中倍率加权。
                // 没有非要害候选时返回空，让原版继续命中流程，而不是强行命中核心。
                hitPart = SelectVanillaRandomCandidate(nonVital, damageDef);
                return hitPart != null;
            }

            if (vital.Count == 0)
            {
                // 自定义身体没有标准来源标签时，核心部位是唯一通用的要害回退。
                hitPart = ResolveCoreFallback(pawn);
                return hitPart != null;
            }

            hitPart = SelectCandidate(pawn, vital, preferVital: true);
            return hitPart != null;
        }

        /// <summary>
        /// 按身体部位定义和完整身体树中的出现序号解析一个部位。
        /// 先确定未过滤的目标记录，再检查缺失、可摧毁性和当前生命值，避免缺失部位改变左右序号。
        /// </summary>
        public static bool TryResolveExact(
            Pawn target,
            BodyPartDef partDef,
            int occurrenceIndex,
            out BodyPartRecord hitPart)
        {
            hitPart = null;
            if (target == null
                || target.RaceProps == null
                || target.RaceProps.body == null
                || target.health == null
                || target.health.hediffSet == null
                || partDef == null
                || occurrenceIndex < 0)
            {
                return false;
            }

            int occurrence = 0;
            foreach (BodyPartRecord part in target.RaceProps.body.AllParts)
            {
                if (part == null || part.def != partDef)
                {
                    continue;
                }

                if (occurrence != occurrenceIndex)
                {
                    occurrence++;
                    continue;
                }

                if (!IsAvailableDamagePart(target, part))
                {
                    return false;
                }

                hitPart = part;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 按原版身体部位选择规则的核心权重随机选择候选。
        /// 这里仍使用本解析器的可摧毁、未缺失和有效生命值过滤，避免选择不能被本效果移除的部位。
        /// </summary>
        private static BodyPartRecord SelectVanillaRandomCandidate(
            List<HitPartCandidate> candidates,
            DamageDef damageDef)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            bool hasCoveredCandidate = false;
            for (int index = 0; index < candidates.Count; index++)
            {
                HitPartCandidate candidate = candidates[index];
                float coverage = candidate.Part != null
                    ? Mathf.Max(0f, candidate.Part.coverageAbs)
                    : 0f;
                float hitChanceFactor = damageDef != null && candidate.Part != null && candidate.Part.def != null
                    ? Mathf.Max(0f, candidate.Part.def.GetHitChanceFactorFor(damageDef))
                    : 1f;
                candidate.Weight = coverage * hitChanceFactor;
                totalWeight += candidate.Weight;
                if (candidate.Weight > 0f)
                {
                    hasCoveredCandidate = true;
                }
            }

            if (!hasCoveredCandidate || totalWeight <= 0f)
            {
                // 与原版没有可用覆盖率时的回退一致：从有效候选中均匀选择。
                totalWeight = candidates.Count;
                for (int index = 0; index < candidates.Count; index++)
                {
                    candidates[index].Weight = 1f;
                }
            }

            float roll = Rand.Range(0f, totalWeight);
            for (int index = 0; index < candidates.Count; index++)
            {
                if (roll <= candidates[index].Weight)
                {
                    return candidates[index].Part;
                }

                roll -= candidates[index].Weight;
            }

            return candidates[candidates.Count - 1].Part;
        }

        /// <summary>
        /// 收集当前身体中仍存在且可被伤害摧毁的部位。
        /// </summary>
        private static List<HitPartCandidate> CollectAvailableParts(Pawn pawn)
        {
            List<HitPartCandidate> result = new List<HitPartCandidate>();
            if (pawn == null
                || pawn.RaceProps == null
                || pawn.RaceProps.body == null
                || pawn.health == null
                || pawn.health.hediffSet == null)
            {
                return result;
            }

            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (!IsAvailableDamagePart(pawn, part))
                {
                    continue;
                }

                int sourceKinds = ResolveSourceKinds(part, pawn);
                result.Add(new HitPartCandidate
                {
                    Part = part,
                    SourceKinds = sourceKinds,
                    IsVital = sourceKinds != 0 || part.IsCorePart
                });
            }

            return result;
        }

        /// <summary>
        /// 判断部位是否仍可被原版伤害链使用。
        /// </summary>
        private static bool IsAvailableDamagePart(Pawn pawn, BodyPartRecord part)
        {
            return pawn != null
                && pawn.health != null
                && pawn.health.hediffSet != null
                && part != null
                && part.def != null
                && part.def.destroyableByDamage
                && !pawn.health.hediffSet.PartIsMissing(part)
                && pawn.health.hediffSet.GetPartHealth(part) > 0f;
        }

        /// <summary>
        /// 没有标准来源标签时，回退到当前可用的核心部位。
        /// </summary>
        private static BodyPartRecord ResolveCoreFallback(Pawn pawn)
        {
            BodyPartRecord core = pawn != null
                && pawn.RaceProps != null
                && pawn.RaceProps.body != null
                ? pawn.RaceProps.body.corePart
                : null;
            return IsAvailableDamagePart(pawn, core) ? core : null;
        }

        /// <summary>
        /// 将原版身体标签映射为能力来源位标记。
        /// </summary>
        private static int ResolveSourceKinds(BodyPartRecord part, Pawn pawn)
        {
            if (part == null || part.def == null || part.def.tags == null)
            {
                return 0;
            }

            int result = 0;
            for (int index = 0; index < part.def.tags.Count; index++)
            {
                BodyPartTagDef tag = part.def.tags[index];
                string defName = tag != null ? tag.defName : null;
                switch (defName)
                {
                    case "ConsciousnessSource":
                        result |= ConsciousnessSource;
                        break;
                    case "BloodPumpingSource":
                        result |= BloodPumpingSource;
                        break;
                    case "BloodFiltrationSource":
                    case "BloodFiltrationKidney":
                    case "BloodFiltrationLiver":
                        result |= BloodFiltrationSource;
                        break;
                    case "BreathingSource":
                        if (pawn.RaceProps != null && pawn.RaceProps.IsFlesh)
                        {
                            result |= BreathingSource;
                        }
                        break;
                    case "MetabolismSource":
                        if (pawn.RaceProps != null && pawn.RaceProps.IsFlesh)
                        {
                            result |= MetabolismSource;
                        }
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// 在给定候选池中执行加权选择。
        /// </summary>
        private static BodyPartRecord SelectCandidate(
            Pawn pawn,
            List<HitPartCandidate> candidates,
            bool preferVital)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            int[] sourceCounts = CountSources(candidates);
            float totalWeight = 0f;
            for (int index = 0; index < candidates.Count; index++)
            {
                HitPartCandidate candidate = candidates[index];
                candidate.Weight = CalculateCandidateWeight(pawn, candidate, sourceCounts, preferVital);
                totalWeight += candidate.Weight;
            }

            if (totalWeight <= 0f)
            {
                return candidates[0].Part;
            }

            float roll = Rand.Range(0f, totalWeight);
            for (int index = 0; index < candidates.Count; index++)
            {
                if (roll <= candidates[index].Weight)
                {
                    return candidates[index].Part;
                }

                roll -= candidates[index].Weight;
            }

            return candidates[candidates.Count - 1].Part;
        }

        /// <summary>
        /// 统计候选池中的来源数量。
        /// </summary>
        private static int[] CountSources(List<HitPartCandidate> candidates)
        {
            int[] counts = new int[5];
            for (int index = 0; index < candidates.Count; index++)
            {
                int kinds = candidates[index].SourceKinds;
                if ((kinds & ConsciousnessSource) != 0) counts[0]++;
                if ((kinds & BloodPumpingSource) != 0) counts[1]++;
                if ((kinds & BreathingSource) != 0) counts[2]++;
                if ((kinds & BloodFiltrationSource) != 0) counts[3]++;
                if ((kinds & MetabolismSource) != 0) counts[4]++;
            }

            return counts;
        }

        /// <summary>
        /// 按来源覆盖数、部位当前血量和最大血量计算候选权重。
        /// </summary>
        private static float CalculateCandidateWeight(
            Pawn pawn,
            HitPartCandidate candidate,
            int[] sourceCounts,
            bool preferVital)
        {
            float weight = 1f;
            if (preferVital)
            {
                weight += SourceWeight(0, ConsciousnessSource, candidate.SourceKinds, sourceCounts);
                weight += SourceWeight(1, BloodPumpingSource, candidate.SourceKinds, sourceCounts);
                weight += SourceWeight(2, BreathingSource, candidate.SourceKinds, sourceCounts);
                weight += SourceWeight(3, BloodFiltrationSource, candidate.SourceKinds, sourceCounts);
                weight += SourceWeight(4, MetabolismSource, candidate.SourceKinds, sourceCounts);
            }

            float maxHealth = Mathf.Max(1f, candidate.Part.def.GetMaxHealth(pawn));
            float currentHealth = Mathf.Max(1f, pawn.health.hediffSet.GetPartHealth(candidate.Part));
            weight += Mathf.Clamp(10f / maxHealth, 0f, 10f);
            weight += Mathf.Clamp(20f / currentHealth, 0f, 20f);
            return weight;
        }

        /// <summary>
        /// 获取来源类别权重；唯一来源优先级更高。
        /// </summary>
        private static float SourceWeight(
            int sourceIndex,
            int source,
            int candidateSources,
            int[] sourceCounts)
        {
            if ((candidateSources & source) == 0)
            {
                return 0f;
            }

            return sourceCounts[sourceIndex] == 1 ? 120f : 20f;
        }

        /// <summary>
        /// 把概率限制在有效范围；非法值按关闭处理。
        /// </summary>
        private static float NormalizeChance(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return Mathf.Clamp01(value);
        }
    }
}
