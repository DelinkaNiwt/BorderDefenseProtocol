using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.CombatModel;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 单侧表达结果构建器。
    /// 它只负责把同一侧的来源材料整理成正式结果集合。
    /// </summary>
    internal sealed class SingleSideExpressionBuilder
    {
        /// <summary>
        /// 为指定侧别生成单侧结果集合。
        /// </summary>
        internal SingleSideExpressionSet Build(
            TriggerSide side,
            IReadOnlyList<ExpressionSourceMaterial> sourceMaterials)
        {
            List<FormalExpressionResult> allResults = new List<FormalExpressionResult>();
            List<FormalExpressionResult> weaponResults = new List<FormalExpressionResult>();

            if (sourceMaterials != null)
            {
                for (int i = 0; i < sourceMaterials.Count; i++)
                {
                    ExpressionSourceMaterial material = sourceMaterials[i];
                    if (material == null || material.Side != side)
                    {
                        continue;
                    }

                    FormalExpressionResult result = BuildResult(side, material);
                    allResults.Add(result);
                    if (result.ResultKind == ExpressionResultKind.Verb)
                    {
                        weaponResults.Add(result);
                    }
                }
            }

            return new SingleSideExpressionSet
            {
                Side = side,
                Results = allResults,
                WeaponResults = weaponResults
            };
        }

        /// <summary>
        /// 把来源材料翻译成正式结果。
        /// </summary>
        private static FormalExpressionResult BuildResult(TriggerSide side, ExpressionSourceMaterial material)
        {
            return new FormalExpressionResult
            {
                Id = material.Id,
                ResultKind = material.ResultKind,
                WeaponMode = material.WeaponMode,
                OriginKind = ToOriginKind(side),
                SourceReference = material.SourceReference,
                SourceVariantKey = material.SourceVariantKey,
                SourceVariantLabel = material.SourceVariantLabel,
                CompositeKind = CompositeExpressionKind.None,
                DisplayLabel = material.DisplayLabel,
                ManualEntryIconTexPath = material.ManualEntryIconTexPath,
                VisualPresetDefName = material.VisualPresetDefName,
                CompositeVisualPresetDefName = material.CompositeVisualPresetDefName,
                ForceSuppressHostEquipment = material.ForceSuppressHostEquipment,
                VisualPriority = material.VisualPriority,
                ManualEntryAggregationKey = BuildManualEntryAggregationKey(material),
                RoleKey = material.RoleKey,
                VerbAttackRole = material.VerbAttackRole,
                Tags = material.Tags,
                ExecutionSlotKey = BuildExecutionSlotKey(material),
                IsSecondaryAttack = material.VerbAttackRole == VerbAttackRole.Secondary,
                Trion = material.Trion,
                IsAvailable = material.IsEnabled,
                CanProject = material.IsEnabled,
                SemanticContext = material.SemanticContext,
                ModeKey = material.ModeKey,
                ExecutionStyle = material.ExecutionStyle != null ? material.ExecutionStyle.Clone() : null,
                VerbProps = material.VerbProps,
                ResolvedVerbSpec = material.ResolvedVerbSpec,
                Tool = material.Tool,
                DeclaredTools = material.DeclaredTools != null ? new List<Tool>(material.DeclaredTools) : new List<Tool>(),
                DeclaredMeleeToolSurfaces = CloneMeleeToolSurfaces(material.DeclaredMeleeToolSurfaces),
                Maneuver = material.Maneuver,
                AbilityDefName = material.AbilityDefName,
                HediffDefName = material.HediffDefName,
                HediffApplyModeKey = material.HediffApplyModeKey,
                PassiveKey = material.PassiveKey,
                ExposedData = material.ExposedData,
                RangedModules = material.RangedModules != null ? CloneRangedModules(material.RangedModules) : new List<RangedModuleMountConfig>()
            };
        }

        /// <summary>
        /// 构建执行层使用的槽位键。
        /// </summary>
        /// <summary>
        /// 为单侧表达结果构建稳定的手动入口聚合键。
        /// 芯片纳入武器类别，避免不同枪械误聚合为同一按钮。
        /// </summary>
        private static string BuildManualEntryAggregationKey(ExpressionSourceMaterial material)
        {
            if (material == null)
            {
                return null;
            }

            string declarationKey = material.SemanticContext != null ? material.SemanticContext.ReasonKey : null;
            string baseKey;
            if (!string.IsNullOrWhiteSpace(declarationKey))
            {
                baseKey = "entry:" + declarationKey;
            }
            else
            {
                string roleKey = !string.IsNullOrWhiteSpace(material.RoleKey) ? material.RoleKey : "role";
                string modeKey = !string.IsNullOrWhiteSpace(material.ModeKey) ? material.ModeKey : "mode";
                baseKey = "entry:" + roleKey + ":" + material.VerbAttackRole + ":" + material.WeaponMode + ":" + modeKey;
            }

            // 追加武器类别后缀，避免同模板但不同枪械误合并按钮。
            string sourceVariantKey = material.SourceVariantKey;
            if (!string.IsNullOrWhiteSpace(sourceVariantKey))
            {
                return baseKey + ":variant:" + sourceVariantKey;
            }

            return baseKey;
        }

        private static string BuildExecutionSlotKey(ExpressionSourceMaterial material)
        {
            if (material == null || material.ResultKind != ExpressionResultKind.Verb)
            {
                return null;
            }

            if (material.Side == TriggerSide.Main)
            {
                return material.VerbAttackRole == VerbAttackRole.Secondary ? "MainSecondary" : "MainPrimary";
            }

            if (material.Side == TriggerSide.Sub)
            {
                return material.VerbAttackRole == VerbAttackRole.Secondary ? "SubSecondary" : "SubPrimary";
            }

            return material.VerbAttackRole == VerbAttackRole.Secondary ? "Secondary" : "Primary";
        }

        /// <summary>
        /// 对近战 Tool 表面做最小浅复制，避免共享可变引用。
        /// </summary>
        private static IReadOnlyList<MeleeToolSurface> CloneMeleeToolSurfaces(
            IReadOnlyList<MeleeToolSurface> surfaces)
        {
            List<MeleeToolSurface> result = new List<MeleeToolSurface>();
            if (surfaces == null)
            {
                return result;
            }

            for (int i = 0; i < surfaces.Count; i++)
            {
                MeleeToolSurface surface = surfaces[i];
                if (surface == null)
                {
                    continue;
                }

                result.Add(new MeleeToolSurface
                {
                    Tool = surface.Tool,
                    VerbProps = surface.VerbProps,
                    Maneuver = surface.Maneuver,
                    DamageDef = surface.DamageDef,
                    DeclaredIndex = surface.DeclaredIndex
                });
            }

            return result;
        }

        /// <summary>
        /// 对模块挂载快照做最小复制，避免结果层回写来源材料。
        /// </summary>
        private static IReadOnlyList<RangedModuleMountConfig> CloneRangedModules(
            IReadOnlyList<RangedModuleMountConfig> modules)
        {
            List<RangedModuleMountConfig> result = new List<RangedModuleMountConfig>();
            if (modules == null)
            {
                return result;
            }

            for (int i = 0; i < modules.Count; i++)
            {
                RangedModuleMountConfig module = modules[i];
                if (module == null)
                {
                    continue;
                }

                result.Add(module.Clone());
            }

            return result;
        }

        /// <summary>
        /// 把 Trigger 侧别翻译成来源关系种类。
        /// </summary>
        private static ExpressionOriginKind ToOriginKind(TriggerSide side)
        {
            switch (side)
            {
                case TriggerSide.Main:
                    return ExpressionOriginKind.Main;
                case TriggerSide.Sub:
                    return ExpressionOriginKind.Sub;
                default:
                    return ExpressionOriginKind.Special;
            }
        }
    }
}
