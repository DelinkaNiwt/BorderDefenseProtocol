using System;
using System.Collections.Generic;
using System.Reflection;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.Information.Vanilla
{
    /// <summary>
    /// 把正规化表达条目映射成原版统计系统能读取的临时 ThingDef。
    /// 工厂不注册 Def、不创建 Thing，也不保存任何缓存。
    /// </summary>
    public static class VanillaWeaponStatSurfaceFactory
    {
        /// <summary>原版 ThingDef 私有 Verb 列表字段。</summary>
        private static readonly FieldInfo ThingDefVerbsField =
            typeof(ThingDef).GetField("verbs", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>原版 ThingDef 交易属性字段。</summary>
        private static readonly FieldInfo ThingDefTradeabilityField =
            typeof(ThingDef).GetField(
                "tradeability",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        /// <summary>原版投射物属性中的私有基础伤害字段。</summary>
        private static readonly FieldInfo ProjectileDamageAmountField =
            typeof(ProjectileProperties).GetField(
                "damageAmountBase",
                BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>原版投射物属性中的私有基础护甲穿透字段。</summary>
        private static readonly FieldInfo ProjectileArmorPenetrationField =
            typeof(ProjectileProperties).GetField(
                "armorPenetrationBase",
                BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// 为当前已发布的正式表达结果建立原版统计表面。
        /// 这是信息读取专用重载，不把内部结果对象暴露到 Content（内容程序集）。
        /// </summary>
        internal static bool TryCreate(
            FormalExpressionResult result,
            out VanillaWeaponStatSurface surface,
            out string failureReason)
        {
            ChipExpressionEntryContract entry = CreateDisplayEntry(result);
            return TryCreate(entry, out surface, out failureReason);
        }

        /// <summary>为当前已发布结果的一把近战 Tool 建立原版统计表面。</summary>
        internal static bool TryCreateMeleeTool(
            FormalExpressionResult result,
            MeleeToolSurface toolSurface,
            out VanillaWeaponStatSurface surface,
            out string failureReason)
        {
            ChipExpressionEntryContract entry = CreateDisplayEntry(result);
            return TryCreateMeleeTool(entry, toolSurface, out surface, out failureReason);
        }

        /// <summary>
        /// 把内部正式结果投影成已有原版统计工厂所需的中性契约。
        /// 这里只复制显示所需字段，不改变正式结果本身。
        /// </summary>
        private static ChipExpressionEntryContract CreateDisplayEntry(
            FormalExpressionResult result)
        {
            if (result == null)
            {
                return null;
            }

            return new ChipExpressionEntryContract
            {
                Id = result.Id,
                DisplayLabel = result.DisplayLabel,
                EntryType = ChipExpressionEntryType.PrimaryVerb,
                VerbAttackRole = result.VerbAttackRole,
                WeaponMode = (VerbExpressionModeConfig)result.WeaponMode,
                VerbProps = result.VerbProps,
                ResolvedVerbSpec = result.ResolvedVerbSpec,
                Tool = result.Tool,
                DeclaredTools = result.DeclaredTools,
                DeclaredMeleeToolSurfaces = result.DeclaredMeleeToolSurfaces,
                ProjectileOverrides = result.ResolvedVerbSpec?.ProjectileOverrides
            };
        }

        /// <summary>
        /// 尝试为一条正规化表达创建临时原版统计表面。
        /// </summary>
        public static bool TryCreate(
            ChipExpressionEntryContract entry,
            out VanillaWeaponStatSurface surface,
            out string failureReason)
        {
            surface = null;
            failureReason = null;
            if (entry == null)
            {
                failureReason = "表达条目为空。";
                return false;
            }

            if (ChipExpressionCapabilityResolver.HasExecutableRangedSurface(entry))
            {
                ThingDef rangedDef;
                if (!TryBuildRangedDef(entry, out rangedDef, out failureReason))
                {
                    return false;
                }

                surface = new VanillaWeaponStatSurface(
                    entry.Id,
                    VerbExpressionModeConfig.Ranged,
                    rangedDef);
                return true;
            }

            if (ChipExpressionCapabilityResolver.HasExecutableMeleeSurface(entry))
            {
                ThingDef meleeDef;
                if (!TryBuildMeleeDef(entry, out meleeDef, out failureReason))
                {
                    return false;
                }

                surface = new VanillaWeaponStatSurface(
                    entry.Id,
                    VerbExpressionModeConfig.Melee,
                    meleeDef);
                return true;
            }

            failureReason = "表达条目没有可执行的近战或远程表面。";
            return false;
        }

        /// <summary>为一把近战 Tool 建立独立的原版统计读取表面。</summary>
        public static bool TryCreateMeleeTool(
            ChipExpressionEntryContract entry,
            MeleeToolSurface toolSurface,
            out VanillaWeaponStatSurface surface,
            out string failureReason)
        {
            surface = null;
            failureReason = null;
            if (entry == null || toolSurface?.Tool == null)
            {
                failureReason = "近战 Tool 表面为空。";
                return false;
            }

            ThingDef temporaryDef = CreateTemporaryDef("melee_tool", out failureReason);
            if (temporaryDef == null)
            {
                return false;
            }

            temporaryDef.tools = new List<Tool> { toolSurface.Tool };
            surface = new VanillaWeaponStatSurface(
                entry.Id + ":melee:" + toolSurface.DeclaredIndex,
                VerbExpressionModeConfig.Melee,
                temporaryDef,
                toolSurface);
            return true;
        }

        /// <summary>建立远程临时定义。</summary>
        private static bool TryBuildRangedDef(
            ChipExpressionEntryContract entry,
            out ThingDef temporaryDef,
            out string failureReason)
        {
            temporaryDef = CreateTemporaryDef("ranged", out failureReason);
            if (temporaryDef == null)
            {
                return false;
            }

            VerbProperties source = entry.VerbProps
                ?? entry.ResolvedVerbSpec?.SurfaceTemplate;
            if (source == null || source.defaultProjectile == null)
            {
                failureReason = "远程条目缺少投射物。";
                return false;
            }

            VerbProperties projected = CloneRangedVerbProperties(entry, source);
            if (projected == null
                || projected.defaultProjectile == null
                || projected.verbClass == null)
            {
                failureReason = "远程条目无法建立完整 VerbProperties 表面。";
                return false;
            }

            projected.defaultProjectile = CloneProjectileDef(
                projected.defaultProjectile,
                entry.ResolvedVerbSpec?.ProjectileOverrides ?? entry.ProjectileOverrides);

            temporaryDef.statBases = BuildRangedStatBases(projected);

            ThingDefVerbsField.SetValue(
                temporaryDef,
                new List<VerbProperties> { projected });
            return true;
        }

        /// <summary>
        /// 把远程 Verb 的原版精度与冷却字段放入临时 ThingDef statBases。
        /// 原版 StatsToDraw 只有在这里看到字段声明时才会显示对应统计条目。
        /// </summary>
        private static List<StatModifier> BuildRangedStatBases(
            VerbProperties projected)
        {
            List<StatModifier> result = new List<StatModifier>();
            if (projected == null)
            {
                return result;
            }

            AddRangedStatBase(result, StatDefOf.AccuracyTouch, projected.accuracyTouch);
            AddRangedStatBase(result, StatDefOf.AccuracyShort, projected.accuracyShort);
            AddRangedStatBase(result, StatDefOf.AccuracyMedium, projected.accuracyMedium);
            AddRangedStatBase(result, StatDefOf.AccuracyLong, projected.accuracyLong);
            AddRangedStatBase(
                result,
                DefDatabase<StatDef>.GetNamedSilentFail("RangedWeapon_Cooldown"),
                projected.defaultCooldownTime);
            return result;
        }

        /// <summary>只有存在有效值时才把一个远程原版统计字段加入临时定义。</summary>
        private static void AddRangedStatBase(
            ICollection<StatModifier> target,
            StatDef stat,
            float value)
        {
            if (target != null && stat != null && value > 0f)
            {
                target.Add(new StatModifier
                {
                    stat = stat,
                    value = value
                });
            }
        }

        /// <summary>建立近战临时定义。</summary>
        private static bool TryBuildMeleeDef(
            ChipExpressionEntryContract entry,
            out ThingDef temporaryDef,
            out string failureReason)
        {
            temporaryDef = CreateTemporaryDef("melee", out failureReason);
            if (temporaryDef == null)
            {
                return false;
            }

            List<Tool> tools = new List<Tool>();
            AddTools(tools, entry.DeclaredTools);
            AddToolsFromSurfaces(tools, entry.DeclaredMeleeToolSurfaces);
            if (tools.Count == 0 && entry.Tool != null)
            {
                tools.Add(entry.Tool);
            }

            if (tools.Count == 0 && entry.ResolvedVerbSpec?.Tool != null)
            {
                tools.Add(entry.ResolvedVerbSpec.Tool);
            }

            if (tools.Count == 0)
            {
                failureReason = "近战条目缺少 Tool。";
                return false;
            }

            temporaryDef.tools = tools;
            return true;
        }

        /// <summary>去重加入声明的近战工具。</summary>
        private static void AddTools(
            ICollection<Tool> target,
            IReadOnlyList<Tool> source)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                Tool tool = source[index];
                if (tool != null && !target.Contains(tool))
                {
                    target.Add(tool);
                }
            }
        }

        /// <summary>去重加入正规化近战工具表面中的工具。</summary>
        private static void AddToolsFromSurfaces(
            ICollection<Tool> target,
            IReadOnlyList<MeleeToolSurface> source)
        {
            if (source == null)
            {
                return;
            }

            for (int index = 0; index < source.Count; index++)
            {
                Tool tool = source[index]?.Tool;
                if (tool != null && !target.Contains(tool))
                {
                    target.Add(tool);
                }
            }
        }

        /// <summary>创建不进入 DefDatabase 的最小 ThingDef。</summary>
        private static ThingDef CreateTemporaryDef(
            string mode,
            out string failureReason)
        {
            failureReason = null;
            if (ThingDefVerbsField == null && string.Equals(
                mode,
                "ranged",
                StringComparison.Ordinal))
            {
                failureReason = "找不到原版 ThingDef 私有 verbs 字段。";
                return null;
            }

            ThingDef temporaryDef = new ThingDef
            {
                defName = "BDP_TemporaryVanillaWeapon_"
                    + mode
                    + "_"
                    + Guid.NewGuid().ToString("N"),
                label = "BDP_VanillaWeaponStatSurfaceLabel".Translate(),
                category = ThingCategory.Item,
                thingClass = typeof(Thing),
                statBases = new List<StatModifier>(),
                comps = new List<CompProperties>()
            };

            if (ThingDefTradeabilityField != null)
            {
                object stockable = ResolveStockableTradeability();
                if (stockable != null)
                {
                    ThingDefTradeabilityField.SetValue(temporaryDef, stockable);
                }
            }

            return temporaryDef;
        }

        /// <summary>复制原版远程统计所需的 VerbProperties 字段。</summary>
        private static VerbProperties CloneRangedVerbProperties(
            ChipExpressionEntryContract entry,
            VerbProperties source)
        {
            ResolvedVerbSpec spec = entry.ResolvedVerbSpec;
            return new VerbProperties
            {
                category = source.category,
                verbClass = spec?.VerbClass ?? source.verbClass,
                label = source.label,
                untranslatedLabel = source.untranslatedLabel,
                isPrimary = true,
                violent = source.violent,
                minRange = spec != null ? spec.MinRange : source.minRange,
                range = spec != null ? spec.Range : source.range,
                rangeStat = source.rangeStat,
                burstShotCount = spec != null && spec.BurstShotCount > 0
                    ? spec.BurstShotCount
                    : source.burstShotCount,
                ticksBetweenBurstShots = spec != null
                    ? spec.TicksBetweenBurstShots
                    : source.ticksBetweenBurstShots,
                showBurstShotStats = source.showBurstShotStats,
                warmupTime = spec != null ? spec.WarmupTime : source.warmupTime,
                defaultCooldownTime = spec != null && spec.DefaultCooldownTime > 0f
                    ? spec.DefaultCooldownTime
                    : source.defaultCooldownTime,
                defaultProjectile = spec?.ProjectileDef ?? source.defaultProjectile,
                forcedMissEvenDispersal = source.forcedMissEvenDispersal,
                accuracyTouch = spec != null ? spec.AccuracyTouch : source.accuracyTouch,
                accuracyShort = spec != null ? spec.AccuracyShort : source.accuracyShort,
                accuracyMedium = spec != null ? spec.AccuracyMedium : source.accuracyMedium,
                accuracyLong = spec != null ? spec.AccuracyLong : source.accuracyLong,
                beamDamageDef = source.beamDamageDef,
                requireLineOfSight = source.requireLineOfSight
            };
        }

        /// <summary>
        /// 在不改动来源投射物 Def 的前提下，把正规化投射物覆盖映射到临时投射物 Def。
        /// 伤害基础值仍先由原版 ProjectileProperties 计算，再只叠加 BDP 声明的倍率。
        /// </summary>
        private static ThingDef CloneProjectileDef(
            ThingDef source,
            ProjectileOverrides overrides)
        {
            if (source?.projectile == null || overrides == null)
            {
                return source;
            }

            bool hasOverride = overrides.damageMultiplier.HasValue
                || overrides.speedMultiplier.HasValue
                || overrides.stoppingPowerMultiplier.HasValue
                || !string.IsNullOrWhiteSpace(overrides.damageDef);
            if (!hasOverride)
            {
                return source;
            }

            ProjectileProperties sourceProperties = source.projectile;
            DamageDef damageDef = sourceProperties.damageDef;
            if (!string.IsNullOrWhiteSpace(overrides.damageDef))
            {
                damageDef = DefDatabase<DamageDef>.GetNamedSilentFail(overrides.damageDef)
                    ?? damageDef;
            }

            ProjectileProperties projectedProperties = new ProjectileProperties
            {
                speed = sourceProperties.speed * (overrides.speedMultiplier ?? 1f),
                flyOverhead = sourceProperties.flyOverhead,
                alwaysFreeIntercept = sourceProperties.alwaysFreeIntercept,
                damageDef = damageDef,
                stoppingPower = sourceProperties.stoppingPower
                    * (overrides.stoppingPowerMultiplier ?? 1f),
                arcHeightFactor = sourceProperties.arcHeightFactor
            };

            CopyProjectilePrivateValues(sourceProperties, projectedProperties, damageDef, overrides);
            return new ThingDef
            {
                defName = "BDP_TemporaryVanillaProjectile_" + Guid.NewGuid().ToString("N"),
                label = source.label,
                category = ThingCategory.Projectile,
                thingClass = source.thingClass,
                projectile = projectedProperties
            };
        }

        /// <summary>复制原版投射物私有伤害值，避免来源 Def 被修改。</summary>
        private static void CopyProjectilePrivateValues(
            ProjectileProperties source,
            ProjectileProperties target,
            DamageDef damageDef,
            ProjectileOverrides overrides)
        {
            int sourceDamage = ReadPrivateInt(ProjectileDamageAmountField, source, -1);
            float sourceArmorPenetration = ReadPrivateFloat(
                ProjectileArmorPenetrationField,
                source,
                -1f);
            if (overrides.damageMultiplier.HasValue)
            {
                int baseDamage = sourceDamage >= 0
                    ? sourceDamage
                    : source.GetDamageAmount(null);
                SetTargetDamageField(target, Mathf.RoundToInt(
                    baseDamage * overrides.damageMultiplier.Value));
                if (sourceArmorPenetration < 0f && damageDef != null)
                {
                    sourceArmorPenetration = damageDef.defaultArmorPenetration;
                }
            }
            else
            {
                SetTargetDamageField(target, sourceDamage);
            }

            if (overrides.damageDef != null && sourceArmorPenetration < 0f && damageDef != null)
            {
                sourceArmorPenetration = damageDef.defaultArmorPenetration;
            }

            SetTargetArmorPenetrationField(target, sourceArmorPenetration);
        }

        /// <summary>通过反射写入临时投射物的基础伤害。</summary>
        private static void SetTargetDamageField(
            ProjectileProperties target,
            int value)
        {
            if (ProjectileDamageAmountField != null)
            {
                ProjectileDamageAmountField.SetValue(target, value);
            }
        }

        /// <summary>通过反射写入临时投射物的基础护甲穿透。</summary>
        private static void SetTargetArmorPenetrationField(
            ProjectileProperties target,
            float value)
        {
            if (ProjectileArmorPenetrationField != null)
            {
                ProjectileArmorPenetrationField.SetValue(target, value);
            }
        }

        /// <summary>读取投射物私有整数，字段缺失时回退默认值。</summary>
        private static int ReadPrivateInt(FieldInfo field, object source, int fallback)
        {
            if (field == null || source == null)
            {
                return fallback;
            }

            object value = field.GetValue(source);
            return value is int ? (int)value : fallback;
        }

        /// <summary>读取投射物私有浮点数，字段缺失时回退默认值。</summary>
        private static float ReadPrivateFloat(FieldInfo field, object source, float fallback)
        {
            if (field == null || source == null)
            {
                return fallback;
            }

            object value = field.GetValue(source);
            return value is float ? (float)value : fallback;
        }

        /// <summary>读取原版 Tradeability 的 Stockable 枚举值。</summary>
        private static object ResolveStockableTradeability()
        {
            if (ThingDefTradeabilityField == null
                || !ThingDefTradeabilityField.FieldType.IsEnum)
            {
                return null;
            }

            try
            {
                return Enum.Parse(
                    ThingDefTradeabilityField.FieldType,
                    "Stockable");
            }
            catch
            {
                return Activator.CreateInstance(ThingDefTradeabilityField.FieldType);
            }
        }
    }
}
