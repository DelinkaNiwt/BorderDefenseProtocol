using System.Collections.Generic;
using BDP.Core.CombatModel;
using BDP.Core.AttackExecution;
using BDP.Core.Expressions;
using BDP.Core.Semantics;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 远程攻击协议的正式入口。
    /// 它把 AttackExecution 已经确认好的上游结果压成远程协议可消费的单一入口对象。
    /// </summary>
    internal sealed class RangedAttackEntry
    {
        /// <summary>
        /// 当前攻击实例标识。
        /// 整条远程链都围绕这次攻击实例展开。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前请求进入执行系统的原因。
        /// 它只说明入口来源，不说明具体模块逻辑。
        /// </summary>
        public AttackExecutionReason RequestReason { get; set; }

        /// <summary>
        /// 当前请求采用的派单意图。
        /// 它只说明是立即施放还是持续攻击命令。
        /// </summary>
        public AttackDispatchIntent DispatchIntent { get; set; }

        /// <summary>
        /// 当前执行宿主 Pawn。
        /// </summary>
        public Pawn Pawn { get; set; }

        /// <summary>
        /// 当前协议入口携带的原始目标。
        /// </summary>
        public LocalTargetInfo Target { get; set; }

        /// <summary>
        /// 当前协议入口携带的语义目标。
        /// 它服务 dual 兄弟侧与命中语义，不默认等于导航目标。
        /// </summary>
        public LocalTargetInfo SemanticTarget { get; set; }

        /// <summary>
        /// 当前会话宿主结果标识。
        /// 它服务攻击会话回溯，不天然等于 emit 的最终源结果。
        /// </summary>
        public string SessionResultId { get; set; }

        /// <summary>
        /// 当前这次远程动作默认主来源的正式结果标识。
        /// 它只服务单来源读取；若一步内存在多来源 emit，真正真值应从 StepEmits 读取。
        /// </summary>
        public string SourceResultId { get; set; }

        /// <summary>
        /// 当前协议入口绑定的会话宿主正式结果。
        /// 对 dual 入口，它通常是复合宿主结果。
        /// </summary>
        public FormalExpressionResult SessionResult { get; set; }

        /// <summary>
        /// 当前协议入口默认主来源结果。
        /// 它只服务单来源读取；若一步内存在多来源 emit，真正真值应从 StepEmits 读取。
        /// </summary>
        public FormalExpressionResult SourceResult { get; set; }
        /// <summary>
        /// 当前结果的武器模式。
        /// 远程协议正常情况下应恒为 Ranged。
        /// </summary>
        public WeaponExpressionMode WeaponMode { get; set; }

        /// <summary>
        /// 当前结果声明的执行风格。
        /// 它属于上游表达真值，不在这里重发明。
        /// </summary>
        public AttackExecutionStyle ExecutionStyle { get; set; }

        /// <summary>
        /// 当前结果的攻击角色。
        /// 它服务主副手、组合表达等来源回溯。
        /// </summary>
        public VerbAttackRole AttackRole { get; set; }

        /// <summary>
        /// 当前远程链携带的语义上下文。
        /// 之后要一路带到 projectile 与 impact。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前远程链携带的统一攻击上下文。
        /// 远程协议前半段跨阶段传递只认这条主干。
        /// </summary>
        public AttackContext AttackContext { get; set; }

        /// <summary>
        /// 当前协议入口实际承接的运行时动作步。
        /// 协议默认基线必须以它为准，不允许重新猜测 AttackExecution 已经给出的真值。
        /// </summary>
        public AttackRuntimeStep RuntimeStep { get; set; }

        /// <summary>
        /// 当前动作步归并进来的 cast 集合。
        /// 模块可读取它做更高层组合判断，但不能回写它。
        /// </summary>
        public IReadOnlyList<AttackExecutionCast> StepCasts { get; set; }

        /// <summary>
        /// 当前动作步实际展开出的 emit 集合。
        /// 这才是默认 fire/projectile baseline 的正式来源。
        /// </summary>
        public IReadOnlyList<AttackExecutionEmit> StepEmits { get; set; }

        /// <summary>
        /// 当前入口是否合法。
        /// 若为 false，后续阶段不得继续执行。
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 当前入口失效时的正式拒绝原因。
        /// </summary>
        public string RejectReason { get; set; }

        /// <summary>
        /// 当前入口创建时的游戏 tick。
        /// 它服务诊断与回溯，不参与业务裁决。
        /// </summary>
        public int CreatedTick { get; set; }

        /// <summary>
        /// 当前攻击入口绑定的模块运行时会话。
        /// 之后需要沿 ProjectileInit 冻结边界继续传给后半段。
        /// </summary>
        public RangedAttackModuleSession ModuleSession { get; set; }
    }
}
