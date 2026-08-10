using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.AttackExecution;
using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 默认表达来源声明提供器。
    /// 它只消费已经解释完成的芯片表达契约。
    /// </summary>
    internal sealed class DefaultExpressionSourceDeclarationProvider : IExpressionSourceDeclarationProvider
    {
        /// <summary>
        /// 芯片定义读取器。
        /// </summary>
        private readonly IChipDefinitionReader chipDefinitionReader;

        /// <summary>
        /// 芯片表达契约解释器。
        /// </summary>
        private readonly IChipExpressionContractInterpreter contractInterpreter;

        /// <summary>
        /// 使用指定依赖构造来源声明提供器。
        /// </summary>
        public DefaultExpressionSourceDeclarationProvider(
            IChipDefinitionReader chipDefinitionReader,
            IChipExpressionContractInterpreter contractInterpreter)
        {
            this.chipDefinitionReader = chipDefinitionReader;
            this.contractInterpreter = contractInterpreter;
        }

        /// <summary>
        /// 仅使用契约解释器构造来源声明提供器。
        /// </summary>
        public DefaultExpressionSourceDeclarationProvider(IChipExpressionContractInterpreter contractInterpreter)
            : this(null, contractInterpreter)
        {
        }

        /// <summary>
        /// 读取指定芯片当前适用的表达来源声明。
        /// </summary>
        public IReadOnlyList<ExpressionSourceDeclaration> GetDeclarations(Thing chip, ITriggerLoadoutReader triggerLoadoutReader)
        {
            List<ExpressionSourceDeclaration> result = new List<ExpressionSourceDeclaration>();
            if (contractInterpreter == null)
            {
                return result;
            }

            ChipExpressionConfig config = ResolveExpressionConfig(chip);
            if (config == null)
            {
                return result;
            }

            ChipExpressionResolvedContract resolvedContract = contractInterpreter.Resolve(chip, config, triggerLoadoutReader);
            if (resolvedContract == null
                || resolvedContract.Contract == null
                || resolvedContract.Contract.Entries == null
                || resolvedContract.Validation == null
                || !resolvedContract.Validation.IsValid)
            {
                return result;
            }

            for (int i = 0; i < resolvedContract.Contract.Entries.Count; i++)
            {
                ChipExpressionEntryContract entry = resolvedContract.Contract.Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
                {
                    continue;
                }

                result.Add(BuildDeclaration(entry));
            }

            return result;
        }

        /// <summary>
        /// 统一通过芯片定义层读取表达配置块。
        /// </summary>
        private ChipExpressionConfig ResolveExpressionConfig(Thing chip)
        {
            if (chipDefinitionReader == null)
            {
                // 优先从制造期 Comp 读取动态配置，回退到 Def 上的静态配置。
                ChipDefinitionConfig manufactured;
                ChipInstanceSurfaceAccess.TryGetDefinition(chip, out manufactured);
                if (manufactured != null)
                {
                    return manufactured.Expression;
                }

                return chip != null && chip.def != null
                    ? chip.def.GetModExtension<ChipExpressionConfig>()
                    : null;
            }

            ChipDefinitionReadResult readResult = chipDefinitionReader.Read(chip);
            if (readResult == null
                || readResult.Validation == null
                || !readResult.Validation.IsValid
                || readResult.Contract == null
                || readResult.Contract.Expression == null
                || !readResult.Contract.Expression.HasExpressionBlock)
            {
                return null;
            }

            return readResult.Contract.Expression.Config;
        }

        /// <summary>
        /// 把正式契约条目翻译成来源声明。
        /// </summary>
        private static ExpressionSourceDeclaration BuildDeclaration(ChipExpressionEntryContract entry)
        {
            return new ExpressionSourceDeclaration
            {
                Id = entry.Id,
                ResultKind = TranslateResultKind(entry.Kind),
                WeaponMode = TranslateWeaponMode(entry.WeaponMode),
                DisplayLabel = entry.DisplayLabel,
                ManualEntryIconTexPath = entry.ManualEntryIconTexPath,
                VisualPresetDefName = entry.VisualPresetDefName,
                CompositeVisualPresetDefName = entry.CompositeVisualPresetDefName,
                ForceSuppressHostEquipment = entry.ForceSuppressHostEquipment,
                VisualPriority = entry.VisualPriority,
                RoleKey = entry.RoleKey,
                VerbAttackRole = entry.VerbAttackRole,
                Tags = entry.Tags,
                Trion = entry.Trion,
                Conditions = entry.Conditions,
                SemanticSourceKind = entry.SemanticSourceKind,
                ModeKey = entry.ModeKey,
                ExecutionStyle = entry.ExecutionStyle != null ? entry.ExecutionStyle.Clone() : null,
                VerbProps = entry.VerbProps,
                ResolvedVerbSpec = entry.ResolvedVerbSpec,
                Tool = entry.Tool,
                DeclaredTools = entry.DeclaredTools != null ? new List<Tool>(entry.DeclaredTools) : new List<Tool>(),
                DeclaredMeleeToolSurfaces = CloneMeleeToolSurfaces(entry.DeclaredMeleeToolSurfaces),
                Maneuver = entry.Maneuver,
                AbilityDefName = entry.AbilityDefName,
                HediffDefName = entry.HediffDefName,
                HediffApplyModeKey = entry.HediffApplyModeKey,
                PassiveKey = entry.PassiveKey,
                ExposedData = TranslateExposedData(entry.ExposedData),
                RangedModules = entry.RangedModules != null ? CloneRangedModules(entry.RangedModules) : new List<RangedModuleMountConfig>()
            };
        }

        /// <summary>
        /// 把契约条目种类翻译成结果种类。
        /// </summary>
        private static ExpressionResultKind TranslateResultKind(ChipExpressionEntryKind kind)
        {
            switch (kind)
            {
                case ChipExpressionEntryKind.PrimaryVerb:
                case ChipExpressionEntryKind.SecondaryVerb:
                    return ExpressionResultKind.Verb;
                case ChipExpressionEntryKind.Ability:
                    return ExpressionResultKind.Ability;
                case ChipExpressionEntryKind.Hediff:
                    return ExpressionResultKind.Hediff;
                case ChipExpressionEntryKind.Passive:
                    return ExpressionResultKind.Passive;
                default:
                    return ExpressionResultKind.Verb;
            }
        }

        /// <summary>
        /// 把配置层武器模式翻译成结果层武器模式。
        /// </summary>
        private static WeaponExpressionMode TranslateWeaponMode(VerbExpressionModeConfig configMode)
        {
            switch (configMode)
            {
                case VerbExpressionModeConfig.Melee:
                    return WeaponExpressionMode.Melee;
                case VerbExpressionModeConfig.Ranged:
                    return WeaponExpressionMode.Ranged;
                default:
                    return WeaponExpressionMode.None;
            }
        }

        /// <summary>
        /// 把被动暴露数据翻译成结果层数据。
        /// </summary>
        private static IReadOnlyList<PassiveExpressionExposedDatum> TranslateExposedData(
            List<PassiveExpressionExposedDatumConfig> configs)
        {
            List<PassiveExpressionExposedDatum> result = new List<PassiveExpressionExposedDatum>();
            if (configs == null)
            {
                return result;
            }

            for (int i = 0; i < configs.Count; i++)
            {
                PassiveExpressionExposedDatumConfig config = configs[i];
                if (config == null || string.IsNullOrWhiteSpace(config.DataKey))
                {
                    continue;
                }

                result.Add(new PassiveExpressionExposedDatum
                {
                    Key = config.DataKey,
                    Value = config.DataValue
                });
            }

            return result;
        }

        /// <summary>
        /// 对解释层生成的近战表面做一次浅复制，避免后续运行时直接回写上游对象。
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
        /// 对模块挂载快照做最小复制，避免运行时回写上游契约对象。
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
    }
}
