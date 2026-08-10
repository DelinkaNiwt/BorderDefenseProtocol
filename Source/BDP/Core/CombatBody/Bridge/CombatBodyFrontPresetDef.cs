using System.Collections.Generic;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体前台预设 Def。
    /// </summary>
    public sealed class CombatBodyFrontPresetDef : Def
    {
        /// <summary>
        /// 当前预设包含的衣物 Def 名称列表。
        /// </summary>
        public List<string> apparelDefNames = new List<string>();
    }
}
