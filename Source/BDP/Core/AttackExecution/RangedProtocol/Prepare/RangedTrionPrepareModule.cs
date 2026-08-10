using BDP.Core.AttackExecution.RangedProtocol.Model;

namespace BDP.Core.AttackExecution.RangedProtocol.Prepare
{
    /// <summary>
    /// 远程动作 Trion 准备模块。
    /// 它只把来源级 Trion 声明翻译成当前轮的正式成本与最低门槛。
    /// </summary>
    internal sealed class RangedTrionPrepareModule : IPrepareStageModule
    {
        public void Contribute(in PrepareStageContext context, PrepareContribution contribution)
        {
            if (contribution == null)
            {
                return;
            }

            RangedAttackEntry entry = context.Entry;
            if (entry?.SourceResult == null)
            {
                return;
            }

            var trion = entry.SourceResult.Trion;
            if (trion == null)
            {
                return;
            }

            if (trion.UseCost > 0f)
            {
                contribution.AddedResourceCost += trion.UseCost;
            }

            if (trion.MinimumRequired > 0f)
            {
                contribution.HasMinimumRequiredCandidate = true;
                contribution.MinimumRequiredCandidate = trion.MinimumRequired;
            }
        }
    }
}
