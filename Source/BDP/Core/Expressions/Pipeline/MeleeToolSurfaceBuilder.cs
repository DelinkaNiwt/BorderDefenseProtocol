using System;
using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.CombatModel;
using RimWorld;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 近战 Tool 表面共用构造器。
    /// 运行时契约和信息模型必须通过同一条路径建立 Tool、Verb 与 DamageDef 的逐项关系。
    /// </summary>
    public static class MeleeToolSurfaceBuilder
    {
        /// <summary>
        /// 按声明顺序为近战条目的每把 Tool 建立独立表面。
        /// </summary>
        public static IReadOnlyList<MeleeToolSurface> Build(
            ChipExpressionEntryConfig config,
            IReadOnlyList<Tool> declaredTools,
            AttackExecutionStyle executionStyle)
        {
            List<MeleeToolSurface> result = new List<MeleeToolSurface>();
            if (config == null || !IsMeleeEntry(config) || declaredTools == null)
            {
                return result;
            }

            for (int index = 0; index < declaredTools.Count; index++)
            {
                Tool tool = declaredTools[index];
                if (tool == null)
                {
                    continue;
                }

                ManeuverDef maneuver = ResolveManeuver(config, tool);
                DamageDef damageDef = ResolveDamageDef(tool, maneuver);
                VerbProperties verbProps = ResolveVerbProps(config, tool, maneuver, damageDef);
                ResolvedVerbSpec spec = ResolvedVerbSpecFactory.FromDeclared(
                    verbProps,
                    tool,
                    declaredTools,
                    null,
                    maneuver,
                    config.DirectTargetLineOfSight,
                    config.ProjectileOverrides);
                ApplyMeleeExecution(spec, config, executionStyle);
                if (spec == null)
                {
                    continue;
                }

                result.Add(new MeleeToolSurface
                {
                    Tool = tool,
                    VerbProps = ResolvedVerbSpecFactory.CreateSurfaceVerbProps(spec),
                    Maneuver = maneuver,
                    DamageDef = damageDef,
                    DeclaredIndex = index
                });
            }

            return result;
        }

        /// <summary>
        /// 解析主近战表面使用的 VerbProperties。
        /// 显式 Verb 优先，其次使用 Tool 的 Maneuver，最后使用统一原版默认表面。
        /// </summary>
        internal static VerbProperties ResolveVerbProps(
            ChipExpressionEntryConfig config,
            Tool tool,
            ManeuverDef maneuver,
            DamageDef damageDef)
        {
            if (config == null)
            {
                return null;
            }

            if (config.VerbProps != null)
            {
                return config.VerbProps;
            }

            if (!IsMeleeEntry(config) || tool == null)
            {
                return null;
            }

            if (maneuver?.verb != null)
            {
                return maneuver.verb;
            }

            return ResolvedVerbSpecFactory.CreateMeleeFallbackVerbProps(damageDef);
        }

        /// <summary>
        /// 解析作者未显式声明时的原版 Maneuver。
        /// </summary>
        internal static ManeuverDef ResolveManeuver(ChipExpressionEntryConfig config, Tool tool)
        {
            if (config?.Maneuver != null)
            {
                return config.Maneuver;
            }

            if (tool?.Maneuvers == null)
            {
                return null;
            }

            foreach (ManeuverDef maneuver in tool.Maneuvers)
            {
                if (maneuver?.verb != null)
                {
                    return maneuver;
                }
            }

            return null;
        }

        /// <summary>
        /// 优先读取 Maneuver 的原版伤害类型，缺失时按 Tool 首个 capacity 使用固定原版映射。
        /// </summary>
        internal static DamageDef ResolveDamageDef(Tool tool, ManeuverDef maneuver)
        {
            if (maneuver?.verb?.meleeDamageDef != null)
            {
                return maneuver.verb.meleeDamageDef;
            }

            ToolCapacityDef capacity = tool?.capacities != null && tool.capacities.Count > 0
                ? tool.capacities[0]
                : null;
            string capacityDefName = capacity?.defName;
            if (string.Equals(capacityDefName, "Stab", StringComparison.OrdinalIgnoreCase))
            {
                return DamageDefOf.Stab;
            }

            if (string.Equals(capacityDefName, "Blunt", StringComparison.OrdinalIgnoreCase))
            {
                return DamageDefOf.Blunt;
            }

            return DamageDefOf.Cut;
        }

        /// <summary>
        /// 把统一 Execution 的近战命中节奏投影到每个近战表面。
        /// </summary>
        private static void ApplyMeleeExecution(
            ResolvedVerbSpec target,
            ChipExpressionEntryConfig config,
            AttackExecutionStyle executionStyle)
        {
            if (executionStyle == null && config?.Execution != null && IsMeleeEntry(config))
            {
                executionStyle = new AttackExecutionStyle
                {
                    Single = new SingleAttackExecutionStyle
                    {
                        meleeHitCount = config.Execution.HitCount > 0 ? config.Execution.HitCount : 1,
                        meleeHitIntervalTicks = config.Execution.HitIntervalTicks > 0
                            ? config.Execution.HitIntervalTicks
                            : 0
                    }
                };
            }

            if (target == null
                || config == null
                || !IsMeleeEntry(config)
                || config.Execution == null
                || executionStyle?.Single == null)
            {
                return;
            }

            target.BurstShotCount = executionStyle.Single.meleeHitCount > 0
                ? executionStyle.Single.meleeHitCount
                : 1;
            target.TicksBetweenBurstShots = executionStyle.Single.meleeHitIntervalTicks > 0
                ? executionStyle.Single.meleeHitIntervalTicks
                : 0;
        }

        /// <summary>
        /// 判断当前条目是否是近战表达。
        /// </summary>
        private static bool IsMeleeEntry(ChipExpressionEntryConfig config)
        {
            return ChipExpressionCapabilityResolver.ResolveWeaponMode(config)
                == VerbExpressionModeConfig.Melee;
        }
    }
}
