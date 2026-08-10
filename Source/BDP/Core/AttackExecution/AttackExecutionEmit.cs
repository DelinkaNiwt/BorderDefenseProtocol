using BDP.Core.Expressions;
using BDP.Core.Semantics;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 攻击计划与运行时动作步共同引用的最小效果载荷。
    /// 对远程而言它通常是一发投射物，对近战而言它通常是一段命中效果。
    /// 它是实际落地边界前的最小载荷单元，必须自带本次效果实例所需的发射真值。
    /// </summary>
    internal sealed class AttackExecutionEmit
    {
        /// <summary>
        /// 当前效果实例所属的攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前效果实例所属的执行组编号。
        /// </summary>
        public int GroupIndex { get; set; }

        /// <summary>
        /// 当前效果实例所属施放动作在组内的顺序编号。
        /// </summary>
        public int CastLocalIndex { get; set; }

        /// <summary>
        /// 当前效果实例所属施放动作的顺序编号。
        /// </summary>
        public int CastOrdinal { get; set; }

        /// <summary>
        /// 当前效果实例在本次施放动作内的顺序编号。
        /// </summary>
        public int EmitLocalIndex { get; set; }

        /// <summary>
        /// 当前效果实例在所属结果展开后的顺序编号。
        /// </summary>
        public int EmitOrdinal { get; set; }

        /// <summary>
        /// 当前效果实例所属 cast 对应的正式结果标识。
        /// 它服务编排回溯，不等于 emit 自身的源结果真值。
        /// </summary>
        public string ResultId { get; set; }

        /// <summary>
        /// 当前效果实例所属 cast 绑定的正式结果。
        /// 它服务编排侧读取，不要求承担 emit 自身的完整真值。
        /// </summary>
        public FormalExpressionResult Result { get; set; }

        /// <summary>
        /// 当前效果实例的源结果标识。
        /// 它回答“这发效果实例最终来自哪条单侧正式结果”。
        /// </summary>
        public string SourceResultId { get; set; }

        /// <summary>
        /// 当前效果实例实际使用的投射物定义。
        /// 对远程效果它是最关键的发射真值之一；对近战效果可为空。
        /// </summary>
        public ThingDef ProjectileDef { get; set; }

        /// <summary>
        /// 当前效果实例实际使用的语义上下文。
        /// 它与 SourceResultId 对齐，不默认沿用复合入口宿主的整体上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前效果实例来源的侧别/槽位键。
        /// 它服务双侧调度诊断与下游按源结果取值，不是运行时宿主身份。
        /// </summary>
        public string OriginSide { get; set; }

        /// <summary>
        /// 当前效果实例的目标。
        /// </summary>
        public LocalTargetInfo Target { get; set; }

        /// <summary>
        /// 当前效果实例的语义目标。
        /// 它服务命中语义与后半段 intended target，不默认等于导航目标。
        /// </summary>
        public LocalTargetInfo SemanticTarget { get; set; }

        /// <summary>
        /// 当前效果实例对应的武器模式。
        /// </summary>
        public WeaponExpressionMode WeaponMode { get; set; }

        /// <summary>
        /// 当前效果实例的发射起点局部偏移。
        /// 这条口主要服务同次齐射内的多弹展开，不改变目标选择语义。
        /// </summary>
        public UnityEngine.Vector3 OriginOffset { get; set; }

        /// <summary>
        /// 当前效果实例是否显式声明了发射点随机散布区间。
        /// </summary>
        public bool HasOriginSpreadRange { get; set; }

        /// <summary>
        /// 当前效果实例横向最小随机偏移。
        /// </summary>
        public float OriginSpreadLateralMin { get; set; }

        /// <summary>
        /// 当前效果实例横向最大随机偏移。
        /// </summary>
        public float OriginSpreadLateralMax { get; set; }

        /// <summary>
        /// 当前效果实例前后最小随机偏移。
        /// </summary>
        public float OriginSpreadForwardMin { get; set; }

        /// <summary>
        /// 当前效果实例前后最大随机偏移。
        /// </summary>
        public float OriginSpreadForwardMax { get; set; }
    }
}
