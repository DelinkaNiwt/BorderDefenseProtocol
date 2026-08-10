using System.Collections.Generic;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 瞄准阶段的正式结果。
    /// 它只记录当前这一枪已经成立的瞄准事实。
    /// </summary>
    internal sealed class AimRecord
    {
        /// <summary>
        /// 当前 Aim 阶段是否已请求中止。
        /// </summary>
        public bool IsAborted { get; set; }

        /// <summary>
        /// 当前 Aim 阶段中止时写回的原因。
        /// </summary>
        public string AbortReason { get; set; }

        /// <summary>
        /// 当前进入 Aim 前的原始目标。
        /// </summary>
        public LocalTargetInfo OriginalTarget { get; set; }

        /// <summary>
        /// 当前 Aim 阶段裁定后的正式目标。
        /// </summary>
        public LocalTargetInfo FinalTarget { get; set; }

        /// <summary>
        /// 当前 Aim 阶段累乘后的命中倍率。
        /// </summary>
        public float AccuracyFactor { get; set; }

        /// <summary>
        /// 当前 Aim 阶段裁定出的强制失准半径。
        /// </summary>
        public float ForcedMissRadius { get; set; }

        /// <summary>
        /// 当前 Aim 阶段附带的标签集合。
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
    }
}
