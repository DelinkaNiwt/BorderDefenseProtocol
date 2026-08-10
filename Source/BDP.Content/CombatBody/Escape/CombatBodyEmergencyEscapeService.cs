using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Support.Diagnostics;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// 紧急脱离执行服务。
    /// 它只负责被动崩解中的紧急脱离附加阶段，不改变退出模式定义。
    /// </summary>
    internal sealed class CombatBodyEmergencyEscapeService
    {
        /// <summary>
        /// 在可用时执行紧急脱离。
        /// 它只在被动崩解退出链路的开头被调用。
        /// </summary>
        internal bool ExecuteEmergencyEscapeIfAvailable(Pawn pawn, CombatBodyEmergencyEscapeResolution resolution)
        {
            if (pawn == null || resolution == null || !resolution.IsAvailable || !pawn.Spawned)
            {
                return false;
            }

            Map map = pawn.Map;
            if (map == null)
            {
                return false;
            }

            IntVec3 origin = pawn.Position;
            IntVec3 destination = CombatBodyEmergencyEscapeRouter.FindEscapeDestination(pawn, map);
            if (!destination.IsValid)
            {
                return false;
            }

            CombatBodyEmergencyEscapeEffects.PlayEntryEffects(origin, map);
            pawn.Position = destination;
            pawn.Notify_Teleported(endCurrentJob: false, resetTweenedPos: true);
            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            pawn.Drawer?.renderer?.EnsureGraphicsInitialized();
            CombatBodyEmergencyEscapeEffects.PlayExitEffects(destination, map);

            TryConsumeSourceChips(pawn, resolution.SourceReferences);

            // 取消征召已移交崩解主链(CombatBodyExitTransaction),紧急脱离分支不再负责。
            Messages.Message(
                "BDP_Message_CombatBody_EmergencyEscapeExecuted".Translate(),
                pawn,
                MessageTypeDefOf.PositiveEvent,
                false);
            return true;
        }

        /// <summary>
        /// 尝试消费产生紧急脱离的全部来源芯片。
        /// </summary>
        private static void TryConsumeSourceChips(Pawn pawn, IReadOnlyList<ExpressionPublishedSourceReference> sourceReferences)
        {
            if (sourceReferences == null || sourceReferences.Count == 0)
            {
                BdpDiagnostics.Once("combatbody.emergency_escape.missing_source." + (pawn != null ? pawn.ThingID : "null"), "紧急脱离执行时缺少来源追踪，本次只执行传送，不消费来源芯片。");
                return;
            }

            for (int i = 0; i < sourceReferences.Count; i++)
            {
                TryConsumeSourceChip(pawn, sourceReferences[i]);
            }
        }

        /// <summary>
        /// 尝试消费单个来源芯片。
        /// </summary>
        private static void TryConsumeSourceChip(Pawn pawn, ExpressionPublishedSourceReference sourceReference)
        {
            if (pawn == null || sourceReference == null || string.IsNullOrWhiteSpace(sourceReference.ChipThingId))
            {
                BdpDiagnostics.Once("combatbody.emergency_escape.missing_source." + (pawn != null ? pawn.ThingID : "null"), "紧急脱离执行时缺少来源追踪，本次只执行传送，不消费来源芯片。");
                return;
            }

            ITriggerLoadoutCommands commands = TriggerSurfaceAccess.ResolveLoadoutCommands(pawn);
            if (commands == null)
            {
                BdpDiagnostics.Once("combatbody.emergency_escape.missing_commands." + pawn.ThingID, "紧急脱离执行时缺少 Trigger 正式命令面，本次只执行传送，不消费来源芯片。");
                return;
            }

            bool consumed = TriggerSurfaceAccess.ResolveLoadoutCommands(pawn)?.TryDestroyLoadedChip(
                sourceReference.Side,
                sourceReference.SlotIndex,
                sourceReference.ChipThingId) ?? false;
            if (!consumed)
            {
                BdpDiagnostics.Once("combatbody.emergency_escape.destroy_failed." + pawn.ThingID + "." + sourceReference.ChipThingId, "紧急脱离来源芯片消费失败。side=" + sourceReference.Side + ", index=" + sourceReference.SlotIndex + ", chipThingId=" + sourceReference.ChipThingId);
            }
        }
    }
}
