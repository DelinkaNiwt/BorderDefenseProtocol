using System.Collections.Generic;
using Verse;

namespace BDP.Core.CombatBody.Wounds.Presentation
{
    /// <summary>
    /// 战斗体伤口表现扩展提供器。
    /// Core 只通知伤口生命周期，不规定具体视觉或其它表现形式。
    /// </summary>
    public interface ICombatBodyWoundPresentationProvider
    {
        /// <summary>
        /// 保存当前 Pawn 对应的轻量表现状态。
        /// </summary>
        void ExposeData(Pawn pawn);

        /// <summary>
        /// 清除当前 Pawn 的全部战斗体派生表现运行时。
        /// </summary>
        void ClearAll(Pawn pawn);

        /// <summary>
        /// 通知一个伤口进入有效的伤口运行时生命周期。
        /// </summary>
        void NotifyWoundAdded(Pawn pawn, Hediff hediff);

        /// <summary>
        /// 通知一个伤口的派生 Trion 流失生命周期已经结束。
        /// </summary>
        void NotifyWoundDrainExpired(Pawn pawn, int hediffLoadId);

        /// <summary>
        /// 通知一个伤口从 Pawn 身上移除。
        /// </summary>
        void NotifyWoundRemoved(Pawn pawn, Hediff hediff);

        /// <summary>
        /// 读档后按 Core 提供的活跃伤口标识重建表现运行时。
        /// </summary>
        void RebuildFromActiveDrains(Pawn pawn, IEnumerable<int> activeHediffLoadIds);

        /// <summary>
        /// 推进当前 Pawn 的表现运行时。
        /// </summary>
        void Tick(Pawn pawn);
    }
}
