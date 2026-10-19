using RimWorld;
using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>生命印记耗尽记录的读写入口。</summary>
    public static class BlackTriggerExhaustionUtility
    {
        private const string ExhaustionRecordDefName = "BDP_BlackTriggerLifeImprintExhausted";

        /// <summary>判断 Pawn 是否已经消耗过形成资格。</summary>
        public static bool IsExhausted(Pawn pawn)
        {
            RecordDef record = ResolveRecord();
            return pawn?.records != null && record != null && pawn.records.GetValue(record) > 0f;
        }

        /// <summary>成功形成后写入永久耗尽记录。</summary>
        public static void MarkExhausted(Pawn pawn)
        {
            RecordDef record = ResolveRecord();
            if (pawn?.records != null && record != null && !IsExhausted(pawn))
            {
                pawn.records.AddTo(record, 1f);
            }
        }

        /// <summary>仅供形成事务在尸体销毁前失败时撤回本次新增标记。</summary>
        public static void RollbackExhaustion(Pawn pawn)
        {
            RecordDef record = ResolveRecord();
            if (pawn?.records != null && record != null && pawn.records.GetValue(record) > 0f)
            {
                pawn.records.AddTo(record, -1f);
            }
        }

        /// <summary>解析隐藏记录 Def。</summary>
        private static RecordDef ResolveRecord()
        {
            return DefDatabase<RecordDef>.GetNamedSilentFail(ExhaustionRecordDefName);
        }
    }
}
