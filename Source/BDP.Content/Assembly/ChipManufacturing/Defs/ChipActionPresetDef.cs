using System.Collections.Generic;
using BDP.Core.Chips;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Defs
{
    /// <summary>
    /// 玩家在制造面板中选择的一项动作预设。
    /// </summary>
    public sealed class ChipActionPresetDef : Def
    {
        /// <summary>动作名称的可选语言包键；为空时沿用 Def 原始标签。</summary>
        public string labelKey;

        /// <summary>读取当前语言下的动作名称。</summary>
        public string ResolvedLabel => !labelKey.NullOrEmpty()
            ? labelKey.Translate().ToString()
            : label;

        /// <summary>动作说明的可选语言包键；为空时沿用 Def 原始说明。</summary>
        public string descriptionKey;

        /// <summary>读取当前语言下的动作说明。</summary>
        public string ResolvedDescription => !descriptionKey.NullOrEmpty()
            ? descriptionKey.Translate().ToString()
            : description;

        /// <summary>动作的唯一原生职业；非武装动作可以为空。</summary>
        public ChipProfessionDef profession;

        /// <summary>动作当前提供的完整芯片配置。</summary>
        public ChipDefinitionConfig config;

        /// <summary>动作对单枚成品追加的具体材料。</summary>
        public List<ThingDefCountClass> costList;

        /// <summary>动作对单枚成品追加的工作量。</summary>
        public float additionalWorkAmount;
    }
}
