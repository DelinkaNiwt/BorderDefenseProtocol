using System.Collections.Generic;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击计划中的单次施放动作。
    /// 它属于计划层展开单位，不直接等于运行时真正消费的一步。
    /// </summary>
    internal sealed class AttackExecutionCast
    {
        /// <summary>
        /// 当前施放动作所属的攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前施放动作所属的执行组编号。
        /// </summary>
        public int GroupIndex { get; set; }

        /// <summary>
        /// 当前施放动作在所属执行组内的顺序编号。
        /// </summary>
        public int CastLocalIndex { get; set; }

        /// <summary>
        /// 当前施放动作在所属结果展开后的顺序编号。
        /// </summary>
        public int CastOrdinal { get; set; }

        /// <summary>
        /// 当前施放动作对应的正式结果标识。
        /// 它回答“这次 cast 属于哪条正式攻击结果”，不等于该 cast 内每个 emit 的完整发射真值。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前施放动作绑定的正式结果。
        /// 这条引用只服务会话与编排读取，不要求承担 emit 级载荷真值。
        /// </summary>
        public FormalExpressionResult Result { get; set; }

        /// <summary>
        /// 当前施放动作的目标。
        /// </summary>
        public LocalTargetInfo Target { get; set; }

        /// <summary>
        /// 当前施放动作对应的槽位/侧别键。
        /// 它服务调度与诊断，不承担 emit 级载荷的唯一来源职责。
        /// </summary>
        public string SlotKey { get; set; }

        /// <summary>
        /// 当前施放动作对应的武器模式。
        /// </summary>
        public WeaponExpressionMode WeaponMode { get; set; }

        /// <summary>
        /// 当前施放动作完成后，到下一次施放建议等待多少 tick。
        /// </summary>
        public int IntervalTicksAfter { get; set; }

        /// <summary>
        /// 当前施放动作是否属于副攻击身份。
        /// </summary>
        public bool IsSecondary { get; set; }

        /// <summary>
        /// 当前施放动作是否属于本次请求的主入口标识。
        /// </summary>
        public bool IsPrimarySelection { get; set; }

        /// <summary>
        /// 当前施放动作是否属于双侧调度中的主侧来源。
        /// </summary>
        public bool IsMainSide { get; set; }

        /// <summary>
        /// 当前施放动作会产生的效果实例集合。
        /// 具体每个效果实例的发射真值由 emit 自身携带。
        /// </summary>
        public IReadOnlyList<AttackExecutionEmit> Emits { get; set; }
    }
}
