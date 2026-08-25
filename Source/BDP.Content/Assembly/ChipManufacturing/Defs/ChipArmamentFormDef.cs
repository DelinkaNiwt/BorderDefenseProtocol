using System.Collections.Generic;
using BDP.Core.Chips;
using BDP.Core.Expressions;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Defs
{
    /// <summary>
    /// 芯片制造使用的武装型预设。
    /// </summary>
    public sealed class ChipArmamentFormDef : Def
    {
        /// <summary>允许使用该武装型的最终职业。</summary>
        public List<ChipProfessionDef> compatibleProfessions;

        /// <summary>
        /// 可选的动作预设白名单；为空表示不限制动作。
        /// 非空时，组合中的每个动作都必须属于该列表。
        /// </summary>
        public List<string> compatibleActionPresetDefNames;

        /// <summary>武装型对动作字段的可选覆盖。</summary>
        public ChipArmamentFormOverrides overrides;

        /// <summary>武装型对投射物字段的可选覆盖。</summary>
        public ProjectileOverrides projectileOverrides;

        /// <summary>武装型对单枚成品追加的具体材料。</summary>
        public List<ThingDefCountClass> additionalCost;

        /// <summary>武装型对单枚成品追加的工作量。</summary>
        public float additionalWorkAmount;

        /// <summary>该武装型允许组合的动作数量；默认每枚芯片只有一个动作。</summary>
        public int maxActionCount = 1;

        /// <summary>是否在制造台的武装型列表中显示。</summary>
        public bool showInManufacturing = true;

        /// <summary>是否把该武装型名称写入成品动态名称。</summary>
        public bool includeInProductLabel = true;

        /// <summary>是否作为满足条件时自动采用的隐藏默认型。</summary>
        public bool implicitDefault;
    }
}
