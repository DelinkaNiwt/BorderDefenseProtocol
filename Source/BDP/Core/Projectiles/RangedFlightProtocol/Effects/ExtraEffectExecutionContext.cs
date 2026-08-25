using BDP.Core.Semantics;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Effects
{
    /// <summary>
    /// 额外效果执行时的中性上下文。
    /// </summary>
    public sealed class ExtraEffectExecutionContext
    {
        /// <summary>
        /// 当前实际效果目标。
        /// </summary>
        public Thing TargetThing { get; set; }

        /// <summary>
        /// 当前实际效果格子。
        /// </summary>
        public IntVec3 TargetCell { get; set; }

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
        /// 当前投射物。
        /// </summary>
        public Projectile Projectile { get; set; }

        /// <summary>
        /// 当前攻击语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前攻击实例标识。
        /// </summary>
        public string AttackInstanceId { get; set; }

        /// <summary>
        /// 当前正式表达结果标识。
        /// </summary>
        public string ResultId { get; set; }
    }
}
