using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体系统与 RimWorld 宿主之间的桥。
    /// 第一版只保留最小宿主读写口。
    /// 它只服务 CombatBody 内部主链，不是对外正式开放面。
    /// </summary>
    internal interface ICombatBodyHost
    {
        /// <summary>
        /// 当前战斗体所属的 Pawn 宿主。
        /// </summary>
        Pawn Pawn { get; }

        /// <summary>
        /// 把宿主切到战斗体状态时应执行的宿主侧动作。
        /// </summary>
        void ApplyCombatBodyTransformation();

        /// <summary>
        /// 把宿主从战斗体状态恢复回来时应执行的宿主侧动作。
        /// </summary>
        void RestoreFromCombatBody();
    }
}
