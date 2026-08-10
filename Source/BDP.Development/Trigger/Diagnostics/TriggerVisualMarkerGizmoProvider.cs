using System.Collections.Generic;
using BDP.Core.Trigger;
using RimWorld;
using Verse;

namespace BDP.Development.Trigger.Diagnostics
{
    /// <summary>
    /// 为手持触发体的角色提供地图画点诊断开关。
    /// 这是只读现场诊断，不参与触发体或战斗体的正式状态。
    /// </summary>
    public sealed class TriggerVisualMarkerGizmoProvider : ITriggerExternalGizmoProvider
    {
        /// <summary>
        /// 仅在上帝模式下构建画点诊断按钮。
        /// </summary>
        public IEnumerable<Gizmo> BuildGizmos(TriggerExternalGizmoContext context)
        {
            if (context?.OwnerPawn == null || !DebugSettings.godMode)
            {
                yield break;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "BDP_Command_TriggerDiagnostics_DrawMarkers".Translate(),
                defaultDesc = "BDP_Command_TriggerDiagnostics_DrawMarkersDesc".Translate(),
                isActive = () => TriggerVisualMarkerSettings.OverlayEnabled,
                toggleAction = delegate
                {
                    TriggerVisualMarkerSettings.OverlayEnabled =
                        !TriggerVisualMarkerSettings.OverlayEnabled;
                    Messages.Message(
                        TriggerVisualMarkerSettings.OverlayEnabled
                            ? "BDP_Command_TriggerDiagnostics_Enabled".Translate()
                            : "BDP_Command_TriggerDiagnostics_Disabled".Translate(),
                        MessageTypeDefOf.NeutralEvent,
                        false);
                }
            };
        }
    }
}
