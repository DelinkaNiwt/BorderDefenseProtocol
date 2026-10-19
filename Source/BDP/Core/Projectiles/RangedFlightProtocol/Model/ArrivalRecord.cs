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
        /// 当前到达快照是否已经有前序模块独占下一段导航。
        /// </summary>
        public bool ExclusiveFlightControl { get; set; }

        /// <summary>是否继续下一段飞行。</summary>
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
        /// 下一段原版命中绑定目标。
        /// </summary>
        public LocalTargetInfo NextBindingTarget { get; set; }

        /// <summary>
        /// 当前到达快照是否由模块显式提交了下一段的 vanilla 命中绑定目标。
        /// 未显式提交时，宿主必须使用 NextTarget，不能继续继承上一段 CurrentTarget。
        /// </summary>
        public bool HasNextBindingTargetOverride { get; set; }

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
