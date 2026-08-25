using System.Collections.Generic;
using BDP.Core.Genes;
using BDP.Core.Trigger;
using RimWorld;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// CombatBody 挂到 Trigger 外部按钮口的正式 provider。
    /// 它只通过 CombatBody 正式 surface 构建玩家入口，不直接访问内部实现。
    /// </summary>
    public sealed class CombatBodyTriggerGizmoProvider : ITriggerExternalGizmoProvider
    {
        /// <summary>
        /// 战斗体按钮排序值：位于 Trion 资源面板之后、其它默认排序的 BDP 按钮之前。
        /// </summary>
        private const float GizmoOrder = -90f;

        /// <summary>
        /// 基于当前 Trigger 宿主上下文构建 CombatBody 按钮。
        /// </summary>
        public IEnumerable<Gizmo> BuildGizmos(TriggerExternalGizmoContext context)
        {
            Pawn pawn = context != null ? context.OwnerPawn : null;
            if (pawn == null)
            {
                yield break;
            }

            // 两个战斗体按钮只属于正式 Trion 使用者。
            // 这条资格判断必须留在 Core，避免由某个内容程序集决定通用入口是否出现。
            if (!TrionGlandEligibility.HasActiveTrionGland(pawn))
            {
                yield break;
            }

            ICombatBodyReader reader = CombatBodySurfaceAccess.ResolveReader(pawn);
            ICombatBodyCommands commands = CombatBodySurfaceAccess.ResolveCommands(pawn);
            if (reader == null || commands == null)
            {
                yield break;
            }

            Command_Action command = new Command_Action();
            command.Order = GizmoOrder;
            switch (reader.Phase)
            {
                case CombatBodyPhase.Inactive:
                    command.defaultLabel = "BDP_Command_CombatBody_Activate".Translate();
                    command.defaultDesc = "BDP_Command_CombatBody_ActivateDesc".Translate();
                    command.action = () => commands.TryActivate();
                    if (!reader.CanActivate())
                    {
                        command.Disable("BDP_Command_CombatBody_TransformLocked".Translate());
                    }
                    break;
                case CombatBodyPhase.Active:
                    command.defaultLabel = "BDP_Command_CombatBody_Release".Translate();
                    command.defaultDesc = "BDP_Command_CombatBody_ReleaseDesc".Translate();
                    command.action = commands.RequestRelease;
                    if (!reader.CanManualDeactivate())
                    {
                        command.Disable("BDP_Command_CombatBody_TransformLocked".Translate());
                    }
                    break;
                case CombatBodyPhase.Collapsing:
                    command.defaultLabel = "BDP_Command_CombatBody_Collapsing".Translate();
                    command.defaultDesc = "BDP_Command_CombatBody_CollapsingDesc".Translate();
                    command.Disable("BDP_Command_CombatBody_CollapsingDisabled".Translate());
                    break;
                case CombatBodyPhase.Cooldown:
                    command.defaultLabel = "BDP_Command_CombatBody_Cooldown".Translate();
                    command.defaultDesc = "BDP_Command_CombatBody_CooldownDesc".Translate();
                    command.Disable("BDP_Command_CombatBody_CooldownDisabled".Translate());
                    break;
                default:
                    command.defaultLabel = "BDP_Command_CombatBody_Generic".Translate();
                    command.defaultDesc = "BDP_Command_CombatBody_GenericDesc".Translate();
                    command.Disable("BDP_Command_CombatBody_GenericDisabled".Translate());
                    break;
            }

            yield return command;
        }
    }
}
