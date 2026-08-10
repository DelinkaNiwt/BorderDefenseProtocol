using System;
using System.Collections.Generic;
using BDP.Core.Requirements;

namespace BDP.Content.Assembly.ChipManufacturing.Resolution
{
    /// <summary>芯片双动作条件合并规则的唯一登记表。</summary>
    public static class ChipRequirementMergeRegistry
    {
        /// <summary>当前正式支持的条件规则。</summary>
        private static readonly List<IChipRequirementMergeRule> Rules =
            new List<IChipRequirementMergeRule>
            {
                new TrionIntensityMergeRule(),
                new SkillLevelMergeRule()
            };

        /// <summary>确认两侧每一种条件都有明确规则。</summary>
        public static bool CanMerge(
            IList<PawnRequirement> first,
            IList<PawnRequirement> second)
        {
            return AllSupported(first) && AllSupported(second);
        }

        /// <summary>按原顺序合并条件；同类同槽取较高门槛，不同技能分别保留。</summary>
        public static List<PawnRequirement> Merge(
            IList<PawnRequirement> first,
            IList<PawnRequirement> second)
        {
            List<PawnRequirement> result = new List<PawnRequirement>();
            AppendMerged(result, first);
            AppendMerged(result, second);
            return result;
        }

        /// <summary>确认列表内每项都有登记规则。</summary>
        private static bool AllSupported(IList<PawnRequirement> requirements)
        {
            if (requirements == null)
            {
                return true;
            }

            for (int index = 0; index < requirements.Count; index++)
            {
                if (requirements[index] == null || FindRule(requirements[index]) == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>逐项追加，并在遇到同槽条件时原位合并。</summary>
        private static void AppendMerged(
            List<PawnRequirement> target,
            IList<PawnRequirement> source)
        {
            if (source == null)
            {
                return;
            }

            for (int sourceIndex = 0; sourceIndex < source.Count; sourceIndex++)
            {
                PawnRequirement candidate = source[sourceIndex];
                IChipRequirementMergeRule rule = FindRule(candidate);
                if (rule == null)
                {
                    throw new InvalidOperationException("条件未登记合并规则。");
                }

                int existingIndex = -1;
                for (int targetIndex = 0; targetIndex < target.Count; targetIndex++)
                {
                    if (rule.BelongsToSameSlot(target[targetIndex], candidate))
                    {
                        existingIndex = targetIndex;
                        break;
                    }
                }

                if (existingIndex >= 0)
                {
                    target[existingIndex] = rule.Merge(target[existingIndex], candidate);
                }
                else
                {
                    target.Add(Clone(candidate));
                }
            }
        }

        /// <summary>查找某项条件的精确类型规则。</summary>
        private static IChipRequirementMergeRule FindRule(PawnRequirement requirement)
        {
            if (requirement == null)
            {
                return null;
            }

            Type type = requirement.GetType();
            for (int index = 0; index < Rules.Count; index++)
            {
                if (Rules[index].RequirementType == type)
                {
                    return Rules[index];
                }
            }

            return null;
        }

        /// <summary>复制当前支持的条件，避免结果反向修改 Def 模板。</summary>
        private static PawnRequirement Clone(PawnRequirement source)
        {
            TrionIntensityRequirement trion = source as TrionIntensityRequirement;
            if (trion != null)
            {
                return new TrionIntensityRequirement { Minimum = trion.Minimum };
            }

            SkillLevelRequirement skill = source as SkillLevelRequirement;
            if (skill != null)
            {
                return new SkillLevelRequirement
                {
                    Skill = skill.Skill,
                    MinimumLevel = skill.MinimumLevel
                };
            }

            throw new InvalidOperationException("条件未登记复制规则。");
        }

        /// <summary>Trion 释放力条件全都占用同一个条件槽。</summary>
        private sealed class TrionIntensityMergeRule : IChipRequirementMergeRule
        {
            /// <summary>当前规则负责的条件类型。</summary>
            public Type RequirementType => typeof(TrionIntensityRequirement);

            /// <summary>两项都是 Trion 释放力条件时属于同槽。</summary>
            public bool BelongsToSameSlot(PawnRequirement first, PawnRequirement second)
            {
                return first is TrionIntensityRequirement
                    && second is TrionIntensityRequirement;
            }

            /// <summary>保留两项中更高的释放力门槛。</summary>
            public PawnRequirement Merge(PawnRequirement first, PawnRequirement second)
            {
                return new TrionIntensityRequirement
                {
                    Minimum = Math.Max(
                        ((TrionIntensityRequirement)first).Minimum,
                        ((TrionIntensityRequirement)second).Minimum)
                };
            }
        }

        /// <summary>技能条件按技能 Def 分槽。</summary>
        private sealed class SkillLevelMergeRule : IChipRequirementMergeRule
        {
            /// <summary>当前规则负责的条件类型。</summary>
            public Type RequirementType => typeof(SkillLevelRequirement);

            /// <summary>只有同一个技能的两项条件才属于同槽。</summary>
            public bool BelongsToSameSlot(PawnRequirement first, PawnRequirement second)
            {
                SkillLevelRequirement firstSkill = first as SkillLevelRequirement;
                SkillLevelRequirement secondSkill = second as SkillLevelRequirement;
                return firstSkill != null
                    && secondSkill != null
                    && firstSkill.Skill == secondSkill.Skill;
            }

            /// <summary>同技能保留较高等级门槛。</summary>
            public PawnRequirement Merge(PawnRequirement first, PawnRequirement second)
            {
                SkillLevelRequirement firstSkill = (SkillLevelRequirement)first;
                SkillLevelRequirement secondSkill = (SkillLevelRequirement)second;
                return new SkillLevelRequirement
                {
                    Skill = firstSkill.Skill,
                    MinimumLevel = Math.Max(firstSkill.MinimumLevel, secondSkill.MinimumLevel)
                };
            }
        }
    }
}
