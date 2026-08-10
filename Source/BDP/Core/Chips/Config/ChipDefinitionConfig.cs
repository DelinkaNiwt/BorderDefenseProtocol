using System.Collections.Generic;
using BDP.Core.Requirements;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 芯片定义层的总配置入口。
    /// 内容作者应从这里声明整枚芯片的正式信息块，
    /// 而不是让下游系统各自直接读取零散 Def 字段。
    /// </summary>
    public sealed class ChipDefinitionConfig : DefModExtension
    {
        /// <summary>
        /// 芯片的画像声明块。
        /// 它回答这枚芯片引用哪个统一登记的主分类。
        /// </summary>
        public ChipProfileConfig Profile;

        /// <summary>
        /// 芯片的装载声明块。
        /// 它回答这枚芯片允许怎样被 Trigger 装载。
        /// </summary>
        public ChipLoadoutConfig Loadout;

        /// <summary>
        /// 芯片的表达声明块。
        /// 它继续沿用表达系统自己的配置结构。
        /// </summary>
        public ChipExpressionConfig Expression;

        /// <summary>
        /// 芯片的 Trion 声明块。
        /// 它只声明芯片本体级 Trion 参数，不持有 Trion 真值。
        /// </summary>
        public ChipTrionConfig Trion;

        /// <summary>
        /// 芯片按作者声明顺序执行的激活条件。
        /// 每枚芯片必须恰好声明一条 Trion 释放力条件。
        /// </summary>
        public List<PawnRequirement> ActivationRequirements;

        /// <summary>
        /// 芯片的扩展声明块集合。
        /// 每个具体扩展类型都应只服务一种明确的可选业务能力。
        /// </summary>
        public List<ChipExtensionConfig> Extensions;

    }
}
