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
        IChipCombinationRecordHolder
    {
        /// <summary>制造完成时从芯片账单复制的组合记录。</summary>
        private ChipCombinationRecord combinationRecord;

        /// <summary>读取当前成品持有的组合记录。</summary>
        public ChipCombinationRecord CombinationRecord => combinationRecord;

        /// <summary>按制造形态顺序公开中性来源键。</summary>
        public IReadOnlyList<string> OrderedSourceKeys =>
            combinationRecord?.OrderedActionPresetDefNames
            ?? (IReadOnlyList<string>)new List<string>();

        /// <summary>以中性变体键公开可空枪壳来源。</summary>
        public string SourceVariantKey => combinationRecord?.GunShellDefName;

        /// <summary>以中性变体标签公开当前仍存在的枪壳名称。</summary>
        public string SourceVariantLabel
        {
            get
            {
                ChipCombinationResolution resolution = ResolveCurrent();
                return resolution.GunShell != null
                    ? "BDP_ChipManufacturing_SourceVariantLabel".Translate(
                        resolution.GunShell.label)
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
        }

        /// <summary>调用统一解析器读取当前 Def 下的最新结果。</summary>
        private ChipCombinationResolution ResolveCurrent()
        {
            return new ChipCombinationResolver().Resolve(combinationRecord);
        }
    }
}
