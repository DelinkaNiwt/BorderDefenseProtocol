using System.Collections.Generic;
using System.Text;
using BDP.Core.Trigger;
using RimWorld;
using Verse;

namespace BDP.Content.Trigger
{
    /// <summary>
    /// 触发体动态市场价值组件的 Def 配置。
    /// </summary>
    public sealed class CompProperties_TriggerBodyMarketValue : CompProperties
    {
        /// <summary>
        /// 将动态市场价值逻辑绑定到此组件定义。
        /// </summary>
        public CompProperties_TriggerBodyMarketValue()
        {
            compClass = typeof(CompTriggerBodyMarketValue);
        }
    }

    /// <summary>
    /// 按触发体正式装载的芯片合成市场价值。
    /// </summary>
    public sealed class CompTriggerBodyMarketValue : ThingComp
    {
        /// <summary>
        /// 单种已装芯片对触发体市场价值的合并贡献。
        /// </summary>
        private sealed class ChipMarketValueContribution
        {
            /// <summary>
            /// 创建指定芯片定义的贡献记录。
            /// </summary>
            public ChipMarketValueContribution(ThingDef chipDef)
            {
                ChipDef = chipDef;
            }

            /// <summary>
            /// 获取参与合并的芯片定义。
            /// </summary>
            public ThingDef ChipDef { get; private set; }

            /// <summary>
            /// 获取并更新该种芯片的堆叠总数。
            /// </summary>
            public int StackCount { get; set; }

            /// <summary>
            /// 获取并更新该种芯片贡献的市场价值总和。
            /// </summary>
            public float MarketValue { get; set; }
        }

        /// <summary>
        /// 为触发体基础市场价值加上各枚实际装载芯片的价值。
        /// 芯片读取与实体去重共用贡献列表，避免详情和实际计价出现差异。
        /// </summary>
        public override float GetStatOffset(StatDef stat)
        {
            float offset = base.GetStatOffset(stat);
            if (stat != StatDefOf.MarketValue)
            {
                return offset;
            }

            foreach (ChipMarketValueContribution contribution in GetChipMarketValueContributions())
            {
                offset += contribution.MarketValue;
            }

            return offset;
        }

        /// <summary>
        /// 在原版市场价值详情中追加实际装载芯片的价值明细。
        /// </summary>
        public override void GetStatsExplanation(StatDef stat, StringBuilder sb, string whitespace = "")
        {
            if (stat != StatDefOf.MarketValue)
            {
                return;
            }

            List<ChipMarketValueContribution> contributions = GetChipMarketValueContributions();
            if (contributions.Count == 0)
            {
                return;
            }

            sb.AppendLine(whitespace + "BDP_TriggerBodyMarketValue_ChipsHeader".Translate() + ":");
            foreach (ChipMarketValueContribution contribution in contributions)
            {
                string value = stat.Worker.ValueToString(
                    contribution.MarketValue,
                    finalized: false,
                    ToStringNumberSense.Offset);
                sb.AppendLine(
                    whitespace
                    + "    "
                    + "BDP_TriggerBodyMarketValue_ChipContribution".Translate(
                        contribution.ChipDef.LabelCap,
                        contribution.StackCount,
                        value));
            }
        }

        /// <summary>
        /// 按芯片定义合并实际装载芯片，并按实体引用去重绑定镜像槽。
        /// </summary>
        private List<ChipMarketValueContribution> GetChipMarketValueContributions()
        {
            List<ChipMarketValueContribution> contributions = new List<ChipMarketValueContribution>();
            ITriggerLoadoutReader reader = TriggerSurfaceAccess.ResolveLoadoutReaderFromThing(parent);
            if (reader == null)
            {
                return contributions;
            }

            HashSet<Thing> countedChips = new HashSet<Thing>();
            Dictionary<ThingDef, ChipMarketValueContribution> contributionByChipDef =
                new Dictionary<ThingDef, ChipMarketValueContribution>();
            foreach (ITriggerSlotState slot in reader.GetAllSlots())
            {
                Thing chip = slot != null ? slot.LoadedChip : null;
                if (chip == null || chip.def == null || !countedChips.Add(chip))
                {
                    continue;
                }

                ChipMarketValueContribution contribution;
                if (!contributionByChipDef.TryGetValue(chip.def, out contribution))
                {
                    contribution = new ChipMarketValueContribution(chip.def);
                    contributionByChipDef.Add(chip.def, contribution);
                    contributions.Add(contribution);
                }

                contribution.StackCount += chip.stackCount;
                contribution.MarketValue += chip.MarketValue * chip.stackCount;
            }

            return contributions;
        }
    }
}
