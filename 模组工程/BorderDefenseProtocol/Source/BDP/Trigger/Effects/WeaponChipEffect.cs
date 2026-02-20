using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// 武器类芯片效果——通过CompTriggerBody的按侧Verb存储向触发体注入Verb/Tool配置。
    ///
    /// v2.0变更（T24）：
    ///   - Activate改为调用SetSideVerbs（按侧存储，支持双武器）
    ///   - Deactivate改为调用ClearSideVerbs
    ///   - 侧别通过CompTriggerBody.ActivatingSide临时上下文获取
    ///
    /// 关键设计（T13）：Activate/Deactivate必须双重重建VerbTracker：
    ///   1. triggerBody的CompTriggerBody.VerbTracker（决定"有哪些Verb"）
    ///   2. pawn.verbTracker（决定"Pawn实际能用哪些Verb"）
    /// </summary>
    public class WeaponChipEffect : IChipEffect
    {
        public void Activate(Pawn pawn, Thing triggerBody)
        {
            var triggerComp = triggerBody.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return;

            // 通过ActivatingSide获取当前操作的侧别
            var side = triggerComp.ActivatingSide ?? SlotSide.Left;

            // 此处myVerbProperties/myTools由具体武器芯片的DefModExtension配置提供
            // 基类实现为stub——具体武器芯片子类应override此方法提供实际数据
            // TODO: 从芯片ThingDef读取Verb/Tool配置
            triggerComp.SetSideVerbs(side, null, null);

            RebuildVerbs(pawn, triggerBody);
        }

        public void Deactivate(Pawn pawn, Thing triggerBody)
        {
            var triggerComp = triggerBody.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return;

            var side = triggerComp.ActivatingSide ?? SlotSide.Left;
            triggerComp.ClearSideVerbs(side);

            RebuildVerbs(pawn, triggerBody);
        }

        public void Tick(Pawn pawn, Thing triggerBody) { }

        public bool CanActivate(Pawn pawn, Thing triggerBody) => true;

        /// <summary>
        /// 双重重建VerbTracker（T13核心修复）。
        /// 先重建武器的VerbTracker，再重建Pawn的verbTracker。
        /// </summary>
        protected static void RebuildVerbs(Pawn pawn, Thing triggerBody)
        {
            triggerBody.TryGetComp<CompTriggerBody>()?.VerbTracker?.InitVerbsFromZero();
            pawn?.verbTracker?.InitVerbsFromZero();
        }
    }
}
