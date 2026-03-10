using System.Collections.Generic;
using System.Linq;
using BDP.Trigger.ShotPipeline.Modules;
using UnityEngine;
using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击管线编排器：无状态工厂类，负责构建和执行射击管线。
    /// 管线分为两个阶段：
    /// 1. Aim 阶段：瞄准解析（单 Tick，TryCastShot 内）
    /// 2. Fire 阶段：射击执行（单 Tick，TryCastShot 内）
    /// </summary>
    public static class ShotPipeline
    {
        // ══════════════════════════════════════════
        //  管线构建
        // ══════════════════════════════════════════

        /// <summary>
        /// 构建默认射击管线（硬编码模块列表）
        /// </summary>
        /// <returns>包含所有启用模块的管线配置</returns>
        public static PipelineConfig Build()
        {
            var config = new PipelineConfig();

            // Aim 阶段模块（按优先级升序执行）
            config.AimModules.Add(new LosCheckModule());
            config.AimModules.Add(new AutoRouteAimModule(priority: 15)); // 必须高于 LosCheckModule(10)

            var anchorModule = new AnchorAimModule(priority: 20, anchorSpread: 0.3f);
            config.AimModules.Add(anchorModule);

            // Aim 渲染器（Targeting 子步骤）
            config.AimRenderers.Add(anchorModule); // AnchorAimModule 同时实现 IShotAimRenderer
            config.AimRenderers.Add(new AreaIndicatorModule());

            // Aim 验证器（Targeting 子步骤）
            // 暂无

            // Fire 阶段模块（按优先级升序执行）
            config.FireModules.Add(new VolleySpreadModule());
            config.FireModules.Add(new TrionCostModule());
            config.FireModules.Add(new FlightDataModule());
            config.FireModules.Add(new AutoRouteFireModule());

            // 按优先级排序
            config.AimModules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            config.FireModules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            return config;
        }

        // ══════════════════════════════════════════
        //  Aim 阶段执行
        // ══════════════════════════════════════════

        /// <summary>
        /// 执行瞄准阶段管线（Resolve 子步骤）
        /// </summary>
        /// <param name="session">射击会话</param>
        /// <param name="config">管线配置</param>
        /// <returns>合并后的瞄准结果</returns>
        public static AimResult ExecuteAim(ShotSession session, PipelineConfig config)
        {
            // 清空上次的意图列表
            session.AimIntents.Clear();

            // 依次执行所有 Aim 模块
            foreach (var module in config.AimModules)
            {
                var intent = module.ResolveAim(session);
                session.AimIntents.Add(intent);
            }

            // 合并意图为结果
            var result = MergeAimIntents(session.AimIntents);
            session.AimResult = result;
            return result;
        }

        /// <summary>
        /// 合并多个 AimIntent 为单个 AimResult
        /// </summary>
        private static AimResult MergeAimIntents(List<AimIntent> intents)
        {
            var result = new AimResult
            {
                FinalTarget = LocalTargetInfo.Invalid,
                AimPoint = Vector3.zero,
                AccuracyMultiplier = 1f
            };

            if (intents == null || intents.Count == 0)
            {
                return result;
            }

            foreach (var intent in intents)
            {
                // 任一模块中止则整体失败
                if (intent.AbortShot)
                {
                    result.Abort = true;
                    result.AbortReason = intent.AbortReason;
                    return result;
                }

                // 合并目标覆盖
                if (intent.OverrideTarget.HasValue)
                {
                    result.FinalTarget = intent.OverrideTarget.Value;
                }

                // 合并瞄准偏移
                if (intent.AimOffset.HasValue)
                {
                    result.AimPoint += intent.AimOffset.Value;
                }

                // 合并锚点（后续模块可覆盖）
                if (intent.AnchorPoints != null && intent.AnchorPoints.Length > 0)
                {
                    result.AnchorPath = new List<IntVec3>(intent.AnchorPoints);
                    if (intent.AnchorSpread.HasValue)
                    {
                        result.AnchorSpread = intent.AnchorSpread.Value;
                    }
                }

                // 合并精度修正（累乘）
                result.AccuracyMultiplier *= intent.AccuracyMultiplier;

                // 合并强制偏移半径（取最大值）
                if (intent.ForcedMissRadius > result.ForcedMissRadius)
                {
                    result.ForcedMissRadius = intent.ForcedMissRadius;
                }
            }

            return result;
        }

        // ══════════════════════════════════════════
        //  Fire 阶段执行
        // ══════════════════════════════════════════

        /// <summary>
        /// 执行射击阶段管线
        /// </summary>
        /// <param name="session">射击会话</param>
        /// <param name="config">管线配置</param>
        /// <returns>合并后的射击结果</returns>
        public static FireResult ExecuteFire(ShotSession session, PipelineConfig config)
        {
            // 清空上次的意图列表
            session.FireIntents.Clear();

            // 依次执行所有 Fire 模块
            foreach (var module in config.FireModules)
            {
                var intent = module.OnFire(session);
                session.FireIntents.Add(intent);
            }

            // 合并意图为结果
            var result = MergeFireIntents(session.FireIntents);
            session.FireResult = result;
            return result;
        }

        /// <summary>
        /// 合并多个 FireIntent 为单个 FireResult
        /// </summary>
        private static FireResult MergeFireIntents(List<FireIntent> intents)
        {
            var result = new FireResult
            {
                DamageMultiplier = 1f,
                SpeedMultiplier = 1f
            };

            if (intents == null || intents.Count == 0)
            {
                return result;
            }

            foreach (var intent in intents)
            {
                // 任一模块中止则整体失败
                if (intent.AbortShot)
                {
                    result.Abort = true;
                    result.AbortReason = intent.AbortReason;
                    return result;
                }

                // 合并投射物覆盖（后续模块优先）
                if (intent.OverrideProjectileDef != null)
                {
                    result.ProjectileDef = intent.OverrideProjectileDef;
                }

                // 合并扩散半径（累加）
                result.SpreadRadius += intent.SpreadRadius;

                // 合并伤害/速度修正（累乘）
                result.DamageMultiplier *= intent.DamageMultiplier;
                result.SpeedMultiplier *= intent.SpeedMultiplier;

                // 合并 Trion 消耗（累加）
                result.TrionCost += intent.TrionCost;
                if (intent.SkipTrionConsumption)
                {
                    result.SkipTrionConsumption = true;
                }

                // 合并自动绕行标志
                if (intent.EnableAutoRoute)
                {
                    result.EnableAutoRoute = true;
                    if (intent.AutoRouteProjectileDef != null)
                    {
                        result.AutoRouteProjectileDef = intent.AutoRouteProjectileDef;
                    }
                }
            }

            return result;
        }

        // ══════════════════════════════════════════
        //  管线配置容器
        // ══════════════════════════════════════════

        /// <summary>
        /// 管线配置：包含所有阶段的模块列表
        /// </summary>
        public class PipelineConfig
        {
            public List<IShotAimModule> AimModules { get; set; }
            public List<IShotAimRenderer> AimRenderers { get; set; }
            public List<IShotAimValidator> AimValidators { get; set; }
            public List<IShotFireModule> FireModules { get; set; }

            public PipelineConfig()
            {
                AimModules = new List<IShotAimModule>();
                AimRenderers = new List<IShotAimRenderer>();
                AimValidators = new List<IShotAimValidator>();
                FireModules = new List<IShotFireModule>();
            }
        }
    }
}

