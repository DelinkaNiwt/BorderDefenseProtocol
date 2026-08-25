using System.Collections.Generic;

namespace BDP.Core.Combos
{
    /// <summary>组合技单侧来源芯片准入配置。</summary>
    public sealed class ComboSourceAdmissionConfig
    {
        /// <summary>允许的最终职业；任意命中即可。</summary>
        public List<string> AllowedProfessions = new List<string>();

        /// <summary>禁止的最终职业；任意命中即拒绝。</summary>
        public List<string> DeniedProfessions = new List<string>();

        /// <summary>允许的主分类；任意命中即可。</summary>
        public List<string> AllowedCategories = new List<string>();

        /// <summary>禁止的主分类；任意命中即拒绝。</summary>
        public List<string> DeniedCategories = new List<string>();

        /// <summary>允许的普通标签；任意命中即可。</summary>
        public List<string> AllowedTags = new List<string>();

        /// <summary>必须全部具备的普通标签。</summary>
        public List<string> RequiredTags = new List<string>();

        /// <summary>禁止的普通标签；任意命中即拒绝。</summary>
        public List<string> DeniedTags = new List<string>();

        /// <summary>允许的来源变体；任意命中即可。</summary>
        public List<string> AllowedSourceVariants = new List<string>();

        /// <summary>禁止的来源变体；任意命中即拒绝。</summary>
        public List<string> DeniedSourceVariants = new List<string>();
    }
}
