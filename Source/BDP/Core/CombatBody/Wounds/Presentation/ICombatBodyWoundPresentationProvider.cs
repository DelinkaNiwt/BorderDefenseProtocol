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
        /// 保存提供器自己的轻量表现状态。
        /// </summary>
        void ExposeData();

        /// <summary>
        /// 清除当前战斗体派生出的全部表现运行时。
        /// </summary>
        void ClearAll();

        /// <summary>
        /// 通知一个伤口进入有效的伤口运行时生命周期。
        /// </summary>
        void NotifyWoundAdded(Pawn pawn, Hediff hediff);

        /// <summary>
        /// 通知一个伤口的派生 Trion 流失生命周期已经结束。
        /// </summary>
        void NotifyWoundDrainExpired(int hediffLoadId);

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
