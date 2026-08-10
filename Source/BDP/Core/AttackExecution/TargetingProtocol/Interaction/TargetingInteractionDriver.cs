namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// 目标交互驱动器。
    /// 它把一轮输入后的 TargetingRecord 裁决成主循环下一步动作。
    /// </summary>
    internal sealed class TargetingInteractionDriver
    {
        /// <summary>
        /// 驱动当前这一轮目标交互记录。
        /// </summary>
        /// <param name="record">当前输入轮次已经完成模块裁决的目标记录。</param>
        /// <returns>交给原版 Targeter 主循环消费的统一驱动结果。</returns>
        public TargetingInteractionDriveResult Drive(TargetingRecord record)
        {
            if (record == null)
            {
                return new TargetingInteractionDriveResult
                {
                    CancelTargeting = true
                };
            }

            ApplyAdvanceDecision(record);

            TargetingInteractionDriveResult result = new TargetingInteractionDriveResult
            {
                TargetingRecord = record,
                FeedbackMessage = record.AdvanceDecision != null ? record.AdvanceDecision.Reason : null
            };

            switch (record.AdvanceDecision != null ? record.AdvanceDecision.Kind : TargetingAdvanceKind.Cancel)
            {
                case TargetingAdvanceKind.Complete:
                    result.EnterConfirm = true;
                    break;
                case TargetingAdvanceKind.Cancel:
                    result.CancelTargeting = true;
                    break;
                case TargetingAdvanceKind.Back:
                case TargetingAdvanceKind.Reject:
                case TargetingAdvanceKind.Continue:
                default:
                    result.KeepTargeting = true;
                    break;
            }

            return result;
        }

        /// <summary>
        /// 把当前推进裁决正式回写到交互会话与输入状态。
        /// </summary>
        /// <param name="record">当前这一轮已经形成推进裁决的目标记录。</param>
        private static void ApplyAdvanceDecision(TargetingRecord record)
        {
            if (record == null || record.AdvanceDecision == null)
            {
                return;
            }

            switch (record.AdvanceDecision.Kind)
            {
                case TargetingAdvanceKind.Complete:
                    record.InputState.IsActive = false;
                    record.InputState.IsComplete = true;
                    if (record.InteractionSession != null)
                    {
                        record.InteractionSession.StepIndex = record.InputState.StepIndex;
                        record.InteractionSession.Complete();
                    }

                    break;
                case TargetingAdvanceKind.Cancel:
                    record.InputState.IsActive = false;
                    record.InputState.IsComplete = false;
                    if (record.InteractionSession != null)
                    {
                        record.InteractionSession.Cancel();
                    }

                    break;
                case TargetingAdvanceKind.Back:
                    record.InputState.IsActive = true;
                    record.InputState.IsComplete = false;
                    record.InputState.StepIndex = record.InputState.StepIndex > 0
                        ? record.InputState.StepIndex - 1
                        : 0;
                    if (record.InteractionSession != null)
                    {
                        record.InteractionSession.StepIndex = record.InputState.StepIndex;
                        record.InteractionSession.Activate();
                    }

                    break;
                case TargetingAdvanceKind.Reject:
                    record.InputState.IsActive = true;
                    record.InputState.IsComplete = false;
                    if (record.InteractionSession != null)
                    {
                        record.InteractionSession.StepIndex = record.InputState.StepIndex;
                        record.InteractionSession.Activate();
                    }

                    break;
                case TargetingAdvanceKind.Continue:
                default:
                    record.InputState.IsActive = true;
                    record.InputState.IsComplete = false;
                    if (record.InteractionSession != null)
                    {
                        record.InteractionSession.StepIndex = record.InputState.StepIndex;
                        record.InteractionSession.Activate();
                    }

                    break;
            }
        }
    }
}
