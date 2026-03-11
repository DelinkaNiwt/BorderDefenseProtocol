using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    /// <summary>
    /// Verb芯片效果——通过CompTriggerBody的按侧Verb存储向触发体注入Verb/Tool配置。
    /// 适用于所有武器类芯片（近战、远程、狙击等）。
    /// 机制：通过RimWorld的Verb系统实现攻击行为。
    ///
    /// v2.0变更（T24）：
    ///   - Activate改为调用SetSideVerbs（按侧存储，支持双武器）
    ///   - Deactivate改为调用ClearSideVerbs
    ///   - 侧别通过CompTriggerBody.ActivatingSide临时上下文获取
    ///
    /// v5.0变更：RebuildVerbs搬迁至CompTriggerBody，本类只负责设置/清除Verb数据。
    /// </summary>
    public class VerbChipEffect : IChipEffect
    {
        public void Activate(Pawn pawn, Thing triggerBody)
        {
            var triggerComp = triggerBody.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return;

            // 通过ActivatingSide获取当前操作的侧别
            var side = triggerComp.ActivatingSide ?? SlotSide.LeftHand;

            // 从ActivatingSlot读取VerbChipConfig（T36：数据存在DefModExtension中）
            var cfg = GetConfig(triggerComp);

            // v9.0统一架构：所有芯片必须提供primaryVerbProps
            if (cfg?.primaryVerbProps == null)
            {
                Log.Error($"[BDP] VerbChipEffect: 芯片缺少primaryVerbProps配置，无法激活。芯片：{triggerComp.ActivatingSlot?.loadedChip?.def?.defName ?? "unknown"}");
                return;
            }

            var verbs = new List<VerbProperties> { cfg.primaryVerbProps };
            triggerComp.SetSideVerbs(side, verbs, cfg.melee?.tools);

            // v5.0：RebuildVerbs已搬迁至CompTriggerBody
            triggerComp.RebuildVerbs(pawn);
        }

        public void Deactivate(Pawn pawn, Thing triggerBody)
        {
            var triggerComp = triggerBody.TryGetComp<CompTriggerBody>();
            if (triggerComp == null) return;

            var side = triggerComp.ActivatingSide ?? SlotSide.LeftHand;
            triggerComp.SetSideVerbs(side, null, null);

            // v5.0：RebuildVerbs已搬迁至CompTriggerBody
            triggerComp.RebuildVerbs(pawn);
        }

        public void Tick(Pawn pawn, Thing triggerBody) { }

        public bool CanActivate(Pawn pawn, Thing triggerBody) => true;

        /// <summary>
        /// 从CompTriggerBody读取VerbChipConfig（委托给通用GetChipExtension）。
        /// </summary>
        private static VerbChipConfig GetConfig(CompTriggerBody triggerComp)
        {
            return triggerComp?.GetChipExtension<VerbChipConfig>();
        }
    }
}
