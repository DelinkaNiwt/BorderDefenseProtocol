using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Semantics;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.Projectiles
{
    /// <summary>
    /// BDP 投射物发射前的最小共用落地服务。
    /// 它只负责生成投射物、绑定冻结计划和复用原版武器特性，不解释具体业务模块。
    /// </summary>
    internal static class BdpProjectileLaunchService
    {
        /// <summary>
        /// 生成并完成 BDP 投射物的发射前初始化。
        /// </summary>
        internal static Projectile CreateProjectileForLaunch(
            Thing mapHost,
            ThingDef projectileDef,
            IntVec3 spawnCell,
            Thing equipmentSource,
            ProjectileInitPlan plan,
            ISemanticContext semanticContext,
            string attackInstanceId,
            string resultId)
        {
            if (projectileDef == null || mapHost?.Map == null || !spawnCell.IsValid)
            {
                return null;
            }

            Projectile projectile = GenSpawn.Spawn(projectileDef, spawnCell, mapHost.Map) as Projectile;
            if (projectile == null)
            {
                return null;
            }

            BdpDamageSemanticBridge.AssignContext(projectile, semanticContext);
            if (projectile is IAttackEffectTraceCarrier traceCarrier)
            {
                traceCarrier.AttackInstanceId = attackInstanceId;
                traceCarrier.ResultId = resultId;
            }

            if (projectile is BdpProjectile bdpProjectile)
            {
                bdpProjectile.BindLaunchPlan(plan);
            }

            ApplyWeaponTraits(projectile, equipmentSource);
            return projectile;
        }

        /// <summary>
        /// 为明确命中目标的发射冻结后续续段所需的最小原版事实。
        /// </summary>
        internal static void FreezeIntendedLaunchResolution(
            ProjectileInitPlan plan,
            LocalTargetInfo actualLaunchTarget,
            Vector3 actualLaunchDestination)
        {
            if (plan == null)
            {
                return;
            }

            plan.LaunchResolution = new ProjectileLaunchResolution
            {
                Disposition = ProjectileLaunchDisposition.IntendedTarget,
                CanHitIntendedTarget = true,
                ContinuationMode = ProjectileLaunchContinuationMode.IntendedRoute,
                ActualLaunchTarget = actualLaunchTarget,
                ActualLaunchDestination = actualLaunchDestination,
                WildMissOffset = Vector3.zero,
                TargetCoverDef = null
            };
        }

        /// <summary>
        /// 冻结掩体结果但保留中间导航；没有中间导航时沿用原版直接射向掩体。
        /// 导航目标与命中资格必须分开，禁止掩体结果跳过路线或续段后恢复命中原目标。
        /// </summary>
        internal static LocalTargetInfo FreezeCoverLaunchResolution(
            ProjectileInitPlan plan,
            LocalTargetInfo navigationTarget,
            LocalTargetInfo semanticTarget,
            LocalTargetInfo terminalTarget,
            Vector3 terminalDestination,
            ThingDef targetCoverDef)
        {
            LocalTargetInfo launchTarget = navigationTarget == semanticTarget
                ? terminalTarget
                : navigationTarget;
            plan.LaunchResolution = new ProjectileLaunchResolution
            {
                Disposition = ProjectileLaunchDisposition.CoverIntercept,
                CanHitIntendedTarget = false,
                ContinuationMode = navigationTarget == semanticTarget
                    ? ProjectileLaunchContinuationMode.None
                    : ProjectileLaunchContinuationMode.IntendedRoute,
                ActualLaunchTarget = launchTarget,
                ActualLaunchDestination = launchTarget.CenterVector3.Yto0(),
                TerminalTarget = terminalTarget,
                TerminalDestination = terminalDestination.Yto0(),
                TargetCoverDef = targetCoverDef
            };
            return launchTarget;
        }

        /// <summary>
        /// 把原版 UniqueWeapon（独特武器）特性中的投射物覆盖复用到已生成实例。
        /// </summary>
        private static void ApplyWeaponTraits(Projectile projectile, Thing equipmentSource)
        {
            if (projectile == null || equipmentSource == null || !equipmentSource.TryGetComp(out CompUniqueWeapon comp))
            {
                return;
            }

            foreach (WeaponTraitDef trait in comp.TraitsListForReading)
            {
                if (trait.damageDefOverride != null)
                {
                    projectile.damageDefOverride = trait.damageDefOverride;
                }

                if (!trait.extraDamages.NullOrEmpty())
                {
                    if (projectile.extraDamages == null)
                    {
                        projectile.extraDamages = new List<ExtraDamage>();
                    }

                    projectile.extraDamages.AddRange(trait.extraDamages);
                }
            }
        }
    }
}
