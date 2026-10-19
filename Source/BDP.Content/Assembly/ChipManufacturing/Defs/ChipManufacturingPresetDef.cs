using System.Collections.Generic;
using BDP.Core.Chips;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Defs
{
    /// <summary>
    /// 玩家在制造面板中选择的一项芯片功能模板。
    /// </summary>
    public sealed class ChipManufacturingPresetDef : Def
    {
        /// <summary>玩家制造入口开关：约束列表及生产账单，不限制已有芯片使用和剧情生成。</summary>
        public bool showInManufacturing = true;

        /// <summary>制造该功能必须全部完成的研究；不限制现有芯片的使用与剧情生成。</summary>
        public List<ResearchProjectDef> researchPrerequisites;

        /// <summary>读取玩家制造入口的研究解锁状态。</summary>
        public bool ResearchFinished => Resolution.ChipManufacturingResearch.AreFinished(researchPrerequisites);

        /// <summary>芯片功能模板名称的可选语言包键；为空时沿用 Def 原始标签。</summary>
        public string labelKey;

        /// <summary>读取当前语言下的芯片功能模板名称。</summary>
        public string ResolvedLabel => !labelKey.NullOrEmpty()
            ? labelKey.Translate().ToString()
            : label;

        /// <summary>芯片功能模板说明的可选语言包键；为空时沿用 Def 原始说明。</summary>
        public string descriptionKey;

        /// <summary>读取当前语言下的芯片功能模板说明。</summary>
        public string ResolvedDescription => !descriptionKey.NullOrEmpty()
            ? descriptionKey.Translate().ToString()
            : description;

        /// <summary>芯片功能模板的唯一原生职业；为空表示该功能不限制最终职业。</summary>
        public ChipProfessionDef profession;

        /// <summary>固定装载创建该功能芯片时显式组合的武装构型；为空时沿用组合解析器的默认规则。</summary>
        public ChipArmamentFormDef fixedLoadoutArmamentForm;

        /// <summary>芯片功能模板当前提供的完整芯片配置。</summary>
        public ChipDefinitionConfig config;

        /// <summary>芯片功能模板对单枚成品追加的具体材料。</summary>
        public List<ThingDefCountClass> costList;

        /// <summary>芯片功能模板对单枚成品追加的工作量。</summary>
        public float additionalWorkAmount;
    }
}
