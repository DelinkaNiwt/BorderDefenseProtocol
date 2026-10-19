using System.Collections.Generic;
using Verse;

namespace BDP.Core.Information
{
    /// <summary>
    /// 面向玩家的芯片功能语义定义。
    /// 它只描述语义名称、排序、允许接收的属性与信息结构，不维护任何实现类型列表。
    /// </summary>
    public sealed class ChipFeatureDef : Def
    {
        /// <summary>功能在同一信息模型中的默认显示顺序。</summary>
        public int DisplayOrder;

        /// <summary>该功能允许接收和展示的属性槽位。</summary>
        public List<ChipFeaturePropertySlot> Properties;

        /// <summary>该功能允许使用的信息组槽位。</summary>
        public List<ChipFeatureGroupSlot> Groups;

        /// <summary>该功能整体允许直接拥有的一层子组槽位。</summary>
        public List<ChipFeatureGroupSlot> Subgroups;
    }
}
