using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>初始门位规划提案；在持有物生成前参与最终选点，按优先级和稳定来源排序。</summary>
    public sealed class GateRedirectRequest
    {
        /// <summary>按确定顺序排序的提案。</summary>
        private readonly List<Proposal> proposals = new List<Proposal>();
        /// <summary>规划使用的地图；此时Wave尚未生成，不能通过Wave.Map取得地图。</summary>
        public Map Map { get; internal set; }
        /// <summary>所属波的内存状态；规划事件期间尚未生成，纯契约测试中可以为空。</summary>
        public Thing_GateArrival Wave { get; internal set; }
        /// <summary>波内门序号。</summary>
        public int GateIndex { get; internal set; }
        /// <summary>本次解析前的目的地。</summary>
        public IntVec3 Original { get; private set; }
        /// <summary>优先级最高的提案；运行时仍需通过地图合法性检查。</summary>
        public IntVec3 Destination { get { return proposals.Count == 0 ? Original : proposals[0].cell; } }
        /// <summary>外部不持有可写集合。</summary>
        internal IEnumerable<IntVec3> Candidates { get { return proposals.Select(p => p.cell).Concat(new[] { Original }); } }

        /// <summary>建立一次初始候选落点的提案收集。</summary>
        public GateRedirectRequest(IntVec3 original) { Original = original; }

        /// <summary>同一建筑应使用自己的稳定 ThingID 作为来源；无效提案会被跳过。</summary>
        public void Propose(IntVec3 cell, int priority, string sourceId)
        {
            if (!cell.IsValid || string.IsNullOrEmpty(sourceId)) return;
            proposals.Add(new Proposal { cell = cell, priority = priority, source = sourceId });
            proposals.Sort((a, b) =>
            {
                int order = b.priority.CompareTo(a.priority);
                if (order == 0) order = string.CompareOrdinal(a.source, b.source);
                if (order == 0) order = a.cell.x.CompareTo(b.cell.x);
                return order == 0 ? a.cell.z.CompareTo(b.cell.z) : order;
            });
        }

        /// <summary>提案只存于本次解析，不写入存档。</summary>
        private struct Proposal
        {
            /// <summary>候选点位。</summary>
            public IntVec3 cell;
            /// <summary>数值越大越优先。</summary>
            public int priority;
            /// <summary>确定性来源标识。</summary>
            public string source;
        }
    }

    /// <summary>门业务的少量对外通知；诱导建筑可订阅解析，也可查询后直接请求重定向。</summary>
    public static class GateArrivalHooks
    {
        /// <summary>只在初始最终选点前调用一次；建筑使用request.Map检查运行条件，人物容器尚未生成。</summary>
        public static event Action<GateRedirectRequest> ResolvingDestination;
        /// <summary>门已开启，序号可用于取得只读门状态。</summary>
        public static event Action<Thing_GateArrival, int> GateOpened;
        /// <summary>门开始关闭，外部可在此停止循环声或添加收尾效果。</summary>
        public static event Action<Thing_GateArrival, int> GateClosing;
        /// <summary>未开启的门已改变目的地。</summary>
        public static event Action<Thing_GateArrival, int> GateRedirected;
        /// <summary>人物已成功落地，不等于敌人被消灭。</summary>
        public static event Action<Thing_GateArrival, Pawn> PawnReleased;
        /// <summary>本波人员全部成功出场，仅发送一次。</summary>
        public static event Action<Thing_GateArrival> ArrivalCompleted;
        /// <summary>出场被取消；任务调用方据此处理自己的失败或撤销逻辑。</summary>
        public static event Action<Thing_GateArrival> ArrivalCancelled;

        /// <summary>查询指定地图当前波次的快照，调用方无需维护全局注册表。</summary>
        public static IReadOnlyList<Thing_GateArrival> ActiveWaves(Map map)
        {
            return map == null ? new List<Thing_GateArrival>() : map.listerThings
                .ThingsOfDef(GateArrivalDefOf.BDP_GateArrivalController).OfType<Thing_GateArrival>().ToList();
        }

        /// <summary>逐个隔离外部回调异常，扩展失败不吞掉人物或中断原版刻更新。</summary>
        private static void Notify(Delegate handlers, Action<Delegate> invoke)
        {
            if (handlers == null) return;
            foreach (Delegate handler in handlers.GetInvocationList())
                try { invoke(handler); }
                catch (Exception error) { Log.Error("BDP_GateArrival_HookError".Translate(error.ToString())); }
        }

        /// <summary>在初始规划阶段收集最终落点提案。</summary>
        internal static void Resolve(GateRedirectRequest request) { Notify(ResolvingDestination, h => ((Action<GateRedirectRequest>)h)(request)); }
        /// <summary>分发开门通知。</summary>
        internal static void Opened(Thing_GateArrival wave, int index) { Notify(GateOpened, h => ((Action<Thing_GateArrival, int>)h)(wave, index)); }
        /// <summary>分发关门通知。</summary>
        internal static void Closing(Thing_GateArrival wave, int index) { Notify(GateClosing, h => ((Action<Thing_GateArrival, int>)h)(wave, index)); }
        /// <summary>分发重定向通知。</summary>
        internal static void Redirected(Thing_GateArrival wave, int index) { Notify(GateRedirected, h => ((Action<Thing_GateArrival, int>)h)(wave, index)); }
        /// <summary>分发落地通知。</summary>
        internal static void Released(Thing_GateArrival wave, Pawn pawn) { Notify(PawnReleased, h => ((Action<Thing_GateArrival, Pawn>)h)(wave, pawn)); }
        /// <summary>分发整波出场完成通知。</summary>
        internal static void Completed(Thing_GateArrival wave) { Notify(ArrivalCompleted, h => ((Action<Thing_GateArrival>)h)(wave)); }
        /// <summary>分发整波取消通知。</summary>
        internal static void Cancelled(Thing_GateArrival wave) { Notify(ArrivalCancelled, h => ((Action<Thing_GateArrival>)h)(wave)); }
    }
}
