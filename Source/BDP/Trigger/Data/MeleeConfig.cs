using System.Collections.Generic;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 近战芯片配置。
    /// 定义近战攻击的工具（tools）和相关参数。
    /// </summary>
    public class MeleeConfig
    {
        /// <summary>
        /// 近战武器Tool配置列表。
        /// 替代ThingDef.tools，避免IsWeapon=true导致的问题。
        /// 每个Tool定义一种攻击方式（如切割、钝击等）。
        /// </summary>
        public List<Tool> tools;
    }
}
