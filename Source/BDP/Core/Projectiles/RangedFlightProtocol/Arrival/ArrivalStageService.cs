using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Arrival
{
    /// <summary>
    /// Arrival 阶段服务。
    /// 它负责判断当前 projectile 是继续飞还是进入命中。
    /// </summary>
    internal sealed class ArrivalStageService
    {
        private readonly List<IArrivalStageModule> modules;
        private readonly List<IRangedStageAddonModule> addons;

        public ArrivalStageService(IEnumerable<IArrivalStageModule> modules, IEnumerable<IRangedStageAddonModule> addons)
        {
            this.modules = modules != null ? new List<IArrivalStageModule>(modules) : new List<IArrivalStageModule>();
            this.addons = addons != null ? new List<IRangedStageAddonModule>(addons) : new List<IRangedStageAddonModule>();
        }

        public ArrivalRecord Execute(Verse.Projectile projectile, ProjectileInitPlan initPlan, FlightRecord flight, RangedAttackModuleSession moduleSession)
        {
            ArrivalRecord record = new ArrivalRecord
            {
                ContinueFlight = flight != null && flight.ContinueFlight && flight.RedirectDestination.HasValue,
                NextDestination = flight != null && flight.RedirectDestination.HasValue
                    ? flight.RedirectDestination.Value
                    : flight != null ? flight.CurrentDestination : UnityEngine.Vector3.zero,
                CurrentTarget = flight != null ? flight.CurrentTarget : Verse.LocalTargetInfo.Invalid,
                NextTarget = flight != null ? flight.CurrentTarget : Verse.LocalTargetInfo.Invalid,
                NextBindingTarget = flight != null ? flight.CurrentTarget : Verse.LocalTargetInfo.Invalid
            };

            for (int i = 0; i < modules.Count; i++)
            {
                ArrivalStageContext context = new ArrivalStageContext(projectile, initPlan, flight, modules[i] as IRangedAttackModuleRuntime, moduleSession);
                ArrivalContribution contribution = new ArrivalContribution();
                modules[i].Contribute(context, contribution);
                if (contribution.Stop.IsRequested)
                {
                    record.ContinueFlight = false;
                }

                if (contribution.HasOverrideContinueFlight)
                {
                    record.ContinueFlight = contribution.OverrideContinueFlight;
                }

                if (contribution.HasNextDestination)
                {
                    record.NextDestination = contribution.NextDestination;
                }

                if (contribution.HasNextTarget)
                {
                    record.NextTarget = contribution.NextTarget;
                }

                if (contribution.HasNextBindingTarget)
                {
                    record.NextBindingTarget = contribution.NextBindingTarget;
                }

                if (contribution.HasNextFlightPathSnapshot)
                {
                    record.NextFlightPathSnapshot = contribution.NextFlightPathSnapshot;
                    record.NextDestination = contribution.NextFlightPathSnapshot != null
                        ? contribution.NextFlightPathSnapshot.End
                        : record.NextDestination;
                }

                AppendTags(record.Tags, contribution.TagsToAppend);
            }

            RangedStageAddonDispatcher.Execute(
                addons,
                new RangedStageAddonContext(
                    RangedStageKind.Arrival,
                    initPlan != null ? initPlan.Launcher : null,
                    projectile?.Map,
                    initPlan != null ? initPlan.AttackInstanceId : null,
                    initPlan != null ? initPlan.ResultId : null,
                    initPlan != null ? initPlan.EmitSequence : -1,
                    projectile,
                    initPlan != null ? initPlan.Launcher : null,
                    initPlan != null ? initPlan.SourceThing : null,
                    initPlan != null ? initPlan.AimTarget : Verse.LocalTargetInfo.Invalid,
                    record.CurrentTarget,
                    record.NextDestination,
                    null,
                    default,
                    initPlan != null ? initPlan.SemanticContext : null,
                    initPlan?.AttackContextSnapshot));

            return record;
        }

        private static void AppendTags(List<string> target, List<string> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(source[i]))
                {
                    target.Add(source[i]);
                }
            }
        }
    }
}
