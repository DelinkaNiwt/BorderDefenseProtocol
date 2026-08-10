using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片特征标签定义。
    /// Core 只提供稳定的 Def 身份；具体标签名称和业务含义由 Content 提供。
    /// </summary>
    public sealed class ChipTagDef : Def
    {
        /// <summary>
        /// 该标签对制造成本的倍率修正（预留）。
        /// 默认 1.0 = 不修正。与全局倍率叠乘。
        /// </summary>
        public float costMultiplier = 1.0f;
    }
}
