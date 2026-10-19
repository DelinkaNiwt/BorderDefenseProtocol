using System.Collections.Generic;
using BDP.Content.Assembly.ChipManufacturing.Model;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Content.Information;
using BDP.Core.Chips;
using RimWorld;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Thing
{
    /// <summary>成品芯片只保存组合选择，每次读取都从当前 Def 重新解析。</summary>
    public sealed class CompManufacturedChip : ThingComp,
        IChipInstanceDefinitionProvider,
        IChipInstanceDeclaredDefinitionProvider,
        IChipSourceReferenceProvider,
        IChipCombinationRecordHolder
    {
        /// <summary>制造完成时从芯片账单复制的组合记录。</summary>
        private ChipCombinationRecord combinationRecord;

        /// <summary>读取当前成品持有的组合记录。</summary>
        public ChipCombinationRecord CombinationRecord => combinationRecord;

        /// <summary>按制造形态顺序公开中性来源键。</summary>
        public IReadOnlyList<string> OrderedSourceKeys =>
            combinationRecord?.OrderedChipFunctionDefNames
            ?? (IReadOnlyList<string>)new List<string>();

        /// <summary>直接公开成品记录中的最终职业，不沿用芯片功能模板兼容关系。</summary>
        public string SourceProfessionKey => combinationRecord?.ProfessionDefName;

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
                        resolution.ArmamentForm.ResolvedLabel)
                    : null;
            }
        }

        /// <summary>读取当前动态物品名称；无效物品通常会在读档迁移时被替换。</summary>
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

        /// <summary>向信息模型提供应用武装构型前的基础配置。</summary>
        public bool TryGetDeclaredChipDefinition(out ChipDefinitionConfig definition)
        {
            ChipCombinationResolution resolution = ResolveCurrent();
            definition = resolution.Status == ChipCombinationResolutionStatus.Valid
                ? resolution.DeclaredConfig
                : null;
            return definition != null;
        }

        /// <summary>向 Content 信息卡发布器提供一次当前有效组合解析结果。</summary>
        public bool TryGetCurrentResolution(out ChipCombinationResolution resolution)
        {
            resolution = ResolveCurrent();
            return resolution != null
                && resolution.Status == ChipCombinationResolutionStatus.Valid
                && resolution.ResolvedConfig != null;
        }

        /// <summary>把统一信息模型和原版武器统计追加到成品物品信息卡。</summary>
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            IEnumerable<StatDrawEntry> baseStats = base.SpecialDisplayStats();
            if (baseStats != null)
            {
                foreach (StatDrawEntry entry in baseStats)
                {
                    yield return entry;
                }
            }

            IEnumerable<StatDrawEntry> published =
                ManufacturedChipInfoCardPublisher.Publish(this);
            if (published == null)
            {
                yield break;
            }

            foreach (StatDrawEntry entry in published)
            {
                if (entry != null)
                {
                    yield return entry;
                }
            }
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
