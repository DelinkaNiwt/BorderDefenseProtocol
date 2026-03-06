using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 路径点构建工具——纯计算，无副作用，不依赖任何模块或宿主。
    /// v5解耦：从GuidedModule.BuildWaypoints搬出，供WaypointBuilder独立调用。
    /// </summary>
    public static class WaypointBuilder
    {
        /// <summary>
        /// 构建路径点列表：IntVec3锚点 → Vector3路径点，应用递增散布偏移。
        /// </summary>
        /// <param name="anchors">锚点坐标列表。</param>
        /// <param name="finalTarget">最终目标。</param>
        /// <param name="anchorSpread">散布半径。</param>
        /// <returns>路径点列表（含最终目标点）。</returns>
        public static List<Vector3> BuildWaypoints(
            List<IntVec3> anchors, LocalTargetInfo finalTarget, float anchorSpread)
        {
            var waypoints = new List<Vector3>();
            int totalSegments = anchors.Count + 1;

            for (int i = 0; i < anchors.Count; i++)
            {
                Vector3 basePos = anchors[i].ToVector3Shifted();
                if (anchorSpread > 0f)
                {
                    float clampedSpread = Mathf.Min(anchorSpread, 0.45f);
                    float factor = (float)(i + 1) / totalSegments;
                    Vector2 offset = Random.insideUnitCircle * clampedSpread * factor;
                    basePos += new Vector3(offset.x, 0f, offset.y);
                }
                waypoints.Add(basePos);
            }

            Vector3 finalPos = finalTarget.Cell.ToVector3Shifted();
            if (anchorSpread > 0f)
            {
                float clampedSpread = Mathf.Min(anchorSpread, 0.45f);
                Vector2 finalOffset = Random.insideUnitCircle * clampedSpread;
                finalPos += new Vector3(finalOffset.x, 0f, finalOffset.y);
            }
            waypoints.Add(finalPos);
            return waypoints;
        }
    }
}
