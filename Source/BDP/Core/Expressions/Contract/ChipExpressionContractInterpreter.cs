using System;
using System.Collections.Generic;
using BDP.Core.AttackExecution;
using BDP.Core.Chips;
using BDP.Core.CombatModel;
using BDP.Core.Expressions.Runtime;
using BDP.Core.Trigger;
using BDP.Core.Verbs;
using RimWorld;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// 芯片表达契约解释器。
    /// 它负责把作者写法翻译成主模组承认的契约。
    /// </summary>
    internal sealed class ChipExpressionContractInterpreter : IChipExpressionContractInterpreter
    {
        /// <summary>
        /// 当前解释器依赖的契约缓存。
        /// </summary>
        private readonly ExpressionContractCache contractCache;

        /// <summary>
        /// 使用默认缓存构造解释器。
        /// </summary>
        public ChipExpressionContractInterpreter()
            : this(new ExpressionContractCache())
        {
        }

        /// <summary>
        /// 使用指定缓存构造解释器。
        /// </summary>
        public ChipExpressionContractInterpreter(ExpressionContractCache contractCache)
        {
            this.contractCache = contractCache ?? new ExpressionContractCache();
        }

        /// <summary>
        /// 解析指定芯片在当前 Trigger 事实下应展开成的正式契约。
        /// </summary>
        public ChipExpressionResolvedContract Resolve(Thing chip, ITriggerLoadoutReader triggerLoadoutReader)
        {
            // 优先从中性实例提供器读取动态配置，回退到 Def 上的静态配置。
            ChipDefinitionConfig manufactured;
            ChipInstanceSurfaceAccess.TryGetDefinition(chip, out manufactured);
            ChipExpressionConfig config = manufactured != null
                ? manufactured.Expression
                : (chip != null && chip.def != null
                    ? chip.def.GetModExtension<ChipExpressionConfig>()
                    : null);
            return Resolve(chip, config, triggerLoadoutReader);
        }

        /// <summary>
        /// 使用指定表达配置解析芯片在当前 Trigger 事实下应展开成的正式契约。
        /// </summary>
        public ChipExpressionResolvedContract Resolve(
            Thing chip,
            ChipExpressionConfig config,
            ITriggerLoadoutReader triggerLoadoutReader)
        {
            string currentModeKey = triggerLoadoutReader != null ? triggerLoadoutReader.GetChipModeKey(chip) : null;

            // 芯片配置来自实例提供器，不适用 Def 级缓存——直接解释。
            return ResolveUncached(config, currentModeKey);
        }

        /// <summary>
        /// 解释一份芯片表达契约。
        /// </summary>
        private static ChipExpressionResolvedContract ResolveUncached(
            ChipExpressionConfig config,
            string currentModeKey)
        {
            ChipExpressionContractValidationResult validation = new ChipExpressionContractValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>()
            };

            if (config == null)
            {
                return new ChipExpressionResolvedContract
                {
                    Contract = new ChipExpressionContract
                    {
                        Entries = new List<ChipExpressionEntryContract>(),
                        DefaultModeKey = null,
                        Modes = new List<ChipExpressionModeContract>()
                    },
                    Validation = validation
                };
            }

            ChipExpressionStructureValidation structureValidation =
                ChipExpressionStructureValidator.Validate(config);
            CopyStructureValidation(structureValidation, validation);

            List<ChipExpressionModeContract> modes = TranslateModes(config.Modes);
            if (!structureValidation.IsValid)
            {
                return new ChipExpressionResolvedContract
                {
                    Contract = new ChipExpressionContract
                    {
                        Entries = new List<ChipExpressionEntryContract>(),
                        DefaultModeKey = config.DefaultModeKey,
                        Modes = modes
                    },
                    Validation = validation
                };
            }

            string effectiveModeKey = ResolveEffectiveModeKey(config, currentModeKey, validation);
            List<ChipExpressionEntryConfig> selectedConfigs =
                SelectActiveEntryConfigs(config, effectiveModeKey, validation);
            List<ChipExpressionEntryContract> entries =
                TranslateEntries(selectedConfigs, effectiveModeKey, validation);
            NormalizeVerbEntries(entries, validation);

            return new ChipExpressionResolvedContract
            {
                Contract = new ChipExpressionContract
                {
                    Entries = entries,
                    DefaultModeKey = config.DefaultModeKey,
                    Modes = modes
                },
                Validation = validation
            };
        }

        /// <summary>
        /// 翻译当前有效的条目集合。
        /// </summary>
        private static List<ChipExpressionEntryContract> TranslateEntries(
            List<ChipExpressionEntryConfig> configs,
            string effectiveModeKey,
            ChipExpressionContractValidationResult validation)
        {
            List<ChipExpressionEntryContract> result = new List<ChipExpressionEntryContract>();
            if (configs == null)
            {
                return result;
            }

            for (int i = 0; i < configs.Count; i++)
            {
                ChipExpressionEntryConfig config = configs[i];
                if (config == null)
                {
                    continue;
                }

                ValidateEntry(config, validation, "表达条目 " + config.Id);
                result.Add(TranslateEntry(config, effectiveModeKey));
            }

            return result;
        }

        /// <summary>
        /// 翻译形态契约集合。
        /// </summary>
        private static List<ChipExpressionModeContract> TranslateModes(
            List<ChipExpressionModeConfig> configs)
        {
            List<ChipExpressionModeContract> result = new List<ChipExpressionModeContract>();
            if (configs == null)
            {
                return result;
            }

            for (int i = 0; i < configs.Count; i++)
            {
                ChipExpressionModeConfig config = configs[i];
                if (config == null || string.IsNullOrWhiteSpace(config.ModeKey))
                {
                    continue;
                }

                result.Add(new ChipExpressionModeContract
                {
                    ModeKey = config.ModeKey,
                    DisplayLabel = config.DisplayLabel,
                    GizmoIconTexPath = string.IsNullOrWhiteSpace(config.GizmoIconTexPath)
                        ? null
                        : config.GizmoIconTexPath,
                    ActiveEntryIds = config.ActiveEntryIds != null
                        ? new List<string>(config.ActiveEntryIds)
                        : new List<string>()
                });
            }

            return result;
        }

        /// <summary>
        /// 把统一结构校验结果复制到表达契约校验结果。
        /// </summary>
        private static void CopyStructureValidation(
            ChipExpressionStructureValidation source,
            ChipExpressionContractValidationResult target)
        {
            if (source == null || target == null)
            {
                return;
            }

            for (int index = 0; index < source.Errors.Count; index++)
            {
                target.Errors.Add(source.Errors[index]);
            }

            for (int index = 0; index < source.Warnings.Count; index++)
            {
                target.Warnings.Add(source.Warnings[index]);
            }

            target.IsValid = source.IsValid;
        }

        /// <summary>
        /// 为缓存解析当前请求对应的稳定形态键。
        /// 空当前形态与显式默认形态共用缓存；未知形态保留原键以保存诊断结果。
        /// </summary>
        private static string ResolveCacheModeKey(
            ChipExpressionConfig config,
            string currentModeKey)
        {
            if (!HasModes(config))
            {
                return null;
            }

            ChipExpressionModeConfig currentMode = FindMode(config.Modes, currentModeKey);
            if (currentMode != null)
            {
                return currentMode.ModeKey;
            }

            return string.IsNullOrWhiteSpace(currentModeKey)
                ? config.DefaultModeKey
                : currentModeKey;
        }

        /// <summary>
        /// 解析本次真正采用的形态键。
        /// 未知运行形态回退默认形态，同时保留诊断警告。
        /// </summary>
        private static string ResolveEffectiveModeKey(
            ChipExpressionConfig config,
            string currentModeKey,
            ChipExpressionContractValidationResult validation)
        {
            if (!HasModes(config))
            {
                return null;
            }

            ChipExpressionModeConfig currentMode = FindMode(config.Modes, currentModeKey);
            if (currentMode != null)
            {
                return currentMode.ModeKey;
            }

            if (!string.IsNullOrWhiteSpace(currentModeKey) && validation != null)
            {
                validation.Warnings.Add(
                    "当前形态 " + currentModeKey
                    + " 不存在，已回退默认形态 " + config.DefaultModeKey + "。");
            }

            ChipExpressionModeConfig defaultMode = FindMode(config.Modes, config.DefaultModeKey);
            return defaultMode != null ? defaultMode.ModeKey : config.DefaultModeKey;
        }

        /// <summary>
        /// 按最终形态的引用顺序从统一目录选取表达条目。
        /// </summary>
        private static List<ChipExpressionEntryConfig> SelectActiveEntryConfigs(
            ChipExpressionConfig config,
            string effectiveModeKey,
            ChipExpressionContractValidationResult validation)
        {
            if (!HasModes(config))
            {
                return config != null && config.Entries != null
                    ? new List<ChipExpressionEntryConfig>(config.Entries)
                    : new List<ChipExpressionEntryConfig>();
            }

            Dictionary<string, ChipExpressionEntryConfig> entriesById =
                new Dictionary<string, ChipExpressionEntryConfig>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < config.Entries.Count; index++)
            {
                ChipExpressionEntryConfig entry = config.Entries[index];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Id))
                {
                    entriesById[entry.Id] = entry;
                }
            }

            List<ChipExpressionEntryConfig> result = new List<ChipExpressionEntryConfig>();
            ChipExpressionModeConfig mode = FindMode(config.Modes, effectiveModeKey);
            if (mode == null || mode.ActiveEntryIds == null)
            {
                validation.IsValid = false;
                validation.Errors.Add("无法找到最终形态 " + effectiveModeKey + " 的条目选择表。");
                return result;
            }

            for (int index = 0; index < mode.ActiveEntryIds.Count; index++)
            {
                string entryId = mode.ActiveEntryIds[index];
                ChipExpressionEntryConfig entry;
                if (!entriesById.TryGetValue(entryId, out entry))
                {
                    validation.IsValid = false;
                    validation.Errors.Add("最终形态 " + mode.ModeKey + " 引用了不存在的表达条目 " + entryId + "。");
                    continue;
                }

                result.Add(entry);
            }

            return result;
        }

        /// <summary>
        /// 判断表达配置是否声明了至少一个形态。
        /// </summary>
        private static bool HasModes(ChipExpressionConfig config)
        {
            return config != null && config.Modes != null && config.Modes.Count > 0;
        }

        /// <summary>
        /// 按不区分大小写的稳定键查找形态。
        /// </summary>
        private static ChipExpressionModeConfig FindMode(
            List<ChipExpressionModeConfig> modes,
            string modeKey)
        {
            if (modes == null || string.IsNullOrWhiteSpace(modeKey))
            {
                return null;
            }

            for (int index = 0; index < modes.Count; index++)
            {
                ChipExpressionModeConfig mode = modes[index];
                if (mode != null
                    && string.Equals(mode.ModeKey, modeKey, StringComparison.OrdinalIgnoreCase))
                {
                    return mode;
                }
            }

            return null;
        }

        /// <summary>
        /// 翻译单条统一条目配置。
        /// </summary>
        private static ChipExpressionEntryContract TranslateEntry(ChipExpressionEntryConfig config, string modeKey)
        {
            List<Tool> declaredTools = ResolveDeclaredTools(config);
            Tool resolvedTool = ResolvePrimaryDeclaredTool(declaredTools);
            VerbProperties resolvedVerbProps = ResolveVerbProps(config, resolvedTool);
            ManeuverDef resolvedManeuver = ResolveManeuver(config, resolvedTool);
            IReadOnlyList<MeleeToolSurface> declaredMeleeToolSurfaces = BuildDeclaredMeleeToolSurfaces(
                config,
                declaredTools);
            ResolvedVerbSpec resolvedVerbSpec = ResolvedVerbSpecFactory.FromDeclared(
                resolvedVerbProps,
                resolvedTool,
                declaredTools,
                declaredMeleeToolSurfaces,
                resolvedManeuver,
                config.DirectTargetLineOfSight,
                config.ProjectileOverrides);
            return new ChipExpressionEntryContract
            {
                Id = config.Id,
                DisplayLabel = config.DisplayLabel,
                ManualEntryIconTexPath = config.Presentation != null ? config.Presentation.ManualEntryIconTexPath : null,
                VisualPresetDefName = config.Presentation != null ? config.Presentation.VisualPresetDefName : null,
                CompositeVisualPresetDefName = config.Presentation != null ? config.Presentation.CompositeVisualPresetDefName : null,
                ForceSuppressHostEquipment = config.Presentation != null && config.Presentation.ForceSuppressHostEquipment,
                VisualPriority = config.Presentation != null ? config.Presentation.VisualPriority : 0,
                Kind = TranslateEntryKind(config.Kind),
                VerbAttackRole = ResolveDeclaredVerbAttackRole(config.Kind),
                RoleKey = config.RoleKey,
                Tags = config.Tags != null ? new List<string>(config.Tags) : new List<string>(),
                Conditions = config.Conditions != null
                    ? new List<ExpressionSourceConditionConfig>(config.Conditions)
                    : new List<ExpressionSourceConditionConfig>(),
                Trion = config.Trion,
                RelationKind = TranslateRelationKind(config.RelationKind),
                ParentEntryId = config.ParentEntryId,
                WeaponMode = config.WeaponMode,
                ModeKey = modeKey,
                ExecutionStyle = ResolveExecutionStyle(config, resolvedTool),
                VerbProps = ResolvedVerbSpecFactory.CreateSurfaceVerbProps(resolvedVerbSpec),
                ResolvedVerbSpec = resolvedVerbSpec,
                Tool = resolvedTool,
                DeclaredTools = declaredTools,
                DeclaredMeleeToolSurfaces = declaredMeleeToolSurfaces,
                Maneuver = resolvedManeuver,
                AbilityDefName = config.AbilityDefName,
                HediffDefName = config.HediffDefName,
                HediffApplyModeKey = config.HediffApplyModeKey,
                PassiveKey = config.PassiveKey,
                ExposedData = config.ExposedData,
                SemanticSourceKind = config.SemanticSourceKind,
                RangedModules = config.RangedModules != null
                    ? CloneRangedModules(config.RangedModules)
                    : new List<RangedModuleMountConfig>(),
                ProjectileOverrides = config.ProjectileOverrides
            };
        }

        /// <summary>
        /// 翻译条目种类。
        /// </summary>
        private static ChipExpressionEntryKind TranslateEntryKind(ChipExpressionEntryKindConfig kind)
        {
            switch (kind)
            {
                case ChipExpressionEntryKindConfig.SecondaryVerb:
                    return ChipExpressionEntryKind.SecondaryVerb;
                case ChipExpressionEntryKindConfig.Ability:
                    return ChipExpressionEntryKind.Ability;
                case ChipExpressionEntryKindConfig.Hediff:
                    return ChipExpressionEntryKind.Hediff;
                case ChipExpressionEntryKindConfig.Passive:
                    return ChipExpressionEntryKind.Passive;
                default:
                    return ChipExpressionEntryKind.PrimaryVerb;
            }
        }

        /// <summary>
        /// 把作者写法的 Verb 主副意图翻译成内部主副标记。
        /// </summary>
        private static VerbAttackRole ResolveDeclaredVerbAttackRole(ChipExpressionEntryKindConfig kind)
        {
            switch (kind)
            {
                case ChipExpressionEntryKindConfig.SecondaryVerb:
                    return VerbAttackRole.Secondary;
                case ChipExpressionEntryKindConfig.PrimaryVerb:
                    return VerbAttackRole.Primary;
                default:
                    return VerbAttackRole.None;
            }
        }

        /// <summary>
        /// 翻译轻量关系种类。
        /// </summary>
        private static ChipExpressionRelationKind TranslateRelationKind(ChipExpressionRelationKindConfig kind)
        {
            switch (kind)
            {
                case ChipExpressionRelationKindConfig.Attached:
                    return ChipExpressionRelationKind.Attached;
                default:
                    return ChipExpressionRelationKind.Independent;
            }
        }

        /// <summary>
        /// 校验单条统一条目是否满足最小契约要求。
        /// </summary>
        private static void ValidateEntry(
            ChipExpressionEntryConfig config,
            ChipExpressionContractValidationResult validation,
            string context)
        {
            if (config == null || validation == null)
            {
                return;
            }

            IReadOnlyList<string> sustainErrors = ExpressionSustainCostPolicy.Validate(
                config.Trion != null ? config.Trion.SustainCostBySourceCount : null,
                context);
            for (int i = 0; i < sustainErrors.Count; i++)
            {
                validation.IsValid = false;
                validation.Errors.Add(sustainErrors[i]);
            }

            switch (config.Kind)
            {
                case ChipExpressionEntryKindConfig.PrimaryVerb:
                case ChipExpressionEntryKindConfig.SecondaryVerb:
                    List<Tool> declaredTools = ResolveDeclaredTools(config);
                    Tool resolvedTool = ResolvePrimaryDeclaredTool(declaredTools);
                    bool meleeEntry = IsMeleeEntry(config);

                    if (!meleeEntry && config.VerbProps == null)
                    {
                        validation.IsValid = false;
                        validation.Errors.Add(context + " 缺少 VerbProps。");
                    }

                    if (config.VerbProps != null && config.VerbProps.verbClass == null)
                    {
                        validation.IsValid = false;
                        validation.Errors.Add(context + " 的 VerbProps 缺少 verbClass。");
                    }

                    if (meleeEntry && resolvedTool == null)
                    {
                        validation.IsValid = false;
                        validation.Errors.Add(context + " 是近战 Verb，但缺少 Tool。");
                    }

                    if (meleeEntry && config.VerbProps == null)
                    {
                        validation.Warnings.Add(context + " 未显式声明 VerbProps，将按 Tool 自动合成最小近战 VerbProps。");
                    }

                    if (meleeEntry && config.Maneuver == null)
                    {
                        validation.Warnings.Add(context + " 是近战 Verb，但未显式声明 Maneuver，将按 Tool capacity 自动推导。");
                    }

                    ValidateExecutionStyle(config, context, meleeEntry, validation);
                    break;

                case ChipExpressionEntryKindConfig.Ability:
                    if (string.IsNullOrWhiteSpace(config.AbilityDefName))
                    {
                        validation.IsValid = false;
                        validation.Errors.Add(context + " 缺少 AbilityDefName。");
                    }
                    break;

                case ChipExpressionEntryKindConfig.Hediff:
                    if (string.IsNullOrWhiteSpace(config.HediffDefName))
                    {
                        validation.IsValid = false;
                        validation.Errors.Add(context + " 缺少 HediffDefName。");
                    }
                    break;

                case ChipExpressionEntryKindConfig.Passive:
                    if (string.IsNullOrWhiteSpace(config.PassiveKey))
                    {
                        validation.IsValid = false;
                        validation.Errors.Add(context + " 缺少 PassiveKey。");
                    }
                    break;
            }

        }

        /// <summary>
        /// 判断当前 Verb 条目是否应按近战初始化链校验 Tool。
        /// </summary>
        private static bool IsMeleeEntry(ChipExpressionEntryConfig config)
        {
            return config != null
                && (config.WeaponMode == VerbExpressionModeConfig.Melee
                    || (config.VerbProps != null && config.VerbProps.IsMeleeAttack));
        }

        /// <summary>
        /// 解析作者声明的全部 Tool。
        /// 显式单 Tool 会被并入多 Tool 候选集，最终顺序以作者书写顺序为准。
        /// </summary>
        private static List<Tool> ResolveDeclaredTools(ChipExpressionEntryConfig config)
        {
            List<Tool> result = new List<Tool>();
            if (config == null)
            {
                return result;
            }

            if (config.Tool != null)
            {
                result.Add(config.Tool);
            }

            if (config.tools == null)
            {
                return result;
            }

            for (int i = 0; i < config.tools.Count; i++)
            {
                Tool declaredTool = config.tools[i];
                if (declaredTool == null || result.Contains(declaredTool))
                {
                    continue;
                }

                result.Add(declaredTool);
            }

            return result;
        }

        /// <summary>
        /// 解析当前条目的主 Tool。
        /// 多 Tool 条目仍会保留完整集合，这里只负责给仍需单 Tool 入口的正式对象取首项。
        /// </summary>
        private static Tool ResolvePrimaryDeclaredTool(IReadOnlyList<Tool> declaredTools)
        {
            return declaredTools != null && declaredTools.Count > 0
                ? declaredTools[0]
                : null;
        }

        /// <summary>
        /// 为当前条目声明的每把 Tool 生成一份近战运行时表面。
        /// 多 tool 仍读取原版字段，只是不再在解释入口丢掉后续项。
        /// </summary>
        private static IReadOnlyList<MeleeToolSurface> BuildDeclaredMeleeToolSurfaces(
            ChipExpressionEntryConfig config,
            IReadOnlyList<Tool> declaredTools)
        {
            List<MeleeToolSurface> result = new List<MeleeToolSurface>();
            if (config == null || !IsMeleeEntry(config) || declaredTools == null)
            {
                return result;
            }

            for (int i = 0; i < declaredTools.Count; i++)
            {
                Tool declaredTool = declaredTools[i];
                if (declaredTool == null)
                {
                    continue;
                }

                result.Add(new MeleeToolSurface
                {
                    Tool = declaredTool,
                    VerbProps = ResolveVerbProps(config, declaredTool),
                    Maneuver = ResolveManeuver(config, declaredTool),
                    DamageDef = ResolveDamageDef(declaredTool),
                    DeclaredIndex = i
                });
            }

            return result;
        }

        /// <summary>
        /// 解析最终要进入正式契约的执行风格。
        /// 作者优先写统一 Execution；若缺省，则按武器模式补最小默认风格。
        /// </summary>
        private static AttackExecutionStyle ResolveExecutionStyle(ChipExpressionEntryConfig config, Tool tool)
        {
            if (config == null)
            {
                return null;
            }

            AttackExecutionStyle declaredStyle = TranslateDeclaredExecution(config);
            if (declaredStyle != null)
            {
                return declaredStyle;
            }

            if (IsMeleeEntry(config))
            {
                return new AttackExecutionStyle
                {
                    Single = new SingleAttackExecutionStyle
                    {
                        MeleeRhythm = MeleeExecutionRhythm.SingleHit,
                        meleeHitCount = 1,
                        meleeHitIntervalTicks = 0
                    }
                };
            }

            return new AttackExecutionStyle
            {
                Single = new SingleAttackExecutionStyle
                {
                    RangedRhythm = RangedExecutionRhythm.Sequential
                }
            };
        }

        /// <summary>
        /// 把作者侧统一 Execution 写法翻译成内部正式执行风格。
        /// 这一层负责把直观字段映射成内部细分模型。
        /// </summary>
        private static AttackExecutionStyle TranslateDeclaredExecution(ChipExpressionEntryConfig config)
        {
            if (config?.Execution == null)
            {
                return null;
            }

            ChipAttackExecutionConfig execution = config.Execution;
            bool meleeEntry = IsMeleeEntry(config);
            int hitCount = execution.HitCount > 0 ? execution.HitCount : 1;
            int hitIntervalTicks = execution.HitIntervalTicks > 0 ? execution.HitIntervalTicks : 0;

            if (meleeEntry)
            {
                return new AttackExecutionStyle
                {
                    Single = new SingleAttackExecutionStyle
                    {
                        MeleeRhythm = TranslateMeleeRhythm(hitCount),
                        meleeHitCount = hitCount,
                        meleeHitIntervalTicks = hitIntervalTicks
                    }
                };
            }

            SingleAttackExecutionStyle rangedStyle = new SingleAttackExecutionStyle
            {
                RangedRhythm = TranslateRangedRhythm(execution.Rhythm)
            };
            ApplyOriginSpreadRange(rangedStyle, execution.OriginSpread);

            return new AttackExecutionStyle
            {
                Single = rangedStyle
            };
        }

        /// <summary>
        /// 把作者侧发射点随机散布区间翻译成正式执行风格。
        /// </summary>
        private static void ApplyOriginSpreadRange(
            SingleAttackExecutionStyle target,
            ChipAttackOriginSpreadConfig source)
        {
            if (target == null || source == null)
            {
                return;
            }

            target.HasOriginSpreadRange = source.LateralMin != 0f
                || source.LateralMax != 0f
                || source.ForwardMin != 0f
                || source.ForwardMax != 0f;
            target.OriginSpreadLateralMin = Min(source.LateralMin, source.LateralMax);
            target.OriginSpreadLateralMax = Max(source.LateralMin, source.LateralMax);
            target.OriginSpreadForwardMin = Min(source.ForwardMin, source.ForwardMax);
            target.OriginSpreadForwardMax = Max(source.ForwardMin, source.ForwardMax);
        }

        /// <summary>
        /// 返回两个 float 中较小的值。
        /// </summary>
        private static float Min(float first, float second)
        {
            return first < second ? first : second;
        }

        /// <summary>
        /// 返回两个 float 中较大的值。
        /// </summary>
        private static float Max(float first, float second)
        {
            return first > second ? first : second;
        }

        /// <summary>
        /// 把近战正式节奏从 HitCount 派生出来。
        /// 近战节奏只由统一 Execution 中的命中次数决定。
        /// </summary>
        private static MeleeExecutionRhythm TranslateMeleeRhythm(int hitCount)
        {
            return hitCount > 1
                ? MeleeExecutionRhythm.MultiHit
                : MeleeExecutionRhythm.SingleHit;
        }

        /// <summary>
        /// 把作者侧统一远程节奏翻译成正式远程节奏。
        /// 未声明时默认 Sequential。
        /// </summary>
        private static RangedExecutionRhythm TranslateRangedRhythm(ChipAttackExecutionRhythmConfig rhythm)
        {
            switch (rhythm)
            {
                case ChipAttackExecutionRhythmConfig.Simultaneous:
                    return RangedExecutionRhythm.Simultaneous;
                default:
                    return RangedExecutionRhythm.Sequential;
            }
        }

        /// <summary>
        /// 解析最终要进入正式契约的 VerbProps。
        /// 近战作者省略 VerbProps 时，这里按最小规则自动合成。
        /// </summary>
        private static VerbProperties ResolveVerbProps(ChipExpressionEntryConfig config, Tool tool)
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

            return new VerbProperties
            {
                verbClass = typeof(BdpVerb_MeleeAttackDamage),
                hasStandardCommand = true,
                label = !string.IsNullOrWhiteSpace(tool.label)
                    ? tool.label
                    : (!string.IsNullOrWhiteSpace(config.DisplayLabel)
                        ? config.DisplayLabel
                        : "BDP_Message_Expression_DefaultMeleeAttack".Translate().ToString()),
                range = 1.42f,
                meleeDamageBaseAmount = tool.power > 0f ? (int)Math.Round(tool.power) : 1,
                meleeDamageDef = ResolveDamageDef(tool),
                defaultCooldownTime = tool.cooldownTime > 0f ? tool.cooldownTime : 1f
            };
        }

        /// <summary>
        /// 解析最终要进入正式契约的 Maneuver。
        /// 作者未显式声明时，按 Tool 的首个 capacity 推导。
        /// </summary>
        private static ManeuverDef ResolveManeuver(ChipExpressionEntryConfig config, Tool tool)
        {
            if (config?.Maneuver != null)
            {
                return config.Maneuver;
            }

            ToolCapacityDef capacity = ResolvePrimaryCapacity(tool);
            if (capacity == null)
            {
                return null;
            }

            List<ManeuverDef> allDefs = DefDatabase<ManeuverDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ManeuverDef current = allDefs[i];
                if (current != null && current.requiredCapacity == capacity)
                {
                    return current;
                }
            }

            return allDefs.Count > 0 ? allDefs[0] : null;
        }

        /// <summary>
        /// 从 Tool 推导近战伤害类型。
        /// </summary>
        private static DamageDef ResolveDamageDef(Tool tool)
        {
            ToolCapacityDef capacity = ResolvePrimaryCapacity(tool);
            string capacityDefName = capacity != null ? capacity.defName : null;
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
        /// 读取 Tool 的首个 capacity。
        /// </summary>
        private static ToolCapacityDef ResolvePrimaryCapacity(Tool tool)
        {
            return tool != null
                && tool.capacities != null
                && tool.capacities.Count > 0
                ? tool.capacities[0]
                : null;
        }

        /// <summary>
        /// 对条目声明的模块挂载列表做最小快照复制。
        /// 当前只冻结顺序与显式配置，不在这里解释业务语义。
        /// </summary>
        private static IReadOnlyList<RangedModuleMountConfig> CloneRangedModules(
            IReadOnlyList<RangedModuleMountConfig> configs)
        {
            List<RangedModuleMountConfig> result = new List<RangedModuleMountConfig>();
            if (configs == null)
            {
                return result;
            }

            for (int i = 0; i < configs.Count; i++)
            {
                RangedModuleMountConfig config = configs[i];
                if (config == null)
                {
                    continue;
                }

                result.Add(config.Clone());
            }

            return result;
        }

        /// <summary>
        /// 校验作者声明的执行风格是否满足当前条目的最低结构要求。
        /// 这一层只做单条结构校验，不做跨结果全局推导。
        /// </summary>
        private static void ValidateExecutionStyle(
            ChipExpressionEntryConfig config,
            string context,
            bool meleeEntry,
            ChipExpressionContractValidationResult validation)
        {
            if (config == null || validation == null)
            {
                return;
            }

            AttackExecutionStyle style = TranslateDeclaredExecution(config);
            if (style == null)
            {
                return;
            }

            if (!meleeEntry
                && config.Execution != null
                && config.Execution.Rhythm == ChipAttackExecutionRhythmConfig.Simultaneous
                && config.Execution.HitIntervalTicks > 0)
            {
                validation.Warnings.Add(context + " 声明为远程齐射时，Execution.HitIntervalTicks 只服务近战，解释器已按 0 处理。");
            }

            if (style.Dual != null && style.Dual.Schedule != DualExecutionSchedule.None)
            {
                validation.IsValid = false;
                validation.Errors.Add(context + " 是单侧条目，不得声明 Dual.Schedule。");
            }

            SingleAttackExecutionStyle single = style.Single;
            if (single == null)
            {
                return;
            }

            if (meleeEntry)
            {
                if (single.RangedRhythm != RangedExecutionRhythm.None)
                {
                    validation.IsValid = false;
                    validation.Errors.Add(context + " 是近战 Verb，不得声明 RangedRhythm。");
                }

                if (config.Execution != null
                    && config.Execution.Rhythm != ChipAttackExecutionRhythmConfig.None
                    && config.Execution.Rhythm != ChipAttackExecutionRhythmConfig.Normal)
                {
                    validation.IsValid = false;
                    validation.Errors.Add(context + " 是近战 Verb，但 Execution.Rhythm 现阶段只能缺省或写 Normal。");
                }

                if (single.MeleeRhythm == MeleeExecutionRhythm.MultiHit && single.meleeHitCount < 2)
                {
                    validation.IsValid = false;
                    validation.Errors.Add(context + " 声明为 MultiHit，但 meleeHitCount 小于 2。");
                }
            }
            else
            {
                if (config.Execution != null && config.Execution.HitCount > 0)
                {
                    validation.Warnings.Add(context + " 是远程 Verb，Execution.HitCount 只服务近战，实际发射数读取 VerbProps.burstShotCount。");
                }

                if (config.Execution != null && config.Execution.HitIntervalTicks > 0)
                {
                    validation.Warnings.Add(context + " 是远程 Verb，Execution.HitIntervalTicks 只服务近战，实际间隔读取 VerbProps.ticksBetweenBurstShots。");
                }

                if (single.MeleeRhythm != MeleeExecutionRhythm.None)
                {
                    validation.IsValid = false;
                    validation.Errors.Add(context + " 是远程 Verb，不得声明 MeleeRhythm。");
                }

                if (config.Execution != null
                    && config.Execution.Rhythm != ChipAttackExecutionRhythmConfig.None
                    && config.Execution.Rhythm != ChipAttackExecutionRhythmConfig.Sequential
                    && config.Execution.Rhythm != ChipAttackExecutionRhythmConfig.Simultaneous)
                {
                    validation.IsValid = false;
                    validation.Errors.Add(context + " 是远程 Verb，但 Execution.Rhythm 只能写 Sequential 或 Simultaneous。");
                }

            }
        }

        /// <summary>
        /// 对最终生效的 Verb 条目做正规化。
        /// 规则是最终结果最多一主一副，且不允许只有副没有主。
        /// </summary>
        private static void NormalizeVerbEntries(
            List<ChipExpressionEntryContract> entries,
            ChipExpressionContractValidationResult validation)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            List<ChipExpressionEntryContract> verbEntries = new List<ChipExpressionEntryContract>();
            for (int i = 0; i < entries.Count; i++)
            {
                ChipExpressionEntryContract entry = entries[i];
                if (entry != null && IsVerbEntry(entry.Kind))
                {
                    verbEntries.Add(entry);
                }
            }

            if (verbEntries.Count == 0)
            {
                return;
            }

            if (verbEntries.Count > 2)
            {
                validation.IsValid = false;
                validation.Errors.Add("一枚芯片最多只允许两条 Verb 条目。");
                return;
            }

            ChipExpressionEntryContract primary = null;
            for (int i = 0; i < verbEntries.Count; i++)
            {
                if (verbEntries[i].VerbAttackRole == VerbAttackRole.Primary)
                {
                    primary = verbEntries[i];
                    break;
                }
            }

            if (primary == null)
            {
                primary = verbEntries[0];
            }

            ChipExpressionEntryContract secondary = null;
            for (int i = 0; i < verbEntries.Count; i++)
            {
                if (!ReferenceEquals(verbEntries[i], primary))
                {
                    secondary = verbEntries[i];
                    break;
                }
            }

            primary.Kind = ChipExpressionEntryKind.PrimaryVerb;
            primary.VerbAttackRole = VerbAttackRole.Primary;

            if (secondary != null)
            {
                secondary.Kind = ChipExpressionEntryKind.SecondaryVerb;
                secondary.VerbAttackRole = VerbAttackRole.Secondary;
            }
        }

        /// <summary>
        /// 判断当前条目是否属于 Verb 通道。
        /// </summary>
        private static bool IsVerbEntry(ChipExpressionEntryKind kind)
        {
            return kind == ChipExpressionEntryKind.PrimaryVerb
                || kind == ChipExpressionEntryKind.SecondaryVerb;
        }
    }
}
