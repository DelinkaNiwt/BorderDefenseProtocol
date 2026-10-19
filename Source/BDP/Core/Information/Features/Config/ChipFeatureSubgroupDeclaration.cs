using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 归属于整个功能或某个信息组的一层子组声明；子组自身不能再包含子组。
    /// </summary>
    public sealed class ChipFeatureSubgroupDeclaration
    {
        /// <summary>所属功能或信息组范围内稳定且唯一的子组实例 Id。</summary>
        public string Id;

        /// <summary>玩家语义信息组 Def。</summary>
        public ChipFeatureGroupDef Group;

        /// <summary>可选的子组差异化语言键。</summary>
        public string LabelKey;

        /// <summary>该子组实际值来源声明。</summary>
        public List<ChipFeatureSourceConfig> Sources;
    }
}
