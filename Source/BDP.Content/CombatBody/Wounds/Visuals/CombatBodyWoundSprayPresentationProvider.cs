using System.Collections.Generic;
using BDP.Core.CombatBody.Wounds.Presentation;
using Verse;

namespace BDP.Content.CombatBody.Wounds.Visuals
{
    /// <summary>
    /// 将伤口表现生命周期接到 Content 喷溅视觉实现。
    /// </summary>
    public sealed class CombatBodyWoundSprayPresentationProvider : ICombatBodyWoundPresentationProvider
    {
        /// <summary>
        /// 喷溅视觉运行时实例。
        /// </summary>
        private readonly CombatBodyWoundSprayRuntime runtime = new CombatBodyWoundSprayRuntime();

        /// <summary>
        /// 保存喷溅视觉需要的轻量状态。
        /// </summary>
        public void ExposeData()
        {
            runtime.ExposeData();
        }

        /// <summary>
        /// 清理当前战斗体派生出的喷溅视觉状态。
        /// </summary>
        public void ClearAll()
        {
            runtime.ClearAll();
        }

        /// <summary>
        /// 响应伤口进入有效 drain 生命周期。
        /// </summary>
        public void NotifyWoundAdded(Pawn pawn, Hediff hediff)
        {
            runtime.NotifyWoundAdded(pawn, hediff);
        }

        /// <summary>
        /// 响应伤口 drain 到期或注销。
        /// </summary>
        public void NotifyWoundDrainExpired(int hediffLoadId)
        {
            runtime.NotifyWoundDrainExpired(hediffLoadId);
        }

        /// <summary>
        /// 响应伤口从运行时移除。
        /// 喷溅实现沿用同一 loadID 清理路径。
        /// </summary>
        public void NotifyWoundRemoved(Pawn pawn, Hediff hediff)
        {
            runtime.NotifyWoundDrainExpired(hediff != null ? hediff.loadID : 0);
        }

        /// <summary>
        /// 按 Core 提供的有效伤口 ID 重建喷溅器。
        /// </summary>
        public void RebuildFromActiveDrains(Pawn pawn, IEnumerable<int> activeHediffLoadIds)
        {
            runtime.RebuildFromActiveDrains(pawn, activeHediffLoadIds);
        }

        /// <summary>
        /// 推进当前 Pawn 的喷溅视觉。
        /// </summary>
        public void Tick(Pawn pawn)
        {
            runtime.Tick(pawn);
        }
    }
}
