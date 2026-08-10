using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using UnityEngine;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Flight
{
    /// <summary>
    /// Flight 阶段服务。
    /// 它负责在有限飞行维度内收集模块意图，并产出正式 FlightRecord。
    /// </summary>
    internal sealed class FlightStageService
    {
        private readonly List<IFlightStageModule> modules;
        private readonly List<IRangedStageAddonModule> addons;

        public FlightStageService(IEnumerable<IFlightStageModule> modules, IEnumerable<IRangedStageAddonModule> addons)
        {
            this.modules = modules != null ? new List<IFlightStageModule>(modules) : new List<IFlightStageModule>();
            this.addons = addons != null ? new List<IRangedStageAddonModule>(addons) : new List<IRangedStageAddonModule>();
        }

        public FlightRecord Execute(Verse.Projectile projectile, ProjectileInitPlan initPlan, FlightRecord previous, RangedAttackModuleSession moduleSession)
        {
            FlightRecord record = previous != null
                ? Clone(previous)
                : new FlightRecord
                {
                    AttackInstanceId = initPlan != null ? initPlan.AttackInstanceId : null,
                    ResultId = initPlan != null ? initPlan.ResultId : null,
                    EmitIndex = initPlan != null ? initPlan.EmitSequence : 0,
                    FlightId = 0,
                    AimTarget = initPlan != null ? initPlan.AimTarget : Verse.LocalTargetInfo.Invalid,
                    CurrentTarget = initPlan != null ? initPlan.CurrentTarget : Verse.LocalTargetInfo.Invalid,
                    CurrentDestination = initPlan != null && initPlan.LaunchTarget.IsValid ? initPlan.LaunchTarget.CenterVector3 : Vector3.zero,
                    SpeedFactor = initPlan != null ? initPlan.InitialSpeedFactor : 1f,
                    DamageFactor = initPlan != null ? initPlan.InitialDamageFactor : 1f,
                    ContinueFlight = true
                };
            record.FlightId++;
            record.HasIntentThisTick = false;
            record.RedirectDestination = null;
            record.ContinueFlight = true;

            List<FlightContribution> contributions = new List<FlightContribution>();
            for (int i = 0; i < modules.Count; i++)
            {
                FlightStageContext context = new FlightStageContext(projectile, initPlan, previous, modules[i] as IRangedAttackModuleRuntime, moduleSession);
                FlightContribution contribution = new FlightContribution();
                modules[i].Contribute(context, contribution);
                contributions.Add(contribution);
                if (contribution.Stop.IsRequested)
                {
                    record.ContinueFlight = false;
                }
            }

            ModuleStageArbitrator exclusiveOwners = FlightStageDimensionPolicy.BuildArbitrator(contributions);
            for (int i = 0; i < contributions.Count; i++)
            {
                FlightContribution contribution = contributions[i];
                bool hasAppliedIntent = false;

                if (contribution.HasRedirectDestination && CanApplyDimension(contribution, FlightDimension.Destination, i, exclusiveOwners))
                {
                    hasAppliedIntent = true;
                    record.RedirectDestination = contribution.RedirectDestination;
                    record.CurrentDestination = contribution.RedirectDestination;
                }

                if (contribution.HasOverrideCurrentTarget && CanApplyDimension(contribution, FlightDimension.CurrentTarget, i, exclusiveOwners))
                {
                    hasAppliedIntent = true;
                    record.CurrentTarget = contribution.OverrideCurrentTarget;
                }

                record.SpeedFactor *= contribution.SpeedFactorMultiplier;
                record.DamageFactor *= contribution.DamageFactorMultiplier;
                if (contribution.SpeedFactorMultiplier != 1f || contribution.DamageFactorMultiplier != 1f)
                {
                    hasAppliedIntent = true;
                }

                if (!contribution.ContinueFlight && CanApplyDimension(contribution, FlightDimension.ContinueFlight, i, exclusiveOwners))
                {
                    hasAppliedIntent = true;
                    record.ContinueFlight = false;
                }

                if (contribution.Stop.IsRequested)
                {
                    hasAppliedIntent = true;
                    record.ContinueFlight = false;
                }

                AppendTags(record.Tags, contribution.TagsToAppend);
                if (contribution.TagsToAppend.Count > 0)
                {
                    hasAppliedIntent = true;
                }

                record.HasIntentThisTick |= hasAppliedIntent;
            }

            RangedStageAddonDispatcher.Execute(
                addons,
                new RangedStageAddonContext(
                    RangedStageKind.Flight,
                    initPlan != null ? initPlan.Launcher : null,
                    projectile?.Map,
                    record.AttackInstanceId,
                    record.ResultId,
                    record.EmitIndex,
                    projectile,
                    initPlan != null ? initPlan.Launcher : null,
                    initPlan != null ? initPlan.SourceThing : null,
                    record.AimTarget,
                    record.CurrentTarget,
                    record.CurrentDestination,
                    null,
                    default,
                    initPlan != null ? initPlan.SemanticContext : null,
                    initPlan?.AttackContextSnapshot));

            return record;
        }

        private static bool CanApplyDimension(
            FlightContribution contribution,
            FlightDimension dimension,
            int moduleIndex,
            ModuleStageArbitrator exclusiveOwners)
        {
            return FlightStageDimensionPolicy.CanApply(exclusiveOwners, contribution, dimension, moduleIndex);
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

        private static FlightRecord Clone(FlightRecord source)
        {
            return new FlightRecord
            {
                AttackInstanceId = source.AttackInstanceId,
                ResultId = source.ResultId,
                EmitIndex = source.EmitIndex,
                FlightId = source.FlightId,
                AimTarget = source.AimTarget,
                CurrentTarget = source.CurrentTarget,
                CurrentDestination = source.CurrentDestination,
                RedirectDestination = source.RedirectDestination,
                SpeedFactor = source.SpeedFactor,
                DamageFactor = source.DamageFactor,
                HasIntentThisTick = source.HasIntentThisTick,
                ContinueFlight = source.ContinueFlight,
                Tags = source.Tags != null ? new List<string>(source.Tags) : new List<string>()
            };
        }
    }
}
