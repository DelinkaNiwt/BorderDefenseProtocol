using System.Collections.Generic;

namespace BDP.Core.Information
{
    /// <summary>
    /// 一组具体芯片功能信息实例的作者声明。
    /// </summary>
    public sealed class ChipFeatureGroupDeclaration
    {
        /// <summary>当前功能范围内稳定且唯一的信息组实例 Id。</summary>
        public string Id;

        /// <summary>玩家语义信息组 Def。</summary>
        public ChipFeatureGroupDef Group;

        /// <summary>可选的信息组差异化语言键。</summary>
        public string LabelKey;

        /// <summary>该信息组实际值来源声明。</summary>
        public List<ChipFeatureSourceConfig> Sources;

        /// <summary>归属于当前信息组的一层子组声明。</summary>
        public List<ChipFeatureSubgroupDeclaration> Subgroups;
    }
}
