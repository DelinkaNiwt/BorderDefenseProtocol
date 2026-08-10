using System.Collections.Generic;
using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体快照排除配置 Def。
    /// 当前先承接最小数据结构，具体规则在后续实现中接回。
    /// </summary>
    public sealed class CombatBodySnapshotConfigDef : Def
    {
        /// <summary>
        /// 需要按具体 Def 排除的 Hediff 名称。
        /// </summary>
        public List<string> excludedHediffs = new List<string>();

        /// <summary>
        /// 需要按 C# 类型排除的 Hediff 类名。
        /// </summary>
        public List<string> excludedHediffClasses = new List<string>();
    }
}
