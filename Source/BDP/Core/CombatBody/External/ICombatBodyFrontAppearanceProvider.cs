using Verse;

namespace BDP.Core.CombatBody.External
{
    /// <summary>
    /// 按 Pawn 解析战斗体前台外观的中性提供器。
    /// </summary>
    public interface ICombatBodyFrontAppearanceProvider
    {
        /// <summary>
        /// 尝试解析指定 Pawn 的个人前台外观计划。
        /// </summary>
        bool TryResolve(Pawn pawn, out CombatBodyFrontAppearancePlan plan);
    }
}
