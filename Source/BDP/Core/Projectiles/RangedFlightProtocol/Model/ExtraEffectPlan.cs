using System.Collections.Generic;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Model
{
    /// <summary>
    /// 额外效果正式计划。
    /// 第一版先统一成轻量参数袋，不引入更重的特效系统。
    /// </summary>
    public sealed class ExtraEffectPlan
    {
        /// <summary>
        /// 当前效果从哪一种原版命中范围取得目标。
        /// </summary>
        public ExtraEffectTargetScope TargetScope { get; set; } = ExtraEffectTargetScope.DirectHitThing;

        /// <summary>效果是否消费本次直接命中的部位事实；不承载选择策略。</summary>
        public ExtraEffectPartTargetMode PartTargetMode { get; set; }

        /// <summary>
        /// 当前额外效果相对于投射物伤害的执行时序。
        /// </summary>
        public ExtraEffectExecutionTiming ExecutionTiming { get; set; } = ExtraEffectExecutionTiming.AfterDamage;

        /// <summary>
        /// 当前额外效果的种类键。
        /// </summary>
        public string EffectKind { get; set; }

        /// <summary>
        /// 当前额外效果命中的 Thing。
        /// </summary>
        public Thing TargetThing { get; set; }

        /// <summary>
        /// 当前额外效果对应的命中身体部位；为空表示效果不限定到部位。
        /// 这里只传递命中事实，不承载部位选择或健康后果逻辑。
        /// </summary>
        public BodyPartRecord HitPart { get; set; }

        /// <summary>
        /// 当前额外效果命中的格子。
        /// </summary>
        public IntVec3 TargetCell { get; set; }

        /// <summary>
        /// 当前额外效果携带的参数字典。
        /// </summary>
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 当前额外效果附带的轻量标签集合。
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 复制当前效果并替换逐目标上下文。
        /// </summary>
        public ExtraEffectPlan CloneForTarget(Thing targetThing, IntVec3 targetCell)
        {
            ExtraEffectPlan result = new ExtraEffectPlan
            {
                TargetScope = TargetScope,
                PartTargetMode = PartTargetMode,
                ExecutionTiming = ExecutionTiming,
                EffectKind = EffectKind,
                TargetThing = targetThing,
                HitPart = HitPart,
                TargetCell = targetCell,
                Parameters = Parameters != null
                    ? new Dictionary<string, string>(Parameters)
                    : new Dictionary<string, string>(),
                Tags = Tags != null ? new List<string>(Tags) : new List<string>()
            };
            return result;
        }
    }
}
