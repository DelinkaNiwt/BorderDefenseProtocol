using System.Collections.Generic;
using BDP.Core.Semantics;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 区域效果正式计划。
    /// </summary>
    public sealed class AreaEffectPlan
    {
        /// <summary>
        /// 当前区域计划使用的伤害类型。
        /// </summary>
        public DamageDef DamageDef { get; set; }

        /// <summary>
        /// 当前区域计划的作用半径。
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// 当前区域计划的伤害量。
        /// </summary>
        public float DamageAmount { get; set; }

        /// <summary>
        /// 当前区域计划的护甲穿透。
        /// </summary>
        public float ArmorPenetration { get; set; }

        /// <summary>
        /// 当前区域计划的中心格。
        /// </summary>
        public IntVec3 Center { get; set; }

        /// <summary>
        /// 当前区域计划的施加者。
        /// </summary>
        public Thing Instigator { get; set; }

        /// <summary>
        /// 当前区域计划追踪的来源武器或宿主。
        /// </summary>
        public Thing Weapon { get; set; }

        /// <summary>
        /// 当前区域计划继承的统一语义上下文。
        /// </summary>
        public ISemanticContext SemanticContext { get; set; }

        /// <summary>
        /// 当前区域效果的原版爆炸视觉、音效和屏幕震动策略。
        /// </summary>
        public ExplosionPresentationPolicy PresentationPolicy { get; set; }

        /// <summary>
        /// 当前区域计划附带的轻量标签集合。
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
