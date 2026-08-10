using System.Collections.Generic;
using BDP.Core.Trion.External;
using UnityEngine;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// CombatBody 对 Trion 状态卡的扩展徽标提供器。
    /// 只通过 CombatBody 正式 surface 读取状态，不反向侵入 Trion 本体。
    /// </summary>
    public sealed class CombatBodyTrionGizmoExtensionProvider : ITrionGizmoExtensionProvider
    {
        /// <summary>
        /// 未激活态徽标颜色。
        /// </summary>
        private static readonly Color InactiveTint = new Color(0.62f, 0.66f, 0.73f);

        /// <summary>
        /// 激活态徽标颜色。
        /// </summary>
        private static readonly Color ActiveTint = new Color(0.38f, 0.92f, 1f);

        /// <summary>
        /// 崩解态徽标颜色。
        /// </summary>
        private static readonly Color CollapsingTint = new Color(1f, 0.66f, 0.28f);

        /// <summary>
        /// 冷却态徽标颜色。
        /// </summary>
        private static readonly Color CooldownTint = new Color(0.75f, 0.78f, 0.84f);

        /// <summary>
        /// 获取当前上下文需要显示的 CombatBody 徽标。
        /// 统一使用同一图形，以颜色高亮/灰暗表达状态切换。
        /// </summary>
        public IEnumerable<TrionGizmoExtensionBadge> GetBadges(TrionGizmoExtensionContext context)
        {
            Pawn pawn = context != null ? context.Owner as Pawn : null;
            if (pawn == null)
            {
                yield break;
            }

            ICombatBodyReader reader = CombatBodySurfaceAccess.ResolveReader(pawn);
            if (reader == null)
            {
                yield break;
            }

            switch (reader.Phase)
            {
                case CombatBodyPhase.Inactive:
                    yield return new TrionGizmoExtensionBadge(
                        icon: null,
                        tooltip: "BDP_Status_CombatBody_Inactive".Translate(),
                        tint: InactiveTint,
                        glyphKey: "combatbody");
                    break;

                case CombatBodyPhase.Active:
                    yield return new TrionGizmoExtensionBadge(
                        icon: null,
                        tooltip: "BDP_Status_CombatBody_Active".Translate(),
                        tint: ActiveTint,
                        glyphKey: "combatbody");
                    break;

                case CombatBodyPhase.Collapsing:
                    yield return new TrionGizmoExtensionBadge(
                        icon: null,
                        tooltip: BuildCollapsingTooltip(reader),
                        tint: CollapsingTint,
                        glyphKey: "combatbody");
                    break;

                case CombatBodyPhase.Cooldown:
                    yield return new TrionGizmoExtensionBadge(
                        icon: null,
                        tooltip: BuildCooldownTooltip(reader),
                        tint: CooldownTint,
                        glyphKey: "combatbody");
                    break;

                default:
                    yield break;
            }

        }

        /// <summary>
        /// 构建崩解态提示文本。
        /// </summary>
        private static string BuildCollapsingTooltip(ICombatBodyReader reader)
        {
            int remainingTicks = reader != null ? reader.GetCollapseRemaining() : 0;
            string reasonText = reader != null && !string.IsNullOrEmpty(reader.CollapseReason)
                ? "BDP_Status_CombatBody_Reason".Translate(
                    CombatBodyCollapseReasonPresenter.Describe(reader.CollapseReason)).ToString()
                : string.Empty;
            return "BDP_Status_CombatBody_Collapsing".Translate(
                FormatTicksAsSeconds(remainingTicks),
                reasonText);
        }

        /// <summary>
        /// 构建冷却态提示文本。
        /// </summary>
        private static string BuildCooldownTooltip(ICombatBodyReader reader)
        {
            int remainingTicks = reader != null ? reader.GetCooldownRemaining() : 0;
            return "BDP_Status_CombatBody_Cooldown".Translate(
                FormatTicksAsSeconds(remainingTicks));
        }

        /// <summary>
        /// 把 tick 转成秒显示。
        /// </summary>
        private static string FormatTicksAsSeconds(int ticks)
        {
            return "BDP_Tick_Seconds".Translate(
                (Mathf.Max(0, ticks) / 60f).ToString("F1"));
        }
    }
}
