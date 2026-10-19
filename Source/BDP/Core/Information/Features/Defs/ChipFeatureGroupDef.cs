using System.Collections.Generic;
using Verse;

namespace BDP.Core.Information
{
    /// <summary>
    /// 面向玩家的一组关联功能信息定义。
    /// </summary>
    public sealed class ChipFeatureGroupDef : Def
    {
        /// <summary>信息组在所属功能中的默认显示顺序。</summary>
        public int DisplayOrder;

        /// <summary>该信息组允许接收和展示的属性槽位。</summary>
        public List<ChipFeaturePropertySlot> Properties;

        /// <summary>该信息组允许包含的一层子组；子组不可继续嵌套。</summary>
        public List<ChipFeatureGroupSlot> Subgroups;
    }
}
