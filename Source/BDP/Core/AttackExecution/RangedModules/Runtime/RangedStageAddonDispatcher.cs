using System.Collections.Generic;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 远程阶段附加挂件分发器。
    /// 它只负责顺序执行附加挂件，不参与阶段主逻辑裁决。
    /// </summary>
    internal static class RangedStageAddonDispatcher
    {
        /// <summary>
        /// 顺序执行当前阶段所有附加挂件。
        /// </summary>
        internal static void Execute(IReadOnlyList<IRangedStageAddonModule> addons, in RangedStageAddonContext context)
        {
            if (addons == null)
            {
                return;
            }

            for (int i = 0; i < addons.Count; i++)
            {
                IRangedStageAddonModule addon = addons[i];
                if (addon == null)
                {
                    continue;
                }

                try
                {
                    addon.AfterStage(context);
                }
                catch (System.Exception ex)
                {
                    RangedModuleStageDiagnostics.LogStageAddonError(
                        context.Stage,
                        addon,
                        context.AttackInstanceId,
                        context.ResultId,
                        context.EmitIndex,
                        ex);
                    throw;
                }
            }
        }
    }
}
