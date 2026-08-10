using System;
using BDP.Core.AttackExecution;
using BDP.Core.AttackExecution.RangedProtocol;
using BDP.Core.AttackExecution.RangedProtocol.Model;
using Verse;

namespace BDP.Core.Verbs
{
    /// <summary>
    /// 远程宿主单轮发射状态。
    /// 它只保存一轮暖机到首发提交之间的 `Trion` 门槛与扣费状态。
    /// </summary>
    internal sealed class RangedVerbRoundState
    {
        /// <summary>
        /// 当前这一轮正式发射计划要求支付的 `Trion` 成本。
        /// </summary>
        private float currentRoundTrionCost;

        /// <summary>
        /// 当前这一轮进入动作与首发提交需要满足的最低 `Trion` 门槛。
        /// </summary>
        private float currentRoundMinimumRequired;

        /// <summary>
        /// 当前这一轮是否已经正式提交过 `Trion` 成本。
        /// </summary>
        private bool hasCommittedRoundTrion;

        /// <summary>
        /// 当前这一轮正式发射计划要求支付的 `Trion` 成本。
        /// 它按轮结算，不按 projectile 逐发结算。
        /// </summary>
        internal float CurrentRoundTrionCost
        {
            get { return currentRoundTrionCost; }
        }

        /// <summary>
        /// 当前这一轮进入动作与首发提交需要满足的最低 `Trion` 门槛。
        /// </summary>
        internal float CurrentRoundMinimumRequired
        {
            get { return currentRoundMinimumRequired; }
        }

        /// <summary>
        /// 当前这一轮是否已经正式提交过 `Trion` 成本。
        /// 它用于避免 burst 内重复扣费。
        /// </summary>
        internal bool HasCommittedRoundTrion
        {
            get { return hasCommittedRoundTrion; }
        }

        /// <summary>
        /// 序列化单轮 `Trion` 状态。
        /// </summary>
        internal void ExposeData()
        {
            Scribe_Values.Look(ref currentRoundTrionCost, "currentRoundTrionCost", 0f);
            Scribe_Values.Look(ref currentRoundMinimumRequired, "currentRoundMinimumRequired", 0f);
            Scribe_Values.Look(ref hasCommittedRoundTrion, "hasCommittedRoundTrion", false);
        }

        /// <summary>
        /// 从当前远程执行上下文覆写单轮 `Trion` 状态。
        /// </summary>
        internal void ApplyExecutionContext(RangedAttackExecutionContext context)
        {
            currentRoundTrionCost = context?.ProtocolResult?.Prepare != null
                ? context.ProtocolResult.Prepare.ResourceCost
                : 0f;
            currentRoundMinimumRequired = context?.ProtocolResult?.Prepare != null
                ? context.ProtocolResult.Prepare.MinimumRequired
                : 0f;
            hasCommittedRoundTrion = false;
        }

        /// <summary>
        /// 用已保存的状态恢复单轮 `Trion` 进度。
        /// </summary>
        internal void Restore(float roundTrionCost, float roundMinimumRequired, bool hasCommittedRoundTrion)
        {
            currentRoundTrionCost = roundTrionCost;
            currentRoundMinimumRequired = roundMinimumRequired;
            this.hasCommittedRoundTrion = hasCommittedRoundTrion;
        }

        /// <summary>
        /// 判断当前这一轮是否存在 `Trion` 需求。
        /// </summary>
        internal bool HasRoundTrionRequirement()
        {
            return CurrentRoundTrionCost > 0f || CurrentRoundMinimumRequired > 0f;
        }

        /// <summary>
        /// 尝试让当前暖机进入单轮 `Trion` 准入。
        /// </summary>
        internal bool TryEnsureRoundTrionAdmission(Pawn pawn, Action<RangedAttackTrionGateResult> showFailure)
        {
            if (!HasRoundTrionRequirement())
            {
                return true;
            }

            RangedAttackTrionGate rangedAttackTrionGate = RangedAttackProtocolSurfaceAccess.ResolveTrionGate(pawn);
            if (rangedAttackTrionGate == null)
            {
                return false;
            }

            RangedAttackTrionGateResult result = rangedAttackTrionGate.TryAdmitWarmup(pawn, BuildCurrentRoundPrepare());
            if (result != null && result.Succeeded)
            {
                return true;
            }

            showFailure?.Invoke(result);
            return false;
        }

        /// <summary>
        /// 尝试在首发前正式提交本轮 `Trion` 成本。
        /// </summary>
        internal bool TryCommitRoundTrionBeforeFirstEmission(Pawn pawn, Action<RangedAttackTrionGateResult> showFailure)
        {
            if (HasCommittedRoundTrion || !HasRoundTrionRequirement())
            {
                return true;
            }

            RangedAttackTrionGate rangedAttackTrionGate = RangedAttackProtocolSurfaceAccess.ResolveTrionGate(pawn);
            if (rangedAttackTrionGate == null)
            {
                return false;
            }

            RangedAttackTrionGateResult result = rangedAttackTrionGate.TryCommitBeforeFirstEmission(pawn, BuildCurrentRoundPrepare());
            if (result != null && result.Succeeded)
            {
                hasCommittedRoundTrion = true;
                return true;
            }

            showFailure?.Invoke(result);
            return false;
        }

        /// <summary>
        /// 清空当前单轮 `Trion` 状态。
        /// </summary>
        internal void Reset()
        {
            currentRoundTrionCost = 0f;
            currentRoundMinimumRequired = 0f;
            hasCommittedRoundTrion = false;
        }

        /// <summary>
        /// 构造当前这一轮要提交给远程 `Trion` 闸门的准备记录。
        /// </summary>
        private PrepareRecord BuildCurrentRoundPrepare()
        {
            return new PrepareRecord
            {
                ResourceCost = CurrentRoundTrionCost,
                MinimumRequired = CurrentRoundMinimumRequired
            };
        }
    }
}
