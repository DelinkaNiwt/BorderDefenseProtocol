using BDP.Core.Expressions;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片定义层对表达块的正式句柄。
    /// 它不复制表达系统内部对象，只转交表达块入口。
    /// </summary>
    internal sealed class ChipExpressionContractHandle
    {
        /// <summary>
        /// 当前芯片是否声明了表达块。
        /// </summary>
        public bool HasExpressionBlock;

        /// <summary>
        /// 当前表达块对应的原始配置引用。
        /// </summary>
        public ChipExpressionConfig Config;

        /// <summary>
        /// 当前表达块使用的结构标签。
        /// 第一版固定为 Entries + Modes。
        /// </summary>
        public string StructureKey;
    }
}
