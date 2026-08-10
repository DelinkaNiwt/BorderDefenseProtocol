using BDP.Core.AttackExecution.RangedProtocol.Model;
using BDP.Core.Trion;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol
{
    /// <summary>
    /// 远程动作 Trion 闸门。
    /// 它只负责 warmup 准入检查与首发前正式提交，不解释成本来源。
    /// </summary>
    internal sealed class RangedAttackTrionGate
    {
        public RangedAttackTrionGateResult TryAdmitWarmup(Pawn pawn, PrepareRecord prepare)
        {
            return EvaluateAffordability(pawn, prepare, false);
        }

        public RangedAttackTrionGateResult TryCommitBeforeFirstEmission(Pawn pawn, PrepareRecord prepare)
        {
            RangedAttackTrionGateResult result = EvaluateAffordability(pawn, prepare, true);
            if (!result.Succeeded)
            {
                return result;
            }

            float resourceCost = prepare != null ? prepare.ResourceCost : 0f;
            if (resourceCost <= 0f)
            {
                return Succeed();
            }

            ITrionCommands commands = TrionSurfaceAccess.ResolveCommands(pawn);
            if (commands != null && commands.TryConsume(resourceCost))
            {
                return Succeed();
            }

            return Fail("round_cost_commit_failed");
        }

        private static RangedAttackTrionGateResult EvaluateAffordability(Pawn pawn, PrepareRecord prepare, bool finalCommit)
        {
            float requiredAvailable = ResolveRequiredAvailable(prepare);
            if (requiredAvailable <= 0f)
            {
                return Succeed();
            }

            ITrionCommands commands = TrionSurfaceAccess.ResolveCommands(pawn);
            if (commands != null && commands.CanAfford(requiredAvailable))
            {
                return Succeed();
            }

            return Fail(finalCommit ? "round_cost_commit_unaffordable" : "round_cost_admission_unaffordable");
        }

        private static float ResolveRequiredAvailable(PrepareRecord prepare)
        {
            float resourceCost = prepare != null ? prepare.ResourceCost : 0f;
            float minimumRequired = prepare != null ? prepare.MinimumRequired : 0f;
            return resourceCost > minimumRequired ? resourceCost : minimumRequired;
        }

        private static RangedAttackTrionGateResult Succeed()
        {
            return new RangedAttackTrionGateResult
            {
                Succeeded = true,
                Reason = null,
                Message = null
            };
        }

        private static RangedAttackTrionGateResult Fail(string reason)
        {
            return new RangedAttackTrionGateResult
            {
                Succeeded = false,
                Reason = reason,
                Message = "BDP_Message_Trion_RangedInsufficient".Translate()
            };
        }
    }
}
