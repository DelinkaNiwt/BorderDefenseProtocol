using System;
using System.Collections.Generic;
using BDP.Core.Trion;
using Verse;

namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>
    /// 战斗体伤口 Trion 持续消耗绑定。
    /// 它只负责把伤口策略结果发布到 Trion 账本，不参与资源结算。
    /// </summary>
    internal sealed class CombatBodyWoundTrionBinding
    {
        /// <summary>
        /// 当前运行期已经发布过的伤口 drain。
        /// </summary>
        private Dictionary<int, TrionDrainKey> publishedKeysByHediffLoadId =
            new Dictionary<int, TrionDrainKey>();

        /// <summary>
        /// 当前运行期每个伤口 drain 的到期 tick。
        /// 伤口没有继续发生变化时，到期后自动注销。
        /// </summary>
        private Dictionary<int, int> expiryTickByHediffLoadId =
            new Dictionary<int, int>();

        /// <summary>
        /// 过期检查复用列表，避免每次低频检查都分配新列表。
        /// </summary>
        private List<int> expiredHediffLoadIds = new List<int>();

        /// <summary>
        /// 保存仍在生效的伤口 drain 到期 tick。
        /// 已发布 key 可由 live Hediff 重建，避免把同一事实存两份。
        /// </summary>
        internal void ExposeData()
        {
            Scribe_Collections.Look(
                ref expiryTickByHediffLoadId,
                "expiryTickByHediffLoadId",
                LookMode.Value,
                LookMode.Value);

            EnsureInternalState();
        }

        /// <summary>
        /// 按当前伤口状态注册、更新或注销 drain。
        /// 每次明确伤口变化都会刷新该伤口的流失到期时间。
        /// </summary>
        internal int UpdateWoundDrain(Pawn pawn, Hediff hediff, int currentTick, int idleTimeoutTicks)
        {
            EnsureInternalState();

            if (pawn == null || hediff == null || !CombatBodyWoundPolicy.IsSupportedWound(hediff))
            {
                return 0;
            }

            if (!CombatBodyWoundTrionDrainUtility.TryResolveDrainPerSecond(hediff, out float drainPerSecond))
            {
                RemoveWoundDrain(pawn, hediff);
                return 0;
            }

            ITrionCommands commands = TrionSurfaceAccess.ResolveCommands(pawn);
            if (commands == null || hediff.loadID <= 0)
            {
                return 0;
            }

            TrionDrainKey key = BuildDrainKey(hediff);
            int expiryTick = currentTick + Math.Max(1, idleTimeoutTicks);
            commands.RegisterDrain(key, drainPerSecond);
            publishedKeysByHediffLoadId[hediff.loadID] = key;
            expiryTickByHediffLoadId[hediff.loadID] = expiryTick;
            return expiryTick;
        }

        /// <summary>
        /// 注销所有已经超过空闲时间的伤口 drain。
        /// 这一步只处理 BDP 派生 drain，不会扫描或改写原版伤口事实。
        /// 返回仍在生效的最早到期 tick；没有活跃 drain 时返回 0。
        /// </summary>
        internal int ExpireIdleDrains(Pawn pawn, int currentTick, List<int> expiredIdsOut)
        {
            EnsureInternalState();
            expiredIdsOut?.Clear();

            if (expiryTickByHediffLoadId.Count == 0)
            {
                return 0;
            }

            expiredHediffLoadIds.Clear();
            foreach (KeyValuePair<int, int> pair in expiryTickByHediffLoadId)
            {
                if (currentTick >= pair.Value)
                {
                    expiredHediffLoadIds.Add(pair.Key);
                }
            }

            if (expiredHediffLoadIds.Count == 0)
            {
                return ResolveNextExpiryTick();
            }

            ITrionCommands commands = TrionSurfaceAccess.ResolveCommands(pawn);
            for (int index = 0; index < expiredHediffLoadIds.Count; index++)
            {
                int hediffLoadId = expiredHediffLoadIds[index];
                if (commands != null && publishedKeysByHediffLoadId.TryGetValue(hediffLoadId, out TrionDrainKey key))
                {
                    commands.UnregisterDrain(key);
                }

                expiredIdsOut?.Add(hediffLoadId);
                publishedKeysByHediffLoadId.Remove(hediffLoadId);
                expiryTickByHediffLoadId.Remove(hediffLoadId);
            }

            expiredHediffLoadIds.Clear();
            return ResolveNextExpiryTick();
        }

        /// <summary>
        /// 注销指定伤口 drain。
        /// </summary>
        internal void RemoveWoundDrain(Pawn pawn, Hediff hediff)
        {
            EnsureInternalState();

            if (pawn == null || hediff == null || hediff.loadID <= 0)
            {
                return;
            }

            if (!publishedKeysByHediffLoadId.TryGetValue(hediff.loadID, out TrionDrainKey key))
            {
                key = BuildDrainKey(hediff);
            }

            TrionSurfaceAccess.ResolveCommands(pawn)?.UnregisterDrain(key);
            publishedKeysByHediffLoadId.Remove(hediff.loadID);
            expiryTickByHediffLoadId.Remove(hediff.loadID);
        }

        /// <summary>
        /// 清理当前运行期所有战斗体伤口 drain。
        /// </summary>
        internal void ClearAll(Pawn pawn)
        {
            EnsureInternalState();

            ITrionCommands commands = TrionSurfaceAccess.ResolveCommands(pawn);
            if (commands != null)
            {
                foreach (TrionDrainKey key in publishedKeysByHediffLoadId.Values)
                {
                    commands.UnregisterDrain(key);
                }
            }

            publishedKeysByHediffLoadId.Clear();
            expiryTickByHediffLoadId.Clear();
            expiredHediffLoadIds.Clear();
        }

        /// <summary>
        /// 读档后把已存的伤口 drain 重新发布到 Trion 账本。
        /// 返回当前仍在生效的最早到期 tick；没有活跃 drain 时返回 0。
        /// </summary>
        internal int RestoreAfterLoad(Pawn pawn, int currentTick)
        {
            EnsureInternalState();
            publishedKeysByHediffLoadId.Clear();

            if (pawn?.health?.hediffSet == null || expiryTickByHediffLoadId.Count == 0)
            {
                return 0;
            }

            expiredHediffLoadIds.Clear();
            foreach (KeyValuePair<int, int> pair in expiryTickByHediffLoadId)
            {
                if (currentTick >= pair.Value)
                {
                    expiredHediffLoadIds.Add(pair.Key);
                    continue;
                }

                Hediff hediff = FindWoundByLoadId(pawn, pair.Key);
                if (hediff == null || !CombatBodyWoundTrionDrainUtility.TryResolveDrainPerSecond(hediff, out float drainPerSecond))
                {
                    expiredHediffLoadIds.Add(pair.Key);
                    continue;
                }

                TrionDrainKey key = BuildDrainKey(hediff);
                TrionSurfaceAccess.ResolveCommands(pawn)?.RegisterDrain(key, drainPerSecond);
                publishedKeysByHediffLoadId[pair.Key] = key;
            }

            for (int index = 0; index < expiredHediffLoadIds.Count; index++)
            {
                expiryTickByHediffLoadId.Remove(expiredHediffLoadIds[index]);
            }

            expiredHediffLoadIds.Clear();
            return ResolveNextExpiryTick();
        }

        /// <summary>
        /// 返回当前仍由伤口 drain 生命周期托管的 Hediff loadID。
        /// 只用于读档后重建视觉运行时，不对外暴露可写集合。
        /// </summary>
        internal IEnumerable<int> GetActiveHediffLoadIds()
        {
            EnsureInternalState();
            return expiryTickByHediffLoadId.Keys;
        }

        /// <summary>
        /// 为伤口实例生成稳定 drain 键。
        /// </summary>
        private static TrionDrainKey BuildDrainKey(Hediff hediff)
        {
            return CombatBodyWoundTrionDrainUtility.BuildDrainKey(hediff);
        }

        /// <summary>
        /// 按 Hediff loadID 找回仍存在的原版伤口。
        /// </summary>
        private static Hediff FindWoundByLoadId(Pawn pawn, int hediffLoadId)
        {
            if (pawn?.health?.hediffSet?.hediffs == null || hediffLoadId <= 0)
            {
                return null;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int index = 0; index < hediffs.Count; index++)
            {
                Hediff hediff = hediffs[index];
                if (hediff != null && hediff.loadID == hediffLoadId && CombatBodyWoundPolicy.IsSupportedWound(hediff))
                {
                    return hediff;
                }
            }

            return null;
        }

        /// <summary>
        /// 解析当前仍在生效的最早 drain 到期 tick。
        /// </summary>
        private int ResolveNextExpiryTick()
        {
            EnsureInternalState();

            int nextExpiryTick = 0;
            foreach (int expiryTick in expiryTickByHediffLoadId.Values)
            {
                if (nextExpiryTick == 0 || expiryTick < nextExpiryTick)
                {
                    nextExpiryTick = expiryTick;
                }
            }

            return nextExpiryTick;
        }

        /// <summary>
        /// 补齐存档恢复后可能为空的运行容器。
        /// </summary>
        private void EnsureInternalState()
        {
            if (publishedKeysByHediffLoadId == null)
            {
                publishedKeysByHediffLoadId = new Dictionary<int, TrionDrainKey>();
            }

            if (expiryTickByHediffLoadId == null)
            {
                expiryTickByHediffLoadId = new Dictionary<int, int>();
            }

            if (expiredHediffLoadIds == null)
            {
                expiredHediffLoadIds = new List<int>();
            }
        }
    }
}
