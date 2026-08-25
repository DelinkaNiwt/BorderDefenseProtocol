using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Core.Chips;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Thing
{
    /// <summary>成品芯片只保存组合选择，每次读取都从当前 Def 重新解析。</summary>
    public sealed class CompManufacturedChip : ThingComp,
        IChipInstanceDefinitionProvider,
        IChipSourceReferenceProvider,
        IChipCombinationRecordHolder,
        ILegacyChipPersistenceMarker
    {
        /// <summary>制造完成时从芯片账单复制的组合记录。</summary>
        private ChipCombinationRecord combinationRecord;

        /// <summary>旧版制造组件保存的动作预设来源；只在读档阶段读取，不再写入新存档。</summary>
        private List<string> legacySourcePresetDefNames;

        /// <summary>旧版制造组件保存的武器类别来源；只用于识别旧物品。</summary>
        private string legacySourceGunClassDefName;

        /// <summary>旧版制造组件保存的武器类别显示名；只用于识别旧物品。</summary>
        private string legacySourceGunClassLabel;

        /// <summary>旧版制造组件保存的实例显示名；只用于识别旧物品。</summary>
        private string legacyCustomLabel;

        /// <summary>读档时发现旧版制造字段后锁存的非法物品标记。</summary>
        private bool legacyPersistenceDetected;

        /// <summary>读取当前成品持有的组合记录。</summary>
        public ChipCombinationRecord CombinationRecord => combinationRecord;

        /// <summary>按制造形态顺序公开中性来源键。</summary>
        public IReadOnlyList<string> OrderedSourceKeys =>
            combinationRecord?.OrderedActionPresetDefNames
            ?? (IReadOnlyList<string>)new List<string>();

        /// <summary>直接公开成品记录中的最终职业，不沿用动作兼容关系。</summary>
        public string SourceProfessionKey => combinationRecord?.ProfessionDefName;

        /// <summary>读取是否发现旧版格式或缺失当前组合记录。</summary>
        public bool LegacyPersistenceDetected => legacyPersistenceDetected || combinationRecord == null;

        /// <summary>以中性变体键公开显式或逻辑生效的武装型来源。</summary>
        public string SourceVariantKey
        {
            get
            {
                if (combinationRecord == null)
                {
                    return null;
                }

                if (!combinationRecord.ArmamentFormDefName.NullOrEmpty())
                {
                    return combinationRecord.ArmamentFormDefName;
                }

                return ResolveCurrent().ArmamentForm?.defName;
            }
        }

        /// <summary>以中性变体标签公开玩家可见武装型名称；隐藏默认型不公开标签。</summary>
        public string SourceVariantLabel
        {
            get
            {
                ChipCombinationResolution resolution = ResolveCurrent();
                return resolution.ArmamentForm != null
                    && resolution.ArmamentForm.includeInProductLabel
                    ? "BDP_ChipManufacturing_SourceVariantLabel".Translate(
                        resolution.ArmamentForm.label)
                    : null;
            }
        }

        /// <summary>读取当前动态物品名称；来源缺失时显式标注但保留物品。</summary>
        public string CurrentLabel
        {
            get
            {
                ChipCombinationResolution resolution = ResolveCurrent();
                if (resolution.Status == ChipCombinationResolutionStatus.Valid)
                {
                    return resolution.ResolvedLabel;
                }

                string fallback = !resolution.ResolvedLabel.NullOrEmpty()
                    ? resolution.ResolvedLabel
                    : combinationRecord?.LastResolvedLabel;
                if (resolution.Status == ChipCombinationResolutionStatus.MissingSource
                    && !fallback.NullOrEmpty())
                {
                    return "BDP_ChipManufacturing_MissingSourceLabel".Translate(fallback);
                }

                return fallback;
            }
        }

        /// <summary>制造完成时首次写入组合记录。</summary>
        public void InitializeFromBill(ChipCombinationRecord record)
        {
            if (combinationRecord == null && record != null)
            {
                combinationRecord = record.Clone();
            }
        }

        /// <summary>仅在当前组合有效时向 Core 提供实时配置。</summary>
        public bool TryGetChipDefinition(out ChipDefinitionConfig definition)
        {
            ChipCombinationResolution resolution = ResolveCurrent();
            definition = resolution.Status == ChipCombinationResolutionStatus.Valid
                ? resolution.ResolvedConfig
                : null;
            return definition != null;
        }

        /// <summary>保存组合记录，不保存任何可重新计算的完整配置。</summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref combinationRecord, "chipCombinationRecord");

            // 旧版本把完整动态配置改为 source* 字段保存；只读这些历史键，避免新存档继续产生兼容字段。
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe_Collections.Look(
                    ref legacySourcePresetDefNames,
                    "sourcePresetDefNames",
                    LookMode.Value);
                Scribe_Values.Look(ref legacySourceGunClassDefName, "sourceGunClassDefName");
                Scribe_Values.Look(ref legacySourceGunClassLabel, "sourceGunClassLabel");
                Scribe_Values.Look(ref legacyCustomLabel, "customLabel");
                legacyPersistenceDetected = legacySourcePresetDefNames != null
                    || !legacySourceGunClassDefName.NullOrEmpty()
                    || !legacySourceGunClassLabel.NullOrEmpty()
                    || !legacyCustomLabel.NullOrEmpty();
            }
        }

        /// <summary>调用统一解析器读取当前 Def 下的最新结果。</summary>
        private ChipCombinationResolution ResolveCurrent()
        {
            return new ChipCombinationResolver().Resolve(combinationRecord);
        }
    }
}
