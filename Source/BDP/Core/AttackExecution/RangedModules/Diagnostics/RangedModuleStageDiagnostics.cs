using BDP.Support.Diagnostics;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 远程模块阶段诊断汇总器。
    /// 它统一记录阶段停止、模块异常和附加挂件异常，避免各阶段自己拼日志格式。
    /// </summary>
    internal static class RangedModuleStageDiagnostics
    {
        /// <summary>
        /// 记录一次阶段停止请求。
        /// </summary>
        internal static void LogStageStop(
            RangedStageKind stage,
            object module,
            string attackInstanceId,
            string resultId,
            int emitIndex,
            string reason)
        {
            BdpDiagnostics.AttackExecution(
                "event=ranged_module_stage_stop"
                + ", stage=" + stage
                + ", module=" + DescribeModule(module)
                + ", attackId=" + Safe(attackInstanceId)
                + ", resultId=" + Safe(resultId)
                + ", emitIndex=" + emitIndex
                + ", reason=" + Safe(reason));
        }

        /// <summary>
        /// 记录一次阶段标准贡献异常。
        /// </summary>
        internal static void LogStageContributionError(
            RangedStageKind stage,
            object module,
            string attackInstanceId,
            string resultId,
            int emitIndex,
            System.Exception exception)
        {
            BdpDiagnostics.AttackExecution(
                "event=ranged_module_stage_contribution_error"
                + ", stage=" + stage
                + ", module=" + DescribeModule(module)
                + ", attackId=" + Safe(attackInstanceId)
                + ", resultId=" + Safe(resultId)
                + ", emitIndex=" + emitIndex
                + ", error=" + Safe(exception != null ? exception.ToString() : null));
        }

        /// <summary>
        /// 记录一次阶段附加挂件异常。
        /// </summary>
        internal static void LogStageAddonError(
            RangedStageKind stage,
            object module,
            string attackInstanceId,
            string resultId,
            int emitIndex,
            System.Exception exception)
        {
            BdpDiagnostics.AttackExecution(
                "event=ranged_module_stage_addon_error"
                + ", stage=" + stage
                + ", module=" + DescribeModule(module)
                + ", attackId=" + Safe(attackInstanceId)
                + ", resultId=" + Safe(resultId)
                + ", emitIndex=" + emitIndex
                + ", error=" + Safe(exception != null ? exception.ToString() : null));
        }

        /// <summary>
        /// 输出模块类型说明。
        /// </summary>
        private static string DescribeModule(object module)
        {
            return module != null ? module.GetType().FullName : "<null>";
        }

        /// <summary>
        /// 统一清理空文本。
        /// </summary>
        private static string Safe(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "<none>" : text;
        }
    }
}
