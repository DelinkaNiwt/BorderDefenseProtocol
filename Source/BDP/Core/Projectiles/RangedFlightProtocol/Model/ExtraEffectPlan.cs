using System.Collections.Generic;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 额外效果正式计划。
    /// 第一版先统一成轻量参数袋，不引入更重的特效系统。
    /// </summary>
    public sealed class ExtraEffectPlan
    {
        /// <summary>
        /// 当前额外效果的种类键。
        /// </summary>
        public string EffectKind { get; set; }

        /// <summary>
        /// 当前额外效果命中的 Thing。
        /// </summary>
        public Thing TargetThing { get; set; }

        /// <summary>
        /// 当前额外效果命中的格子。
        /// </summary>
        public IntVec3 TargetCell { get; set; }

        /// <summary>
        /// 当前额外效果携带的参数字典。
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 当前额外效果附带的轻量标签集合。
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
