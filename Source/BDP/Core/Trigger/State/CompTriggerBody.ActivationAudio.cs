using BDP.Core.Trigger.Runtime;
using Verse;

namespace BDP.Core.Trigger
{
    /// <summary>
    /// CompTriggerBody 的激活音效生命周期桥。
    /// 它只把 owner 真值交给 Core 通用音效控制器，不承载具体芯片业务。
    /// </summary>
    public sealed partial class CompTriggerBody
    {
        /// <summary>
        /// 在主运行时 owner 的每次 tick 中同步激活前摇音效。
        /// </summary>
        internal void SyncActivationAudioForRuntimeTick()
        {
            EnsureInternalState();
            activationAudioController?.Sync(
                OwnerPawn,
                GetSwitchContext,
                GetSlot,
                GetChipActivationAudio,
                GetCurrentTick());
        }

        /// <summary>
        /// 在芯片正式激活提交后播放完成音效。
        /// </summary>
        private void NotifyActivationAudioCommitted(TriggerSide side, Thing chip)
        {
            activationAudioController?.NotifyActivationCommitted(
                side,
                chip,
                GetChipActivationAudio(chip),
                OwnerPawn,
                GetCurrentTick());
        }

        /// <summary>
        /// 在某侧正式停用时结束该侧前摇持续音效。
        /// </summary>
        private void NotifyActivationAudioDeactivated(TriggerSide side)
        {
            activationAudioController?.StopSide(side);
        }

        /// <summary>
        /// 在脱离装备或外围投影清空时结束全部激活音效。
        /// </summary>
        internal void ClearActivationAudio()
        {
            activationAudioController?.Clear();
        }
    }
}
