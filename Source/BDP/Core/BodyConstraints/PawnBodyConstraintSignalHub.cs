using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.BodyConstraints
{
    /// <summary>
    /// Pawn 身体约束信号总线。
    /// 它只负责发布与查询中性上游变化，不直接理解 Trigger 或其它下游业务。
    /// </summary>
    internal static class PawnBodyConstraintSignalHub
    {
        /// <summary>
        /// 每个 Pawn 当前的身体约束版本。
        /// 键使用 thingIDNumber，避免长期强持有 Pawn 引用。
        /// </summary>
        private static readonly Dictionary<int, int> VersionByPawnId = new Dictionary<int, int>();

        /// <summary>
        /// 身体约束变化时广播。
        /// 下游若需要，可订阅这条中性事件。
        /// </summary>
        public static event Action<PawnBodyConstraintChangedArgs> Changed;

        /// <summary>
        /// 发布一次身体约束变化。
        /// </summary>
        public static void Publish(Pawn pawn, PawnBodyConstraintChangeKind changeKind)
        {
            if (pawn == null)
            {
                return;
            }

            int pawnId = pawn.thingIDNumber;
            int nextVersion = 1;
            int currentVersion;
            if (VersionByPawnId.TryGetValue(pawnId, out currentVersion))
            {
                nextVersion = currentVersion + 1;
            }

            VersionByPawnId[pawnId] = nextVersion;
            Changed?.Invoke(new PawnBodyConstraintChangedArgs
            {
                Pawn = pawn,
                ChangeKind = changeKind,
                Version = nextVersion
            });
        }

        /// <summary>
        /// 查询指定 Pawn 当前的身体约束版本。
        /// 未发布过变化时返回 0。
        /// </summary>
        public static int GetVersion(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0;
            }

            int version;
            return VersionByPawnId.TryGetValue(pawn.thingIDNumber, out version) ? version : 0;
        }
    }
}
