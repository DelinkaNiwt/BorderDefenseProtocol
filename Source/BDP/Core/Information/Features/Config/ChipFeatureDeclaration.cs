using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 一项具体芯片功能实例的作者声明。
    /// 同一个 FeatureDef 可以通过不同 Id 声明多个独立实例。
    /// </summary>
    public sealed class ChipFeatureDeclaration
    {
        /// <summary>当前配置范围内稳定且唯一的功能实例 Id。</summary>
        public string Id;

        /// <summary>玩家语义功能 Def。</summary>
        public ChipFeatureDef Feature;

        /// <summary>可选的实例差异化语言键。</summary>
        public string LabelKey;

        /// <summary>该功能实例的实际值来源声明。</summary>
        public List<ChipFeatureSourceConfig> Sources;

        /// <summary>该功能实例内关联信息的分组声明。</summary>
        public List<ChipFeatureGroupDeclaration> Groups;

        /// <summary>归属于整个功能实例的一层子组声明。</summary>
        public List<ChipFeatureSubgroupDeclaration> Subgroups;

        /// <summary>复杂功能的结构化子功能。</summary>
        public List<ChipFeatureDeclaration> Children;
    }
}
