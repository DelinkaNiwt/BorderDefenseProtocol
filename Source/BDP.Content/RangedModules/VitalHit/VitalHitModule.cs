using BDP.Content.HitPartPolicy;
using BDP.Core.AttackExecution;
using BDP.Core.Projectiles.RangedFlightProtocol.Impact;
using Verse;

namespace BDP.Content.RangedModules.VitalHit
{
    /// <summary>
    /// 动态命中部位模块。
    /// 它只在单体直接伤害的 Impact 阶段提交命中部位，不替换原版伤害类型和承伤链。
    /// </summary>
    public sealed class VitalHitModule :
        IRangedAttackModuleRuntime,
        IImpactStageModule
    {
        /// <summary>
        /// 当前模块绑定的冻结配置。
        /// </summary>
        private VitalHitConfig config;

        /// <summary>
        /// 初始化当前模块运行时。
        /// </summary>
        void IRangedAttackModuleRuntime.Initialize(RangedAttackModuleRuntimeContext context)
        {
            config = context != null && context.Config is VitalHitConfig typedConfig
                ? typedConfig.CloneTyped()
                : new VitalHitConfig();
        }

        /// <summary>
        /// 在单体直接命中时按统一策略解析目标当前的命中部位。
        /// </summary>
        void IImpactStageModule.Contribute(
            in ImpactStageContext context,
            ImpactContribution contribution)
        {
            if (contribution == null
                || config == null
                || !context.IsValidHit
                || !(context.HitThing is Pawn target)
                || context.Projectile == null
                || IsAreaProjectile(context.Projectile))
            {
                return;
            }

            if (config.SelectionMode == HitPartSelectionMode.ExactDefOccurrence)
            {
                contribution.HasHitPartOverride = true;
                if (HitPartPolicyResolver.TryResolveExact(target, config.ExactBodyPartDef,
                    config.ExactBodyPartOccurrenceIndex, out BodyPartRecord exactPart))
                {
                    contribution.HitPartOverride = exactPart;
                }
                return;
            }

            HitPartPolicyConfig policy = new HitPartPolicyConfig
            {
                PolicyKey = config.SelectionMode.ToString(),
                Chance = config.VitalHitChance
            };

            if (HitPartPolicyResolver.TryResolve(
                    context.Launcher,
                    target,
                    context.Projectile.DamageDef,
                    null,
                    policy,
                    out BodyPartRecord hitPart)
                && hitPart != null)
            {
                contribution.HitPartOverride = hitPart;
            }
        }

        /// <summary>
        /// 判断当前投射物是否会进入范围爆炸链。
        /// 范围爆炸的逐目标命中部位继续交给原版处理。
        /// </summary>
        private static bool IsAreaProjectile(Projectile projectile)
        {
            return projectile.def != null
                && projectile.def.projectile != null
                && projectile.def.projectile.explosionRadius > 0f;
        }
    }
}
