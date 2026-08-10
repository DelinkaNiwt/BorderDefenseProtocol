using System.Collections.Generic;
using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片画像配置。
    /// 它只回答这枚芯片从静态语义上是什么，不夹带运行结果。
    /// </summary>
    public sealed class ChipProfileConfig : IExposable
    {
        /// <summary>
        /// 当前芯片统一登记的主分类。
        /// 具体分类由内容定义提供，Core 只要求引用有效的 ChipCategoryDef。
        /// </summary>
        public ChipCategoryDef Category;

        /// <summary>
        /// 当前芯片的零个或多个正式特征标签。
        /// 标签必须引用已登记的 ChipTagDef，不能填写自由文本。
        /// </summary>
        public List<ChipTagDef> Tags = new List<ChipTagDef>();

        /// <summary>
        /// RimWorld XML 反序列化兼容口。
        /// 当前保持最小空实现即可。
        /// </summary>
        public void ExposeData()
        {
        }
    }
}
