using System;
using System.Linq;
using Verse;

namespace BDP.Content.Factions.GateArrival
{
    /// <summary>无地图依赖的时序计算；人物和门的生命期由波控制器管理。</summary>
    public static class GateArrivalSchedule
    {
        /// <summary>原版 Exit 材质开启0.2秒、维持0.2秒，偏移0.07秒；0.3秒取稳定段中部。</summary>
        private const float StableAge = 0.3f;
        /// <summary>0.6秒越过原版材质的消退段；这里只控制形态生命期，不冻结着色器时间。</summary>
        private const float EndAge = 0.6f;
        /// <summary>指定门的预定开启时刻。</summary>
        public static int OpenTick(int start, int index, GateArrivalSettings settings)
        { return start + settings.openDelayTicks + index * settings.gateOpenIntervalTicks; }

        /// <summary>给定实际显现时刻后，等待配置时长且至少等到展开动画结束。</summary>
        public static int ReleaseReadyTick(int openedTick, GateArrivalSettings settings)
        { return openedTick + Math.Max(settings.openingTicks, settings.releaseDelayTicks); }

        /// <summary>使用创建波次时唯一抽取的偏移；保留固定间隔，随机窗口本身不按门序号累加。</summary>
        public static int OpenTickWithOffset(int start, int index, int randomOffset, GateArrivalSettings settings)
        {
            int offset = index == 0 ? 0 : Math.Max(settings.gateOpenRandomOffsetTicks.min,
                Math.Min(settings.gateOpenRandomOffsetTicks.max, randomOffset));
            return OpenTick(start, index, settings) + offset;
        }

        /// <summary>先抽零延迟，再按归一化曲线用原版加权选择正刻数；每门只在创建时调用一次。</summary>
        public static int SampleOpeningOffset(GateArrivalSettings settings)
        {
            IntRange range = settings.gateOpenRandomOffsetTicks;
            if (range.max <= 0) return 0;
            if (range.min <= 0 && Rand.Chance(settings.gateOpenZeroProbability)) return 0;
            int first = Math.Max(1, range.min);
            int span = range.max - first;
            Func<int, float> weight = tick =>
            {
                float value = settings.gateOpenDelayWeightCurve == null ? 1f
                    : settings.gateOpenDelayWeightCurve.Evaluate(span == 0 ? 0f : (tick - first) / (float)span);
                return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Math.Max(0f, value);
            };
            var candidates = Enumerable.Range(first, span + 1).Where(tick => weight(tick) > 0f).ToList();
            // 全零或无有效权重时只退回本次正区间均匀抽取，不改变独立的零延迟概率。
            return candidates.Count == 0 ? Rand.RangeInclusive(first, range.max) : candidates.RandomElementByWeight(weight);
        }

        /// <summary>同一批的人数不超过本门剩余人数。</summary>
        public static int BatchCount(int pending, GateArrivalSettings settings)
        { return settings.releaseAllAtOnce ? pending : Math.Min(pending, settings.batchSize); }

        /// <summary>维持效果覆盖等待及出兵阶段；动画未展开、已关门或未配置时不发射。</summary>
        public static bool ShouldMaintainEffect(int now, int openedTick, int closingTick, GateArrivalSettings settings)
        { return settings.maintainEffecter != null && openedTick >= 0 && closingTick < 0 && now >= openedTick + settings.openingTicks; }

        /// <summary>计算开始关门时刻；统一关闭跟随全波最后落地者，分门关闭跟随本门最后落地者。</summary>
        public static int CloseReadyTick(int openedTick, int gateLastReleaseTick, int waveLastReleaseTick,
            bool closeTogether, GateArrivalSettings settings)
        {
            int releaseTick = closeTogether ? waveLastReleaseTick : gateLastReleaseTick;
            if (releaseTick < 0) releaseTick = openedTick;
            int minimumVisibleUntil = openedTick + Math.Max(settings.openingTicks, settings.minimumOpenTicks);
            int delayedCloseUntil = releaseTick + settings.closeDelayTicks;
            return Math.Max(minimumVisibleUntil, delayedCloseUntil);
        }

        /// <summary>只固定原版特效的生命期参数；材质内部纹理仍由原版着色器驱动。</summary>
        public static float VirtualAge(int now, int opened, int closed, GateArrivalSettings settings)
        {
            if (closed >= 0) return StableAge + (EndAge - StableAge) * Math.Min(1f, Math.Max(0, now - closed) / (float)settings.closingTicks);
            return StableAge * Math.Min(1f, Math.Max(0, now - opened) / (float)settings.openingTicks);
        }
    }
}
