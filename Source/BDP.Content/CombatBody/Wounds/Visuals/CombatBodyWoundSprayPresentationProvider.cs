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
        /// 按 Pawn 身份隔离的喷溅视觉运行时。
        /// </summary>
        private readonly Dictionary<int, CombatBodyWoundSprayRuntime> runtimesByPawnId =
            new Dictionary<int, CombatBodyWoundSprayRuntime>();

        /// <summary>
        /// 保存喷溅视觉需要的轻量状态。
        /// </summary>
        public void ExposeData(Pawn pawn)
        {
            ResolveRuntime(pawn, true)?.ExposeData();
        }

        /// <summary>
        /// 清理当前战斗体派生出的喷溅视觉状态。
        /// </summary>
        public void ClearAll(Pawn pawn)
        {
            if (pawn == null || !runtimesByPawnId.TryGetValue(pawn.thingIDNumber, out CombatBodyWoundSprayRuntime runtime))
            {
                return;
            }

            runtime.ClearAll();
            runtimesByPawnId.Remove(pawn.thingIDNumber);
        }

        /// <summary>
        /// 响应伤口进入有效 drain 生命周期。
        /// </summary>
        public void NotifyWoundAdded(Pawn pawn, Hediff hediff)
        {
            ResolveRuntime(pawn, true)?.NotifyWoundAdded(pawn, hediff);
        }

        /// <summary>
        /// 响应伤口 drain 到期或注销。
        /// </summary>
        public void NotifyWoundDrainExpired(Pawn pawn, int hediffLoadId)
        {
            ResolveRuntime(pawn, false)?.NotifyWoundDrainExpired(hediffLoadId);
        }

        /// <summary>
        /// 响应伤口从运行时移除。
        /// 喷溅实现沿用同一 loadID 清理路径。
        /// </summary>
        public void NotifyWoundRemoved(Pawn pawn, Hediff hediff)
        {
            ResolveRuntime(pawn, false)?.NotifyWoundDrainExpired(hediff != null ? hediff.loadID : 0);
        }

        /// <summary>
        /// 按 Core 提供的有效伤口 ID 重建喷溅器。
        /// </summary>
        public void RebuildFromActiveDrains(Pawn pawn, IEnumerable<int> activeHediffLoadIds)
        {
            ResolveRuntime(pawn, true)?.RebuildFromActiveDrains(pawn, activeHediffLoadIds);
        }

        /// <summary>
        /// 推进当前 Pawn 的喷溅视觉。
        /// </summary>
        public void Tick(Pawn pawn)
        {
            ResolveRuntime(pawn, false)?.Tick(pawn);
        }

        /// <summary>
        /// 按 Pawn 身份读取或创建独立喷溅运行时。
        /// </summary>
        private CombatBodyWoundSprayRuntime ResolveRuntime(Pawn pawn, bool createIfMissing)
        {
            if (pawn == null)
            {
                return null;
            }

            int pawnId = pawn.thingIDNumber;
            if (runtimesByPawnId.TryGetValue(pawnId, out CombatBodyWoundSprayRuntime runtime))
            {
                return runtime;
            }

            if (!createIfMissing)
            {
                return null;
            }

            runtime = new CombatBodyWoundSprayRuntime();
            runtimesByPawnId.Add(pawnId, runtime);
            return runtime;
        }
    }
}
