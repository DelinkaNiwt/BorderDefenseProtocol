using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Content.Chameleon
{
    /// <summary>
    /// 变色龙攻击后关断服务。
    /// 它只通过 Core 已公开的表达与 Trigger 表面定位来源，不触碰战斗体 Trion 账本。
    /// </summary>
    public static class ChameleonAttackShutdownService
    {
        /// <summary>
        /// 变色龙表达使用的 Hediff DefName（定义名）。
        /// </summary>
        private const string ChameleonHediffDefName = "BDP_Hediff_Chameleon";

        /// <summary>
        /// 立即请求关闭承载当前变色龙表达的主槽或副槽。
        /// 由于该芯片 Def 声明停用延迟为 0 tick，这条正式请求会在当前调用中完成。
        /// </summary>
        public static bool TryDeactivateImmediately(AttackActionSuccess attack)
        {
            return attack != null && TryDeactivateImmediately(attack.Pawn);
        }

        /// <summary>
        /// 按攻击来源 Pawn 定位变色龙表达并立即发出停用请求。
        /// </summary>
        private static bool TryDeactivateImmediately(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            ExpressionPublishedProjectionSnapshot projection =
                ExpressionSurfaceAccess.ResolvePublishedProjection(pawn);
            IReadOnlyList<ExpressionPublishedResultSnapshot> results =
                ResolveChameleonResults(projection);
            if (results == null || results.Count == 0)
            {
                return false;
            }

            ITriggerLoadoutReader reader = TriggerSurfaceAccess.ResolveLoadoutReader(pawn);
            ITriggerLoadoutCommands commands = TriggerSurfaceAccess.ResolveLoadoutCommands(pawn);
            if (reader == null || commands == null)
            {
                return false;
            }

            for (int index = 0; index < results.Count; index++)
            {
                ExpressionPublishedResultSnapshot result = results[index];
                ExpressionPublishedSourceReference source = result != null
                    ? result.SourceReference
                    : null;
                if (source == null || string.IsNullOrWhiteSpace(source.ChipThingId))
                {
                    continue;
                }

                ITriggerSlotState slot = reader.GetActiveSlot(source.Side);
                if (slot == null
                    || !slot.IsActive
                    || slot.LoadedChip == null
                    || slot.LoadedChip.ThingID != source.ChipThingId)
                {
                    continue;
                }

                return commands.RequestDeactivate(source.Side);
            }

            return false;
        }

        /// <summary>
        /// 从公开投影中筛选变色龙 Hediff 来源。
        /// </summary>
        private static IReadOnlyList<ExpressionPublishedResultSnapshot> ResolveChameleonResults(
            ExpressionPublishedProjectionSnapshot projection)
        {
            if (projection == null || projection.HediffResultsByDefName == null)
            {
                return null;
            }

            IReadOnlyList<ExpressionPublishedResultSnapshot> results;
            return projection.HediffResultsByDefName.TryGetValue(
                ChameleonHediffDefName,
                out results)
                ? results
                : null;
        }
    }
}
