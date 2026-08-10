using System.Collections.Generic;
using BDP.Core.CombatBody;
using BDP.Core.Trion.External;
using UnityEngine;
using Verse;

namespace BDP.Content.CombatBody.Escape
{
    /// <summary>
    /// 紧急脱离 Trion 徽标 Content 提供器。
    /// </summary>
    public sealed class CombatBodyEmergencyEscapeGizmoExtensionProvider : ITrionGizmoExtensionProvider
    {
        /// <summary>
        /// 紧急脱离三态解析器。
        /// </summary>
        private static readonly CombatBodyEmergencyEscapeBadgeStateResolver StateResolver =
            new CombatBodyEmergencyEscapeBadgeStateResolver(new CombatBodyEmergencyEscapeResolver());

        /// <summary>
        /// 紧急脱离状态贴图。
        /// </summary>
        private static readonly Texture2D EmergencyEscapeIcon =
            ContentFinder<Texture2D>.Get("UI/CombatBody/EmergencyEscapeStatus");

        /// <summary>
        /// 已搭载但未就绪的灰暗色。
        /// </summary>
        private static readonly Color InactiveTint = new Color(0.45f, 0.50f, 0.58f);

        /// <summary>
        /// 已就绪的高亮色。
        /// </summary>
        private static readonly Color ReadyTint = new Color(0.30f, 1f, 0.78f);

        /// <summary>
        /// 返回当前紧急脱离徽标。
        /// </summary>
        public IEnumerable<TrionGizmoExtensionBadge> GetBadges(TrionGizmoExtensionContext context)
        {
            Pawn pawn = context?.Owner as Pawn;
            if (pawn == null)
            {
                yield break;
            }

            ICombatBodyReader combatBodyReader = CombatBodySurfaceAccess.ResolveReader(pawn);
            CombatBodyEmergencyEscapeBadgeState state = StateResolver.Resolve(pawn, combatBodyReader);
            switch (state)
            {
                case CombatBodyEmergencyEscapeBadgeState.InstalledNotReady:
                    yield return new TrionGizmoExtensionBadge(
                        icon: EmergencyEscapeIcon,
                        tooltip: "BDP_Status_EmergencyEscape_NotReady".Translate(),
                        tint: InactiveTint);
                    break;

                case CombatBodyEmergencyEscapeBadgeState.Ready:
                    yield return new TrionGizmoExtensionBadge(
                        icon: EmergencyEscapeIcon,
                        tooltip: "BDP_Status_EmergencyEscape_Ready".Translate(),
                        tint: ReadyTint);
                    break;
            }
        }
    }
}
