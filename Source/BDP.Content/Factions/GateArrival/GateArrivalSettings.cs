using System;
using Verse;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>一波门的参数快照；开门节奏与出兵节奏彼此独立，全部使用游戏刻。</summary>
    public sealed class GateArrivalSettings : IExposable
    {
        /// <summary>每扇门的目标出场人数；袭击人数决定实际门数。</summary>
        public int pawnsPerGate = 4;
        /// <summary>单波最多打开的门数，避免大规模袭击产生过多特效点。</summary>
        public int maxGateCount = 6;
        /// <summary>落点附近允许放置人物的半径。</summary>
        public int placementRadius = 8;
        /// <summary>首门开启前的等待。</summary>
        public int openDelayTicks = 60;
        /// <summary>相邻门开启间隔，零表示同时开启。</summary>
        public int gateOpenIntervalTicks;
        /// <summary>每扇后续门相对基础时刻的随机偏移窗口；不是逐门累加间隔，首门始终为零。</summary>
        public IntRange gateOpenRandomOffsetTicks = new IntRange(0, 0);
        /// <summary>窗口包含0时，后续门直接零延迟的概率；首门始终为零，不参与抽取。</summary>
        public float gateOpenZeroProbability = 0.4f;
        /// <summary>正延迟区间的相对权重；横轴0～1按当前窗口缩放，与秒数无关。</summary>
        public SimpleCurve gateOpenDelayWeightCurve = DefaultOpeningWeightCurve();
        /// <summary>开门动画时长。</summary>
        public int openingTicks = 12;
        /// <summary>是否等待全部门开启后才开始出兵。</summary>
        public bool waitForAllGates;
        /// <summary>本门一次释放全部人员，覆盖每批人数。</summary>
        public bool releaseAllAtOnce;
        /// <summary>分批模式每批人数。</summary>
        public int batchSize = 1;
        /// <summary>门开始显现到首批人员出场的总等待；至少等到展开动画结束。</summary>
        public int releaseDelayTicks;
        /// <summary>全波或本门最后一人成功落地后，到开始关门之间的等待时长。</summary>
        public int closeDelayTicks = 60;
        /// <summary>两批之间的间隔，也作为受阻后的重试间隔。</summary>
        public int releaseIntervalTicks = 60;
        /// <summary>是否等整波人员出完后统一关闭。</summary>
        public bool closeTogether = true;
        /// <summary>门开启后至少维持的时长，允许纯视觉门。</summary>
        public int minimumOpenTicks;
        /// <summary>关门动画时长。</summary>
        public int closingTicks = 12;
        /// <summary>相对原版折跃门的绘制尺寸。</summary>
        public float size = 1f;
        /// <summary>展开完成至开始关门之间的可选原版维持效果；为空时整体禁用。</summary>
        public EffecterDef maintainEffecter;

        /// <summary>隔离每波参数，避免修改定义或开发参数影响已运行的波次。</summary>
        public GateArrivalSettings Snapshot()
        {
            var copy = (GateArrivalSettings)MemberwiseClone();
            copy.pawnsPerGate = Math.Max(1, pawnsPerGate);
            copy.maxGateCount = Math.Max(1, maxGateCount);
            copy.placementRadius = Math.Max(1, placementRadius);
            copy.openDelayTicks = Math.Max(0, openDelayTicks);
            copy.gateOpenIntervalTicks = Math.Max(0, gateOpenIntervalTicks);
            copy.gateOpenRandomOffsetTicks = new IntRange(
                Math.Max(0, Math.Min(gateOpenRandomOffsetTicks.min, gateOpenRandomOffsetTicks.max)),
                Math.Max(0, Math.Max(gateOpenRandomOffsetTicks.min, gateOpenRandomOffsetTicks.max)));
            copy.gateOpenZeroProbability = float.IsNaN(gateOpenZeroProbability) || float.IsInfinity(gateOpenZeroProbability)
                ? 0.4f : Math.Max(0f, Math.Min(1f, gateOpenZeroProbability));
            copy.gateOpenDelayWeightCurve = gateOpenDelayWeightCurve == null || gateOpenDelayWeightCurve.PointsCount == 0
                ? DefaultOpeningWeightCurve() : new SimpleCurve(gateOpenDelayWeightCurve.Points);
            copy.openingTicks = Math.Max(1, openingTicks);
            copy.releaseDelayTicks = Math.Max(0, releaseDelayTicks);
            copy.closeDelayTicks = Math.Max(0, closeDelayTicks);
            copy.releaseIntervalTicks = Math.Max(1, releaseIntervalTicks);
            copy.batchSize = Math.Max(1, batchSize);
            copy.minimumOpenTicks = Math.Max(0, minimumOpenTicks);
            copy.closingTicks = Math.Max(1, closingTicks);
            copy.size = float.IsNaN(size) || float.IsInfinity(size) ? 1f : Math.Max(0.1f, size);
            return copy;
        }

        /// <summary>根据本波实际袭击人数计算门数；最终数量还会受有效地图落点数限制。</summary>
        public int GateCountFor(int pawnCount)
        {
            if (pawnCount <= 0) return 0;
            int targetPawnsPerGate = Math.Max(1, pawnsPerGate);
            int configuredMaximum = Math.Max(1, maxGateCount);
            int calculatedCount = 1 + (pawnCount - 1) / targetPawnsPerGate;
            return Math.Min(calculatedCount, configuredMaximum);
        }

        /// <summary>默认从短延迟高权重下降至长延迟低权重；上界保留少量概率。</summary>
        private static SimpleCurve DefaultOpeningWeightCurve()
        { return new SimpleCurve { new CurvePoint(0f, 1f), new CurvePoint(1f, 0.05f) }; }

        /// <summary>参数随波保存，读档不重新读取可能已修改的定义。</summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnsPerGate, "pawnsPerGate", 4);
            Scribe_Values.Look(ref maxGateCount, "maxGateCount", 6);
            Scribe_Values.Look(ref placementRadius, "placementRadius", 8);
            Scribe_Values.Look(ref openDelayTicks, "openDelayTicks", 60);
            Scribe_Values.Look(ref gateOpenIntervalTicks, "gateOpenIntervalTicks");
            Scribe_Values.Look(ref gateOpenRandomOffsetTicks, "gateOpenRandomOffsetTicks", new IntRange(0, 0));
            Scribe_Values.Look(ref gateOpenZeroProbability, "gateOpenZeroProbability", 0.4f);
            Scribe_Deep.Look(ref gateOpenDelayWeightCurve, "gateOpenDelayWeightCurve");
            Scribe_Values.Look(ref openingTicks, "openingTicks", 12);
            Scribe_Values.Look(ref waitForAllGates, "waitForAllGates");
            Scribe_Values.Look(ref releaseAllAtOnce, "releaseAllAtOnce");
            Scribe_Values.Look(ref batchSize, "batchSize", 1);
            Scribe_Values.Look(ref releaseDelayTicks, "releaseDelayTicks");
            Scribe_Values.Look(ref closeDelayTicks, "closeDelayTicks", 60);
            Scribe_Values.Look(ref releaseIntervalTicks, "releaseIntervalTicks", 60);
            Scribe_Values.Look(ref closeTogether, "closeTogether", true);
            Scribe_Values.Look(ref minimumOpenTicks, "minimumOpenTicks");
            Scribe_Values.Look(ref closingTicks, "closingTicks", 12);
            Scribe_Values.Look(ref size, "size", 1f);
            Scribe_Defs.Look(ref maintainEffecter, "maintainEffecter");
        }
    }

    /// <summary>抵达方式定义上的参数，不向基础设施层加入派系业务。</summary>
    public sealed class GateArrivalModeExtension : DefModExtension
    {
        /// <summary>该抵达方式采用的门点样式；默认保留原有边缘落点。</summary>
        public GateArrivalLandingPattern landingPattern = GateArrivalLandingPattern.Edge;
        /// <summary>越线落点所要求的最低原版避险网格值。</summary>
        public int minimumDanger = 45;
        /// <summary>越线落点与估算殖民地中心之间必须保留的距离。</summary>
        public int colonyCoreBuffer = 12;
        /// <summary>同一波门点尽量保持的最小间距。</summary>
        public int minimumGateSpacing = 12;
        /// <summary>每次抵达均复制一份，避免修改定义影响已运行波次。</summary>
        public GateArrivalSettings settings = new GateArrivalSettings();

        /// <summary>限制门落点参数到可计算范围，并复制本抵达方式的时序配置。</summary>
        public GateArrivalModeExtension Snapshot()
        {
            return new GateArrivalModeExtension
            {
                landingPattern = landingPattern,
                minimumDanger = Math.Max(1, Math.Min(255, minimumDanger)),
                colonyCoreBuffer = Math.Max(0, Math.Min(256, colonyCoreBuffer)),
                minimumGateSpacing = Math.Max(0, Math.Min(256, minimumGateSpacing)),
                settings = (settings ?? new GateArrivalSettings()).Snapshot()
            };
        }
    }
}
