using System.Collections.Generic;
using BDP.Core.AttackExecution;
using RimWorld;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 手动入口到 Trigger 按钮宿主的内部桥。
    /// 它只负责让按钮宿主从正式手动入口投影取数，不负责重新决定哪些结果应该生成入口。
    /// </summary>
    internal static class ExpressionManualGizmoBridge
    {
        /// <summary>
        /// 当前默认使用的手动入口按钮解析器。
        /// </summary>
        private static readonly DefaultManualEntryGizmoResolver GizmoResolver = new DefaultManualEntryGizmoResolver();

        /// <summary>
        /// 为指定 Pawn 构建当前手动入口对应的按钮集合。
        /// </summary>
        public static IEnumerable<Gizmo> BuildGizmos(Pawn pawn)
        {
            if (pawn == null)
            {
                yield break;
            }

            if (AttackExecutionSurfaceAccess.ResolveEntry(pawn) == null)
            {
                yield break;
            }

            IExpressionReader reader = ExpressionSurfaceAccess.ResolveReader(pawn);
            if (reader == null)
            {
                yield break;
            }

            ManualEntryProjection projection = reader.GetManualProjection(pawn);
            if (projection == null || projection.Groups == null || projection.Groups.Count == 0)
            {
                yield break;
            }

            foreach (Gizmo gizmo in GizmoResolver.Resolve(pawn, projection))
            {
                if (gizmo != null)
                {
                    yield return gizmo;
                }
            }
        }
    }
}
