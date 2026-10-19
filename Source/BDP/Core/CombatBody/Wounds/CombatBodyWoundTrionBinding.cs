using System;
using System.Collections.Generic;
using BDP.Core.Trion;
using Verse;

namespace BDP.Core.CombatBody.Wounds
{
    /// <summary>每条最终伤口发布一条持续消耗，扣费由资源账本统一处理。</summary>
    internal sealed class CombatBodyWoundTrionBinding
    {
        /// <summary>独立伤口的速率与期限，不迁移旧版部位派生登记。</summary>
        private List<CombatBodyWoundDrain> woundDrains = new List<CombatBodyWoundDrain>();
        /// <summary>注销使用实际发布键，避免标签变化后遗留来源。</summary>
        private readonly Dictionary<Hediff, TrionDrainKey> publishedKeys = new Dictionary<Hediff, TrionDrainKey>();

        /// <summary>只保存派生登记，原版健康系统仍保存伤口本体。</summary>
        internal void ExposeData()
        {
            Scribe_Collections.Look(ref woundDrains, "woundDrains", LookMode.Deep);
            EnsureInternalState();
        }

        /// <summary>只累加本次有效损伤；已过期来源重新开始，断口不重复累加。</summary>
        internal int UpdateWoundDrain(Pawn pawn, Hediff wound, float effectiveDamage, int currentTick, int idleTimeoutTicks)
        {
            // 本刻受伤可能早于宿主Tick（逐刻更新）；先收掉到期来源，表现代表不能继续选旧伤。
            ExpireIdleDrains(pawn, currentTick, null);
            ReconcileActiveWounds(pawn);
            if (wound?.pawn != pawn
                || !CombatBodyWoundTrionDrainUtility.TryResolveWoundDrainPerSecond(wound, effectiveDamage, out float rate)) return 0;
            ITrionCommands commands = TrionSurfaceAccess.ResolveCommands(pawn);
            if (commands == null) return 0;
            CombatBodyWoundDrain drain = FindDrain(wound);
            if (drain == null)
            {
                drain = new CombatBodyWoundDrain { wound = wound };
                woundDrains.Add(drain);
            }
            // 原版断肢递归结束后可能再次通知同一断口，断口始终覆盖为一次全额。
            drain.drainPerSecond = wound is Hediff_MissingPart || currentTick >= drain.expiryTick
                ? rate : drain.drainPerSecond + rate;
            drain.expiryTick = currentTick + Math.Max(1, idleTimeoutTicks);
            Publish(commands, drain);
            return drain.expiryTick;
        }

        /// <summary>到期只删除本伤口来源，返回其他伤口中最早的期限。</summary>
        internal int ExpireIdleDrains(Pawn pawn, int currentTick, List<int> expiredIdsOut)
        {
            EnsureInternalState();
            expiredIdsOut?.Clear();
            for (int index = woundDrains.Count - 1; index >= 0; index--)
            {
                CombatBodyWoundDrain drain = woundDrains[index];
                if (currentTick < drain.expiryTick) continue;
                if (drain.wound != null) expiredIdsOut?.Add(drain.wound.loadID);
                RemoveAt(pawn, index);
            }
            return ResolveNextExpiryTick();
        }

        /// <summary>移除后核对最终列表，其他有效来源不重算、不续期。</summary>
        internal void RemoveWoundDrain(Pawn pawn, Hediff hediff)
        {
            ReconcileActiveWounds(pawn);
        }

        /// <summary>清理被移除、愈合或断肢替代的伤口，不注册旧伤。</summary>
        internal void ReconcileActiveWounds(Pawn pawn)
        {
            EnsureInternalState();
            for (int index = woundDrains.Count - 1; index >= 0; index--)
                if (woundDrains[index]?.wound?.pawn != pawn
                    || !CombatBodyWoundTrionDrainUtility.IsLiveWound(woundDrains[index].wound)) RemoveAt(pawn, index);
        }

        /// <summary>战斗体最终退出时注销全部派生来源。</summary>
        internal void ClearAll(Pawn pawn)
        {
            EnsureInternalState();
            for (int index = woundDrains.Count - 1; index >= 0; index--) RemoveAt(pawn, index);
        }

        /// <summary>读档只重发保存速率，严格保留期限与仍有效的原版引用。</summary>
        internal int RestoreAfterLoad(Pawn pawn, int currentTick)
        {
            EnsureInternalState();
            publishedKeys.Clear();
            ITrionCommands commands = pawn != null ? TrionSurfaceAccess.ResolveCommands(pawn) : null;
            for (int index = woundDrains.Count - 1; index >= 0; index--)
            {
                CombatBodyWoundDrain drain = woundDrains[index];
                if (commands == null || drain?.wound?.pawn != pawn || drain.drainPerSecond <= 0f
                    || currentTick >= drain.expiryTick || !CombatBodyWoundTrionDrainUtility.IsLiveWound(drain.wound))
                {
                    woundDrains.RemoveAt(index);
                    continue;
                }
                Publish(commands, drain);
            }
            return ResolveNextExpiryTick();
        }

        /// <summary>每部位只选一个尚有效来源供地图喷射，不增加既有粒子密度。</summary>
        internal IEnumerable<int> GetActiveHediffLoadIds(Pawn pawn)
        {
            EnsureInternalState();
            List<int> ids = new List<int>();
            HashSet<BodyPartRecord> parts = new HashSet<BodyPartRecord>();
            for (int index = 0; index < woundDrains.Count; index++)
            {
                Hediff wound = woundDrains[index].wound;
                if (CombatBodyWoundTrionDrainUtility.IsLiveWound(wound) && parts.Add(wound.Part)) ids.Add(wound.loadID);
            }
            return ids;
        }

        /// <summary>本部位表现代表只能来自现存有效登记，不能随便选旧伤。</summary>
        internal Hediff FindRepresentativeWound(BodyPartRecord part)
        {
            for (int index = 0; index < woundDrains.Count; index++)
                if (woundDrains[index].wound?.Part == part
                    && CombatBodyWoundTrionDrainUtility.IsLiveWound(woundDrains[index].wound)) return woundDrains[index].wound;
            return null;
        }

        /// <summary>按原版最终对象找到唯一登记。</summary>
        private CombatBodyWoundDrain FindDrain(Hediff wound)
        {
            for (int index = 0; index < woundDrains.Count; index++)
                if (woundDrains[index].wound == wound) return woundDrains[index];
            return null;
        }

        /// <summary>发布保存速率，标签变化时先注销实际旧键。</summary>
        private void Publish(ITrionCommands commands, CombatBodyWoundDrain drain)
        {
            TrionDrainKey key = CombatBodyWoundTrionDrainUtility.BuildDrainKey(drain.wound);
            if (publishedKeys.TryGetValue(drain.wound, out TrionDrainKey oldKey) && oldKey != key) commands.UnregisterDrain(oldKey);
            commands.RegisterDrain(key, drain.drainPerSecond, TrionDrainKind.WoundLeak);
            publishedKeys[drain.wound] = key;
        }

        /// <summary>注销实际键并移除对应保存记录。</summary>
        private void RemoveAt(Pawn pawn, int index)
        {
            Hediff wound = woundDrains[index]?.wound;
            if (wound != null && publishedKeys.TryGetValue(wound, out TrionDrainKey key))
            {
                TrionSurfaceAccess.ResolveCommands(pawn)?.UnregisterDrain(key);
                publishedKeys.Remove(wound);
            }
            woundDrains.RemoveAt(index);
        }

        /// <summary>返回剩余伤口中最早的期限。</summary>
        private int ResolveNextExpiryTick()
        {
            int next = 0;
            for (int index = 0; index < woundDrains.Count; index++)
                if (next <= 0 || woundDrains[index].expiryTick < next) next = woundDrains[index].expiryTick;
            return next;
        }

        /// <summary>缺少本版节点时使用空表，不启动存档旧伤。</summary>
        private void EnsureInternalState()
        {
            if (woundDrains == null) woundDrains = new List<CombatBodyWoundDrain>();
        }
    }
}
