using System;
using System.Collections.Generic;

namespace BDP.Core.Admission
{
    /// <summary>
    /// 中性字符串集合准入求值器。
    /// 固定按黑名单、白名单、必须项顺序裁定，所有键忽略大小写。
    /// </summary>
    public static class ValueSetAdmissionEvaluator
    {
        /// <summary>按指定规则检查候选字符串集合。</summary>
        public static ValueSetAdmissionResult Evaluate(
            IEnumerable<string> candidates,
            ValueSetAdmissionRule rule)
        {
            HashSet<string> candidateSet = BuildSet(candidates);
            ValueSetAdmissionRule safeRule = rule ?? new ValueSetAdmissionRule();

            string matched;
            if (TryFindContained(candidateSet, safeRule.DeniedAny, out matched))
            {
                return Failure(ValueSetAdmissionFailureKind.Denied, matched);
            }

            if (HasValues(safeRule.AllowedAny)
                && !TryFindContained(candidateSet, safeRule.AllowedAny, out matched))
            {
                return Failure(ValueSetAdmissionFailureKind.NotAllowed, null);
            }

            string missing;
            if (TryFindMissing(candidateSet, safeRule.RequiredAll, out missing))
            {
                return Failure(ValueSetAdmissionFailureKind.RequiredMissing, missing);
            }

            return new ValueSetAdmissionResult
            {
                IsAllowed = true,
                FailureKind = ValueSetAdmissionFailureKind.None,
                FailureValue = null
            };
        }

        /// <summary>把候选值收拢为忽略大小写的有效集合。</summary>
        private static HashSet<string> BuildSet(IEnumerable<string> values)
        {
            HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (values == null)
            {
                return result;
            }

            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    result.Add(value);
                }
            }

            return result;
        }

        /// <summary>判断配置列表是否含至少一项有效值。</summary>
        private static bool HasValues(IEnumerable<string> values)
        {
            if (values == null)
            {
                return false;
            }

            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>查找第一项同时存在于候选集合中的配置值。</summary>
        private static bool TryFindContained(
            HashSet<string> candidates,
            IEnumerable<string> configured,
            out string matched)
        {
            matched = null;
            if (configured == null)
            {
                return false;
            }

            foreach (string value in configured)
            {
                if (!string.IsNullOrWhiteSpace(value) && candidates.Contains(value))
                {
                    matched = value;
                    return true;
                }
            }

            return false;
        }

        /// <summary>查找第一项未包含在候选集合中的必须值。</summary>
        private static bool TryFindMissing(
            HashSet<string> candidates,
            IEnumerable<string> required,
            out string missing)
        {
            missing = null;
            if (required == null)
            {
                return false;
            }

            foreach (string value in required)
            {
                if (!string.IsNullOrWhiteSpace(value) && !candidates.Contains(value))
                {
                    missing = value;
                    return true;
                }
            }

            return false;
        }

        /// <summary>构建稳定失败结果。</summary>
        private static ValueSetAdmissionResult Failure(
            ValueSetAdmissionFailureKind kind,
            string value)
        {
            return new ValueSetAdmissionResult
            {
                IsAllowed = false,
                FailureKind = kind,
                FailureValue = value
            };
        }
    }
}
