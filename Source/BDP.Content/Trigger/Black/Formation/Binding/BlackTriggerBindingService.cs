using Verse;

namespace BDP.Content.Trigger.Black.Formation
{
    /// <summary>黑触发器全局一人一件与绑定者死亡解绑服务。</summary>
    public static class BlackTriggerBindingService
    {
        /// <summary>判断人物是否已绑定另一件黑触发器。</summary>
        public static bool HasOtherBoundBlackTrigger(Pawn pawn, Thing except)
        {
            return BlackTriggerBindingIndex.HasOther(pawn, except);
        }

        /// <summary>绑定者死亡时，即使物品已掉落或被偷也能解除编码。</summary>
        public static void NotifyBinderDeath(Pawn pawn)
        {
            CompBlackTriggerBiocodable comp = BlackTriggerBindingIndex.Find(pawn);
            if (comp != null) comp.UnCode();
        }
    }
}
