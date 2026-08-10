using System.Collections.Generic;
using Verse;

namespace BDP.Content.Assembly.ChipManufacturing.Defs
{
    /// <summary>
    /// 芯片制造职业定义；职业全局可空，填写时只能引用一个 Def。
    /// </summary>
    public sealed class ChipProfessionDef : Def
    {
        /// <summary>当前职业允许选择的动作原生职业。</summary>
        public List<ChipProfessionDef> acceptedActionProfessions;
    }
}
