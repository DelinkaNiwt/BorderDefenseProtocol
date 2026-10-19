using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Combos;
using Verse;

namespace BDP.Core.Expressions.Runtime
{
    /// <summary>
    /// 组合技运行时索引。
    /// 它把无序双芯片键索引到候选 ComboDef 集合，允许同来源键组合按成品准入和运行时范围区分结果。
    /// </summary>
    internal sealed class ComboRuntimeIndex
    {
        /// <summary>
        /// 当前已建立的无序双芯片索引。
        /// </summary>
        private readonly Dictionary<string, List<ComboDef>> combosByUnorderedPair =
            new Dictionary<string, List<ComboDef>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 上次建索引时观测到的 ComboDef 数量。
        /// </summary>
        private int indexedComboCount = -1;

        /// <summary>
        /// 按两枚芯片 Thing 匹配第一份来源键、成品准入与运行时范围都成立的组合技。
        /// 来源键只取制造来源首个芯片功能定义 DefName，不回退到物理 ThingDef。
        /// </summary>
        internal ComboDefinitionReadResult FindMatch(
            Thing firstSourceChip,
            Thing secondSourceChip,
            Func<ComboDef, ComboDefinitionReadResult> read)
        {
            string ignoredFailureReason;
            return FindMatch(
                firstSourceChip,
                secondSourceChip,
                null,
                null,
                null,
                null,
                read,
                out ignoredFailureReason);
        }

        /// <summary>按两枚芯片匹配组合技，并返回集中诊断失败摘要。</summary>
        internal ComboDefinitionReadResult FindMatch(
            Thing firstSourceChip,
            Thing secondSourceChip,
            Func<ComboDef, ComboDefinitionReadResult> read,
            out string failureReason)
        {
            return FindMatch(
                firstSourceChip,
                secondSourceChip,
                null,
                null,
                null,
                null,
                read,
                out failureReason);
        }

        /// <summary>按两枚芯片及其当前形态/姿态匹配组合技。</summary>
        internal ComboDefinitionReadResult FindMatch(
            Thing firstSourceChip,
            Thing secondSourceChip,
            string firstModeKey,
            string firstStanceKey,
            string secondModeKey,
            string secondStanceKey,
            Func<ComboDef, ComboDefinitionReadResult> read)
        {
            string ignoredFailureReason;
            return FindMatch(
                firstSourceChip,
                secondSourceChip,
                firstModeKey,
                firstStanceKey,
                secondModeKey,
                secondStanceKey,
                read,
                out ignoredFailureReason);
        }

        /// <summary>按两枚芯片及其当前形态/姿态匹配组合技，并返回集中诊断失败摘要。</summary>
        internal ComboDefinitionReadResult FindMatch(
            Thing firstSourceChip,
            Thing secondSourceChip,
            string firstModeKey,
            string firstStanceKey,
            string secondModeKey,
            string secondStanceKey,
            Func<ComboDef, ComboDefinitionReadResult> read,
            out string failureReason)
        {
            failureReason = null;
            if (firstSourceChip == null || secondSourceChip == null)
            {
                failureReason = "Main/Sub 至少一侧没有芯片。";
                return null;
            }

            EnsureIndex();

            string firstSourceIdentity = GetComboIdentity(firstSourceChip);
            string secondSourceIdentity = GetComboIdentity(secondSourceChip);

            if (string.IsNullOrWhiteSpace(firstSourceIdentity) || string.IsNullOrWhiteSpace(secondSourceIdentity))
            {
                failureReason = "至少一侧芯片没有可用的首个来源键。";
                return null;
            }

            List<ComboDef> comboDefs;
            if (!combosByUnorderedPair.TryGetValue(
                    BuildUnorderedPairKey(firstSourceIdentity, secondSourceIdentity),
                    out comboDefs))
            {
                failureReason = "当前来源键组合没有候选 ComboDef。";
                return null;
            }

            ComboSourceAdmissionSnapshot firstSourceSnapshot =
                ComboSourceAdmissionEvaluator.BuildSnapshot(
                    firstSourceChip,
                    firstModeKey,
                    firstStanceKey);
            ComboSourceAdmissionSnapshot secondSourceSnapshot =
                ComboSourceAdmissionEvaluator.BuildSnapshot(
                    secondSourceChip,
                    secondModeKey,
                    secondStanceKey);
            bool foundValidDefinition = false;
            bool sourceVariantMismatch = false;
            ComboDefinitionReadResult matchedReadResult = null;
            for (int index = 0; index < comboDefs.Count; index++)
            {
                ComboDefinitionReadResult readResult = read != null ? read(comboDefs[index]) : null;
                if (readResult?.Validation == null
                    || !readResult.Validation.IsValid
                    || readResult.Contract == null)
                {
                    continue;
                }

                foundValidDefinition = true;
                bool forwardMatchPassed = MatchesAssignmentWithScope(
                        firstSourceIdentity,
                        secondSourceIdentity,
                        firstSourceSnapshot,
                        secondSourceSnapshot,
                        readResult.Contract.FirstSourceKey,
                        readResult.Contract.SecondSourceKey,
                        readResult.Contract.FirstSourceAdmission,
                        readResult.Contract.SecondSourceAdmission,
                        readResult.Contract.FirstSourceScope,
                        readResult.Contract.SecondSourceScope,
                        readResult.Contract.RequireSameSourceVariant);
                bool reverseMatchPassed = MatchesAssignmentWithScope(
                        firstSourceIdentity,
                        secondSourceIdentity,
                        firstSourceSnapshot,
                        secondSourceSnapshot,
                        readResult.Contract.SecondSourceKey,
                        readResult.Contract.FirstSourceKey,
                        readResult.Contract.SecondSourceAdmission,
                        readResult.Contract.FirstSourceAdmission,
                        readResult.Contract.SecondSourceScope,
                        readResult.Contract.FirstSourceScope,
                        readResult.Contract.RequireSameSourceVariant);
                if (readResult.Contract.RequireSameSourceVariant
                    && !ComboSourceAdmissionEvaluator.AreSourceVariantsCompatible(
                        firstSourceSnapshot,
                        secondSourceSnapshot)
                    && (MatchesIdentityAndAdmission(
                            firstSourceIdentity,
                            secondSourceIdentity,
                            firstSourceSnapshot,
                            secondSourceSnapshot,
                            readResult.Contract.FirstSourceKey,
                            readResult.Contract.SecondSourceKey,
                            readResult.Contract.FirstSourceAdmission,
                            readResult.Contract.SecondSourceAdmission)
                        || MatchesIdentityAndAdmission(
                            firstSourceIdentity,
                            secondSourceIdentity,
                            firstSourceSnapshot,
                            secondSourceSnapshot,
                            readResult.Contract.SecondSourceKey,
                            readResult.Contract.FirstSourceKey,
                            readResult.Contract.SecondSourceAdmission,
                            readResult.Contract.FirstSourceAdmission)))
                {
                    sourceVariantMismatch = true;
                }
                if (forwardMatchPassed || reverseMatchPassed)
                {
                    if (matchedReadResult != null)
                    {
                        failureReason = "ComboMatchAmbiguous：同一来源键组合有多份 ComboDef 同时通过成品来源准入。";
                        return null;
                    }

                    readResult.SourceBinding = new ComboSourceBinding
                    {
                        FirstSourceChip = forwardMatchPassed
                            ? firstSourceChip
                            : secondSourceChip,
                        SecondSourceChip = forwardMatchPassed
                            ? secondSourceChip
                            : firstSourceChip
                    };
                    matchedReadResult = readResult;
                }
            }

            if (matchedReadResult != null)
            {
                return matchedReadResult;
            }

            failureReason = sourceVariantMismatch
                ? "SourceVariantMismatch：第一、第二来源项的来源变体不一致。"
                : foundValidDefinition
                ? "候选 ComboDef 的正向与反向来源项准入均未通过。"
                : "来源键组合存在候选 ComboDef，但候选定义均未通过合法性校验。";
            return null;
        }

        /// <summary>
        /// 确保当前索引与已加载 ComboDef 集合保持同步。
        /// </summary>
        private void EnsureIndex()
        {
            int comboCount = DefDatabase<ComboDef>.AllDefsListForReading.Count;
            if (indexedComboCount == comboCount && combosByUnorderedPair.Count > 0)
            {
                return;
            }

            combosByUnorderedPair.Clear();
            List<ComboDef> allDefs = DefDatabase<ComboDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ComboDef comboDef = allDefs[i];
                if (comboDef == null
                    || string.IsNullOrWhiteSpace(comboDef.firstSourceKey)
                    || string.IsNullOrWhiteSpace(comboDef.secondSourceKey))
                {
                    continue;
                }

                string key = BuildUnorderedPairKey(comboDef.firstSourceKey, comboDef.secondSourceKey);
                List<ComboDef> indexed;
                if (!combosByUnorderedPair.TryGetValue(key, out indexed))
                {
                    indexed = new List<ComboDef>();
                    combosByUnorderedPair.Add(key, indexed);
                }

                indexed.Add(comboDef);
            }

            indexedComboCount = comboCount;
        }

        /// <summary>检查一项具体来源键与第一、第二来源项的准入分配。</summary>
        private static bool MatchesAssignment(
            string actualFirstSourceIdentity,
            string actualSecondSourceIdentity,
            ComboSourceAdmissionSnapshot firstSourceSnapshot,
            ComboSourceAdmissionSnapshot secondSourceSnapshot,
            string expectedFirstSourceIdentity,
            string expectedSecondSourceIdentity,
            ComboSourceAdmissionContract expectedFirstSourceAdmission,
            ComboSourceAdmissionContract expectedSecondSourceAdmission,
            bool requireSameSourceVariant)
        {
            return MatchesAssignmentWithScope(
                actualFirstSourceIdentity,
                actualSecondSourceIdentity,
                firstSourceSnapshot,
                secondSourceSnapshot,
                expectedFirstSourceIdentity,
                expectedSecondSourceIdentity,
                expectedFirstSourceAdmission,
                expectedSecondSourceAdmission,
                null,
                null,
                requireSameSourceVariant);
        }

        /// <summary>检查来源项身份、准入和形态姿态范围的具体分配。</summary>
        internal static bool MatchesAssignmentWithScope(
            string actualFirstSourceIdentity,
            string actualSecondSourceIdentity,
            ComboSourceAdmissionSnapshot firstSourceSnapshot,
            ComboSourceAdmissionSnapshot secondSourceSnapshot,
            string expectedFirstSourceIdentity,
            string expectedSecondSourceIdentity,
            ComboSourceAdmissionContract expectedFirstSourceAdmission,
            ComboSourceAdmissionContract expectedSecondSourceAdmission,
            ComboSourceScopeContract expectedFirstSourceScope,
            ComboSourceScopeContract expectedSecondSourceScope,
            bool requireSameSourceVariant)
        {
            return MatchesIdentityAndAdmission(
                    actualFirstSourceIdentity,
                    actualSecondSourceIdentity,
                    firstSourceSnapshot,
                    secondSourceSnapshot,
                    expectedFirstSourceIdentity,
                    expectedSecondSourceIdentity,
                    expectedFirstSourceAdmission,
                    expectedSecondSourceAdmission)
                && MatchesScope(firstSourceSnapshot, expectedFirstSourceScope)
                && MatchesScope(secondSourceSnapshot, expectedSecondSourceScope)
                && (!requireSameSourceVariant
                    || ComboSourceAdmissionEvaluator.AreSourceVariantsCompatible(
                        firstSourceSnapshot,
                        secondSourceSnapshot));
        }

        /// <summary>
        /// 检查来源项当前形态与姿态范围。
        /// 没有运行时状态的旧入口不阻断匹配；正式表达解析路径总会提供状态。
        /// </summary>
        private static bool MatchesScope(
            ComboSourceAdmissionSnapshot actual,
            ComboSourceScopeContract expected)
        {
            if (expected == null || actual == null || !actual.HasRuntimeState)
            {
                return true;
            }

            return MatchesOptionalKey(actual.ModeKey, expected.ModeKey)
                && MatchesOptionalKey(actual.StanceKey, expected.StanceKey);
        }

        /// <summary>执行单个可选范围键的大小写不敏感比较。</summary>
        private static bool MatchesOptionalKey(string actual, string expected)
        {
            return string.IsNullOrWhiteSpace(expected)
                || string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>检查来源项身份与各自单侧准入，不包含来源变体配对条件。</summary>
        private static bool MatchesIdentityAndAdmission(
            string actualFirstSourceIdentity,
            string actualSecondSourceIdentity,
            ComboSourceAdmissionSnapshot firstSourceSnapshot,
            ComboSourceAdmissionSnapshot secondSourceSnapshot,
            string expectedFirstSourceIdentity,
            string expectedSecondSourceIdentity,
            ComboSourceAdmissionContract expectedFirstSourceAdmission,
            ComboSourceAdmissionContract expectedSecondSourceAdmission)
        {
            return string.Equals(actualFirstSourceIdentity, expectedFirstSourceIdentity, StringComparison.OrdinalIgnoreCase)
                && string.Equals(actualSecondSourceIdentity, expectedSecondSourceIdentity, StringComparison.OrdinalIgnoreCase)
                && ComboSourceAdmissionEvaluator.IsAllowed(firstSourceSnapshot, expectedFirstSourceAdmission)
                && ComboSourceAdmissionEvaluator.IsAllowed(secondSourceSnapshot, expectedSecondSourceAdmission);
        }

        /// <summary>
        /// 从芯片 Thing 提取 Combo 匹配用来源键。
        /// 取制造来源首个芯片功能定义 DefName 作为芯片的身份键。
        /// </summary>
        private static string GetComboIdentity(Thing chip)
        {
            ChipSourceReferenceSnapshot source = ChipInstanceSurfaceAccess.ReadSourceReference(chip);
            if (source.OrderedSourceKeys != null && source.OrderedSourceKeys.Count > 0)
            {
                return source.OrderedSourceKeys[0];
            }

            return null;
        }

        /// <summary>
        /// 为两枚芯片 DefName 生成稳定无序键。
        /// </summary>
        private static string BuildUnorderedPairKey(string firstDefName, string secondDefName)
        {
            if (string.IsNullOrWhiteSpace(firstDefName) || string.IsNullOrWhiteSpace(secondDefName))
            {
                return string.Empty;
            }

            return string.Compare(firstDefName, secondDefName, StringComparison.OrdinalIgnoreCase) <= 0
                ? firstDefName + "|" + secondDefName
                : secondDefName + "|" + firstDefName;
        }
    }
}
