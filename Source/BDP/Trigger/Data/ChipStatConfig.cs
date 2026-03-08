using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 芯片Stat修正配置（DefModExtension）。
    /// 允许芯片通过XML声明式地影响装备者的属性。
    ///
    /// 使用示例：
    /// <code>
    /// &lt;ThingDef&gt;
    ///   &lt;defName&gt;BDP_Chip_NanoShield&lt;/defName&gt;
    ///   &lt;modExtensions&gt;
    ///     &lt;li Class="BDP.Trigger.ChipStatConfig"&gt;
    ///       &lt;equippedStatOffsets&gt;
    ///         &lt;ArmorRating_Sharp&gt;0.15&lt;/ArmorRating_Sharp&gt;
    ///         &lt;MoveSpeed&gt;-0.3&lt;/MoveSpeed&gt;
    ///       &lt;/equippedStatOffsets&gt;
    ///       &lt;equippedStatFactors&gt;
    ///         &lt;PainShockThreshold&gt;1.5&lt;/PainShockThreshold&gt;
    ///       &lt;/equippedStatFactors&gt;
    ///     &lt;/li&gt;
    ///   &lt;/modExtensions&gt;
    /// &lt;/ThingDef&gt;
    /// </code>
    ///
    /// 武器身份代理系统 阶段3。
    /// </summary>
    public class ChipStatConfig : DefModExtension
    {
        /// <summary>
        /// 装备者stat加成（加算）。
        /// 例：ArmorRating_Sharp +0.15 表示增加0.15护甲。
        /// </summary>
        public List<StatModifier> equippedStatOffsets;

        /// <summary>
        /// 装备者stat倍率（乘算）。
        /// 例：MoveSpeed ×1.2 表示移动速度提升20%。
        /// </summary>
        public List<StatModifier> equippedStatFactors;
    }
}
