using System.Collections.Generic;
using Verse;

namespace BDP.Core.PathInput
{
    /// <summary>
    /// 路径输入状态 — 目标选择期间玩家累积的锚点链与最终目标。
    /// 它是纯数据容器，不包含任何输入处理逻辑。
    /// 支持 IExposable 用于存档读写。
    /// </summary>
    public class PathInputState : IExposable
    {
        /// <summary>当前已追加的锚点列表（有序，先加的在前）。</summary>
        public List<PathAnchor> Anchors { get; set; } = new List<PathAnchor>();

        /// <summary>是否已经记录了最终目标。</summary>
        public bool HasFinalTarget { get; set; }

        /// <summary>当前记录的最终目标。</summary>
        public LocalTargetInfo FinalTarget { get; set; }

        /// <summary>当前最终目标是否是 Thing（非地面格）。</summary>
        public bool FinalIsThing { get; set; }

        /// <summary>
        /// 获取完整路径点序列（锚点 + 最终目标）。
        /// 仅在 HasFinalTarget 时有效，否则返回锚点列表的副本。
        /// </summary>
        public List<PathAnchor> GetAllWaypoints()
        {
            List<PathAnchor> waypoints = new List<PathAnchor>();
            for (int i = 0; i < Anchors.Count; i++)
            {
                if (Anchors[i] != null)
                {
                    waypoints.Add(Anchors[i].CloneTyped());
                }
            }
            if (HasFinalTarget && FinalTarget.IsValid)
            {
                waypoints.Add(PathAnchor.FromCell(FinalTarget.Cell));
            }
            return waypoints;
        }

        /// <summary>清空当前输入状态。</summary>
        public void Reset()
        {
            Anchors.Clear();
            HasFinalTarget = false;
            FinalTarget = LocalTargetInfo.Invalid;
            FinalIsThing = false;
        }

        /// <summary>深度复制当前输入状态。</summary>
        public PathInputState CloneTyped()
        {
            PathInputState clone = new PathInputState
            {
                HasFinalTarget = HasFinalTarget,
                FinalTarget = FinalTarget,
                FinalIsThing = FinalIsThing
            };

            for (int i = 0; i < Anchors.Count; i++)
            {
                if (Anchors[i] != null)
                {
                    clone.Anchors.Add(Anchors[i].CloneTyped());
                }
            }

            return clone;
        }

        /// <summary>存档序列化。</summary>
        public void ExposeData()
        {
            List<PathAnchor> anchors = Anchors;
            bool hasFinalTarget = HasFinalTarget;
            LocalTargetInfo finalTarget = FinalTarget;
            bool finalIsThing = FinalIsThing;

            Scribe_Collections.Look(ref anchors, "anchors", LookMode.Deep);
            Scribe_Values.Look(ref hasFinalTarget, "hasFinalTarget", false);
            Scribe_TargetInfo.Look(ref finalTarget, "finalTarget");
            Scribe_Values.Look(ref finalIsThing, "finalIsThing", false);

            Anchors = anchors ?? new List<PathAnchor>();
            HasFinalTarget = hasFinalTarget;
            FinalTarget = finalTarget;
            FinalIsThing = finalIsThing;
        }
    }
}
