using BDP.Core.Semantics;
using BDP.Core.CombatModel;
using System.Collections.Generic;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 单条结构化说明结果。
    /// 它只描述正式结果本身，不暴露内部解析过程。
    /// </summary>
    internal sealed class ExpressionInfoProjectionEntry
    {
        /// <summary>
        /// 当前结果标识。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前结果显示名。
        /// </summary>
        public string DisplayLabel { get; set; }

        /// <summary>
        /// 当前结果大类。
        /// </summary>
        public ExpressionResultKind ResultKind { get; set; }

        /// <summary>
        /// 当前结果来源关系。
        /// </summary>
        public ExpressionOriginKind OriginKind { get; set; }

        /// <summary>
        /// 当前结果高层关系类型。
        /// </summary>
        public CompositeExpressionKind CompositeKind { get; set; }

        /// <summary>
        /// 当前结果角色标识。
        /// </summary>
        public string RoleKey { get; set; }

        /// <summary>
        /// 当前结果形态键。
        /// </summary>
        public string ModeKey { get; set; }

        /// <summary>
        /// 当前结果是否来自某个形态块。
        /// </summary>
        public bool IsModeDerived { get; set; }

        /// <summary>
        /// 当前结果是否已携带 Verb 属性定义。
        /// </summary>
        public bool HasVerbProps { get; set; }

        /// <summary>
        /// 当前结果的 Verb 类名。
        /// 只有 Verb 类结果且存在定义时才填写。
        /// </summary>
        public string VerbClassName { get; set; }

        /// <summary>
        /// 当前结果指向的 Ability 定义名。
        /// </summary>
        public string AbilityDefName { get; set; }

        /// <summary>
        /// 当前结果指向的 Hediff 定义名。
        /// </summary>
        public string HediffDefName { get; set; }

        /// <summary>
        /// 当前结果的 Hediff 应用方式键。
        /// </summary>
        public string HediffApplyModeKey { get; set; }

        /// <summary>
        /// 当前结果的被动声明键。
        /// </summary>
        public string PassiveKey { get; set; }

        /// <summary>
        /// 当前结果武器模式。
        /// </summary>
        public WeaponExpressionMode WeaponMode { get; set; }

        /// <summary>
        /// 当前结果声明的远程执行节奏。
        /// 没有正式声明时为 None。
        /// </summary>
        public RangedExecutionRhythm RangedExecutionRhythm { get; set; }

        /// <summary>
        /// 当前结果声明的近战执行节奏。
        /// 没有正式声明时为 None。
        /// </summary>
        public MeleeExecutionRhythm MeleeExecutionRhythm { get; set; }

        /// <summary>
        /// 当前结果声明的双侧调度方式。
        /// 非双侧复合结果应为 None。
        /// </summary>
        public DualExecutionSchedule DualExecutionSchedule { get; set; }

        /// <summary>
        /// 当前结果正式声明的一次远程动作发射数。
        /// </summary>
        public int ShotCount { get; set; }

        /// <summary>
        /// 当前结果正式声明的一次近战动作命中数。
        /// </summary>
        public int HitCount { get; set; }

        /// <summary>
        /// 当前结果是否可用。
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// 当前结果是否允许进入后续投影。
        /// </summary>
        public bool CanProject { get; set; }

        /// <summary>
        /// 当前结果当前对应的发布键。
        /// 没有稳定发布键时应为空。
        /// </summary>
        public string PublishedKey { get; set; }

        /// <summary>
        /// 当前结果当前是否具备最小发布条件。
        /// 它只服务说明层诊断，不代替运行时副作用结果。
        /// </summary>
        public bool IsPublished { get; set; }

        /// <summary>
        /// 当前结果若来自复合结果，则这里保留其来源结果标识。
        /// </summary>
        public IReadOnlyList<string> SourceResultIds { get; set; }

        /// <summary>
        /// 当前结果是否是默认主远程。
        /// </summary>
        public bool IsPrimaryRanged { get; set; }

        /// <summary>
        /// 当前结果是否是默认主近战。
        /// </summary>
        public bool IsPrimaryMelee { get; set; }

        /// <summary>
        /// 当前结果是否是当前执行表达。
        /// </summary>
        public bool IsCurrentExecuting { get; set; }
    }
}
