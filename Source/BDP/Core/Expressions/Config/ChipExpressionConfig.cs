using System.Collections.Generic;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片 Def 上挂载的表达原始配置。
    /// 它只保留内容作者写下来的条目和形态写法，
    /// 不在这里直接裁定最终哪些表达成立。
    /// </summary>
    public sealed class ChipExpressionConfig : DefModExtension
    {
        /// <summary>
        /// 这枚芯片声明的基础表达条目集合。
        /// 每条条目自己说明属于哪一类，而不是再分四个列表。
        /// </summary>
        public List<ChipExpressionEntryConfig> Entries;

        /// <summary>
        /// 多形态芯片在尚无当前形态时使用的默认形态键。
        /// 单形态芯片不得填写。
        /// </summary>
        public string DefaultModeKey;

        /// <summary>
        /// 这枚芯片声明的形态块集合。
        /// 每个形态块只选择统一目录中的条目。
        /// </summary>
        public List<ChipExpressionModeConfig> Modes;
    }
}
