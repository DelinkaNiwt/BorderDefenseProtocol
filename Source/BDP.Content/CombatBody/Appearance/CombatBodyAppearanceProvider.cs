using BDP.Core.CombatBody.External;
using Verse;

namespace BDP.Content.CombatBody.Appearance
{
    /// <summary>
    /// 将 Content 个人服装账本接入 Core 前台外观注册表。
    /// </summary>
    public sealed class CombatBodyAppearanceProvider : ICombatBodyFrontAppearanceProvider
    {
        /// <summary>
        /// 尝试读取指定 Pawn 的个人外观计划。
        /// </summary>
        public bool TryResolve(Pawn pawn, out CombatBodyFrontAppearancePlan plan)
        {
            plan = null;
            if (pawn == null
                || !CombatBodyAppearanceLedgerService.TryGetProfile(
                    pawn,
                    out CombatBodyAppearanceProfile profile))
            {
                return false;
            }

            return CombatBodyAppearanceTemplateResolver.TryResolve(pawn, profile, out plan);
        }
    }
}
