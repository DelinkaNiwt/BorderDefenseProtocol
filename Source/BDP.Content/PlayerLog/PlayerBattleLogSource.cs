using System;
using System.Linq;
using BDP.Core.CombatLog;
using BDP.Core.Chips;
using BDP.Core.Combos;
using BDP.Core.Expressions;
using BDP.Core.Semantics;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using RimWorld;
using Verse;

namespace BDP.Content.PlayerLog
{
    /// <summary>历史条目的最小来源身份；不保存完整表达结果或已翻译整句。</summary>
    internal sealed class PlayerBattleLogSource : IExposable
    {
        /// <summary>来源定义类型。</summary>
        private string definitionType;
        /// <summary>来源定义名。</summary>
        private string definitionName;
        /// <summary>来源表达条目标识。</summary>
        private string entryId;
        /// <summary>来源武装变体标识。</summary>
        private string variantId;
        /// <summary>动态条目的语言键。</summary>
        private string labelKey;

        /// <summary>独立能力产生的攻击使用能力名作为来源取词，不依赖具体技能名称。</summary>
        internal bool IsAbilitySource => definitionType == typeof(AbilityDef).FullName;

        /// <summary>复制当次身份，后续不读取人物当前装备。</summary>
        internal static PlayerBattleLogSource Capture(ISemanticContext context)
        {
            PlayerBattleLogSource source = new PlayerBattleLogSource
            {
                definitionType = Read(context, BattleLogEntryFactoryAccess.SourceTypeKey),
                definitionName = Read(context, BattleLogEntryFactoryAccess.SourceDefKey),
                entryId = Read(context, BattleLogEntryFactoryAccess.SourceEntryKey),
                variantId = Read(context, BattleLogEntryFactoryAccess.SourceVariantKey),
                labelKey = Read(context, BattleLogEntryFactoryAccess.SourceLabelKey)
            };
            source.ResolveInstanceOrigin(Read(context, BattleLogEntryFactoryAccess.SourceOriginKeys));
            return source;
        }

        /// <summary>创建条目时把动态物品外壳还原为固定模板条目，之后无需芯片实例或制造配方。</summary>
        private void ResolveInstanceOrigin(string originKeys)
        {
            if (originKeys.NullOrEmpty() || definitionType != typeof(ThingDef).FullName) return;
            var functions = originKeys.Split('|').Select(key => key.NullOrEmpty() ? null
                : DefDatabase<ChipManufacturingPresetDef>.GetNamedSilentFail(key)).ToList();
            if (!ChipExpressionMergeService.TryResolveSourceEntry(functions, entryId,
                out ChipManufacturingPresetDef function, out ChipExpressionEntryConfig entry)) return;
            definitionType = typeof(ChipManufacturingPresetDef).FullName;
            definitionName = function.defName;
            entryId = entry.Id;
            labelKey = entry.DisplayLabelKey;
        }

        /// <summary>显示时从语言键或固定定义读取名字，未知来源返回空。</summary>
        internal string ResolveLabel()
        {
            string label = !labelKey.NullOrEmpty() && labelKey.CanTranslate() ? labelKey.Translate().ToString() : null;
            if (!definitionName.NullOrEmpty() && definitionType == typeof(ComboDef).FullName)
            {
                ComboDef definition = DefDatabase<ComboDef>.GetNamedSilentFail(definitionName);
                ExpressionSourceConfigBase entry = definition?.Expression?.Entries?.FirstOrDefault(item => item.Id == entryId);
                label = label ?? ResolveEntryLabel(entry) ?? definition?.label;
            }
            else if (!definitionName.NullOrEmpty() && definitionType == typeof(ThingDef).FullName)
            {
                ThingDef definition = DefDatabase<ThingDef>.GetNamedSilentFail(definitionName);
                ChipExpressionConfig config = definition?.GetModExtension<ChipDefinitionConfig>()?.Expression
                    ?? definition?.GetModExtension<ChipExpressionConfig>();
                ExpressionSourceConfigBase entry = config?.Entries
                    ?.FirstOrDefault(item => item.Id == entryId);
                // 声明条目缺失时不能把动态物品容器名冒充攻击名；旧记录缺失身份就省略来源。
                label = label ?? ResolveEntryLabel(entry) ?? (entryId.NullOrEmpty() ? definition?.label : null);
            }
            else if (!definitionName.NullOrEmpty() && definitionType == typeof(ChipManufacturingPresetDef).FullName)
            {
                ChipManufacturingPresetDef definition = DefDatabase<ChipManufacturingPresetDef>.GetNamedSilentFail(definitionName);
                ExpressionSourceConfigBase entry = definition?.config?.Expression?.Entries
                    ?.FirstOrDefault(item => item.Id == entryId);
                label = label ?? ResolveEntryLabel(entry) ?? definition?.ResolvedLabel;
            }

            // 其它原版或模组定义复用原版按类型查Def的入口，无需逐个增加名称解析分支。
            if (label.NullOrEmpty() && !definitionName.NullOrEmpty() && !definitionType.NullOrEmpty()
                && !(definitionType == typeof(ThingDef).FullName && !entryId.NullOrEmpty()))
            {
                Type type = GenTypes.GetTypeInAnyAssembly(definitionType);
                if (type != null && !type.IsAbstract && typeof(Def).IsAssignableFrom(type))
                    label = GenDefDatabase.GetDef(type, definitionName, false)?.label;
            }

            if (!label.NullOrEmpty() && !variantId.NullOrEmpty())
            {
                ChipArmamentFormDef variant = DefDatabase<ChipArmamentFormDef>.GetNamedSilentFail(variantId);
                if (variant != null && variant.includeInProductLabel)
                {
                    label += "BDP_SourceVariant_NameSeparator".Translate().ToString()
                        + "BDP_ChipManufacturing_SourceVariantLabel".Translate(variant.ResolvedLabel);
                }
            }
            return label;
        }

        /// <summary>读取已有配置的可本地化显示名。</summary>
        private static string ResolveEntryLabel(ExpressionSourceConfigBase entry)
        {
            return entry == null ? null : !entry.DisplayLabelKey.NullOrEmpty()
                && entry.DisplayLabelKey.CanTranslate() ? entry.DisplayLabelKey.Translate().ToString() : entry.DisplayLabel;
        }

        /// <summary>从已有中性语义数据复制单个身份字段。</summary>
        private static string Read(ISemanticContext context, string key)
        {
            string value;
            return context?.ExtraData != null && context.ExtraData.TryGetValue(key, out value) ? value : null;
        }

        /// <summary>只保存来源身份，语言切换后重新读取名称。</summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref definitionType, "definitionType");
            Scribe_Values.Look(ref definitionName, "definitionName");
            Scribe_Values.Look(ref entryId, "entryId");
            Scribe_Values.Look(ref variantId, "variantId");
            Scribe_Values.Look(ref labelKey, "labelKey");
        }
    }
}
