using System.Collections.Generic;
using BDP.Core.Projectiles.RangedFlightProtocol.Arrival;
using BDP.Core.Projectiles.RangedFlightProtocol.Flight;
using BDP.Core.Projectiles.RangedFlightProtocol.Hit;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;

namespace BDP.Core.Projectiles.RangedFlightProtocol
{
    /// <summary>
    /// 远程飞行后半段协议的统一装配入口。
    /// 它只负责持有默认模块表并暴露已装配服务，不承接 projectile 宿主职责。
    /// </summary>
    internal static class RangedFlightProtocolSurfaceAccess
    {
        /// <summary>
        /// 当前正式运行路径统一使用的飞行协议服务实例。
        /// </summary>
        private static readonly RangedFlightProtocolService Service = new RangedFlightProtocolService(
            CreateFlightModules(),
            CreateArrivalModules(),
            CreateHitModules(),
            CreateImpactModules());

        /// <summary>
        /// 读取当前已装配好的飞行协议服务。
        /// </summary>
        public static RangedFlightProtocolService Resolve()
        {
            return Service;
        }

        /// <summary>
        /// 创建默认飞行阶段模块表。
        /// 无模块时返回空表，保持现有 baseline 行为。
        /// 返回顺序就是正式协议顺序，未来芯片 Def 声明顺序必须原样保留。
        /// </summary>
        private static IEnumerable<IFlightStageModule> CreateFlightModules()
        {
            return new IFlightStageModule[0];
        }

        /// <summary>
        /// 创建默认到达阶段模块表。
        /// 无模块时返回空表，保持现有 baseline 行为。
        /// 返回顺序就是正式协议顺序，未来芯片 Def 声明顺序必须原样保留。
        /// </summary>
        private static IEnumerable<IArrivalStageModule> CreateArrivalModules()
        {
            return new IArrivalStageModule[0];
        }

        /// <summary>
        /// 创建默认命中阶段模块表。
        /// 无模块时返回空表，保持现有 baseline 行为。
        /// 返回顺序就是正式协议顺序，未来芯片 Def 声明顺序必须原样保留。
        /// </summary>
        private static IEnumerable<IHitStageModule> CreateHitModules()
        {
            return new IHitStageModule[0];
        }

        /// <summary>
        /// 创建默认落地阶段模块表。
        /// 无模块时返回空表，保持现有 baseline 行为。
        /// 返回顺序就是正式协议顺序，未来芯片 Def 声明顺序必须原样保留。
        /// </summary>
        private static IEnumerable<IImpactStageModule> CreateImpactModules()
        {
            return new IImpactStageModule[0];
        }
    }
}
