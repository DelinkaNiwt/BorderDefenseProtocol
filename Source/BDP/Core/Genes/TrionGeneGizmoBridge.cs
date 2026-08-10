using System.Collections.Generic;
using BDP.Core.Trion;
using RimWorld;
using Verse;

namespace BDP.Core.Genes
{
    /// <summary>
    /// Gene 与 Trion 状态条之间的桥接层。
    /// 只负责从 Pawn 解析正式 Trion 读取口并生成 gizmo。
    /// </summary>
    public static class TrionGeneGizmoBridge
    {
        /// <summary>
        /// 为指定 Pawn 构建 Trion 相关 gizmo。
        /// </summary>
        public static IEnumerable<Gizmo> BuildGizmos(Pawn pawn)
        {
            if (pawn == null || pawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            ITrionReader reader = TrionSurfaceAccess.ResolveReader(pawn);
            if (reader == null || reader.Max <= 0f)
            {
                yield break;
            }

            yield return new Gizmo_TrionStatus(pawn, reader);

            if (!DebugSettings.godMode)
            {
                yield break;
            }

            ITrionCommands commands = TrionSurfaceAccess.ResolveCommands(pawn);
            if (commands == null)
            {
                yield break;
            }

            yield return CreateDebugAction("+50", "调试：Trion 当前值 +50。", () => commands.AdjustCurrent(50f), pawn);
            yield return CreateDebugAction("-50", "调试：Trion 当前值 -50。", () => commands.AdjustCurrent(-50f), pawn);
            yield return CreateDebugAction("MAX", "调试：Trion 当前值设为上限。", () => commands.TrySetCurrent(reader.Max), pawn);
            yield return CreateDebugAction("0", "调试：Trion 当前值设为 0。", () => commands.TrySetCurrent(0f), pawn);
        }

        private static Gizmo CreateDebugAction(string label, string description, System.Func<TrionCurrentWriteResult> action, Pawn pawn)
        {
            return new Command_Action
            {
                defaultLabel = label,
                defaultDesc = description,
                action = delegate
                {
                    TrionCurrentWriteResult result = action != null ? action() : null;
                    if (result != null && !string.IsNullOrWhiteSpace(result.Message))
                    {
                        Messages.Message(result.Message, pawn, MessageTypeDefOf.RejectInput, false);
                    }
                }
            };
        }
    }
}
