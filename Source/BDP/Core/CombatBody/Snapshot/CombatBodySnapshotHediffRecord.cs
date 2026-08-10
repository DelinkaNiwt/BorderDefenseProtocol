using Verse;

namespace BDP.Core.CombatBody
{
    /// <summary>
    /// 战斗体 Hediff 快照记录。
    /// 严格按旧版已确认字段集记录。
    /// </summary>
    internal sealed class CombatBodySnapshotHediffRecord : IExposable
    {
        public string defName;
        public float severity;
        public string bodyPartDefName;
        public int bodyPartIndex;
        public int ageTicks;
        public int? level;
        public bool? isPermanent;
        public int? painCategory;
        public string sourceLabel;
        public string sourceDefName;
        public string sourceToolLabel;
        public bool? isFresh;
        public string lastInjuryDefName;

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref severity, "severity", 0f);
            Scribe_Values.Look(ref bodyPartDefName, "bodyPartDefName");
            Scribe_Values.Look(ref bodyPartIndex, "bodyPartIndex", 0);
            Scribe_Values.Look(ref ageTicks, "ageTicks", 0);
            Scribe_Values.Look(ref level, "level");
            Scribe_Values.Look(ref isPermanent, "isPermanent");
            Scribe_Values.Look(ref painCategory, "painCategory");
            Scribe_Values.Look(ref sourceLabel, "sourceLabel");
            Scribe_Values.Look(ref sourceDefName, "sourceDefName");
            Scribe_Values.Look(ref sourceToolLabel, "sourceToolLabel");
            Scribe_Values.Look(ref isFresh, "isFresh");
            Scribe_Values.Look(ref lastInjuryDefName, "lastInjuryDefName");
        }
    }
}
