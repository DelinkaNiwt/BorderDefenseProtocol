using System.Collections.Generic;
using BDP.Content.Factions.GateArrival;
using Verse;

namespace BDP.Development.Factions
{
    /// <summary>可整体移除的门诱导调试按钮；不使用原版或正式业务的Harmony补丁。</summary>
    [StaticConstructorOnStartup]
    public static class GateInducerDebugGizmos
    {
        /// <summary>注册到本建筑的可选按钮扩展。</summary>
        static GateInducerDebugGizmos() { CompGateInducer.GatheringGizmos += Append; }

        /// <summary>只有上帝模式或开发按钮可见时才追加覆盖开关。</summary>
        private static void Append(CompGateInducer comp, List<Gizmo> gizmos)
        {
            if (!DebugSettings.godMode && !DebugSettings.ShowDevGizmos) return;
            gizmos.Add(new Command_Toggle
            {
                defaultLabel = "BDP_Dev_GateInducer_ForceOperation".Translate(),
                defaultDesc = "BDP_Dev_GateInducer_ForceOperationDescription".Translate(),
                isActive = () => comp.DebugForcedOperation,
                toggleAction = () => comp.SetDebugForcedOperation(!comp.DebugForcedOperation)
            });
        }
    }
}
