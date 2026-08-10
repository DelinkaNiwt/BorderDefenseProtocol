using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体 Need 快照记录。
    /// </summary>
    internal sealed class CombatBodySnapshotNeedRecord : IExposable
    {
        /// <summary>
        /// NeedDef 名称。
        /// </summary>
        public string needDefName;

        /// <summary>
        /// 当前 Need 等级值。
        /// </summary>
        public float curLevel;

        /// <summary>
        /// 存读档 Need 快照记录。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref needDefName, "needDefName");
            Scribe_Values.Look(ref curLevel, "curLevel", 0f);
        }
    }
}
