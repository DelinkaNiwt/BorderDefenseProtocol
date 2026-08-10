using System.Collections.Generic;
using Verse;

namespace BDP.Content.CombatBody.Wounds.Visuals
{
    /// <summary>
    /// 管理 Active 期间所有伤口喷射器。
    /// 它只以伤口 drain 生命周期为输入，不自行判断 Trion 事实。
    /// </summary>
    internal sealed class CombatBodyWoundSprayRuntime
    {
        /// <summary>
        /// 同一 Pawn 最多同时保留的活跃喷射器数量。
        /// </summary>
        private const int MaxActiveEmitters = 12;

        /// <summary>
        /// 活跃喷射器集合，key 为 Hediff loadID。
        /// </summary>
        private Dictionary<int, CombatBodyWoundSprayEmitter> emitters =
            new Dictionary<int, CombatBodyWoundSprayEmitter>();

        /// <summary>
        /// 读档后恢复同一伤口切口倾斜角的轻量存档表。
        /// </summary>
        private Dictionary<int, float> cutTiltByHediffLoadId =
            new Dictionary<int, float>();

        /// <summary>
        /// Tick 遍历复用列表，避免遍历中集合变化。
        /// </summary>
        private List<CombatBodyWoundSprayEmitter> tmpEmitters =
            new List<CombatBodyWoundSprayEmitter>();

        /// <summary>
        /// 重建时记录当前仍活跃的 drain id。
        /// </summary>
        private HashSet<int> tmpActiveIds = new HashSet<int>();

        /// <summary>
        /// 清理 cutTilt 存档时复用的待删 id 列表。
        /// </summary>
        private List<int> tmpStaleIds = new List<int>();

        /// <summary>
        /// 保存可感知的视觉随机量。
        /// emitter 本身是运行时对象，读档后由 active drain 重建。
        /// </summary>
        internal void ExposeData()
        {
            Scribe_Collections.Look(
                ref cutTiltByHediffLoadId,
                "cutTiltByHediffLoadId",
                LookMode.Value,
                LookMode.Value);

            EnsureInternalState();
        }

        /// <summary>
        /// 响应伤口进入 drain 生命周期。
        /// </summary>
        internal void NotifyWoundAdded(Pawn pawn, Hediff hediff)
        {
            EnsureInternalState();
            if (pawn == null || hediff == null || hediff.loadID <= 0)
            {
                return;
            }

            if (emitters.TryGetValue(hediff.loadID, out CombatBodyWoundSprayEmitter existing))
            {
                cutTiltByHediffLoadId[hediff.loadID] = existing.CutTilt;
                existing.NotifyBurst();
                return;
            }

            if (emitters.Count >= MaxActiveEmitters)
            {
                return;
            }

            float? savedCutTilt = null;
            if (cutTiltByHediffLoadId.TryGetValue(hediff.loadID, out float cutTilt))
            {
                savedCutTilt = cutTilt;
            }

            CombatBodyWoundSprayEmitter emitter = new CombatBodyWoundSprayEmitter(hediff, savedCutTilt);
            emitters[hediff.loadID] = emitter;
            cutTiltByHediffLoadId[hediff.loadID] = emitter.CutTilt;
            emitter.NotifyBurst();
        }

        /// <summary>
        /// 响应指定伤口 drain 注销。
        /// </summary>
        internal void NotifyWoundDrainExpired(int hediffLoadId)
        {
            EnsureInternalState();
            if (hediffLoadId <= 0)
            {
                return;
            }

            emitters.Remove(hediffLoadId);
            cutTiltByHediffLoadId.Remove(hediffLoadId);
        }

        /// <summary>
        /// 清理当前 Active 派生出的全部喷射状态。
        /// </summary>
        internal void ClearAll()
        {
            EnsureInternalState();
            emitters.Clear();
            cutTiltByHediffLoadId.Clear();
            tmpEmitters.Clear();
            tmpActiveIds.Clear();
            tmpStaleIds.Clear();
        }

        /// <summary>
        /// 推进所有活跃喷射器。
        /// </summary>
        internal void Tick(Pawn pawn)
        {
            EnsureInternalState();
            if (emitters.Count == 0)
            {
                return;
            }

            tmpEmitters.Clear();
            foreach (CombatBodyWoundSprayEmitter emitter in emitters.Values)
            {
                tmpEmitters.Add(emitter);
            }

            for (int index = 0; index < tmpEmitters.Count; index++)
            {
                tmpEmitters[index].Tick(pawn);
            }

            tmpEmitters.Clear();
        }

        /// <summary>
        /// 读档后按仍然活跃的伤口 drain 重建喷射器。
        /// 重建不触发 burst，只恢复持续喷射。
        /// </summary>
        internal void RebuildFromActiveDrains(Pawn pawn, IEnumerable<int> activeHediffLoadIds)
        {
            EnsureInternalState();
            emitters.Clear();
            tmpActiveIds.Clear();

            if (pawn?.health?.hediffSet == null || activeHediffLoadIds == null)
            {
                RemoveInactiveCutTilts();
                return;
            }

            foreach (int hediffLoadId in activeHediffLoadIds)
            {
                if (hediffLoadId <= 0 || !tmpActiveIds.Add(hediffLoadId))
                {
                    continue;
                }

                Hediff hediff = FindWoundByLoadId(pawn, hediffLoadId);
                if (hediff == null || emitters.Count >= MaxActiveEmitters)
                {
                    continue;
                }

                float? savedCutTilt = null;
                if (cutTiltByHediffLoadId.TryGetValue(hediffLoadId, out float cutTilt))
                {
                    savedCutTilt = cutTilt;
                }

                CombatBodyWoundSprayEmitter emitter = new CombatBodyWoundSprayEmitter(hediff, savedCutTilt);
                emitters[hediffLoadId] = emitter;
                cutTiltByHediffLoadId[hediffLoadId] = emitter.CutTilt;
            }

            RemoveInactiveCutTilts();
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
                if (hediff != null && hediff.loadID == hediffLoadId)
                {
                    return hediff;
                }
            }

            return null;
        }

        /// <summary>
        /// 移除已经不在 active drain 集合内的切口倾斜角记录。
        /// </summary>
        private void RemoveInactiveCutTilts()
        {
            tmpStaleIds.Clear();
            foreach (int hediffLoadId in cutTiltByHediffLoadId.Keys)
            {
                if (!tmpActiveIds.Contains(hediffLoadId))
                {
                    tmpStaleIds.Add(hediffLoadId);
                }
            }

            for (int index = 0; index < tmpStaleIds.Count; index++)
            {
                cutTiltByHediffLoadId.Remove(tmpStaleIds[index]);
            }

            tmpStaleIds.Clear();
        }

        /// <summary>
        /// 补齐存档恢复后可能为空的运行容器。
        /// </summary>
        private void EnsureInternalState()
        {
            if (emitters == null)
            {
                emitters = new Dictionary<int, CombatBodyWoundSprayEmitter>();
            }

            if (cutTiltByHediffLoadId == null)
            {
                cutTiltByHediffLoadId = new Dictionary<int, float>();
            }

            if (tmpEmitters == null)
            {
                tmpEmitters = new List<CombatBodyWoundSprayEmitter>();
            }

            if (tmpActiveIds == null)
            {
                tmpActiveIds = new HashSet<int>();
            }

            if (tmpStaleIds == null)
            {
                tmpStaleIds = new List<int>();
            }
        }
    }
}
