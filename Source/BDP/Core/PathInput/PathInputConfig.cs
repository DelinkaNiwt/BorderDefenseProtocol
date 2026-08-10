using System;
using Verse;

namespace BDP.Core.PathInput
{
    /// <summary>
    /// 路径输入中性配置。
    /// 只包含与"输入收集"相关的字段，不含任何射击或执行语义。
    /// 消费层（毒蛇模块 / 蚱蜢 Verb）各自持有实例并按需定制。
    /// </summary>
    public sealed class PathInputConfig
    {
        /// <summary>允许玩家追加的最大锚点数量（默认 8）。</summary>
        public int MaxAnchors = 8;

        /// <summary>是否允许把地面格作为最终目标（默认 true）。</summary>
        public bool AllowGroundFinal = true;

        /// <summary>是否允许把 Thing 作为最终目标（默认 true）。</summary>
        public bool AllowThingFinal = true;

        /// <summary>
        /// 锚点格合法性校验委托（可选）。
        /// 签名：(Map, IntVec3) → bool。
        /// 默认值：InBounds + Walkable。
        /// </summary>
        public Func<Map, IntVec3, bool> AnchorCellValidator { get; set; }

        /// <summary>
        /// 段通视校验委托（可选）。
        /// 签名：(Map, fromCell, toCell) → bool。
        /// 默认值：GenSight.LineOfSight。
        /// </summary>
        public Func<Map, IntVec3, IntVec3, bool> SegmentValidator { get; set; }

        /// <summary>
        /// 锚点追加前的自定义校验委托（可选）。
        /// 在 AnchorCellValidator + SegmentValidator 通过后、正式追加前调用。
        /// 返回 null 表示通过；返回非空字符串作为拒绝原因。
        /// 签名：(Map, candidateCell, currentState) → rejectReason or null。
        /// </summary>
        public Func<Map, IntVec3, PathInputState, string> AnchorAppendValidator { get; set; }

        /// <summary>
        /// 最终目标确认前的自定义校验委托（可选）。
        /// 返回 null 表示通过；返回非空字符串作为拒绝原因。
        /// 签名：(LocalTargetInfo, Pawn, PathInputState) → rejectReason or null。
        /// </summary>
        public Func<LocalTargetInfo, Pawn, PathInputState, string> FinalTargetValidator { get; set; }

        /// <summary>深度复制当前配置。</summary>
        public PathInputConfig CloneTyped()
        {
            return new PathInputConfig
            {
                MaxAnchors = MaxAnchors,
                AllowGroundFinal = AllowGroundFinal,
                AllowThingFinal = AllowThingFinal,
                AnchorCellValidator = AnchorCellValidator,
                SegmentValidator = SegmentValidator,
                AnchorAppendValidator = AnchorAppendValidator,
                FinalTargetValidator = FinalTargetValidator
            };
        }
    }
}
