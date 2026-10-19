using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using BDP.Core.Requirements;
using RimWorld;
using Verse;

namespace BDP.Core.Information
{
    /// <summary>
    /// 将有效芯片定义适配为信息解析器使用的中性输入。
    /// 适配器只负责提取事实，不负责决定公开哪些字段。
    /// </summary>
    public static class ChipInformationDefinitionInputAdapter
    {
        /// <summary>把有效芯片定义转换为中性解析输入。</summary>
        public static ChipInformationParseInput Create(
            ChipDefinitionConfig definition,
            ChipInformationIdentity identity,
            ChipInformationRuntimeState runtime,
            IEnumerable<ChipInformationProvenance> additionalSources)
        {
            return Create(
                definition,
                identity,
                runtime,
                additionalSources,
                null);
        }

        /// <summary>把芯片或武装构型的语言化描述附加到定义输入。</summary>
        public static ChipInformationParseInput Create(
            ChipDefinitionConfig definition,
            ChipInformationIdentity identity,
            ChipInformationRuntimeState runtime,
            IEnumerable<ChipInformationProvenance> additionalSources,
            IEnumerable<ChipInformationDescription> descriptions)
        {
            return Create(
                definition,
                identity,
                runtime,
                additionalSources,
                descriptions,
                null);
        }

        /// <summary>把业务侧已构造的中性信息块附加到定义输入。</summary>
        public static ChipInformationParseInput Create(
            ChipDefinitionConfig definition,
            ChipInformationIdentity identity,
            ChipInformationRuntimeState runtime,
            IEnumerable<ChipInformationProvenance> additionalSources,
            IEnumerable<ChipInformationDescription> descriptions,
            IEnumerable<ChipInformationFactBlock> additionalBlocks)
        {
            ChipInformationFacts facts = new ChipInformationFacts();
            List<ChipInformationProvenance> sources =
                new List<ChipInformationProvenance>();
            AddDefinitionFacts(definition, facts, sources);
            AddExpressionFacts(definition?.Expression, facts);
            AddActivationFacts(definition?.ActivationRequirements, facts);
            AddModeContexts(definition?.Expression, facts);
            AddDescriptionBlocks(facts, descriptions);
            AddRange(facts.StructuredBlocks, additionalBlocks);
            AddUnknownExtensions(definition?.Extensions, facts);
            AddRange(sources, additionalSources);
            return new ChipInformationParseInput(
                identity,
                facts,
                runtime ?? ChipInformationRuntimeInputAdapter.CreateStatic(),
                sources);
        }

        /// <summary>创建玩家可见的语言化描述结构块。</summary>
        private static void AddDescriptionBlocks(
            ChipInformationFacts facts,
            IEnumerable<ChipInformationDescription> descriptions)
        {
            if (facts == null || descriptions == null)
            {
                return;
            }

            List<ChipInformationFactBlock> children = new List<ChipInformationFactBlock>();
            int index = 0;
            foreach (ChipInformationDescription description in descriptions)
            {
                if (description == null || string.IsNullOrWhiteSpace(description.Text))
                {
                    continue;
                }

                string sourceKey = string.IsNullOrWhiteSpace(description.SourceKey)
                    ? "description." + index
                    : description.SourceKey;
                List<ChipInformationFactValue> fields = new List<ChipInformationFactValue>();
                AddExpressionField(
                    fields,
                    "displayLabel",
                    "BDP_Info_Field_DisplayLabel",
                    ChipInformationValueKind.Text,
                    ChipInformationValue.FromText(description.DisplayLabel),
                    sourceKey,
                    ChipInformationSourceKind.DeclaredDefinition);
                AddExpressionField(
                    fields,
                    "description",
                    "BDP_Info_Field_Description",
                    ChipInformationValueKind.Text,
                    ChipInformationValue.FromText(description.Text),
                    sourceKey,
                    ChipInformationSourceKind.DeclaredDefinition);
                children.Add(new ChipInformationFactBlock(
                    index.ToString(),
                    "description",
                    "BDP_Info_Block_Description",
                    index,
                    fields,
                    null));
                index++;
            }

            if (children.Count > 0)
            {
                facts.StructuredBlocks.Add(new ChipInformationFactBlock(
                    "Descriptions",
                    "descriptions",
                    "BDP_Info_Block_Descriptions",
                    2,
                    null,
                    children));
            }
        }

        /// <summary>记录 Core 未声明解释器的扩展，避免把原始对象倾倒进模型。</summary>
        private static void AddUnknownExtensions(
            IList<ChipExtensionConfig> extensions,
            ChipInformationFacts facts)
        {
            if (extensions == null || facts == null)
            {
                return;
            }

            for (int index = 0; index < extensions.Count; index++)
            {
                ChipExtensionConfig extension = extensions[index];
                if (extension != null)
                {
                    facts.Unknowns.Add(new ChipInformationUnknownFact(
                        "extension." + index,
                        "information.extension_unhandled",
                        extension.GetType().FullName));
                }
            }
        }

        /// <summary>使用静态状态创建定义输入。</summary>
        public static ChipInformationParseInput Create(
            ChipDefinitionConfig definition,
            ChipInformationIdentity identity)
        {
            return Create(
                definition,
                identity,
                ChipInformationRuntimeInputAdapter.CreateStatic(),
                null);
        }

        /// <summary>提取画像、装载和 Trion 事实。</summary>
        private static void AddDefinitionFacts(
            ChipDefinitionConfig definition,
            ChipInformationFacts facts,
            ICollection<ChipInformationProvenance> sources)
        {
            if (definition == null || facts == null)
            {
                return;
            }

            if (definition.Profile != null)
            {
                if (definition.Profile.Category != null)
                {
                    AddFact(
                        facts.ProfileFields,
                        "category",
                        "BDP_Info_Field_Category",
                        ChipInformationValueKind.Enum,
                        ChipInformationValue.FromDefReference(
                            typeof(ChipCategoryDef), definition.Profile.Category.defName),
                        null,
                        "definition.profile",
                        ChipInformationSourceKind.DeclaredDefinition,
                        ChipInformationContributionKind.Base,
                        sources);
                }

                if (definition.Profile.Tags != null && definition.Profile.Tags.Count > 0)
                {
                    List<ChipInformationValue> tags = new List<ChipInformationValue>();
                    for (int index = 0; index < definition.Profile.Tags.Count; index++)
                    {
                        if (definition.Profile.Tags[index] != null)
                        {
                            tags.Add(ChipInformationValue.FromDefReference(
                                typeof(ChipTagDef), definition.Profile.Tags[index].defName));
                        }
                    }

                    AddFact(
                        facts.ProfileFields,
                        "tags",
                        "BDP_Info_Field_Tags",
                        ChipInformationValueKind.List,
                        ChipInformationValue.FromList(tags),
                        null,
                        "definition.profile",
                        ChipInformationSourceKind.DeclaredDefinition,
                        ChipInformationContributionKind.Base,
                        sources);
                }
            }

            if (definition.Loadout != null)
            {
                AddFact(
                    facts.LoadoutFields,
                    "slotRegion",
                    "BDP_Info_Field_SlotRegion",
                    ChipInformationValueKind.Enum,
                    ChipInformationValue.FromLocalizedEnum(
                        definition.Loadout.SlotRegion.ToString(),
                        "BDP_ChipManufacturing_SlotRegion_" + definition.Loadout.SlotRegion),
                    null,
                    "definition.loadout",
                    ChipInformationSourceKind.DeclaredDefinition,
                    ChipInformationContributionKind.Base,
                    sources);
                AddFact(
                    facts.LoadoutFields,
                    "slotOccupancy",
                    "BDP_Info_Field_SlotOccupancy",
                    ChipInformationValueKind.Enum,
                    ChipInformationValue.FromLocalizedEnum(
                        definition.Loadout.SlotOccupancy.ToString(),
                        "BDP_ChipManufacturing_SlotOccupancy_" + definition.Loadout.SlotOccupancy),
                    null,
                    "definition.loadout",
                    ChipInformationSourceKind.DeclaredDefinition,
                    ChipInformationContributionKind.Base,
                    sources);
                AddTicksFact(
                    facts.LoadoutFields,
                    "activationDelay",
                    "BDP_Info_Field_ActivationDelay",
                    definition.Loadout.ActivationDelayTicks,
                    "definition.loadout",
                    sources);
                AddTicksFact(
                    facts.LoadoutFields,
                    "deactivationDelay",
                    "BDP_Info_Field_DeactivationDelay",
                    definition.Loadout.DeactivationDelayTicks,
                    "definition.loadout",
                    sources);
                AddDefListFact(
                    facts.LoadoutFields,
                    "activationExclusionGroups",
                    "BDP_Info_Field_ActivationExclusionGroups",
                    definition.Loadout.ActivationExclusionGroups,
                    "definition.loadout",
                    sources);
                AddDefinitionReferenceFact(
                    facts.LoadoutFields,
                    "activationWarmupStartSound",
                    "BDP_Info_Field_ActivationWarmupStartSound",
                    typeof(SoundDef),
                    definition.Loadout.ActivationAudio?.ActivationWarmupStartSound?.defName,
                    "definition.loadout.activationAudio",
                    sources);
                AddDefinitionReferenceFact(
                    facts.LoadoutFields,
                    "activationWarmupLoopSound",
                    "BDP_Info_Field_ActivationWarmupLoopSound",
                    typeof(SoundDef),
                    definition.Loadout.ActivationAudio?.ActivationWarmupLoopSound?.defName,
                    "definition.loadout.activationAudio",
                    sources);
                AddDefinitionReferenceFact(
                    facts.LoadoutFields,
                    "activationWarmupEndSound",
                    "BDP_Info_Field_ActivationWarmupEndSound",
                    typeof(SoundDef),
                    definition.Loadout.ActivationAudio?.ActivationWarmupEndSound?.defName,
                    "definition.loadout.activationAudio",
                    sources);
            }

            if (definition.Trion != null)
            {
                AddFact(
                    facts.TrionFields,
                    "capacityCost",
                    "BDP_Info_Field_CapacityCost",
                    ChipInformationValueKind.Number,
                    ChipInformationValue.FromNumber(definition.Trion.CapacityCost),
                    "BDP_Info_Unit_Trion",
                    "definition.trion",
                    ChipInformationSourceKind.DeclaredDefinition,
                    ChipInformationContributionKind.Base,
                    sources);
                AddFact(
                    facts.TrionFields,
                    "activationCost",
                    "BDP_Info_Field_ActivationCost",
                    ChipInformationValueKind.Number,
                    ChipInformationValue.FromNumber(definition.Trion.ActivationCost),
                    "BDP_Info_Unit_Trion",
                    "definition.trion",
                    ChipInformationSourceKind.DeclaredDefinition,
                    ChipInformationContributionKind.Base,
                    sources);
            }
        }

        /// <summary>提取激活条件的静态描述。</summary>
        private static void AddActivationFacts(
            IList<PawnRequirement> requirements,
            ChipInformationFacts facts)
        {
            if (requirements == null || facts == null)
            {
                return;
            }

            for (int index = 0; index < requirements.Count; index++)
            {
                PawnRequirement requirement = requirements[index];
                if (requirement is TrionIntensityRequirement trionRequirement)
                {
                    facts.ActivationConditions.Add(new ChipInformationConditionFact(
                        "requirement." + index,
                        "trion_intensity",
                        "BDP_Requirement_TrionIntensityLabel",
                        ChipInformationValue.FromNumber(trionRequirement.Minimum),
                        "BDP_Unit_Level"));
                    continue;
                }

                if (requirement is SkillLevelRequirement skillRequirement)
                {
                    facts.ActivationConditions.Add(new ChipInformationConditionFact(
                        "requirement." + index,
                        "skill_level",
                        "BDP_Requirement_SkillLevelLabel",
                        ChipInformationValue.FromNumber(skillRequirement.MinimumLevel),
                        "BDP_Unit_Level",
                        ChipInformationValue.FromDefReference(
                            typeof(SkillDef),
                            skillRequirement.Skill?.defName)));
                }
            }
        }

        /// <summary>提取默认表达以及形态内部表达事实。</summary>
        private static void AddExpressionFacts(
            ChipExpressionConfig expression,
            ChipInformationFacts facts)
        {
            if (expression == null || expression.Entries == null || facts == null)
            {
                return;
            }

            for (int index = 0; index < expression.Entries.Count; index++)
            {
                ChipExpressionEntryConfig entry = expression.Entries[index];
                ChipInformationExpressionFact fact = CreateExpressionFact(
                    entry,
                    null,
                    null);
                if (fact != null)
                {
                    facts.Expressions.Add(fact);
                }
            }
        }

        /// <summary>提取所有形态与姿态上下文。</summary>
        private static void AddModeContexts(
            ChipExpressionConfig expression,
            ChipInformationFacts facts)
        {
            if (expression == null || expression.Modes == null || facts == null)
            {
                return;
            }

            for (int modeIndex = 0; modeIndex < expression.Modes.Count; modeIndex++)
            {
                ChipExpressionModeConfig mode = expression.Modes[modeIndex];
                if (mode == null || string.IsNullOrWhiteSpace(mode.ModeKey))
                {
                    continue;
                }

                List<ChipInformationExpressionFact> modeExpressions =
                    CreateSelectedExpressionFacts(
                        expression.Entries,
                        mode.ActiveEntryIds,
                        mode.ModeKey,
                        null);
                facts.Contexts.Add(new ChipInformationContextFact(
                    mode.ModeKey,
                    string.IsNullOrWhiteSpace(mode.DisplayLabelKey)
                        ? "BDP_Info_Field_Mode"
                        : mode.DisplayLabelKey,
                    modeExpressions));

                if (mode.Stances == null)
                {
                    continue;
                }

                for (int stanceIndex = 0; stanceIndex < mode.Stances.Count; stanceIndex++)
                {
                    ChipExpressionStanceConfig stance = mode.Stances[stanceIndex];
                    if (stance == null || string.IsNullOrWhiteSpace(stance.StanceKey))
                    {
                        continue;
                    }

                    facts.Contexts.Add(new ChipInformationContextFact(
                        mode.ModeKey + "/" + stance.StanceKey,
                        string.IsNullOrWhiteSpace(stance.DisplayLabelKey)
                            ? "BDP_Info_Field_Stance"
                            : stance.DisplayLabelKey,
                        CreateSelectedExpressionFacts(
                            expression.Entries,
                            stance.ActiveEntryIds,
                            mode.ModeKey,
                            stance.StanceKey),
                        parentContextKey: mode.ModeKey));
                }
            }
        }

        /// <summary>按形态或姿态声明的条目 ID 选择表达事实。</summary>
        private static List<ChipInformationExpressionFact> CreateSelectedExpressionFacts(
            IList<ChipExpressionEntryConfig> entries,
            IList<string> activeEntryIds,
            string modeKey,
            string stanceKey)
        {
            List<ChipInformationExpressionFact> result =
                new List<ChipInformationExpressionFact>();
            if (entries == null)
            {
                return result;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                ChipExpressionEntryConfig entry = entries[index];
                if (entry == null || !IsSelected(entry.Id, activeEntryIds))
                {
                    continue;
                }

                ChipInformationExpressionFact fact = CreateExpressionFact(
                    entry,
                    modeKey,
                    stanceKey);
                if (fact != null)
                {
                    result.Add(fact);
                }
            }

            return result;
        }

        /// <summary>创建一条通用表达事实。</summary>
        private static ChipInformationExpressionFact CreateExpressionFact(
            ChipExpressionEntryConfig entry,
            string modeKey,
            string stanceKey)
        {
            if (entry == null)
            {
                return null;
            }

            string channelKey = GetChannelKey(entry);
            if (string.IsNullOrWhiteSpace(channelKey))
            {
                return null;
            }

            List<ChipInformationFactValue> fields = new List<ChipInformationFactValue>();
            string sourceKey = string.IsNullOrWhiteSpace(entry.Id)
                ? channelKey
                : entry.Id;
            string labelKey = string.IsNullOrWhiteSpace(entry.DisplayLabelKey)
                ? "BDP_Info_Field_Expression"
                : entry.DisplayLabelKey;
            AddExpressionField(
                fields,
                "weaponMode",
                "BDP_Info_Field_WeaponMode",
                ChipInformationValueKind.Enum,
                ChipInformationKnownValueFactory.FromWeaponMode(
                    entry.WeaponMode.ToString()),
                sourceKey,
                ChipInformationSourceKind.DeclaredDefinition);
            AddExpressionField(
                fields,
                "abilityDef",
                "BDP_Info_Field_Ability",
                ChipInformationValueKind.Enum,
                ChipInformationValue.FromDefReference(typeof(AbilityDef), entry.AbilityDefName),
                sourceKey,
                ChipInformationSourceKind.DeclaredDefinition);
            AddExpressionField(
                fields,
                "hediffDef",
                "BDP_Info_Field_Hediff",
                ChipInformationValueKind.Enum,
                ChipInformationValue.FromDefReference(typeof(HediffDef), entry.HediffDefName),
                sourceKey,
                ChipInformationSourceKind.DeclaredDefinition);
            AddExpressionField(
                fields,
                "hediffApplyMode",
                "BDP_Info_Field_HediffApplyMode",
                ChipInformationValueKind.Enum,
                ChipInformationKnownValueFactory.FromHediffApplyMode(entry.HediffApplyModeKey),
                sourceKey,
                ChipInformationSourceKind.DeclaredDefinition);
            AddExpressionField(
                fields,
                "hediffControlMode",
                "BDP_Info_Field_HediffControlMode",
                ChipInformationValueKind.Enum,
                ChipInformationKnownValueFactory.FromHediffControlMode(entry.HediffControlMode),
                sourceKey,
                ChipInformationSourceKind.DeclaredDefinition);
            AddExpressionField(
                fields,
                "passiveKey",
                "BDP_Info_Field_Passive",
                ChipInformationValueKind.Enum,
                StringValue(entry.PassiveIdentityKey),
                sourceKey,
                ChipInformationSourceKind.DeclaredDefinition);
            AddExposedFields(fields, entry, sourceKey);
            return new ChipInformationExpressionFact(
                sourceKey,
                labelKey,
                channelKey,
                sourceKey,
                ChipInformationSourceKind.DeclaredDefinition,
                entry.WeaponMode.ToString(),
                modeKey,
                stanceKey,
                true,
                fields,
                null,
                null);
        }

        /// <summary>提取作者显式声明的轻量附加字段。</summary>
        private static void AddExposedFields(
            ICollection<ChipInformationFactValue> fields,
            ChipExpressionEntryConfig entry,
            string sourceKey)
        {
            if (entry.ExposedData == null)
            {
                return;
            }

            for (int index = 0; index < entry.ExposedData.Count; index++)
            {
                PassiveExpressionExposedDatumConfig datum = entry.ExposedData[index];
                if (datum == null || string.IsNullOrWhiteSpace(datum.DataKey))
                {
                    continue;
                }

                AddExpressionField(
                    fields,
                    "exposed." + datum.DataKey,
                    "BDP_Info_Field_ExposedData",
                    ChipInformationValueKind.Text,
                    ChipInformationValue.FromText(datum.DataValue),
                    sourceKey,
                    ChipInformationSourceKind.DeclaredDefinition);
            }
        }

        /// <summary>获取定义层表达通道键。</summary>
        private static string GetChannelKey(ChipExpressionEntryConfig entry)
        {
            switch (entry.EntryType)
            {
                case ChipExpressionEntryTypeConfig.Ability:
                    return "ability";
                case ChipExpressionEntryTypeConfig.Hediff:
                    return "hediff";
                case ChipExpressionEntryTypeConfig.Passive:
                    return "passive";
                default:
                    VerbExpressionModeConfig resolvedMode =
                        ChipExpressionCapabilityResolver.ResolveWeaponMode(entry);
                    if (resolvedMode == VerbExpressionModeConfig.Melee)
                    {
                        return "melee";
                    }

                    if (resolvedMode == VerbExpressionModeConfig.Ranged)
                    {
                        return "ranged";
                    }

                    return null;
            }
        }

        /// <summary>判断条目是否在上下文选择集合中。</summary>
        private static bool IsSelected(string entryId, IList<string> activeEntryIds)
        {
            if (activeEntryIds == null || activeEntryIds.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < activeEntryIds.Count; index++)
            {
                if (string.Equals(entryId, activeEntryIds[index], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>保留配置中声明的原始游戏刻整数。</summary>
        private static void AddTicksFact(
            ICollection<ChipInformationFactValue> target,
            string fieldKey,
            string labelKey,
            int ticks,
            string sourceKey,
            ICollection<ChipInformationProvenance> sources)
        {
            if (ticks < 0)
            {
                return;
            }

            AddFact(
                target,
                fieldKey,
                labelKey,
                ChipInformationValueKind.Integer,
                ChipInformationValue.FromInteger(ticks),
                "BDP_Info_Unit_Ticks",
                sourceKey,
                ChipInformationSourceKind.DeclaredDefinition,
                ChipInformationContributionKind.Base,
                sources);
        }

        /// <summary>添加原始定义对象引用；未配置引用时不添加字段。</summary>
        private static void AddDefinitionReferenceFact(
            ICollection<ChipInformationFactValue> target,
            string fieldKey,
            string labelKey,
            Type definitionType,
            string defName,
            string sourceKey,
            ICollection<ChipInformationProvenance> sources)
        {
            ChipInformationValue value = ChipInformationValue.FromDefReference(
                definitionType,
                defName);
            if (value == null)
            {
                return;
            }

            AddFact(
                target,
                fieldKey,
                labelKey,
                ChipInformationValueKind.Enum,
                value,
                null,
                sourceKey,
                ChipInformationSourceKind.DeclaredDefinition,
                ChipInformationContributionKind.Base,
                sources);
        }

        /// <summary>添加 Def 引用列表事实。</summary>
        private static void AddDefListFact(
            ICollection<ChipInformationFactValue> target,
            string fieldKey,
            string labelKey,
            IList<ChipExclusionGroupDef> values,
            string sourceKey,
            ICollection<ChipInformationProvenance> sources)
        {
            if (values == null || values.Count == 0)
            {
                return;
            }

            List<ChipInformationValue> list = new List<ChipInformationValue>();
            for (int index = 0; index < values.Count; index++)
            {
                if (values[index] != null)
                {
                    list.Add(ChipInformationValue.FromDefReference(
                        typeof(ChipExclusionGroupDef), values[index].defName));
                }
            }

            AddFact(
                target,
                fieldKey,
                labelKey,
                ChipInformationValueKind.List,
                ChipInformationValue.FromList(list),
                null,
                sourceKey,
                ChipInformationSourceKind.DeclaredDefinition,
                ChipInformationContributionKind.Base,
                sources);
        }

        /// <summary>添加表达字段并保留基础来源。</summary>
        private static void AddExpressionField(
            ICollection<ChipInformationFactValue> target,
            string fieldKey,
            string labelKey,
            ChipInformationValueKind valueKind,
            ChipInformationValue value,
            string sourceKey,
            ChipInformationSourceKind sourceKind)
        {
            if (value == null)
            {
                return;
            }

            target.Add(new ChipInformationFactValue(
                fieldKey,
                labelKey,
                valueKind,
                value,
                null,
                new[]
                {
                    new ChipInformationProvenance(
                        sourceKey,
                        sourceKind,
                        ChipInformationContributionKind.Base,
                        value,
                        null,
                        true)
                }));
        }

        /// <summary>添加定义字段并将其来源注册到输入。</summary>
        private static void AddFact(
            ICollection<ChipInformationFactValue> target,
            string fieldKey,
            string labelKey,
            ChipInformationValueKind valueKind,
            ChipInformationValue value,
            string unitKey,
            string sourceKey,
            ChipInformationSourceKind sourceKind,
            ChipInformationContributionKind contributionKind,
            ICollection<ChipInformationProvenance> sources)
        {
            ChipInformationProvenance provenance = new ChipInformationProvenance(
                sourceKey,
                sourceKind,
                contributionKind,
                value,
                null,
                true);
            target.Add(new ChipInformationFactValue(
                fieldKey,
                labelKey,
                valueKind,
                value,
                unitKey,
                new[] { provenance }));
            AddUniqueSource(sources, provenance);
        }

        /// <summary>把非空内部标识转换为不自动翻译的枚举值。</summary>
        private static ChipInformationValue StringValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : ChipInformationValue.FromEnum(value);
        }

        /// <summary>去重添加来源记录。</summary>
        private static void AddUniqueSource(
            ICollection<ChipInformationProvenance> target,
            ChipInformationProvenance source)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (ChipInformationProvenance existing in target)
            {
                if (existing != null
                    && string.Equals(existing.SourceKey, source.SourceKey, StringComparison.Ordinal)
                    && existing.SourceKind == source.SourceKind)
                {
                    return;
                }
            }

            target.Add(source);
        }

        /// <summary>复制外部来源集合。</summary>
        private static void AddRange<T>(
            ICollection<T> target,
            IEnumerable<T> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (T item in source)
            {
                if (item != null)
                {
                    target.Add(item);
                }
            }
        }
    }
}
