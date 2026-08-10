using System.Collections.Generic;
using BDP.Core.Semantics;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 直接伤害正式计划。
    /// </summary>
    public sealed class DamagePlan
    {
        /// <summary>
        /// 当前计划使用的伤害类型。
        /// </summary>
        public DamageDef DamageDef { get; set; }

        /// <summary>
        /// 当前计划的伤害量。
        /// </summary>
        public float Amount { get; set; }

        /// <summary>
        /// 当前计划的护甲穿透。
        /// </summary>
        public float ArmorPenetration { get; set; }

        /// <summary>
        /// 当前计划的伤害施加者。
        /// </summary>
        public Thing Instigator { get; set; }

        /// <summary>
        /// 当前计划追踪的来源武器或宿主。
        /// </summary>
        public Thing Weapon { get; set; }

        /// <summary>
        /// 当前计划关联的意图目标。
        /// </summary>
        public LocalTargetInfo IntendedTarget { get; set; }

        /// <summary>
        /// 当前计划继承的统一语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前计划附带的轻量标签集合。
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
