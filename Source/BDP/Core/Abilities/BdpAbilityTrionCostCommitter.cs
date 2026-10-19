using RimWorld;
using BDP.Core.Trion;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// BDP 表达能力的 Trion（触力能）成本提交器。
    /// 它只复用现有成本组件，不决定具体 Ability Verb 的施放流程。
    /// </summary>
    public static class BdpAbilityTrionCostCommitter
    {
        /// <summary>
        /// 遍历指定能力的全部 BDP Trion 成本组件并正式提交。
        /// 没有成本组件时保持成功，因此普通短距跳跃继续免费。
        /// </summary>
        public static bool TryCommit(Ability ability)
        {
            if (ability?.EffectComps == null)
            {
                return true;
            }

            for (int index = 0; index < ability.EffectComps.Count; index++)
            {
                if (ability.EffectComps[index] is CompAbilityEffect_BdpTrionCost trionCostComp
                    && !trionCostComp.TryCommitCastCost())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>向预览提供整批费用；与实际提交读取同一组表达成本，不写资源。</summary>
        public static bool TryGetTotalCost(Ability ability, int count, out float total, out string reason)
        {
            float minimum;
            return TryGetBatchBudget(ability, count, out total, out minimum, out reason);
        }

        /// <summary>检查整批数量；合并同一角色的成本组件，防止逐组件扣费产生半支付。</summary>
        public static bool CanAfford(Ability ability, int count, out string reason)
        {
            float total;
            float minimum;
            return TryGetBatchBudget(ability, count, out total, out minimum, out reason)
                && CheckBatchPayment(ability, total, minimum, out reason);
        }

        /// <summary>批量施放只提交一次总费用；旧单次入口仍保持既有行为。</summary>
        public static bool TryCommit(Ability ability, int count)
        {
            float total;
            float minimum;
            string reason;
            if (!TryGetBatchBudget(ability, count, out total, out minimum, out reason)
                || !CheckBatchPayment(ability, total, minimum, out reason))
            {
                if (ability?.pawn != null) Messages.Message(reason, ability.pawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (total <= 0f || TrionSurfaceAccess.ResolveCommands(ability.pawn).TryConsume(total)) return true;
            Messages.Message("BDP_Message_Ability_TrionInsufficient".Translate(), ability.pawn, MessageTypeDefOf.RejectInput, false);
            return false;
        }

        /// <summary>读取所有成本声明；总费用累加、最低资格取最大，均不直接写资源。</summary>
        private static bool TryGetBatchBudget(Ability ability, int count,
            out float total, out float minimum, out string reason)
        {
            total = 0f;
            minimum = 0f;
            reason = "BDP_BatchAbility_InvalidRequest".Translate();
            if (ability?.pawn == null || count <= 0) return false;
            try
            {
                foreach (CompAbilityEffect effect in ability.EffectComps)
                {
                    var cost = effect as CompAbilityEffect_BdpTrionCost;
                    if (cost == null) continue;
                    total += BatchAbilityBudget.TotalCost(cost.TrionCost, count);
                    minimum = System.Math.Max(minimum, cost.MinimumRequired);
                }
                if (float.IsNaN(total) || float.IsInfinity(total)
                    || float.IsNaN(minimum) || float.IsInfinity(minimum)) return false;
            }
            catch (System.ArgumentOutOfRangeException) { return false; }
            reason = null;
            return true;
        }

        /// <summary>资源不足或宿主缺失时复用既有拒绝文案。</summary>
        private static bool CheckBatchPayment(Ability ability, float total, float minimum, out string reason)
        {
            reason = null;
            float required = BatchAbilityBudget.RequiredCost(total, minimum, 1);
            if (required <= 0f) return true;
            ITrionCommands commands = TrionSurfaceAccess.ResolveCommands(ability.pawn);
            if (commands == null) reason = "BDP_Message_Ability_MissingTrionHost".Translate();
            else if (!commands.CanAfford(required))
                reason = "BDP_Message_Ability_TrionInsufficient".Translate();
            return reason == null;
        }
    }
}
