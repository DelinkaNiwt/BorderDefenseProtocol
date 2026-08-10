using BDP.Core.BodyConstraints;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// Trigger 身体约束信号桥。
    /// 它只负责把上游身体缺失事实桥接到当前主 Trigger 的即时禁用结算，不承载身体语义规则本身。
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class TriggerBodyConstraintSignalBridge
    {
        /// <summary>
        /// 模组加载时接上线一次身体约束事实桥。
        /// </summary>
        static TriggerBodyConstraintSignalBridge()
        {
            PawnBodyConstraintSignalHub.Changed += OnPawnBodyConstraintChanged;
        }

        /// <summary>
        /// 在身体缺失事实发生后，立即把变化桥接到当前主 Trigger。
        /// </summary>
        private static void OnPawnBodyConstraintChanged(PawnBodyConstraintChangedArgs args)
        {
            if (args == null || args.Pawn == null)
            {
                return;
            }

            if (args.ChangeKind != PawnBodyConstraintChangeKind.MissingPartChanged)
            {
                return;
            }

            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(args.Pawn);
            triggerBody?.ApplyBodyConstraintChangeImmediately();
        }
    }
}
