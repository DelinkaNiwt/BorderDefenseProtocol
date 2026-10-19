using System.Collections.Generic;
using Verse;

namespace BDP.Content.FloatingFlight
{
    /// <summary>
    /// 当前存档的 Pawn 浮空偏好与过渡账本。
    /// </summary>
    public sealed class GameComponent_FloatingFlight : GameComponent
    {
        /// <summary>当前存档中的 Pawn 浮空记录集合。</summary>
        private List<FloatingFlightRecord> records = new List<FloatingFlightRecord>();

        /// <summary>供原版 GameComponent 自动构造。</summary>
        public GameComponent_FloatingFlight(Game game)
        {
        }

        /// <summary>只读遍历当前存档的浮空记录。</summary>
        public IEnumerable<FloatingFlightRecord> Records
        {
            get { return records; }
        }

        /// <summary>查找指定 Pawn 的既有浮空记录。</summary>
        public bool TryGetRecord(Pawn pawn, out FloatingFlightRecord record)
        {
            record = null;
            if (pawn == null || records == null)
            {
                return false;
            }

            for (int index = 0; index < records.Count; index++)
            {
                FloatingFlightRecord candidate = records[index];
                if (candidate != null && candidate.Matches(pawn))
                {
                    candidate.BindPawn(pawn);
                    record = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>查找或创建指定 Pawn 的默认关闭浮空记录。</summary>
        public FloatingFlightRecord GetOrCreateRecord(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            if (TryGetRecord(pawn, out FloatingFlightRecord record))
            {
                return record;
            }

            // 所有创建入口共用业务准入，避免普通移动或外部误调用污染存档。
            if (!FloatingFlightUtility.IsActuallyFlying(pawn)
                && !FloatingFlightCapabilityResolver.HasCapability(pawn))
            {
                return null;
            }

            record = new FloatingFlightRecord(pawn);
            records.Add(record);
            return record;
        }

        /// <summary>移除已经确认永久销毁的 Pawn 记录。</summary>
        public void RemoveRecord(Pawn pawn)
        {
            if (pawn == null || records == null)
            {
                return;
            }

            for (int index = records.Count - 1; index >= 0; index--)
            {
                FloatingFlightRecord record = records[index];
                if (record != null && record.Matches(pawn))
                {
                    records.RemoveAt(index);
                }
            }
        }

        /// <summary>清理永久丢弃和无能力、无偏好、无过渡的空记录，保留玩家暂时停用芯片的模式。</summary>
        public void RemoveDiscardedRecords()
        {
            if (records == null)
            {
                records = new List<FloatingFlightRecord>();
                return;
            }

            for (int index = records.Count - 1; index >= 0; index--)
            {
                FloatingFlightRecord record = records[index];
                if (record?.Pawn != null
                    && ((record.Pawn.Destroyed && record.Pawn.Discarded)
                        || !HasUsefulState(record)))
                {
                    records.RemoveAt(index);
                }
            }
        }

        /// <summary>判断记录是否仍有业务用途；默认关闭的旧污染记录不能进入状态控制器。</summary>
        private static bool HasUsefulState(FloatingFlightRecord record)
        {
            Pawn pawn = record?.Pawn;
            return pawn != null
                && (record.PreferredMode != FloatingFlightMode.Off
                    || record.PendingSafeLanding
                    || record.GroundedForRoofSegment
                    || FloatingFlightUtility.IsActuallyFlying(pawn)
                    || FloatingFlightCapabilityResolver.HasCapability(pawn)
                    || (record.TransitionStartFactor > 0.001f
                        && FloatingFlightStateController.GetPresentationFactor(
                            pawn, record, FloatingFlightStateController.CurrentTick) > 0.001f));
        }

        /// <summary>保存或读取当前存档的 Pawn 浮空记录。</summary>
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref records, "floatingFlightRecords", LookMode.Deep);
            if (records == null)
            {
                records = new List<FloatingFlightRecord>();
            }
        }

        /// <summary>每 tick 收口已登记 Pawn 的实际飞行状态与生命周期。</summary>
        public override void GameComponentTick()
        {
            if (records == null)
            {
                records = new List<FloatingFlightRecord>();
                return;
            }

            // 先移除旧版留下的普通人物空记录，不能先执行可能改位置的强制落地逻辑。
            RemoveDiscardedRecords();
            for (int index = 0; index < records.Count; index++)
            {
                FloatingFlightRecord record = records[index];
                if (record?.Pawn != null)
                {
                    FloatingFlightStateController.Reconcile(record.Pawn, record);
                }
            }
        }
    }
}
