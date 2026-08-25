using System.Collections.Generic;

namespace BDP.Core.Combos
{
    /// <summary>组合技单侧来源准入的只读正式契约。</summary>
    internal sealed class ComboSourceAdmissionContract
    {
        /// <summary>允许的最终职业。</summary>
        public IReadOnlyList<string> AllowedProfessions;
        /// <summary>禁止的最终职业。</summary>
        public IReadOnlyList<string> DeniedProfessions;
        /// <summary>允许的主分类。</summary>
        public IReadOnlyList<string> AllowedCategories;
        /// <summary>禁止的主分类。</summary>
        public IReadOnlyList<string> DeniedCategories;
        /// <summary>允许的普通标签。</summary>
        public IReadOnlyList<string> AllowedTags;
        /// <summary>必须全部具备的普通标签。</summary>
        public IReadOnlyList<string> RequiredTags;
        /// <summary>禁止的普通标签。</summary>
        public IReadOnlyList<string> DeniedTags;
        /// <summary>允许的来源变体。</summary>
        public IReadOnlyList<string> AllowedSourceVariants;
        /// <summary>禁止的来源变体。</summary>
        public IReadOnlyList<string> DeniedSourceVariants;
    }
}
