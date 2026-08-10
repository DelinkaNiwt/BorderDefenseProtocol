using BDP.Core.Trigger;
using Verse;

namespace BDP.Core.VerbHosting
{
    /// <summary>
    /// VerbHosting 对内接口获取面。
    /// 其它模块统一从这里拿正式宿主 binding 与正式壳 verb，不再读取临时宿主实例。
    /// </summary>
    internal static class VerbHostSurfaceAccess
    {
        /// <summary>
        /// 读取指定 Pawn 当前 TriggerBody 上的正式宿主绑定管理器。
        /// </summary>
        private static TriggerBodyVerbHostManager ResolveManager(Pawn pawn)
        {
            CompTriggerBody triggerBody = TriggerSurfaceAccess.ResolveComp(pawn);
            return triggerBody != null ? triggerBody.VerbHostManager : null;
        }

        /// <summary>
        /// 按正式结果标识读取当前正式宿主 binding。
        /// </summary>
        public static bool TryGetByResultId(Pawn pawn, string resultId, out BdpFormalVerbBinding binding)
        {
            binding = null;
            TriggerBodyVerbHostManager manager = ResolveManager(pawn);
            return manager != null
                && !string.IsNullOrWhiteSpace(resultId)
                && manager.TryGetByResultId(resultId, out binding);
        }

    }
}
