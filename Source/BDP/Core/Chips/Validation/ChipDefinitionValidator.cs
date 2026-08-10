using System;
using System.Collections.Generic;
using BDP.Core.Requirements;
using BDP.Core.CombatModel;
using BDP.Core.Expressions;
using RimWorld;
using Verse;

namespace BDP.Core.Chips
{
    /// <summary>
    /// 默认芯片定义最低合法性校验器。
    /// </summary>
    internal sealed class ChipDefinitionValidator
    {
        /// <summary>
        /// 校验指定芯片契约是否满足最低正式要求。
        /// </summary>
        public ChipDefinitionValidationResult Validate(ChipDefinitionContract contract)
        {
            List<ChipDefinitionValidationMessage> errors = new List<ChipDefinitionValidationMessage>();
            List<ChipDefinitionValidationMessage> warnings = new List<ChipDefinitionValidationMessage>();

            if (contract == null)
            {
                return new ChipDefinitionValidationResult
                {
                    IsValid = false,
                    Errors = errors,
                    Warnings = warnings
                };
            }

            if (contract.Profile == null)
            {
                errors.Add(BuildMessage(
                    "ProfileMissing",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Profile,
                    "该芯片缺少画像声明块，主模组无法判断它是什么类型的芯片。"));
            }

            if (contract.Loadout == null)
            {
                errors.Add(BuildMessage(
                    "LoadoutMissing",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Loadout,
                    "该芯片缺少装载声明块，Trigger 无法按正式规则装载它。"));
            }

            if (contract.Expression == null || !contract.Expression.HasExpressionBlock)
            {
                errors.Add(BuildMessage(
                    "ExpressionMissing",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Expression,
                    "该芯片缺少表达声明块，主模组无法知道它能表达什么。"));
            }

            if (contract.Trion == null)
            {
                errors.Add(BuildMessage(
                    "TrionMissing",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Trion,
                    "该芯片缺少本体级 Trion 声明块，主模组无法判断装上和启动它本身的代价。"));
            }

            ValidateProfile(contract.Profile, errors);
            ValidateLoadout(contract.Loadout, errors);
            ValidateExpression(contract.Expression, errors, warnings);
            ValidateTrion(contract.Trion, warnings);
            ValidateActivationRequirements(contract.ActivationRequirements, errors);
            ValidateExtensions(contract.Extensions, errors);

            return new ChipDefinitionValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            };
        }

        /// <summary>
        /// 校验画像声明块。
        /// </summary>
        private static void ValidateProfile(
            ChipProfileContract profile,
            List<ChipDefinitionValidationMessage> errors)
        {
            if (profile == null)
            {
                return;
            }

            if (profile.Category == null)
            {
                errors.Add(BuildMessage(
                    "ChipCategoryMissing",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Profile,
                    "该芯片没有引用已登记的芯片分类，信息系统无法知道它属于哪一类芯片。"));
            }

            HashSet<ChipTagDef> seenTags = new HashSet<ChipTagDef>();
            foreach (ChipTagDef tag in profile.Tags ?? new List<ChipTagDef>())
            {
                if (tag == null)
                {
                    errors.Add(BuildMessage(
                        "ChipTagMissing",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Profile,
                        "该芯片的标签列表包含空项；每个标签都必须引用已登记的芯片标签定义。"));
                    continue;
                }

                if (!seenTags.Add(tag))
                {
                    errors.Add(BuildMessage(
                        "ChipTagDuplicate",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Profile,
                        "该芯片重复声明了同一个特征标签；请保留一项即可。"));
                }
            }
        }

        /// <summary>
        /// 校验装载声明块。
        /// </summary>
        private static void ValidateLoadout(
            ChipLoadoutContract loadout,
            List<ChipDefinitionValidationMessage> errors)
        {
            if (loadout == null)
            {
                return;
            }

            bool isKnownRegion = System.Enum.IsDefined(
                typeof(ChipSlotRegion),
                loadout.SlotRegion);
            if (!isKnownRegion || loadout.SlotRegion == ChipSlotRegion.Unspecified)
            {
                errors.Add(BuildMessage(
                    "SlotRegionMissingOrInvalid",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Loadout,
                    "该芯片没有填写合法的槽位区域；必须明确选择主副槽区或特殊槽区。"));
            }

            bool isKnownOccupancy = System.Enum.IsDefined(
                typeof(ChipSlotOccupancy),
                loadout.SlotOccupancy);
            if (!isKnownOccupancy || loadout.SlotOccupancy == ChipSlotOccupancy.Unspecified)
            {
                errors.Add(BuildMessage(
                    "SlotOccupancyMissingOrInvalid",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Loadout,
                    "该芯片没有填写合法的槽位占用方式；必须明确选择单槽或成对主副槽。"));
            }

            if (loadout.SlotRegion == ChipSlotRegion.Special
                && loadout.SlotOccupancy == ChipSlotOccupancy.PairedHands)
            {
                errors.Add(BuildMessage(
                    "SlotOccupancyRegionConflict",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Loadout,
                    "特殊槽区不能使用成对主副槽占用；请把占用方式改为单槽，或把槽位区域改为主副槽区。"));
            }

            if (loadout.ActivationDelayTicks < -1)
            {
                errors.Add(BuildMessage(
                    "ActivationDelayTicksInvalid",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Loadout,
                    "芯片启用延迟只能未填写、填写0立即完成，或填写正整数游戏刻。"));
            }

            if (loadout.DeactivationDelayTicks < -1)
            {
                errors.Add(BuildMessage(
                    "DeactivationDelayTicksInvalid",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Loadout,
                    "芯片停用延迟只能未填写、填写0立即完成，或填写正整数游戏刻。"));
            }

            HashSet<ChipExclusionGroupDef> seenExclusionGroups =
                new HashSet<ChipExclusionGroupDef>();
            foreach (ChipExclusionGroupDef exclusionGroup in
                loadout.ActivationExclusionGroups ?? new List<ChipExclusionGroupDef>())
            {
                if (exclusionGroup == null)
                {
                    errors.Add(BuildMessage(
                        "ActivationExclusionGroupMissing",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Loadout,
                        "该芯片的启用互斥组中存在空引用；请删除空项或引用有效的互斥组定义。"));
                    continue;
                }

                if (!seenExclusionGroups.Add(exclusionGroup))
                {
                    errors.Add(BuildMessage(
                        "ActivationExclusionGroupDuplicate",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Loadout,
                        "该芯片重复引用了同一个启用互斥组；同一组只应填写一次。"));
                }
            }
        }

        /// <summary>
        /// 校验表达声明块。
        /// </summary>
        private static void ValidateExpression(
            ChipExpressionContractHandle expression,
            List<ChipDefinitionValidationMessage> errors,
            List<ChipDefinitionValidationMessage> warnings)
        {
            if (expression == null || !expression.HasExpressionBlock)
            {
                return;
            }

            if (expression.Config == null)
            {
                errors.Add(BuildMessage(
                    "ExpressionBlockInvalid",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Expression,
                    "该芯片声明了表达块，但表达块配置不存在，表达系统不会接受它。"));
                return;
            }

            if ((expression.Config.Entries == null || expression.Config.Entries.Count == 0)
                && (expression.Config.Modes == null || expression.Config.Modes.Count == 0))
            {
                errors.Add(BuildMessage(
                    "ExpressionBlockEmpty",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Expression,
                    "该芯片声明了表达块，但表达块里没有任何条目或形态，表达系统不会接受它。"));
            }

            ChipExpressionStructureValidation structureValidation =
                ChipExpressionStructureValidator.Validate(expression.Config);
            for (int index = 0; index < structureValidation.Errors.Count; index++)
            {
                errors.Add(BuildMessage(
                    "ExpressionStructureInvalid",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Expression,
                    structureValidation.Errors[index]));
            }

            for (int index = 0; index < structureValidation.Warnings.Count; index++)
            {
                warnings.Add(BuildMessage(
                    "ExpressionStructureWarning",
                    ChipDefinitionValidationSeverity.Warning,
                    ChipDefinitionDeclaredBlock.Expression,
                    structureValidation.Warnings[index]));
            }

            ValidateExpressionEntries(expression.Config.Entries, "基础条目", errors, warnings);
        }

        /// <summary>
        /// 校验基础表达条目集合。
        /// </summary>
        private static void ValidateExpressionEntries(
            List<BDP.Core.Expressions.ChipExpressionEntryConfig> entries,
            string scope,
            List<ChipDefinitionValidationMessage> errors,
            List<ChipDefinitionValidationMessage> warnings)
        {
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                BDP.Core.Expressions.ChipExpressionEntryConfig entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                string context = scope + " " + (!string.IsNullOrWhiteSpace(entry.Id) ? entry.Id : "#" + i);
                ValidateExpressionEntry(entry, context, errors, warnings);
            }
        }

        /// <summary>
        /// 校验单条表达条目的正式必填项。
        /// </summary>
        private static void ValidateExpressionEntry(
            BDP.Core.Expressions.ChipExpressionEntryConfig entry,
            string context,
            List<ChipDefinitionValidationMessage> errors,
            List<ChipDefinitionValidationMessage> warnings)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.Kind == BDP.Core.Expressions.ChipExpressionEntryKindConfig.PrimaryVerb
                || entry.Kind == BDP.Core.Expressions.ChipExpressionEntryKindConfig.SecondaryVerb)
            {
                bool meleeEntry = IsMeleeEntry(entry);
                Tool resolvedTool = ResolvePrimaryDeclaredTool(ResolveDeclaredTools(entry));

                if (!meleeEntry && entry.VerbProps == null)
                {
                    errors.Add(BuildMessage(
                        "ExpressionVerbPropsMissing",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 缺少 VerbProps。"));
                    return;
                }

                if (entry.VerbProps != null && entry.VerbProps.verbClass == null)
                {
                    errors.Add(BuildMessage(
                        "ExpressionVerbClassMissing",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 的 VerbProps 缺少 verbClass。"));
                }

                if (meleeEntry && resolvedTool == null)
                {
                    errors.Add(BuildMessage(
                        "ExpressionMeleeToolMissing",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 是近战 Verb，但缺少 Tool。"));
                }

                if (meleeEntry && entry.VerbProps == null)
                {
                    warnings.Add(BuildMessage(
                        "ExpressionMeleeVerbPropsSynthesized",
                        ChipDefinitionValidationSeverity.Warning,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 未显式声明 VerbProps，将按 Tool 自动合成最小近战 VerbProps。"));
                }

                if (meleeEntry && entry.Maneuver == null)
                {
                    warnings.Add(BuildMessage(
                        "ExpressionMeleeManeuverMissing",
                        ChipDefinitionValidationSeverity.Warning,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 是近战 Verb，但未显式声明 Maneuver，将按 Tool capacity 自动推导。"));
                }

                ValidateExecutionStyle(entry, context, meleeEntry, errors, warnings);
                return;
            }

            if (entry.Kind == BDP.Core.Expressions.ChipExpressionEntryKindConfig.Ability)
            {
                ValidateAbilityExpressionEntry(entry, context, errors);
                return;
            }

            if (entry.Kind == BDP.Core.Expressions.ChipExpressionEntryKindConfig.Hediff)
            {
                ValidateHediffExpressionEntry(entry, context, errors, warnings);
                return;
            }

            if (entry.Kind == BDP.Core.Expressions.ChipExpressionEntryKindConfig.Passive)
            {
                ValidatePassiveExpressionEntry(entry, context, errors);
            }
        }

        /// <summary>
        /// 校验 Ability 条目的最低合法性。
        /// </summary>
        private static void ValidateAbilityExpressionEntry(
            BDP.Core.Expressions.ChipExpressionEntryConfig entry,
            string context,
            List<ChipDefinitionValidationMessage> errors)
        {
            if (entry == null || errors == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.AbilityDefName))
            {
                errors.Add(BuildMessage(
                    "ExpressionAbilityDefMissing",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Expression,
                    context + " 缺少 AbilityDefName。"));
            }
        }

        /// <summary>
        /// 校验 Hediff 条目的最低合法性与首轮业务边界。
        /// </summary>
        private static void ValidateHediffExpressionEntry(
            BDP.Core.Expressions.ChipExpressionEntryConfig entry,
            string context,
            List<ChipDefinitionValidationMessage> errors,
            List<ChipDefinitionValidationMessage> warnings)
        {
            if (entry == null || errors == null || warnings == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.HediffDefName))
            {
                errors.Add(BuildMessage(
                    "ExpressionHediffDefMissing",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Expression,
                    context + " 缺少 HediffDefName。"));
                return;
            }

            HediffDef resolvedHediffDef = ResolveDeclaredHediffDef(entry.HediffDefName);
            if (resolvedHediffDef == null)
            {
                errors.Add(BuildMessage(
                    "ExpressionHediffDefNotFound",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Expression,
                    context + " 指向的 HediffDef 不存在，表达系统无法把它发布到原版 Hediff 宿主。"));
                return;
            }

            if (!IsExpressionHostHediffDef(resolvedHediffDef))
            {
                errors.Add(BuildMessage(
                    "ExpressionHediffDefHostClassInvalid",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Expression,
                    context + " 指向的 HediffDef 不是 BDP 表达宿主 Def，它的 hediffClass 必须继承 BdpExpressionHostHediff。"));
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.HediffApplyModeKey))
            {
                return;
            }

            if (!string.Equals(entry.HediffApplyModeKey, "countToSeverity", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(BuildMessage(
                    "ExpressionHediffApplyModeUnsupported",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Expression,
                    context + " 的 HediffApplyModeKey 当前只支持留空或 countToSeverity。"));
            }
        }

        /// <summary>
        /// 校验 Passive 条目的最低合法性。
        /// </summary>
        private static void ValidatePassiveExpressionEntry(
            BDP.Core.Expressions.ChipExpressionEntryConfig entry,
            string context,
            List<ChipDefinitionValidationMessage> errors)
        {
            if (entry == null || errors == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(entry.PassiveKey))
            {
                errors.Add(BuildMessage(
                    "ExpressionPassiveKeyMissing",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Expression,
                    context + " 缺少 PassiveKey。"));
            }
        }

        /// <summary>
        /// 解析表达条目声明的 HediffDef。
        /// 这里只负责按 DefName 找到正式 Def，不承担额外兼容兜底。
        /// </summary>
        private static HediffDef ResolveDeclaredHediffDef(string hediffDefName)
        {
            return string.IsNullOrWhiteSpace(hediffDefName)
                ? null
                : DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
        }

        /// <summary>
        /// 判断当前 HediffDef 是否属于 BDP 表达宿主 Def。
        /// 正式边界看 <c>hediffClass</c>，不再看命名前缀。
        /// </summary>
        private static bool IsExpressionHostHediffDef(HediffDef hediffDef)
        {
            Type hediffClass = hediffDef != null ? hediffDef.hediffClass : null;
            return hediffClass != null
                && typeof(BDP.Core.Expressions.BdpExpressionHostHediff).IsAssignableFrom(hediffClass);
        }

        /// <summary>
        /// 判断当前表达条目是否应按近战规则校验。
        /// </summary>
        private static bool IsMeleeEntry(BDP.Core.Expressions.ChipExpressionEntryConfig entry)
        {
            return entry != null
                && (entry.WeaponMode == BDP.Core.Expressions.VerbExpressionModeConfig.Melee
                    || (entry.VerbProps != null && entry.VerbProps.IsMeleeAttack));
        }

        /// <summary>
        /// 解析作者声明的全部近战 Tool。
        /// </summary>
        private static List<Tool> ResolveDeclaredTools(BDP.Core.Expressions.ChipExpressionEntryConfig entry)
        {
            List<Tool> result = new List<Tool>();
            if (entry == null)
            {
                return result;
            }

            if (entry.Tool != null)
            {
                result.Add(entry.Tool);
            }

            if (entry.tools == null)
            {
                return result;
            }

            for (int i = 0; i < entry.tools.Count; i++)
            {
                Tool declaredTool = entry.tools[i];
                if (declaredTool == null || result.Contains(declaredTool))
                {
                    continue;
                }

                result.Add(declaredTool);
            }

            return result;
        }

        /// <summary>
        /// 解析当前校验链需要使用的主 Tool。
        /// 多 Tool 正式集合仍会完整保留，这里只取首项完成最小合法性判断。
        /// </summary>
        private static Tool ResolvePrimaryDeclaredTool(IReadOnlyList<Tool> declaredTools)
        {
            return declaredTools != null && declaredTools.Count > 0
                ? declaredTools[0]
                : null;
        }

        /// <summary>
        /// 校验表达条目声明的执行风格是否满足最低结构要求。
        /// 这里仍只做单条结构校验，不做跨结果全局推导。
        /// </summary>
        private static void ValidateExecutionStyle(
            BDP.Core.Expressions.ChipExpressionEntryConfig entry,
            string context,
            bool meleeEntry,
            List<ChipDefinitionValidationMessage> errors,
            List<ChipDefinitionValidationMessage> warnings)
        {
            if (entry == null || errors == null || warnings == null)
            {
                return;
            }

            AttackExecutionStyle style = TranslateDeclaredExecution(entry, meleeEntry);
            if (style == null)
            {
                return;
            }

            if (style.Dual != null && style.Dual.Schedule != DualExecutionSchedule.None)
            {
                errors.Add(BuildMessage(
                    "ExpressionDualExecutionStyleInvalid",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.Expression,
                    context + " 是单侧条目，不得声明 Dual.Schedule。"));
            }

            SingleAttackExecutionStyle single = style.Single;
            if (single == null)
            {
                return;
            }

            if (meleeEntry)
            {
                if (entry.Execution != null
                    && entry.Execution.Rhythm != BDP.Core.Expressions.ChipAttackExecutionRhythmConfig.None
                    && entry.Execution.Rhythm != BDP.Core.Expressions.ChipAttackExecutionRhythmConfig.Normal)
                {
                    errors.Add(BuildMessage(
                        "ExpressionMeleeExecutionRhythmInvalid",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 是近战 Verb，但 Execution.Rhythm 现阶段只能缺省或写 Normal。"));
                }

                if (single.RangedRhythm != RangedExecutionRhythm.None)
                {
                    errors.Add(BuildMessage(
                        "ExpressionMeleeRangedRhythmInvalid",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 是近战 Verb，不得声明 RangedRhythm。"));
                }

                if (single.MeleeRhythm == MeleeExecutionRhythm.MultiHit && single.meleeHitCount < 2)
                {
                    errors.Add(BuildMessage(
                        "ExpressionMeleeHitCountInvalid",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 声明为 MultiHit，但 meleeHitCount 小于 2。"));
                }
            }
            else
            {
                if (entry.Execution != null && entry.Execution.HitCount > 0)
                {
                    warnings.Add(BuildMessage(
                        "ExpressionRangedCountIgnored",
                        ChipDefinitionValidationSeverity.Warning,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 是远程 Verb，Execution.HitCount 只服务近战，实际发射数读取 VerbProps.burstShotCount。"));
                }

                if (entry.Execution != null && entry.Execution.HitIntervalTicks > 0)
                {
                    warnings.Add(BuildMessage(
                        "ExpressionRangedIntervalIgnored",
                        ChipDefinitionValidationSeverity.Warning,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 是远程 Verb，Execution.HitIntervalTicks 只服务近战，实际间隔读取 VerbProps.ticksBetweenBurstShots。"));
                }

                if (entry.Execution != null
                    && entry.Execution.Rhythm == BDP.Core.Expressions.ChipAttackExecutionRhythmConfig.Simultaneous
                    && entry.Execution.HitIntervalTicks > 0)
                {
                    warnings.Add(BuildMessage(
                        "ExpressionVolleyIntervalIgnored",
                        ChipDefinitionValidationSeverity.Warning,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 声明为远程齐射时，Execution.HitIntervalTicks 只服务近战，解释器会按 0 处理。"));
                }

                if (entry.Execution != null
                    && entry.Execution.Rhythm != BDP.Core.Expressions.ChipAttackExecutionRhythmConfig.None
                    && entry.Execution.Rhythm != BDP.Core.Expressions.ChipAttackExecutionRhythmConfig.Sequential
                    && entry.Execution.Rhythm != BDP.Core.Expressions.ChipAttackExecutionRhythmConfig.Simultaneous)
                {
                    errors.Add(BuildMessage(
                        "ExpressionRangedExecutionRhythmInvalid",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 是远程 Verb，但 Execution.Rhythm 只能写 Sequential 或 Simultaneous。"));
                }

                if (single.MeleeRhythm != MeleeExecutionRhythm.None)
                {
                    errors.Add(BuildMessage(
                        "ExpressionRangedMeleeRhythmInvalid",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Expression,
                        context + " 是远程 Verb，不得声明 MeleeRhythm。"));
                }

            }
        }

        /// <summary>
        /// 把作者侧统一 Execution 写法翻译成用于校验的正式执行风格。
        /// 这里复用和解释器一致的最小默认规则，但不把它当作运行时对象来源。
        /// </summary>
        private static AttackExecutionStyle TranslateDeclaredExecution(
            BDP.Core.Expressions.ChipExpressionEntryConfig entry,
            bool meleeEntry)
        {
            if (entry?.Execution == null)
            {
                return null;
            }

            int hitCount = entry.Execution.HitCount > 0 ? entry.Execution.HitCount : 1;
            int hitIntervalTicks = entry.Execution.HitIntervalTicks > 0 ? entry.Execution.HitIntervalTicks : 0;
            if (meleeEntry)
            {
                return new AttackExecutionStyle
                {
                    Single = new SingleAttackExecutionStyle
                    {
                        MeleeRhythm = hitCount > 1
                            ? MeleeExecutionRhythm.MultiHit
                            : MeleeExecutionRhythm.SingleHit,
                        meleeHitCount = hitCount,
                        meleeHitIntervalTicks = hitIntervalTicks
                    }
                };
            }

            SingleAttackExecutionStyle rangedStyle = new SingleAttackExecutionStyle
            {
                RangedRhythm = entry.Execution.Rhythm == BDP.Core.Expressions.ChipAttackExecutionRhythmConfig.Simultaneous
                    ? RangedExecutionRhythm.Simultaneous
                    : RangedExecutionRhythm.Sequential
            };
            if (entry.Execution.OriginSpread != null)
            {
                rangedStyle.HasOriginSpreadRange = entry.Execution.OriginSpread.LateralMin != 0f
                    || entry.Execution.OriginSpread.LateralMax != 0f
                    || entry.Execution.OriginSpread.ForwardMin != 0f
                    || entry.Execution.OriginSpread.ForwardMax != 0f;
                rangedStyle.OriginSpreadLateralMin = Math.Min(entry.Execution.OriginSpread.LateralMin, entry.Execution.OriginSpread.LateralMax);
                rangedStyle.OriginSpreadLateralMax = Math.Max(entry.Execution.OriginSpread.LateralMin, entry.Execution.OriginSpread.LateralMax);
                rangedStyle.OriginSpreadForwardMin = Math.Min(entry.Execution.OriginSpread.ForwardMin, entry.Execution.OriginSpread.ForwardMax);
                rangedStyle.OriginSpreadForwardMax = Math.Max(entry.Execution.OriginSpread.ForwardMin, entry.Execution.OriginSpread.ForwardMax);
            }

            return new AttackExecutionStyle
            {
                Single = rangedStyle
            };
        }

        /// <summary>
        /// 校验 Trion 声明块。
        /// </summary>
        private static void ValidateTrion(
            ChipTrionContract trion,
            List<ChipDefinitionValidationMessage> warnings)
        {
            if (trion == null)
            {
                return;
            }

            if (trion.CapacityCost < 0f)
            {
                warnings.Add(BuildMessage(
                    "TrionCapacityNegative",
                    ChipDefinitionValidationSeverity.Warning,
                    ChipDefinitionDeclaredBlock.Trion,
                    "该芯片的 Trion 占用值为负数，后续系统应谨慎处理它。"));
            }
        }

        /// <summary>
        /// 校验激活条件集合的共同结构、标准条件数量与单条作者配置。
        /// </summary>
        private static void ValidateActivationRequirements(
            IReadOnlyList<PawnRequirement> requirements,
            List<ChipDefinitionValidationMessage> errors)
        {
            int intensityRequirementCount = 0;
            if (requirements != null)
            {
                for (int i = 0; i < requirements.Count; i++)
                {
                    if (requirements[i] is TrionIntensityRequirement)
                    {
                        intensityRequirementCount++;
                    }
                }
            }

            IReadOnlyList<PawnRequirementValidationIssue> issues =
                PawnRequirementListValidator.Instance.Validate(requirements);
            for (int i = 0; i < issues.Count; i++)
            {
                PawnRequirementValidationIssue issue = issues[i];
                string code = ResolveActivationRequirementIssueCode(issue);
                if (code == "TrionIntensityRequirementDuplicate")
                {
                    // 芯片自己的“只能一条”诊断在下方统一输出，避免同一错误重复两次。
                    continue;
                }

                errors.Add(BuildMessage(
                    code,
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.ActivationRequirements,
                    "第 " + (issue.Index + 1) + " 条激活条件无效：" + issue.Message));
            }

            if (intensityRequirementCount == 0)
            {
                errors.Add(BuildMessage(
                    "TrionIntensityRequirementMissing",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.ActivationRequirements,
                    "每枚芯片都必须声明一条 Trion 释放力门槛。"));
            }
            else if (intensityRequirementCount > 1)
            {
                errors.Add(BuildMessage(
                    "TrionIntensityRequirementDuplicate",
                    ChipDefinitionValidationSeverity.Error,
                    ChipDefinitionDeclaredBlock.ActivationRequirements,
                    "同一枚芯片只能声明一条 Trion 释放力门槛。"));
            }
        }

        /// <summary>
        /// 为 Core 自带条件提供明确错误码；扩展条件统一使用中性错误码。
        /// </summary>
        private static string ResolveActivationRequirementErrorCode(
            PawnRequirement requirement)
        {
            if (requirement is TrionIntensityRequirement)
            {
                return "TrionIntensityRequirementInvalid";
            }

            if (requirement is SkillLevelRequirement)
            {
                return "SkillLevelRequirementInvalid";
            }

            return "ActivationRequirementInvalid";
        }

        /// <summary>
        /// 把中性列表问题映射成芯片定义层稳定错误码。
        /// </summary>
        private static string ResolveActivationRequirementIssueCode(
            PawnRequirementValidationIssue issue)
        {
            if (issue == null)
            {
                return "ActivationRequirementInvalid";
            }

            if (issue.Code == "EntryMissing")
            {
                return "ActivationRequirementEntryMissing";
            }

            if (issue.Code == "TrionIntensityDuplicate")
            {
                return "TrionIntensityRequirementDuplicate";
            }

            if (issue.Code == "SkillDuplicate")
            {
                return "SkillLevelRequirementDuplicate";
            }

            return ResolveActivationRequirementErrorCode(issue.Requirement);
        }

        /// <summary>
        /// 校验强类型静态扩展集合。
        /// Core 只检查共同结构，不解释任何具体业务字段。
        /// </summary>
        private static void ValidateExtensions(
            IReadOnlyList<ChipExtensionConfig> extensions,
            List<ChipDefinitionValidationMessage> errors)
        {
            if (extensions == null)
            {
                return;
            }

            HashSet<Type> declaredTypes = new HashSet<Type>();
            for (int i = 0; i < extensions.Count; i++)
            {
                ChipExtensionConfig extension = extensions[i];
                if (extension == null)
                {
                    errors.Add(BuildMessage(
                        "ChipExtensionEntryMissing",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Extensions,
                        "该芯片的扩展列表存在空条目。"));
                    continue;
                }

                Type extensionType = extension.GetType();
                if (!declaredTypes.Add(extensionType))
                {
                    errors.Add(BuildMessage(
                        "ChipExtensionTypeDuplicated",
                        ChipDefinitionValidationSeverity.Error,
                        ChipDefinitionDeclaredBlock.Extensions,
                        "该芯片重复声明了同一种具体扩展：" + extensionType.FullName));
                }
            }
        }

        /// <summary>
        /// 构建单条校验消息。
        /// </summary>
        private static ChipDefinitionValidationMessage BuildMessage(
            string code,
            ChipDefinitionValidationSeverity severity,
            ChipDefinitionDeclaredBlock? block,
            string message)
        {
            return new ChipDefinitionValidationMessage
            {
                Code = code,
                Severity = severity,
                Block = block,
                Message = message
            };
        }
    }
}
