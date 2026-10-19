using System.Collections.Generic;
using Verse;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>单门可保存进度；人物由本门地图持有物深存，门状态保留调度引用。</summary>
    public sealed class GateArrivalPoint : IExposable
    {
        /// <summary>启动时的位置，供外部比较原定点位和诱导后的点位。</summary>
        internal IntVec3 originalPosition;
        /// <summary>实际目的地，只能通过波控制器改写。</summary>
        internal IntVec3 position;
        /// <summary>预定开启时点。</summary>
        internal int plannedOpenTick;
        /// <summary>实际开启时点；负数表示还没开门。</summary>
        internal int openedTick = -1;
        /// <summary>下次尝试出兵的时点。</summary>
        internal int nextReleaseTick;
        /// <summary>本门最后一名人员成功落地的时点；失败的放置尝试不会改写。</summary>
        internal int lastPawnReleasedTick = -1;
        /// <summary>开始关闭时点；负数表示尚未关闭。</summary>
        internal int closingTick = -1;
        /// <summary>本门人物引用，不能与容器重复深存。</summary>
        internal List<Pawn> pending = new List<Pawn>();
        /// <summary>位于当前门格、唯一深存本门待出场人物的地图 Thing。</summary>
        internal Thing_GateArrivalPawnHolder pawnHolder;
        /// <summary>瞬态原版视觉发射器，不写入存档；读档后按门状态恢复，不重跑门或出兵。</summary>
        private Effecter maintainEffecter;
        /// <summary>当前目的地的只读视图。</summary>
        public IntVec3 Position { get { return position; } }
        /// <summary>诱导不会改写原定位置。</summary>
        public IntVec3 OriginalPosition { get { return originalPosition; } }
        /// <summary>供外部诊断读取已保存的预定开门时刻。</summary>
        public int PlannedOpenTick { get { return plannedOpenTick; } }
        /// <summary>供外部诊断读取实际开门时刻；负数表示尚未开启。</summary>
        public int OpenedTick { get { return openedTick; } }
        /// <summary>已开启的门默认禁止重定向。</summary>
        public bool IsOpen { get { return openedTick >= 0; } }
        /// <summary>已开始关闭。</summary>
        public bool IsClosing { get { return closingTick >= 0; } }
        /// <summary>尚未出场人数。</summary>
        public int PendingCount { get { return pending.Count; } }

        /// <summary>只由波控制器的游戏刻驱动；原版处理随机发射、视野裁剪及粒子寿命。</summary>
        internal void TickMaintainedEffect(Map map, int now, GateArrivalSettings settings)
        {
            if (!GateArrivalSchedule.ShouldMaintainEffect(now, openedTick, closingTick, settings) || position.Fogged(map))
            {
                CleanupMaintainedEffect();
                return;
            }
            if (maintainEffecter == null)
            {
                // 不调用Trigger重播折跃爆发；原版Chance子效果由EffectTick独立随机喷发。
                maintainEffecter = settings.maintainEffecter.Spawn();
                maintainEffecter.scale = settings.size;
            }
            var target = new TargetInfo(position, map);
            maintainEffecter.EffectTick(target, target);
        }

        /// <summary>关门、取消和卸载统一停止发射；已有短电火花交由原版自然淡出。</summary>
        internal void CleanupMaintainedEffect()
        {
            maintainEffecter?.Cleanup();
            maintainEffecter = null;
        }

        /// <summary>门与波一同保存，读档不会重新分配人物或重新计时。</summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref originalPosition, "originalPosition");
            Scribe_Values.Look(ref position, "position");
            Scribe_Values.Look(ref plannedOpenTick, "plannedOpenTick");
            Scribe_Values.Look(ref openedTick, "openedTick", -1);
            Scribe_Values.Look(ref nextReleaseTick, "nextReleaseTick");
            Scribe_Values.Look(ref lastPawnReleasedTick, "lastPawnReleasedTick", -1);
            Scribe_Values.Look(ref closingTick, "closingTick", -1);
            Scribe_Collections.Look(ref pending, "pending", LookMode.Reference);
            Scribe_References.Look(ref pawnHolder, "pawnHolder");
        }
    }
}
