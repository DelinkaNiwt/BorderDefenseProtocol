using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>仅消费明确接入的宿主限制，普通人物默认保持现有行为。</summary>
    public static class TriggerFunctionAvailability
    {
        /// <summary>任一宿主限制拒绝就不发布功能；不把拒绝解释为槽位停用。</summary>
        public static bool Allows(Pawn pawn)
        {
            if (pawn?.AllComps == null) return true;
            foreach (ThingComp comp in pawn.AllComps)
                if (comp is ITriggerFunctionAvailability host && !host.TriggerFunctionsAvailable) return false;
            return true;
        }
    }
}
