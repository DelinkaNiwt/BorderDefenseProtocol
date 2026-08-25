using System.Collections.Generic;
using BDP.Core.Admission;
using BDP.Core.Chips;
using Verse;

namespace BDP.Core.Combos
{
    /// <summary>组合技来源芯片身份适配与准入求值器。</summary>
    internal static class ComboSourceAdmissionEvaluator
    {
        /// <summary>从芯片正式定义和实例来源记录构建成品身份快照。</summary>
        public static ComboSourceAdmissionSnapshot BuildSnapshot(Thing chip)
        {
            ChipSourceReferenceSnapshot source = ChipInstanceSurfaceAccess.ReadSourceReference(chip);
            ChipDefinitionReadResult readResult = ChipSurfaceAccess.Read(chip);
            ChipProfileContract profile = readResult?.Contract?.Profile;
            List<string> tags = new List<string>();
            if (profile?.Tags != null)
            {
                for (int index = 0; index < profile.Tags.Count; index++)
                {
                    if (profile.Tags[index] != null)
                    {
                        tags.Add(profile.Tags[index].defName);
                    }
                }
            }

            return new ComboSourceAdmissionSnapshot
            {
                ProfessionKey = source.SourceProfessionKey,
                CategoryKey = profile?.Category?.defName,
                TagKeys = tags.AsReadOnly(),
                SourceVariantKey = source.SourceVariantKey
            };
        }

        /// <summary>检查一枚实际芯片是否满足指定 Combo 侧的全部维度。</summary>
        public static bool IsAllowed(
            ComboSourceAdmissionSnapshot snapshot,
            ComboSourceAdmissionContract admission)
        {
            if (admission == null)
            {
                return true;
            }

            ComboSourceAdmissionSnapshot safe = snapshot ?? new ComboSourceAdmissionSnapshot();
            return EvaluateScalar(
                    safe.ProfessionKey,
                    admission.AllowedProfessions,
                    admission.DeniedProfessions)
                && EvaluateScalar(
                    safe.CategoryKey,
                    admission.AllowedCategories,
                    admission.DeniedCategories)
                && ValueSetAdmissionEvaluator.Evaluate(
                    safe.TagKeys,
                    BuildRule(admission.AllowedTags, admission.RequiredTags, admission.DeniedTags)).IsAllowed
                && EvaluateScalar(
                    safe.SourceVariantKey,
                    admission.AllowedSourceVariants,
                    admission.DeniedSourceVariants);
        }

        /// <summary>
        /// 判断第一、第二来源项的来源变体是否兼容。
        /// 空快照、空字符串和全空白均表示没有来源变体；只有双方都没有来源变体，
        /// 或双方拥有同一个非空来源变体键时，才允许组合继续匹配。
        /// </summary>
        internal static bool AreSourceVariantsCompatible(
            ComboSourceAdmissionSnapshot firstSourceSnapshot,
            ComboSourceAdmissionSnapshot secondSourceSnapshot)
        {
            string firstSourceVariantKey = NormalizeSourceVariantKey(firstSourceSnapshot?.SourceVariantKey);
            string secondSourceVariantKey = NormalizeSourceVariantKey(secondSourceSnapshot?.SourceVariantKey);
            if (firstSourceVariantKey == null || secondSourceVariantKey == null)
            {
                return firstSourceVariantKey == null && secondSourceVariantKey == null;
            }

            return string.Equals(
                firstSourceVariantKey,
                secondSourceVariantKey,
                System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>把来源变体键归一化为非空稳定比较值。</summary>
        private static string NormalizeSourceVariantKey(string sourceVariantKey)
        {
            return string.IsNullOrWhiteSpace(sourceVariantKey)
                ? null
                : sourceVariantKey.Trim();
        }

        /// <summary>按单值候选执行白名单和黑名单检查。</summary>
        private static bool EvaluateScalar(
            string candidate,
            IReadOnlyList<string> allowed,
            IReadOnlyList<string> denied)
        {
            List<string> candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                candidates.Add(candidate);
            }

            return ValueSetAdmissionEvaluator.Evaluate(
                candidates,
                BuildRule(allowed, null, denied)).IsAllowed;
        }

        /// <summary>把芯片维度列表适配为中性集合规则。</summary>
        private static ValueSetAdmissionRule BuildRule(
            IReadOnlyList<string> allowed,
            IReadOnlyList<string> required,
            IReadOnlyList<string> denied)
        {
            return new ValueSetAdmissionRule
            {
                AllowedAny = allowed != null ? new List<string>(allowed) : new List<string>(),
                RequiredAll = required != null ? new List<string>(required) : new List<string>(),
                DeniedAny = denied != null ? new List<string>(denied) : new List<string>()
            };
        }
    }
}
