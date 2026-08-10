using System.Collections.Generic;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达来源声明提供器。
    /// 它只负责从芯片与当前形态中读取正式来源声明，不负责运行时结果裁定。
    /// </summary>
    internal interface IExpressionSourceDeclarationProvider
    {
        /// <summary>
        /// 读取指定芯片当前适用的表达来源声明。
        /// </summary>
        IReadOnlyList<ExpressionSourceDeclaration> GetDeclarations(Thing chip, ITriggerLoadoutReader triggerLoadoutReader);
    }
}
