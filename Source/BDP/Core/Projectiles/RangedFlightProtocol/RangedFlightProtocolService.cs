using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Expressions;
using BDP.Core.Projectiles.RangedFlightProtocol.Arrival;
using BDP.Core.Projectiles.RangedFlightProtocol.Flight;
using BDP.Core.Projectiles.RangedFlightProtocol.Hit;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol
{
    /// <summary>
    /// 远程飞行协议总入口。
    /// 它把 projectile 在途行为正式收口成 Flight/Arrival/Hit/Impact 四段。
    /// </summary>
    internal sealed class RangedFlightProtocolService
    {
        /// <summary>
        /// 当前飞行协议持有的飞行阶段服务。
        /// </summary>
        private readonly List<IFlightStageModule> flightModules;

        /// <summary>
        /// 当前飞行协议持有的到达阶段服务。
        /// </summary>
        private readonly List<IArrivalStageModule> arrivalModules;

        /// <summary>
        /// 当前飞行协议持有的命中阶段服务。
        /// </summary>
        private readonly List<IHitStageModule> hitModules;

        /// <summary>
        /// 当前飞行协议持有的落地阶段服务。
        /// </summary>
        private readonly List<IImpactStageModule> impactModules;

        internal RangedFlightProtocolService(
            IEnumerable<IFlightStageModule> flightModules,
            IEnumerable<IArrivalStageModule> arrivalModules,
            IEnumerable<IHitStageModule> hitModules,
            IEnumerable<IImpactStageModule> impactModules)
        {
            this.flightModules = flightModules != null ? new List<IFlightStageModule>(flightModules) : new List<IFlightStageModule>();
            this.arrivalModules = arrivalModules != null ? new List<IArrivalStageModule>(arrivalModules) : new List<IArrivalStageModule>();
            this.hitModules = hitModules != null ? new List<IHitStageModule>(hitModules) : new List<IHitStageModule>();
            this.impactModules = impactModules != null ? new List<IImpactStageModule>(impactModules) : new List<IImpactStageModule>();
        }

        public FlightRecord ExecuteFlight(Projectile projectile, ProjectileInitPlan initPlan, FlightRecord previous)
        {
            RangedAttackModuleSession session = CreateModuleSession(initPlan);
            return CreateFlightStageService(session).Execute(projectile, initPlan, previous, session);
        }

        public ArrivalRecord ExecuteArrival(Projectile projectile, ProjectileInitPlan initPlan, FlightRecord flight)
        {
            RangedAttackModuleSession session = CreateModuleSession(initPlan);
            return CreateArrivalStageService(session).Execute(projectile, initPlan, flight, session);
        }

        public HitRecord ExecuteHit(Projectile projectile, ProjectileInitPlan initPlan, FlightRecord flight, ArrivalRecord arrival, Thing hitThing)
        {
            RangedAttackModuleSession session = CreateModuleSession(initPlan);
            return CreateHitStageService(session).Execute(projectile, initPlan, flight, arrival, hitThing, session);
        }

        public ImpactPlan ExecuteImpact(Projectile projectile, ProjectileInitPlan initPlan, FlightRecord flight, HitRecord hit)
        {
            RangedAttackModuleSession session = CreateModuleSession(initPlan);
            return CreateImpactStageService(session).Execute(projectile, initPlan, flight, hit, session);
        }

        /// <summary>
        /// 尝试按当前投射物计划重建后半段可用的模块运行时会话。
        /// 这里只读取当前已发布结果并导入冻结攻击上下文，不再依赖旧的计划内运行时会话。
        /// </summary>
        private static RangedAttackModuleSession CreateModuleSession(ProjectileInitPlan initPlan)
        {
            Pawn launcher = initPlan != null ? initPlan.Launcher : null;
            if (launcher == null
                || initPlan == null
                || string.IsNullOrWhiteSpace(initPlan.ResultId)
                || !AttackExecutionSurfaceAccess.TryGetPublishedResult(launcher, initPlan.ResultId, out _, out FormalExpressionResult result))
            {
                return null;
            }

            RangedAttackModuleSession session = AttackExecutionSurfaceAccess.CreateRangedModuleSession(launcher, result);
            session?.ImportPrivateContexts(initPlan.AttackContextSnapshot);
            return session;
        }

        private FlightStageService CreateFlightStageService(RangedAttackModuleSession session)
        {
            return new FlightStageService(
                ComposeModules(flightModules, session != null ? session.GetFlightModules() : null),
                session != null ? session.GetAddonModules() : null);
        }

        private ArrivalStageService CreateArrivalStageService(RangedAttackModuleSession session)
        {
            return new ArrivalStageService(
                ComposeModules(arrivalModules, session != null ? session.GetArrivalModules() : null),
                session != null ? session.GetAddonModules() : null);
        }

        private HitStageService CreateHitStageService(RangedAttackModuleSession session)
        {
            return new HitStageService(
                ComposeModules(hitModules, session != null ? session.GetHitModules() : null),
                session != null ? session.GetAddonModules() : null);
        }

        private ImpactStageService CreateImpactStageService(RangedAttackModuleSession session)
        {
            return new ImpactStageService(
                ComposeModules(impactModules, session != null ? session.GetImpactModules() : null),
                session != null ? session.GetAddonModules() : null);
        }

        private static IReadOnlyList<TModule> ComposeModules<TModule>(
            IReadOnlyList<TModule> baselineModules,
            IReadOnlyList<TModule> sessionModules)
        {
            List<TModule> result = new List<TModule>();
            AppendModules(result, baselineModules);
            AppendModules(result, sessionModules);
            return result;
        }

        private static void AppendModules<TModule>(
            List<TModule> target,
            IReadOnlyList<TModule> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    target.Add(source[i]);
                }
            }
        }
    }
}
