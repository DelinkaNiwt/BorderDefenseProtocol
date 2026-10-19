using System;
using System.Linq;
using BDP.Core.AttackExecution;
using BDP.Core.Expressions;
using BDP.Core.Trigger;
using BDP.Core.Trigger.Runtime;
using BDP.Core.Information.Vanilla;
using RimWorld;
using Verse;

namespace BDP.Core.Information
{
    /// <summary>
    /// 从当前已发布战斗投影建立自动攻击显示快照。
    /// 这是原版信息面板和外部兼容层共享的只读入口。
    /// </summary>
    public static class TriggerBodyAutomaticAttackDisplayProvider
    {
        /// <summary>
        /// 读取当前触发体已发布投影的版本号。
        /// 兼容层只用它判断显示缓存是否失效，不读取或复制战斗数据。
        /// </summary>
        public static bool TryGetProjectionVersion(
            CompTriggerBody triggerBody,
            out int projectionVersion)
        {
            projectionVersion = 0;
            if (triggerBody == null)
            {
                return false;
            }

            try
            {
                TriggerCombatProjectionState projection = triggerBody.PublishedCombatProjection;
                if (projection == null)
                {
                    return false;
                }

                projectionVersion = projection.ProjectionVersion;
                return true;
            }
            catch
            {
                projectionVersion = 0;
                return false;
            }
        }

        /// <summary>
        /// 按当前小人自动攻击优先级读取一条显示快照。
        /// 无有效表达或读取异常时返回 false，不触发投影重建。
        /// </summary>
        public static bool TryBuild(
            CompTriggerBody triggerBody,
            Pawn pawn,
            out TriggerBodyAutomaticAttackDisplaySnapshot snapshot)
        {
            snapshot = null;
            if (triggerBody == null)
            {
                return false;
            }

            try
            {
                TriggerCombatProjectionState projection = triggerBody.PublishedCombatProjection;
                ExpressionSnapshot expressionSnapshot = projection?.Snapshot;
                if (projection == null || expressionSnapshot == null)
                {
                    return false;
                }

                FormalExpressionResult result = ResolveAutomaticResult(triggerBody, expressionSnapshot);
                if (!IsDisplayable(result))
                {
                    return false;
                }

                return TryBuildSnapshot(projection, result, pawn ?? triggerBody.OwnerPawn, out snapshot);
            }
            catch
            {
                snapshot = null;
                return false;
            }
        }

        /// <summary>按实际自动攻击入口优先选择远程，否则选择近战。</summary>
        private static FormalExpressionResult ResolveAutomaticResult(
            CompTriggerBody triggerBody,
            ExpressionSnapshot snapshot)
        {
            if (triggerBody.HasActiveRangedChip() && IsDisplayable(snapshot.PrimaryRanged))
            {
                return snapshot.PrimaryRanged;
            }

            if (IsDisplayable(snapshot.PrimaryMelee))
            {
                return snapshot.PrimaryMelee;
            }

            return IsDisplayable(snapshot.PrimaryRanged) ? snapshot.PrimaryRanged : null;
        }

        /// <summary>建立一条远程或近战的最终显示快照。</summary>
        private static bool TryBuildSnapshot(
            TriggerCombatProjectionState projection,
            FormalExpressionResult result,
            Pawn pawn,
            out TriggerBodyAutomaticAttackDisplaySnapshot snapshot)
        {
            snapshot = null;
            VanillaWeaponStatSurface surface;
            string failureReason;
            if (!VanillaWeaponStatSurfaceFactory.TryCreate(result, out surface, out failureReason)
                || surface?.Definition == null)
            {
                return false;
            }

            TriggerBodyAutomaticAttackDisplaySnapshot resultSnapshot = new TriggerBodyAutomaticAttackDisplaySnapshot
            {
                ProjectionVersion = projection.ProjectionVersion,
                ResultId = result.Id,
                DisplayLabel = result.DisplayLabel,
                WeaponMode = result.WeaponMode == WeaponExpressionMode.Melee
                    ? AutomaticAttackDisplayMode.Melee
                    : AutomaticAttackDisplayMode.Ranged
            };

            if (result.WeaponMode == WeaponExpressionMode.Melee)
            {
                MeleeToolSurface toolSurface = ResolveMeleeToolSurface(result);
                if (toolSurface == null
                    || !VanillaMeleeStatProjection.TryCreate(toolSurface, out VanillaMeleeStatProjection melee))
                {
                    return false;
                }

                resultSnapshot.Damage = melee.Damage;
                resultSnapshot.ArmorPenetration = melee.ArmorPenetration;
                resultSnapshot.MeleeAttackCount = melee.HitCount;
                resultSnapshot.MeleeAttackIntervalSeconds = melee.HitIntervalTicks / 60f;
                resultSnapshot.MeleeCycleSeconds = melee.CycleSeconds;
                resultSnapshot.MeleeAverageDps = melee.DamagePerSecond;
                resultSnapshot.MeleeMaxDamage = melee.Damage;
                resultSnapshot.MeleeHitChance = ResolveMeleeHitChance(surface.Definition, pawn);
                resultSnapshot.HasFlameDamage = HasExtraDamage(toolSurface.Tool, DamageDefOf.Flame);
                resultSnapshot.HasEmpDamage = HasExtraDamage(toolSurface.Tool, DamageDefOf.EMP);
            }
            else if (result.WeaponMode == WeaponExpressionMode.Ranged)
            {
                if (!TryReadRangedSnapshot(surface, out resultSnapshot))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            resultSnapshot.ProjectionVersion = projection.ProjectionVersion;
            resultSnapshot.ResultId = result.Id;
            resultSnapshot.DisplayLabel = result.DisplayLabel;

            snapshot = resultSnapshot;
            return snapshot.IsValid;
        }

        /// <summary>从正式结果读取当前实际使用的近战工具。</summary>
        private static MeleeToolSurface ResolveMeleeToolSurface(FormalExpressionResult result)
        {
            if (result?.DeclaredMeleeToolSurfaces != null
                && result.DeclaredMeleeToolSurfaces.Count > 0)
            {
                return result.DeclaredMeleeToolSurfaces[0];
            }

            if (result?.Tool == null || result.VerbProps == null)
            {
                return null;
            }

            return new MeleeToolSurface
            {
                Tool = result.Tool,
                VerbProps = result.VerbProps,
                DamageDef = result.VerbProps.meleeDamageDef,
                DeclaredIndex = 0
            };
        }

        /// <summary>读取远程原版 Verb 表面和投射物最终值。</summary>
        private static bool TryReadRangedSnapshot(
            VanillaWeaponStatSurface surface,
            out TriggerBodyAutomaticAttackDisplaySnapshot snapshot)
        {
            snapshot = null;
            VerbProperties verb = surface.Definition.Verbs?.FirstOrDefault(v => !v.IsMeleeAttack);
            if (verb == null || verb.defaultProjectile?.projectile == null)
            {
                return false;
            }

            ProjectileProperties projectile = verb.defaultProjectile.projectile;
            float damage = projectile.GetDamageAmount(null, null);
            float armorPenetration = projectile.GetArmorPenetration(null, null);
            int burstCount = Math.Max(1, verb.burstShotCount);
            int burstIntervalTicks = Math.Max(0, verb.ticksBetweenBurstShots);
            float cycleSeconds = verb.warmupTime
                + verb.defaultCooldownTime
                + ((burstCount - 1) * burstIntervalTicks / 60f);

            snapshot = new TriggerBodyAutomaticAttackDisplaySnapshot
            {
                ProjectionVersion = 1,
                ResultId = surface.EntryId,
                DisplayLabel = surface.EntryId,
                WeaponMode = AutomaticAttackDisplayMode.Ranged,
                Damage = damage,
                ArmorPenetration = armorPenetration,
                RangedRange = verb.range,
                RangedWarmupSeconds = verb.warmupTime,
                RangedCooldownSeconds = verb.defaultCooldownTime,
                RangedBurstCount = burstCount,
                RangedBurstIntervalTicks = burstIntervalTicks,
                RangedStoppingPower = projectile.stoppingPower,
                RangedDamageType = projectile.damageDef,
                RangedFireRate = burstCount > 1 && burstIntervalTicks > 0
                    ? (float?)(3600f / burstIntervalTicks)
                    : null,
                RangedAverageDps = cycleSeconds > 0f
                    ? damage * burstCount / cycleSeconds
                    : 0f
            };
            return true;
        }

        /// <summary>读取原版近战命中概率，失败时返回原版默认值。</summary>
        private static float ResolveMeleeHitChance(ThingDef definition, Pawn pawn)
        {
            StatDef hitChance = StatDefOf.MeleeHitChance;
            float pawnChance = pawn != null
                ? StatExtension.GetStatValue((Thing)pawn, hitChance, true, -1)
                : 1f;
            float weaponChance = definition != null
                ? StatExtension.GetStatValueAbstract((BuildableDef)definition, hitChance, null)
                : 1f;
            return pawnChance * weaponChance;
        }

        /// <summary>读取近战工具额外伤害类型。</summary>
        private static bool HasExtraDamage(Tool tool, DamageDef damageDef)
        {
            return tool?.extraMeleeDamages != null
                && tool.extraMeleeDamages.Any(extra => extra != null && extra.def == damageDef);
        }

        /// <summary>判断结果是否是可供显示的近战或远程 Verb。</summary>
        private static bool IsDisplayable(FormalExpressionResult result)
        {
            return result != null
                && result.IsAvailable
                && result.CanProject
                && result.OutputChannel == ExpressionOutputChannel.Verb
                && (result.WeaponMode == WeaponExpressionMode.Melee
                    || result.WeaponMode == WeaponExpressionMode.Ranged);
        }
    }
}
