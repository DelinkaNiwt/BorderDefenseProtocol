using System.Collections.Generic;
using BDP.Core.Expressions;
using RimWorld;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 装备状态下的额外按钮构建服务。
    /// 它属于 Trigger 对外面的一部分，不再挂在投影目录里。
    /// 这里负责“把哪些按钮摆出来”，不负责正式执行起手。
    /// </summary>
    internal static class TriggerEquippedGizmoService
    {
        /// <summary>
        /// 构建当前装备状态下的全部额外按钮。
        /// Trigger 只负责把表达层和外部扩展按钮挂出来，正式攻击执行统一由 AttackExecution 承接。
        /// </summary>
        public static IEnumerable<Gizmo> BuildEquippedGizmos(
            ITriggerLoadoutReader loadoutReader,
            ITriggerLoadoutCommands loadoutCommands,
            Pawn ownerPawn)
        {
            if (ownerPawn == null)
            {
                yield break;
            }

            foreach (Gizmo gizmo in ExpressionManualGizmoBridge.BuildGizmos(ownerPawn))
            {
                yield return gizmo;
            }

            if (!TriggerExternalGizmoRegistry.HasProviders)
            {
                yield break;
            }

            foreach (Gizmo gizmo in TriggerExternalGizmoRegistry.BuildGizmos(new TriggerExternalGizmoContext
                     {
                         LoadoutReader = loadoutReader,
                         InteractionReader = TriggerSurfaceAccess.ResolveInteractionReader(ownerPawn),
                         LoadoutCommands = loadoutCommands,
                         OwnerPawn = ownerPawn
                     }))
            {
                yield return gizmo;
            }
        }
    }
}
