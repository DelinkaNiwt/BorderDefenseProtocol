using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Support.Diagnostics
{
    /// <summary>
    /// BDP 统一诊断工具。
    /// 目标不是“任何时机都强行打印”，而是：
    /// - 在开发模式下提供一次性与节流日志；
    /// - 在游戏状态尚未就绪时自动静默；
    /// - 不允许诊断工具本身反过来导致模组启动崩溃。
    /// </summary>
    public static class BdpDiagnostics
    {
        /// <summary>
        /// 已经打印过一次的 key。
        /// </summary>
        private static readonly HashSet<string> SeenKeys = new HashSet<string>();
        /// <summary>
        /// 每个 key 上次打印的时间点。
        /// </summary>
        private static readonly Dictionary<string, int> LastTickByKey = new Dictionary<string, int>();
        /// <summary>
        /// 最近一次观察到的时间点，用来识别新时间线。
        /// </summary>
        private static int lastObservedTick = -1;

        /// <summary>
        /// 攻击执行诊断的外部开关解析器。
        /// 当前为空时回退到全局诊断开关，后续可正式接入设置系统。
        /// </summary>
        private static Func<bool> attackExecutionSwitchResolver;

        /// <summary>
        /// 当前是否允许输出诊断日志。
        /// 这里必须容忍游戏早期初始化阶段，因为那时 Prefs 等对象可能尚未稳定。
        /// </summary>
        public static bool Enabled
        {
            get
            {
                try
                {
                    return Prefs.DevMode;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 当前是否允许输出攻击执行诊断日志。
        /// 当前默认跟随全局诊断开关，后续可通过正式设置入口改写。
        /// </summary>
        public static bool AttackExecutionEnabled
        {
            get
            {
                if (attackExecutionSwitchResolver == null)
                {
                    return Enabled;
                }

                try
                {
                    return attackExecutionSwitchResolver();
                }
                catch
                {
                    return Enabled;
                }
            }
        }

        /// <summary>
        /// 绑定攻击执行诊断的正式开关解析器。
        /// 这里先预留统一接入口，避免后续再去各业务层散落接设置。
        /// </summary>
        public static void BindAttackExecutionSwitchResolver(Func<bool> resolver)
        {
            attackExecutionSwitchResolver = resolver;
        }

        /// <summary>
        /// 按 key 只输出一次诊断日志。
        /// </summary>
        public static void Once(string key, string message)
        {
            if (!Enabled)
            {
                return;
            }

            int currentTick = GetSafeCurrentTick();
            ResetIfTimelineChanged(currentTick);

            // 同一个 key 只打一次。
            if (SeenKeys.Contains(key))
            {
                return;
            }

            SeenKeys.Add(key);
            SafeLog("[BDP诊断] " + message);
        }

        /// <summary>
        /// 按最小间隔节流输出诊断日志。
        /// </summary>
        public static void Throttled(string key, string message, int minIntervalTicks = 60)
        {
            if (!Enabled)
            {
                return;
            }

            int currentTick = GetSafeCurrentTick();
            ResetIfTimelineChanged(currentTick);

            // 只有超过最小间隔才允许再次打印。
            int lastTick;
            if (LastTickByKey.TryGetValue(key, out lastTick) &&
                currentTick >= 0 &&
                lastTick >= 0 &&
                currentTick >= lastTick &&
                currentTick - lastTick < minIntervalTicks)
            {
                return;
            }

            LastTickByKey[key] = currentTick;
            SafeLog("[BDP诊断] " + message);
        }

        /// <summary>
        /// 输出攻击执行专用诊断日志。
        /// 它不参与 once/throttle，目的是完整保留一次攻击从计划到实际施放的过程。
        /// </summary>
        public static void AttackExecution(string message)
        {
            if (!AttackExecutionEnabled)
            {
                return;
            }

            SafeLog("[BDP攻击执行] " + message);
        }

        /// <summary>
        /// 按 key 节流输出攻击执行专用诊断日志。
        /// 只用于可能被原版每 tick 探测的边界，避免诊断本身刷屏。
        /// </summary>
        public static void AttackExecutionThrottled(string key, string message, int minIntervalTicks = 60)
        {
            if (!AttackExecutionEnabled)
            {
                return;
            }

            int currentTick = GetSafeCurrentTick();
            ResetIfTimelineChanged(currentTick);

            int lastTick;
            if (LastTickByKey.TryGetValue(key, out lastTick) &&
                currentTick >= 0 &&
                lastTick >= 0 &&
                currentTick >= lastTick &&
                currentTick - lastTick < minIntervalTicks)
            {
                return;
            }

            LastTickByKey[key] = currentTick;
            SafeLog("[BDP攻击执行] " + message);
        }

        /// <summary>
        /// 安全读取当前游戏 tick，未就绪时返回 -1。
        /// </summary>
        private static int GetSafeCurrentTick()
        {
            try
            {
                return Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 在切换到新时间线时重置一次性与节流状态。
        /// </summary>
        private static void ResetIfTimelineChanged(int currentTick)
        {
            if (currentTick < 0)
            {
                return;
            }

            // 当检测到 tick 倒退，说明大概率切到了新存档或新时间线，
            // 此时要把一次性与节流状态一并清空。
            if (lastObservedTick >= 0 && currentTick < lastObservedTick)
            {
                SeenKeys.Clear();
                LastTickByKey.Clear();
                SafeLog("[BDP诊断] 检测到新的时间线或新存档，已重置诊断节流状态。");
            }

            lastObservedTick = currentTick;
        }

        /// <summary>
        /// 安全输出日志，不让诊断链影响正式业务。
        /// </summary>
        private static void SafeLog(string message)
        {
            BdpDiagnosticSinkRegistry.Write(message);
        }
    }
}
