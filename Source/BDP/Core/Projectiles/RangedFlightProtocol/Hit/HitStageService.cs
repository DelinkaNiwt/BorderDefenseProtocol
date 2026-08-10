using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Projectiles.RangedFlightProtocol.Model;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Hit
{
    /// <summary>
    /// Hit 阶段服务。
    /// 它负责把命中现场压成正式 HitRecord。
    /// </summary>
    internal sealed class HitStageService
    {
        private readonly List<IHitStageModule> modules;
        private readonly List<IRangedStageAddonModule> addons;

        public HitStageService(IEnumerable<IHitStageModule> modules, IEnumerable<IRangedStageAddonModule> addons)
        {
            this.modules = modules != null ? new List<IHitStageModule>(modules) : new List<IHitStageModule>();
            this.addons = addons != null ? new List<IRangedStageAddonModule>(addons) : new List<IRangedStageAddonModule>();
        }

        public HitRecord Execute(Projectile projectile, ProjectileInitPlan initPlan, FlightRecord flight, ArrivalRecord arrival, Thing hitThing, RangedAttackModuleSession moduleSession)
        {
            HitRecord record = new HitRecord
            {
                IsValidHit = hitThing != null,
                HitThing = hitThing,
                HitCell = projectile != null ? projectile.Position : IntVec3.Invalid
            };

            for (int i = 0; i < modules.Count; i++)
            {
                HitStageContext context = new HitStageContext(projectile, initPlan, flight, arrival, hitThing, modules[i] as IRangedAttackModuleRuntime, moduleSession);
                HitContribution contribution = new HitContribution();
                modules[i].Contribute(context, contribution);
                if (contribution.Stop.IsRequested)
                {
                    record.IsValidHit = false;
                    record.HitThing = null;
                }

                if (contribution.HasOverrideHitThing)
                {
                    record.HitThing = contribution.OverrideHitThing;
                    record.IsValidHit = contribution.OverrideHitThing != null;
                }

                if (contribution.HasOverrideHitCell)
                {
                    record.HitCell = contribution.OverrideHitCell;
                }

                record.ForceGround |= contribution.ForceGround;
                AppendTags(record.Tags, contribution.TagsToAppend);
            }

            RangedStageAddonDispatcher.Execute(
                addons,
                new RangedStageAddonContext(
                    RangedStageKind.Hit,
                    initPlan != null ? initPlan.Launcher : null,
                    projectile?.Map,
                    initPlan != null ? initPlan.AttackInstanceId : null,
                    initPlan != null ? initPlan.ResultId : null,
                    initPlan != null ? initPlan.EmitSequence : -1,
                    projectile,
                    initPlan != null ? initPlan.Launcher : null,
                    initPlan != null ? initPlan.SourceThing : null,
                    initPlan != null ? initPlan.AimTarget : LocalTargetInfo.Invalid,
                    flight != null ? flight.CurrentTarget : initPlan != null ? initPlan.CurrentTarget : LocalTargetInfo.Invalid,
                    flight != null ? flight.CurrentDestination : default,
                    record.HitThing,
                    record.HitCell,
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
