using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Defs
{
    /// <summary>
    /// 芯片制造使用的武装构型定义。
    /// </summary>
    public sealed class ChipArmamentFormDef : Def
    {
        /// <summary>武装构型名称的可选语言键；为空时沿用 Def 中的默认名称。</summary>
        public string labelKey;

        /// <summary>读取当前语言下的武装构型名称。</summary>
        public string ResolvedLabel => !labelKey.NullOrEmpty()
            ? labelKey.Translate().ToString()
            : label;

        /// <summary>武装构型说明的可选语言键；为空时沿用 Def 中的默认说明。</summary>
        public string descriptionKey;

        /// <summary>读取当前语言下的武装构型说明。</summary>
        public string ResolvedDescription => !descriptionKey.NullOrEmpty()
            ? descriptionKey.Translate().ToString()
            : description;

        /// <summary>允许使用该武装型的最终职业。</summary>
        public List<ChipProfessionDef> compatibleProfessions;

        /// <summary>
        /// 可选的芯片功能模板白名单；为空表示不限制芯片功能模板。
        /// 非空时，组合中的每个芯片功能模板都必须属于该列表。
        /// </summary>
        public List<string> compatibleChipFunctionDefNames;

        /// <summary>武装型对表达条目字段的可选覆盖。</summary>
        public ChipArmamentFormOverrides overrides;

        /// <summary>武装型对投射物字段的可选覆盖。</summary>
        public ProjectileOverrides projectileOverrides;

        /// <summary>武装型对单枚成品追加的具体材料。</summary>
        public List<ThingDefCountClass> additionalCost;

        /// <summary>武装型对单枚成品追加的工作量。</summary>
        public float additionalWorkAmount;

        /// <summary>该武装型允许组合的芯片功能模板数量；默认每枚芯片只有一个。</summary>
        public int maxFunctionCount = 1;

        /// <summary>是否在制造台的武装型列表中显示。</summary>
        public bool showInManufacturing = true;

        /// <summary>制造该构型必须全部完成的研究；不限制现有装备的解析与使用。</summary>
        public List<ResearchProjectDef> researchPrerequisites;

        /// <summary>读取玩家制造入口的研究解锁状态。</summary>
        public bool ResearchFinished => Resolution.ChipManufacturingResearch.AreFinished(researchPrerequisites);

        /// <summary>是否把该武装型名称写入成品动态名称。</summary>
        public bool includeInProductLabel = true;

        /// <summary>是否作为满足条件时自动采用的隐藏默认型。</summary>
        public bool implicitDefault;
    }
}
