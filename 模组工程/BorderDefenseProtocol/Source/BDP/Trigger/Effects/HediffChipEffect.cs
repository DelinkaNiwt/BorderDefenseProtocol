using BDP.Core;
using RimWorld;
using Verse;

namespace BDP.Trigger
{
    // ═══════════════════════════════════════════════════════════════════════
    //  抽象基类：HediffChipEffectBase
    //  职责：提供Hediff芯片效果的通用框架和辅助方法
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Hediff芯片效果抽象基类。
    /// 提供通用的配置读取和Hediff操作方法，子类实现具体的Severity计算逻辑。
    /// </summary>
    public abstract class HediffChipEffectBase : IChipEffect
    {
        public void Activate(Pawn pawn, Thing triggerBody)
        {
            var cfg = GetConfig(triggerBody);
            if (cfg?.hediffDef == null) return;

            var chipComp = triggerBody.TryGetComp<CompTriggerBody>();
            var slot = chipComp?.ActivatingSlot;

            // 检查是否已存在相同的hediff
            var existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(cfg.hediffDef);

            // 子类计算目标Severity
            float targetSeverity = CalculateActivateSeverity(chipComp, slot, cfg.hediffDef);

            if (existingHediff != null)
            {
                // 已存在：更新Severity
                existingHediff.Severity = targetSeverity;
                Log.Message($"[BDP-{GetType().Name}] {pawn.LabelShort} 更新Hediff Severity: {targetSeverity}");
            }
            else
            {
                // 不存在：添加新hediff
                var newHediff = pawn.health.AddHediff(cfg.hediffDef);
                newHediff.Severity = targetSeverity;
                Log.Message($"[BDP-{GetType().Name}] {pawn.LabelShort} 添加Hediff，Severity={targetSeverity}");
            }
        }

        public void Deactivate(Pawn pawn, Thing triggerBody)
        {
            var cfg = GetConfig(triggerBody);
            if (cfg?.hediffDef == null) return;

            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(cfg.hediffDef);
            if (hediff == null) return;

            var chipComp = triggerBody.TryGetComp<CompTriggerBody>();

            // 子类决定如何处理关闭
            HandleDeactivate(pawn, chipComp, hediff, cfg.hediffDef);
        }

        public void Tick(Pawn pawn, Thing triggerBody) { }

        public bool CanActivate(Pawn pawn, Thing triggerBody) => true;

        /// <summary>
        /// 子类实现：计算激活时的目标Severity。
        /// </summary>
        protected abstract float CalculateActivateSeverity(CompTriggerBody chipComp, ChipSlot slot, HediffDef hediffDef);

        /// <summary>
        /// 子类实现：处理关闭时的Hediff更新或移除。
        /// </summary>
        protected abstract void HandleDeactivate(Pawn pawn, CompTriggerBody chipComp, Hediff hediff, HediffDef hediffDef);

        /// <summary>
        /// 从CompTriggerBody读取HediffChipConfig。
        /// </summary>
        protected static HediffChipConfig GetConfig(Thing triggerBody)
        {
            return triggerBody.TryGetComp<CompTriggerBody>()?.GetChipExtension<HediffChipConfig>();
        }

        /// <summary>
        /// 统计当前有多少个激活的芯片会添加相同的hediff。
        /// 用于芯片叠加场景。
        /// </summary>
        protected static int CountActiveChipsWithSameHediff(CompTriggerBody chipComp, HediffDef hediffDef)
        {
            if (chipComp == null || hediffDef == null) return 0;

            int count = 0;
            foreach (var slot in chipComp.AllActiveSlots())
            {
                if (slot?.loadedChip == null) continue;

                var slotChipComp = slot.loadedChip.TryGetComp<TriggerChipComp>();
                if (slotChipComp == null) continue;

                var effects = slotChipComp.GetModeEffects(slot.currentModeIndex);
                if (effects == null) continue;

                foreach (var effect in effects)
                {
                    if (effect is HediffChipEffectBase)
                    {
                        var slotCfg = slot.loadedChip.def.GetModExtension<HediffChipConfig>();
                        if (slotCfg?.hediffDef == hediffDef)
                        {
                            count++;
                            break;
                        }
                    }
                }
            }

            return count;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  子类1：ModeBasedHediffChipEffect（形态切换型）
    //  职责：根据芯片形态索引设置Severity，用于单芯片多形态场景（如光魂芯片）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 形态切换型Hediff芯片效果。
    /// Severity由芯片的形态索引决定（形态0→Severity=1, 形态1→Severity=2）。
    /// 适用场景：光魂芯片（巨盾模式/重刃模式）。
    /// </summary>
    public class ModeBasedHediffChipEffect : HediffChipEffectBase
    {
        protected override float CalculateActivateSeverity(CompTriggerBody chipComp, ChipSlot slot, HediffDef hediffDef)
        {
            var modeIndex = slot?.GetCurrentModeIndex() ?? 0;
            float severity = modeIndex + 1f;
            Log.Message($"[BDP-ModeBasedHediff] 形态{modeIndex} → Severity={severity}");
            return severity;
        }

        protected override void HandleDeactivate(Pawn pawn, CompTriggerBody chipComp, Hediff hediff, HediffDef hediffDef)
        {
            // 形态切换型：关闭时直接移除hediff
            pawn.health.RemoveHediff(hediff);
            Log.Message($"[BDP-ModeBasedHediff] {pawn.LabelShort} 移除Hediff");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  子类2：StackableHediffChipEffect（芯片叠加型）
    //  职责：根据激活的芯片数量设置Severity，用于多芯片叠加场景（如能量盾芯片）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 芯片叠加型Hediff芯片效果。
    /// Severity由激活的芯片数量决定（1个芯片→Severity=1, 2个芯片→Severity=2）。
    /// 适用场景：能量盾芯片（多个芯片叠加提升防护范围和成功率）。
    /// </summary>
    public class StackableHediffChipEffect : HediffChipEffectBase
    {
        protected override float CalculateActivateSeverity(CompTriggerBody chipComp, ChipSlot slot, HediffDef hediffDef)
        {
            int activeChipCount = CountActiveChipsWithSameHediff(chipComp, hediffDef);
            Log.Message($"[BDP-StackableHediff] 激活芯片数{activeChipCount} → Severity={activeChipCount}");
            return activeChipCount;
        }

        protected override void HandleDeactivate(Pawn pawn, CompTriggerBody chipComp, Hediff hediff, HediffDef hediffDef)
        {
            // 芯片叠加型：统计剩余芯片数量
            // 注意：此时当前槽位还未标记为关闭，所以需要减1
            int remainingChipCount = CountActiveChipsWithSameHediff(chipComp, hediffDef) - 1;

            if (remainingChipCount > 0)
            {
                // 还有其他芯片激活，更新Severity
                hediff.Severity = remainingChipCount;
                Log.Message($"[BDP-StackableHediff] {pawn.LabelShort} 更新Hediff Severity: {remainingChipCount} (剩余激活芯片数)");
            }
            else
            {
                // 没有其他芯片激活，移除hediff
                pawn.health.RemoveHediff(hediff);
                Log.Message($"[BDP-StackableHediff] {pawn.LabelShort} 移除Hediff");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  配置类：HediffChipConfig
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Hediff芯片的DefModExtension配置。</summary>
    public class HediffChipConfig : DefModExtension
    {
        public HediffDef hediffDef;
    }
}
