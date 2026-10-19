using Verse;

namespace BDP.Core.Combos
{
    /// <summary>
    /// 一次运行时组合匹配实际采用的第一、第二来源芯片。
    /// 它只描述本次候选对的来源顺序，不改变 ComboDef 的声明内容。
    /// </summary>
    internal sealed class ComboSourceBinding
    {
        /// <summary>
        /// 按 ComboDef firstSourceKey 绑定的实际第一来源芯片。
        /// </summary>
        public Thing FirstSourceChip;

        /// <summary>
        /// 按 ComboDef secondSourceKey 绑定的实际第二来源芯片。
        /// </summary>
        public Thing SecondSourceChip;
    }
}
