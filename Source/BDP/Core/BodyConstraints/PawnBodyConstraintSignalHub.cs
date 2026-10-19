using System;
using System.Runtime.CompilerServices;
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
        /// 身体版本属于真实 Pawn 实例；弱键在角色释放时自动释放记录，不跨存档串编号。
        /// </summary>
        private static readonly ConditionalWeakTable<Pawn, VersionState> Versions =
            new ConditionalWeakTable<Pawn, VersionState>();

        /// <summary>弱表所需的可变版本值，不保存 Pawn 反向引用。</summary>
        private sealed class VersionState
        {
            /// <summary>该角色已发布的身体约束版本。</summary>
            public int Version;
        }

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

            VersionState state = Versions.GetValue(pawn, key => new VersionState());
            int nextVersion = ++state.Version;
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

            VersionState state;
            return Versions.TryGetValue(pawn, out state) ? state.Version : 0;
        }
    }
}
