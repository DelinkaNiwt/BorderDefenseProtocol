using Verse;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 浮空偏好账本的最小正式访问入口。
    /// </summary>
    public static class FloatingFlightLedgerService
    {
        /// <summary>从当前存档解析浮空 GameComponent。</summary>
        public static GameComponent_FloatingFlight ResolveLedger()
        {
            return Current.Game == null
                ? null
                : Current.Game.GetComponent<GameComponent_FloatingFlight>();
        }

        /// <summary>尝试读取指定 Pawn 的浮空记录。</summary>
        public static bool TryGetRecord(Pawn pawn, out FloatingFlightRecord record)
        {
            record = null;
            GameComponent_FloatingFlight ledger = ResolveLedger();
            return ledger != null && ledger.TryGetRecord(pawn, out record);
        }

        /// <summary>读取或创建指定 Pawn 的默认浮空记录。</summary>
        public static FloatingFlightRecord GetOrCreateRecord(Pawn pawn)
        {
            GameComponent_FloatingFlight ledger = ResolveLedger();
            return ledger == null ? null : ledger.GetOrCreateRecord(pawn);
        }
    }
}
