using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>少数特殊适格规则可使用的自定义判断基类。</summary>
    public abstract class BlackTriggerEligibilityWorker
    {
        /// <summary>判断人物是否满足该条适格条件。</summary>
        public abstract bool Matches(Pawn pawn, BlackTriggerEligibilityCondition condition);
    }
}
