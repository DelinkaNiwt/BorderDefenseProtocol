using BDP.Core.CombatBody;
using Verse;

namespace BDP.Content.PlayerLog.CombatBody
{
    /// <summary>只订阅战斗体阶段事件，不保存另一份阶段状态。</summary>
    public sealed class CompCombatBodyPlayerLog : ThingComp
    {
        /// <summary>当前已绑定的事件口；不写入存档。</summary>
        private ICombatBodyEvents events;

        /// <summary>新人物组件完成创建后订阅，包含尚未入图的人物。</summary>
        public override void PostPostMake()
        {
            base.PostPostMake();
            BindEvents();
        }

        /// <summary>入图时确保订阅，不重播当前阶段。</summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            BindEvents();
        }

        /// <summary>全部引用恢复后重新绑定；读档本身不生成日志。</summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit) BindEvents();
        }

        /// <summary>销毁时解除订阅。</summary>
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            UnbindEvents();
            base.PostDestroy(mode, previousMap);
        }

        /// <summary>只在对象发生替换时重绑，防止重复订阅。</summary>
        private void BindEvents()
        {
            ICombatBodyEvents next = CombatBodySurfaceAccess.ResolveEvents(parent as Pawn);
            if (ReferenceEquals(events, next)) return;
            UnbindEvents();
            events = next;
            if (events != null) events.PhaseChanged += OnPhaseChanged;
        }

        /// <summary>清理当前事件引用。</summary>
        private void UnbindEvents()
        {
            if (events != null) events.PhaseChanged -= OnPhaseChanged;
            events = null;
        }

        /// <summary>只记录实际启动、开始崩解、主动解除三个确认点。</summary>
        private void OnPhaseChanged(CombatBodyPhaseChangedArgs args)
        {
            if (args == null || args.PreviousPhase == args.CurrentPhase) return;
            string key = null;
            if (args.CurrentPhase == CombatBodyPhase.Active)
                key = "BDP_Log_CombatBodyActivated";
            else if (args.CurrentPhase == CombatBodyPhase.Collapsing)
                key = args.Reason == "TrionAvailableDepleted"
                    ? "BDP_Log_CombatBodyCollapsedDepleted" : "BDP_Log_CombatBodyCollapsed";
            else if (args.CurrentPhase == CombatBodyPhase.Cooldown
                && args.PreviousPhase == CombatBodyPhase.Active && args.Reason == "Release")
                key = "BDP_Log_CombatBodyReleased";
            if (key != null) CombatBodyPlayerLog.Record(parent as Pawn, key);
        }
    }
}
