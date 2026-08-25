using System.Collections.Generic;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using BDP.Core.Semantics;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// 攻击生产者实际产生的一次目标判定。
    /// </summary>
    /// <remarks>
    /// 该对象表示一次发生，不表示唯一目标集合。
    /// 同一个 Thing 可以通过多个事件进入派发器；Core 不在这里去重。
    /// </remarks>
    public sealed class AttackTargetEvent
    {
        /// <summary>
        /// 当前目标事件来自哪一种通用攻击入口。
        /// </summary>
        public AttackTargetEventSource Source { get; set; }

        /// <summary>
        /// 当前攻击目标。
        /// </summary>
        public Thing TargetThing { get; set; }

        /// <summary>
        /// 当前攻击目标所在格子。
        /// </summary>
        public IntVec3 TargetCell { get; set; }

        /// <summary>
        /// 当前事件要派发的额外效果计划。
        /// </summary>
        public IReadOnlyList<ExtraEffectPlan> ExtraEffects { get; set; }

        /// <summary>
        /// 当前目标所在地图。
        /// </summary>
        public Map Map { get; set; }

        /// <summary>
        /// 当前攻击施加者。
        /// </summary>
        public Thing Instigator { get; set; }

        /// <summary>
        /// 当前攻击来源宿主。
        /// </summary>
        public Thing SourceThing { get; set; }

        /// <summary>
        /// 当前 BDP 投射物。
        /// </summary>
        public Projectile Projectile { get; set; }

        /// <summary>
        /// 当前攻击继承的语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前表达结果标识。
        /// </summary>
        public string ResultId { get; set; }
    }

    /// <summary>
    /// 攻击目标事件的通用入口来源。
    /// </summary>
    public enum AttackTargetEventSource
    {
        /// <summary>
        /// 没有其他目标生产者时的直接命中入口。
        /// </summary>
        DirectImpact,

        /// <summary>
        /// 由某个攻击生产者逐目标产生的事件。
        /// </summary>
        ProducedTarget
    }
}
