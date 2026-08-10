using System.Collections.Generic;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片画像声明结果。
    /// 它只承载主模组承认后的静态芯片画像。
    /// </summary>
    internal sealed class ChipProfileContract
    {
        /// <summary>
        /// 当前芯片被主模组承认的统一主分类。
        /// </summary>
        public ChipCategoryDef Category;

        /// <summary>
        /// 当前芯片解析后的正式特征标签集合。
        /// </summary>
        public IReadOnlyList<ChipTagDef> Tags;
    }
}
