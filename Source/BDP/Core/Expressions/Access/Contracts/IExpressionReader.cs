using BDP.Core.Trigger.Runtime;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 表达系统正式只读面。
    /// 它只暴露主模组内其它系统读取表达结果所需的稳定入口，不暴露内部运算模块。
    /// </summary>
    internal interface IExpressionReader
    {
        /// <summary>
        /// 读取指定 Pawn 当前已发布的全部正式表达结果。
        /// 这条口只读已发布投影，不触发运行时推进。
        /// </summary>
        IReadOnlyList<FormalExpressionResult> GetExpressionResults(Pawn pawn);

        /// <summary>
        /// 按类别读取指定 Pawn 当前已发布的正式表达结果。
        /// 这条口只做结果筛取，不参与结果生成。
        /// </summary>
        IReadOnlyList<FormalExpressionResult> GetExpressionResults(Pawn pawn, ExpressionResultKind kind);

        /// <summary>
        /// 读取指定 Pawn 当前已发布的全部 Verb 结果。
        /// </summary>
        IReadOnlyList<FormalExpressionResult> GetVerbResults(Pawn pawn);

        /// <summary>
        /// 读取指定 Pawn 当前已发布的全部 Ability 结果。
        /// </summary>
        IReadOnlyList<FormalExpressionResult> GetAbilityResults(Pawn pawn);

        /// <summary>
        /// 读取指定 Pawn 当前已发布的全部 Hediff 结果。
        /// </summary>
        IReadOnlyList<FormalExpressionResult> GetHediffResults(Pawn pawn);

        /// <summary>
        /// 读取指定 Pawn 当前已发布的全部 Passive 结果。
        /// </summary>
        IReadOnlyList<FormalExpressionResult> GetPassiveResults(Pawn pawn);

        /// <summary>
        /// 按被动键读取指定 Pawn 当前已发布的 Passive 结果。
        /// </summary>
        IReadOnlyList<FormalExpressionResult> GetPassiveResults(Pawn pawn, string passiveKey);

        /// <summary>
        /// 判断指定 Pawn 当前是否存在可用的目标 PassiveKey。
        /// </summary>
        bool HasPassiveKey(Pawn pawn, string passiveKey);

        /// <summary>
        /// 尝试读取指定 Pawn 当前第一条可用的目标 Passive 结果。
        /// </summary>
        bool TryGetPassive(Pawn pawn, string passiveKey, out FormalExpressionResult result);

        /// <summary>
        /// 读取指定 Pawn 当前已发布的战斗投影。
        /// 普通读取只消费已发布结果，不触发运行时协调或快照重建。
        /// </summary>
        TriggerCombatProjectionState GetCombatProjection(Pawn pawn);

        /// <summary>
        /// 读取指定 Pawn 当前说明投影结果。
        /// 常规读取默认不附带芯片定义/契约诊断。
        /// </summary>
        ExpressionInfoProjection GetInfoProjection(Pawn pawn, bool includeDiagnostics = false);

        /// <summary>
        /// 读取指定 Pawn 当前手动入口投影结果。
        /// </summary>
        ManualEntryProjection GetManualProjection(Pawn pawn);

        /// <summary>
        /// 读取指定 Pawn 当前视觉投影结果。
        /// </summary>
        VisualExpressionProjection GetVisualProjection(Pawn pawn);
    }
}
