using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>调用方可显式指定每扇门的人员分配，避免把兵种、人数和分组算法写死。</summary>
    public sealed class GateArrivalGroup
    {
        /// <summary>初始候选目的地，持有物生成前参与诱导并确定最终落点。</summary>
        public IntVec3 position;
        /// <summary>属于此门的未落地人物；启动时复制集合。</summary>
        public List<Pawn> pawns = new List<Pawn>();
    }

    /// <summary>一波门的可保存控制物，使用原版容器和放置流程，战斗策略交给原版队伍。</summary>
    public sealed class Thing_GateArrival : Thing
    {
        /// <summary>每门人物由门位持有物深存；此处仅保存各门进度。</summary>
        private List<GateArrivalPoint> gates = new List<GateArrivalPoint>();
        /// <summary>本波独立参数快照。</summary>
        private GateArrivalSettings settings = new GateArrivalSettings();
        /// <summary>人物落地后统一朝向。</summary>
        private Rot4 spawnRotation = Rot4.North;
        /// <summary>防止读档或重复更新重复发送完成通知。</summary>
        private bool completed;
        /// <summary>防止销毁流程重复取消。</summary>
        private bool cancelled;
        /// <summary>防止外部解析回调递归调用出场更新。</summary>
        private bool resolving;
        /// <summary>由出场发起方传入，不能仅因人物敌对就把任务出场认作袭击。</summary>
        private bool isHostileRaid;
        /// <summary>诱导装置用于匹配默认的仅敌方袭击模式。</summary>
        public bool IsHostileRaid { get { return isHostileRaid; } }
        /// <summary>向外提供可查询而不可替换的门列表。</summary>
        public IReadOnlyList<GateArrivalPoint> Gates { get { return gates.AsReadOnly(); } }
        /// <summary>各门原版人物容器中仍待出场的人数总和。</summary>
        public int PendingCount { get { return gates.Where(g => g.pawnHolder != null).Sum(g => g.pawnHolder.InnerContainer.Count); } }
        /// <summary>已完成出场，不代表袭击战斗结束。</summary>
        public bool ArrivalComplete { get { return completed; } }
        /// <summary>绘制器读取本波参数，不向外暴露可写快照。</summary>
        internal GateArrivalSettings Settings { get { return settings; } }

        /// <summary>原版反射创建及深存读取都需要无参构造。</summary>
        public Thing_GateArrival() { }

        /// <summary>启动显式门分组；输入校验在接管人物之前完成，不允许重复或已生成的人物。</summary>
        public static Thing_GateArrival Start(Map map, IReadOnlyList<GateArrivalGroup> groups,
            GateArrivalSettings configuration, Rot4 rotation, bool isHostileRaid = false)
        {
            if (map == null || groups == null || groups.Count == 0) throw new ArgumentException("BDP_GateArrival_InvalidGroups".Translate());
            var unique = new HashSet<Pawn>();
            foreach (var group in groups)
            {
                if (group == null || !IsValidDestination(map, group.position) || group.pawns == null)
                    throw new ArgumentException("BDP_GateArrival_InvalidGroups".Translate());
                foreach (Pawn pawn in group.pawns)
                    if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.Spawned || pawn.holdingOwner != null || !unique.Add(pawn))
                        throw new ArgumentException("BDP_GateArrival_InvalidGroups".Translate());
            }
            var wave = (Thing_GateArrival)ThingMaker.MakeThing(GateArrivalDefOf.BDP_GateArrivalController);
            wave.settings = (configuration ?? new GateArrivalSettings()).Snapshot();
            wave.spawnRotation = rotation;
            wave.isHostileRaid = isHostileRaid;
            int now = Find.TickManager.TicksGame;
            for (int i = 0; i < groups.Count; i++)
                wave.gates.Add(new GateArrivalPoint
                {
                    originalPosition = groups[i].position,
                    position = groups[i].position,
                    // 随机数仅在创建时抽取一次；预定时点随门保存，读档和刻更新不重抽。
                    plannedOpenTick = GateArrivalSchedule.OpenTickWithOffset(now, i,
                        i == 0 ? 0 : GateArrivalSchedule.SampleOpeningOffset(wave.settings), wave.settings),
                    // 首批出兵只以实际开门时刻判断，不再维护一条重复的预估出兵路线。
                    nextReleaseTick = 0,
                    pending = new List<Pawn>(groups[i].pawns)
                });
            // 先完成全部最终选点，再生成任何人物持有物。原版信件随后读取的PositionHeld一开始就是最终门位。
            wave.ResolveInitialDestinations(map);
            try
            {
                GenSpawn.Spawn(wave, wave.gates[0].position, map);
                foreach (GateArrivalPoint gate in wave.gates)
                {
                    gate.pawnHolder = (Thing_GateArrivalPawnHolder)ThingMaker.MakeThing(GateArrivalDefOf.BDP_GateArrivalPawnHolder);
                    GenSpawn.Spawn(gate.pawnHolder, gate.position, map);
                }
                for (int i = 0; i < groups.Count; i++)
                    foreach (Pawn pawn in groups[i].pawns)
                    {
                        // 任务人物原先由世界列表保存；接管后只能由所属门容器深存和管理。
                        if (Find.WorldPawns.Contains(pawn)) Find.WorldPawns.RemovePawn(pawn);
                        if (wave.gates[i].pawnHolder.InnerContainer.TryAdd(pawn)) continue;
                        if (!Find.WorldPawns.Contains(pawn)) Find.WorldPawns.PassToWorld(pawn);
                        throw new InvalidOperationException("BDP_GateArrival_InvalidGroups".Translate());
                    }
            }
            catch
            {
                wave.AbortStart();
                throw;
            }
            return wave;
        }

        /// <summary>诱导参与一次初始规划，只改内存候选，不先生成再挪动；开门刻不重复解析。</summary>
        private void ResolveInitialDestinations(Map map)
        {
            for (int i = 0; i < gates.Count; i++)
            {
                GateArrivalPoint gate = gates[i];
                var request = new GateRedirectRequest(gate.originalPosition) { Map = map, Wave = this, GateIndex = i };
                resolving = true;
                try { GateArrivalHooks.Resolve(request); }
                finally { resolving = false; }
                if (cancelled || Destroyed) throw new InvalidOperationException("BDP_GateArrival_InvalidGroups".Translate());
                foreach (IntVec3 candidate in request.Candidates)
                    if (IsValidDestination(map, candidate)) { gate.position = candidate; break; }
                if (!IsValidDestination(map, gate.position))
                    throw new InvalidOperationException("BDP_GateArrival_InvalidGroups".Translate());
            }
        }

        /// <summary>普通抵达使用均匀分配；特殊袭击可改用显式分组重载。</summary>
        public static Thing_GateArrival Start(Map map, List<Pawn> pawns, List<IntVec3> positions,
            GateArrivalSettings configuration, Rot4 rotation, bool isHostileRaid = false)
        {
            if (pawns == null || positions == null || positions.Count == 0)
                throw new ArgumentException("BDP_GateArrival_InvalidGroups".Translate());
            var groups = positions.Select(p => new GateArrivalGroup { position = p }).ToList();
            for (int i = 0; i < pawns.Count; i++) groups[i % groups.Count].pawns.Add(pawns[i]);
            return Start(map, groups, configuration, rotation, isHostileRaid);
        }

        /// <summary>统一落点底线；不限定落在边缘还是基地内，具体策略由外部提供。</summary>
        public static bool IsValidDestination(Map map, IntVec3 cell)
        { return map != null && cell.InBounds(map) && cell.Standable(map) && !cell.Fogged(map); }

        /// <summary>只允许尚未开启的门改变点位；已出场人物始终不随门移动。</summary>
        public bool TryRedirect(int gateIndex, IntVec3 destination)
        {
            if (!Spawned || cancelled || gateIndex < 0 || gateIndex >= gates.Count || gates[gateIndex].IsOpen
                || !IsValidDestination(Map, destination)) return false;
            if (gates[gateIndex].position == destination) return true;
            if (gates[gateIndex].pawnHolder == null || !gates[gateIndex].pawnHolder.MoveTo(destination, Map)) return false;
            gates[gateIndex].position = destination;
            GateArrivalHooks.Redirected(this, gateIndex);
            return true;
        }

        /// <summary>所有节奏走游戏刻；暂停冻结出场进度，绘制仍每帧进行。</summary>
        protected override void Tick()
        {
            if (cancelled || resolving) return;
            int now = Find.TickManager.TicksGame;
            for (int i = 0; i < gates.Count && Spawned; i++)
            {
                GateArrivalPoint gate = gates[i];
                if (!gate.IsOpen && now >= gate.plannedOpenTick)
                {
                    // 初始门位已在Start中确定；此处不再诱导或重抽。施工堵住时保留既有等待及显式改点接口。
                    if (!IsValidDestination(Map, gate.position)) continue;
                    gate.openedTick = now;
                    GateArrivalHooks.Opened(this, i);
                }
            }
            if (!Spawned || cancelled) return;
            bool allOpen = gates.All(g => g.IsOpen);
            int lastOpened = allOpen ? gates.Max(g => g.openedTick) : -1;
            foreach (GateArrivalPoint gate in gates)
            {
                if (!gate.IsOpen || gate.IsClosing || (settings.waitForAllGates && !allOpen)) continue;
                int readyAt = GateArrivalSchedule.ReleaseReadyTick(
                    settings.waitForAllGates ? lastOpened : gate.openedTick, settings);
                if (now < Math.Max(readyAt, gate.nextReleaseTick)) continue;
                ReleaseBatch(gate);
                if (!Spawned || cancelled) return;
                gate.nextReleaseTick = now + settings.releaseIntervalTicks;
            }
            bool allReleased = allOpen && PendingCount == 0;
            if (allReleased && !completed)
            {
                completed = true;
                GateArrivalHooks.Completed(this);
                if (!Spawned || cancelled) return;
            }
            int waveLastReleaseTick = gates.Where(g => g.lastPawnReleasedTick >= 0)
                .Select(g => g.lastPawnReleasedTick).DefaultIfEmpty(-1).Max();
            foreach (GateArrivalPoint gate in gates)
                if (gate.IsOpen && !gate.IsClosing && gate.pawnHolder != null && gate.pawnHolder.InnerContainer.Count == 0
                    && (!settings.closeTogether || allReleased)
                    && now >= GateArrivalSchedule.CloseReadyTick(gate.openedTick, gate.lastPawnReleasedTick,
                        waveLastReleaseTick, settings.closeTogether, settings))
                {
                    gate.closingTick = now;
                    GateArrivalHooks.Closing(this, gates.IndexOf(gate));
                    if (!Spawned || cancelled) return;
                }
            // 关门状态已在本刻更新；先停止对应发射器，保证开始关门同刻不再生成电弧。
            foreach (GateArrivalPoint gate in gates) gate.TickMaintainedEffect(Map, now, settings);
            if (gates.All(g => g.IsClosing && now >= g.closingTick + settings.closingTicks)) Destroy();
        }

        /// <summary>参考授勋飞船卸载：原版容器尝试附近落地，失败时仍留在容器并等待下批重试。</summary>
        private void ReleaseBatch(GateArrivalPoint gate)
        {
            int count = GateArrivalSchedule.BatchCount(gate.pending.Count, settings);
            foreach (Pawn pawn in gate.pending.Take(count).ToList())
            {
                Pawn dropped;
                if (!gate.pawnHolder.InnerContainer.TryDrop(pawn, gate.position, Map, ThingPlaceMode.Near, out dropped, null,
                    cell => IsValidDestination(Map, cell) && cell.DistanceToSquared(gate.position) <= settings.placementRadius * settings.placementRadius
                        && (!pawn.Downed || !cell.GetThingList(Map).OfType<Pawn>().Any()))) continue;
                gate.pending.Remove(pawn);
                gate.lastPawnReleasedTick = Find.TickManager.TicksGame;
                dropped.Rotation = spawnRotation;
                GateArrivalHooks.Released(this, dropped);
                if (!Spawned || cancelled) return;
            }
        }

        /// <summary>主动取消出场；已落地人物继续由原版队伍管理，未落地人物交回世界。</summary>
        public void Cancel()
        {
            CancelRemaining();
            if (!Destroyed) Destroy();
        }

        /// <summary>启动未能完成时回滚已接管的人物和持有物，不发送成功抵达后的取消通知。</summary>
        private void AbortStart()
        {
            cancelled = true;
            ReturnHeldPawnsToWorld();
            if (!Destroyed) Destroy();
        }

        /// <summary>取消不使用杀死人物或击败队伍的流程，不向任务发送 AllEnemiesDefeated。</summary>
        private void CancelRemaining()
        {
            if (cancelled || completed) return;
            cancelled = true;
            ReturnHeldPawnsToWorld();
            GateArrivalHooks.Cancelled(this);
        }

        /// <summary>逐门清空原版持有容器；人物不因地图卸载或控制器销毁而丢失。</summary>
        private void ReturnHeldPawnsToWorld()
        {
            var affected = new HashSet<Lord>();
            foreach (GateArrivalPoint gate in gates)
            {
                Thing_GateArrivalPawnHolder holder = gate.pawnHolder;
                if (holder != null)
                    foreach (Pawn pawn in holder.InnerContainer.ToList<Pawn>())
                    {
                        Lord lord = pawn.GetLord();
                        if (lord != null) { lord.RemovePawn(pawn); affected.Add(lord); }
                        holder.InnerContainer.Remove(pawn);
                        // 地图移除时原版可能已归还一次，避免重复归还产生红字。
                        if (!Find.WorldPawns.Contains(pawn)) Find.WorldPawns.PassToWorld(pawn);
                    }
                gate.pending.Clear();
            }
            foreach (Lord lord in affected)
                if (lord.ownedPawns.Count == 0) lord.Map.lordManager.RemoveLord(lord);
        }

        /// <summary>结束波次时删除门持有物；意外残留人物先安全归还世界。</summary>
        private void CleanupPawnHolders()
        {
            if (PendingCount > 0) ReturnHeldPawnsToWorld();
            foreach (GateArrivalPoint gate in gates)
            {
                Thing_GateArrivalPawnHolder holder = gate.pawnHolder;
                if (holder != null && !holder.Destroyed) holder.Destroy(DestroyMode.Vanish);
                gate.pawnHolder = null;
            }
        }

        /// <summary>异常外部删除控制物时，也先处理人物所有权。</summary>
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            foreach (GateArrivalPoint gate in gates) gate.CleanupMaintainedEffect();
            CancelRemaining();
            CleanupPawnHolders();
            base.DeSpawn(mode);
        }

        /// <summary>保存波次参数和门进度；各门持有物分别深存人物。</summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref settings, "settings");
            Scribe_Collections.Look(ref gates, "gates", LookMode.Deep);
            Scribe_Values.Look(ref spawnRotation, "spawnRotation");
            Scribe_Values.Look(ref completed, "completed");
            Scribe_Values.Look(ref cancelled, "cancelled");
            Scribe_Values.Look(ref isHostileRaid, "isHostileRaid");
        }
    }
}
