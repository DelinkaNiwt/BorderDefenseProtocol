using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 到达阶段的正式结果。
    /// 它回答当前 projectile 是继续飞、换目标，还是进入命中。
    /// </summary>
    internal sealed class ArrivalRecord
    {
        /// <summary>
        /// 当前到达快照是否要求进入下一段飞行。
        /// </summary>
        public bool ContinueFlight { get; set; }

        /// <summary>
        /// 当前到达快照裁定出的下一段目的地。
        /// </summary>
        public Vector3 NextDestination { get; set; }

        /// <summary>
        /// 当前到达快照保留的现场目标。
        /// </summary>
        public LocalTargetInfo CurrentTarget { get; set; }

        /// <summary>
        /// 当前到达快照裁定出的下一段正式目标。
        /// </summary>
        public LocalTargetInfo NextTarget { get; set; }

        /// <summary>
        /// 褰撳墠鍒拌揪蹇収瑁佸畾鍑虹殑涓嬩竴娈?vanilla 鍛戒腑缁戝畾鐩爣銆?
        /// </summary>
        public LocalTargetInfo NextBindingTarget { get; set; }

        /// <summary>
        /// 当前到达快照裁定出的下一段飞行路径快照。
        /// </summary>
        public ProjectileFlightPathSnapshot NextFlightPathSnapshot { get; set; }

        /// <summary>
        /// 当前到达快照附带的标签集合。
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
