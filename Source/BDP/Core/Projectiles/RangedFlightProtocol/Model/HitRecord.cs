using System.Collections.Generic;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 命中阶段的正式结果。
    /// 它只回答这次 impact 命中了谁，不直接生成伤害计划。
    /// </summary>
    internal sealed class HitRecord
    {
        /// <summary>
        /// 当前命中快照是否成立为有效命中。
        /// </summary>
        public bool IsValidHit { get; set; }

        /// <summary>
        /// 当前命中快照命中的 Thing。
        /// </summary>
        public Thing HitThing { get; set; }

        /// <summary>
        /// 当前命中快照落点所在的格子。
        /// </summary>
        public IntVec3 HitCell { get; set; }

        /// <summary>
        /// 当前命中快照是否强制视为地面命中。
        /// </summary>
        public bool ForceGround { get; set; }

        /// <summary>
        /// 当前命中快照附带的标签集合。
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
