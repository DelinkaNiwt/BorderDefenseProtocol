using Verse;

namespace BDP.Trigger.ShotPipeline.Modules
{
    /// <summary>
    /// 齐射散布模块：注入齐射散布半径到FireIntent
    /// </summary>
    public class VolleySpreadModule : IShotFireModule
    {
        public int Priority { get; }
        private readonly float spreadRadius;

        public VolleySpreadModule(int priority, float spreadRadius)
        {
            Priority = priority;
            this.spreadRadius = spreadRadius;
        }

        public FireIntent OnFire(ShotSession session)
        {
            var intent = FireIntent.Default;
            intent.SpreadRadius = spreadRadius;
            return intent;
        }
    }

    /// <summary>
    /// Trion 消耗模块
    /// 计算并执行 Trion 资源消耗，迁移自各 Verb 的 ChipUsageCostHelper 调用
    /// </summary>
    public class TrionCostModule : IShotFireModule
    {
        private readonly TrionCostConfig _config;

        public int Priority => _config?.Priority ?? 20;

        public TrionCostModule(TrionCostConfig config = null)
        {
            _config = config ?? new TrionCostConfig();
        }

        public FireIntent OnFire(ShotSession session)
        {
            // 如果配置跳过消耗，直接返回
            if (_config.SkipConsumption)
            {
                return new FireIntent
                {
                    SkipTrionConsumption = true,
                    DamageMultiplier = 1f,
                    SpeedMultiplier = 1f
                };
            }

            // 从芯片配置读取 usageCost
            float baseCost = ChipUsageCostHelper.GetUsageCost(session.Context.ChipThing);

            // 应用倍率
            float finalCost = baseCost * _config.CostMultiplier;

            // 检查 Trion 是否足够
            if (finalCost > 0f)
            {
                var caster = session.Context.Caster;
                if (caster == null)
                {
                    return new FireIntent
                    {
                        AbortShot = true,
                        AbortReason = "施法者为空",
                        DamageMultiplier = 1f,
                        SpeedMultiplier = 1f
                    };
                }

                var trionComp = caster.GetComp<BDP.Core.CompTrion>();
                if (trionComp == null || trionComp.Available < finalCost)
                {
                    // Trion 不足，中止射击
                    return new FireIntent
                    {
                        AbortShot = true,
                        AbortReason = "Trion 不足",
                        DamageMultiplier = 1f,
                        SpeedMultiplier = 1f
                    };
                }
            }

            // 返回消耗意图（实际消耗由宿主在 TryCastShot 后执行）
            return new FireIntent
            {
                TrionCost = finalCost,
                DamageMultiplier = 1f,
                SpeedMultiplier = 1f
            };
        }
    }

    /// <summary>
    /// 飞行数据模块：准备手动引导飞行数据。
    /// 迁移自 VerbFlightState.AttachManualFlight。
    ///
    /// 职责：
    /// - 检查 AimResult 是否有手动锚点路径
    /// - 如果有，将路径数据封装为 FlightDataConfig 存入 SharedData
    /// - 供 TryCastShot 在生成弹道时读取并注入
    /// </summary>
    public class FlightDataModule : IShotFireModule
    {
        public int Priority => 30;

        public FireIntent OnFire(ShotSession session)
        {
            // 检查瞄准结果是否有引导路径
            var aimResult = session.AimResult;
            if (!aimResult.HasGuidedPath)
            {
                return FireIntent.Default;
            }

            // 封装飞行数据配置
            var flightData = new FlightDataConfig
            {
                AnchorPath = aimResult.AnchorPath,
                FinalTarget = aimResult.FinalTarget,
                AnchorSpread = aimResult.AnchorSpread
            };

            // 存入共享数据槽，供 TryCastShot 读取
            session.SharedData["FlightData"] = flightData;

            return FireIntent.Default;
        }
    }

    /// <summary>
    /// 自动绕行射击模块：为自动绕行路径启用弹道注入。
    /// 迁移自 VerbFlightState.AttachAutoRouteFlight。
    ///
    /// 职责：
    /// - 检查 AimResult 是否由 AutoRouteAimModule 产生（通过 RouteResult 判断）
    /// - 如果是自动绕行路径，设置 EnableAutoRoute 标志
    /// - 供 TryCastShot 在生成弹道时读取并注入自动绕行路径
    /// </summary>
    public class AutoRouteFireModule : IShotFireModule
    {
        public int Priority => 40;

        public FireIntent OnFire(ShotSession session)
        {
            var intent = FireIntent.Default;

            // 检查是否有自动绕行路由结果
            if (session.RouteResult == null || !session.RouteResult.Value.IsValid)
            {
                return intent;
            }

            // 检查瞄准结果是否有引导路径（由 AutoRouteAimModule 产生）
            var aimResult = session.AimResult;
            if (!aimResult.HasGuidedPath)
            {
                return intent;
            }

            // 启用自动绕行标志
            intent.EnableAutoRoute = true;

            return intent;
        }
    }
}
