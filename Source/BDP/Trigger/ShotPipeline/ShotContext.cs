using Verse;

namespace BDP.Trigger.ShotPipeline
{
    /// <summary>
    /// 射击上下文：管线入口的初始数据快照，整个管线期间只读
    /// </summary>
    public class ShotContext
    {
        // 施法者信息
        public Pawn Caster { get; private set; }
        public CompTriggerBody TriggerComp { get; private set; }

        // 目标信息
        public LocalTargetInfo Target { get; private set; }
        public IntVec3 CasterPosition { get; private set; }

        // 武器信息
        public Verb_BDPRangedBase Verb { get; private set; }
        public VerbChipConfig ChipConfig { get; private set; }
        public SlotSide? ChipSide { get; private set; }
        public Thing ChipThing { get; private set; }

        // 配置快照
        public RangedConfig RangedConfig { get; private set; }
        public GuidedConfig GuidedConfig { get; private set; }
        public FiringPattern FiringPattern { get; private set; }
        public VerbProperties VerbProps { get; private set; }

        // 投射物定义（明确字段，避免模块从多源查找）
        public ThingDef ProjectileDef { get; private set; }

        // ══════════════════════════════════════════
        //  构造函数
        // ══════════════════════════════════════════

        /// <summary>
        /// 创建射击上下文（快照所有必要数据）
        /// </summary>
        public ShotContext(
            Pawn caster,
            CompTriggerBody triggerComp,
            LocalTargetInfo target,
            Verb_BDPRangedBase verb,
            VerbChipConfig chipConfig,
            SlotSide? chipSide,
            Thing chipThing,
            ThingDef projectileDef)
        {
            Caster = caster;
            TriggerComp = triggerComp;
            Target = target;
            CasterPosition = caster.Position;
            Verb = verb;
            ChipConfig = chipConfig;
            ChipSide = chipSide;
            ChipThing = chipThing;

            // 快照配置（避免后续修改影响管线）
            RangedConfig = chipConfig?.ranged;
            GuidedConfig = chipConfig?.ranged?.guided;
            FiringPattern = chipConfig?.primaryFiringPattern ?? FiringPattern.Sequential;
            VerbProps = verb?.verbProps;
            ProjectileDef = projectileDef;
        }
    }
}
