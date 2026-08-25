using System;
using System.Collections.Generic;
using BDP.Core.Chips;
using Verse;
using Verse.Sound;

namespace BDP.Core.Trigger.Runtime
{
    /// <summary>
    /// Trigger 芯片激活音效生命周期控制器。
    /// 它只处理声明过的 SoundDef，不关心具体芯片业务，也不依赖皇权灵能程序集。
    /// </summary>
    internal sealed class TriggerActivationAudioController
    {
        /// <summary>
        /// 各侧当前正在播放的激活前摇状态。
        /// </summary>
        private readonly Dictionary<TriggerSide, ActivationAudioState> states =
            new Dictionary<TriggerSide, ActivationAudioState>();

        /// <summary>
        /// 最近一次已经播放开始音效的芯片身份和游戏刻。
        /// 用于避免成对主副槽为同一枚逻辑芯片重复播放一次开始音效。
        /// </summary>
        private string lastStartedThingId;
        private int lastStartedTick = -1;

        /// <summary>
        /// 最近一次已经播放完成音效的芯片身份和游戏刻。
        /// 用于避免成对主副槽为同一枚逻辑芯片重复播放一次完成音效。
        /// </summary>
        private string lastCompletedThingId;
        private int lastCompletedTick = -1;

        /// <summary>
        /// 初始化三侧的音效状态槽。
        /// </summary>
        public TriggerActivationAudioController()
        {
            states.Add(TriggerSide.Main, new ActivationAudioState());
            states.Add(TriggerSide.Sub, new ActivationAudioState());
            states.Add(TriggerSide.Special, new ActivationAudioState());
        }

        /// <summary>
        /// 按当前切换上下文同步激活前摇音效。
        /// WaitingForConflicts（等待互斥冲突解除）阶段不会播放前摇音效。
        /// </summary>
        internal void Sync(
            Pawn pawn,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<Thing, ChipActivationAudioContract> getAudio,
            int currentTick)
        {
            if (pawn == null || !pawn.Spawned)
            {
                Clear();
                return;
            }

            SyncSide(
                TriggerSide.Main,
                pawn,
                getSwitchContext,
                getSlot,
                getAudio,
                currentTick);
            SyncSide(
                TriggerSide.Sub,
                pawn,
                getSwitchContext,
                getSlot,
                getAudio,
                currentTick);
            SyncSide(
                TriggerSide.Special,
                pawn,
                getSwitchContext,
                getSlot,
                getAudio,
                currentTick);
        }

        /// <summary>
        /// 通知某侧芯片已经正式激活，播放完成音效并结束前摇持续音效。
        /// </summary>
        internal void NotifyActivationCommitted(
            TriggerSide side,
            Thing chip,
            ChipActivationAudioContract audio,
            Pawn pawn,
            int currentTick)
        {
            ActivationAudioState state = GetState(side);
            string thingId = chip != null ? chip.ThingID : null;
            ChipActivationAudioContract effectiveAudio = state != null
                && state.TargetThingId == thingId
                && state.Audio != null
                ? state.Audio
                : audio;

            if (effectiveAudio != null
                && effectiveAudio.WarmupEndSound != null
                && pawn != null
                && pawn.Spawned
                && (lastCompletedThingId != thingId || lastCompletedTick != currentTick))
            {
                effectiveAudio.WarmupEndSound.PlayOneShot(pawn);
                lastCompletedThingId = thingId;
                lastCompletedTick = currentTick;
            }

            StopSide(side);
        }

        /// <summary>
        /// 结束指定侧的前摇持续音效，不播放完成音效。
        /// </summary>
        internal void StopSide(TriggerSide side)
        {
            ActivationAudioState state = GetState(side);
            if (state == null)
            {
                return;
            }

            if (state.LoopSustainer != null && !state.LoopSustainer.Ended)
            {
                state.LoopSustainer.End();
            }

            state.LoopSustainer = null;
            state.TargetThingId = null;
            state.Audio = null;
        }

        /// <summary>
        /// 统一结束所有侧的音效状态。
        /// </summary>
        internal void Clear()
        {
            StopSide(TriggerSide.Main);
            StopSide(TriggerSide.Sub);
            StopSide(TriggerSide.Special);
            lastStartedThingId = null;
            lastStartedTick = -1;
            lastCompletedThingId = null;
            lastCompletedTick = -1;
        }

        /// <summary>
        /// 同步单侧的激活前摇阶段。
        /// </summary>
        private void SyncSide(
            TriggerSide side,
            Pawn pawn,
            Func<TriggerSide, SwitchContext> getSwitchContext,
            Func<TriggerSide, int, TriggerSlotState> getSlot,
            Func<Thing, ChipActivationAudioContract> getAudio,
            int currentTick)
        {
            SwitchContext context = getSwitchContext != null
                ? getSwitchContext(side)
                : null;
            if (context == null || context.phase == SwitchPhase.WaitingForConflicts)
            {
                StopSide(side);
                return;
            }

            if (context.phase != SwitchPhase.Activating)
            {
                StopSide(side);
                return;
            }

            TriggerSlotState targetSlot = getSlot != null
                ? getSlot(side, context.targetSlotIndex)
                : null;
            Thing chip = targetSlot != null ? targetSlot.LoadedChip : null;
            ChipActivationAudioContract audio = getAudio != null
                ? getAudio(chip)
                : null;
            if (chip == null || audio == null)
            {
                StopSide(side);
                return;
            }

            ActivationAudioState state = GetState(side);
            string thingId = chip.ThingID;
            if (state.TargetThingId != thingId)
            {
                StopSide(side);
                state = GetState(side);
                state.TargetThingId = thingId;
                state.Audio = audio;

                if (audio.WarmupStartSound != null
                    && (lastStartedThingId != thingId || lastStartedTick != currentTick))
                {
                    audio.WarmupStartSound.PlayOneShot(pawn);
                    lastStartedThingId = thingId;
                    lastStartedTick = currentTick;
                }
            }
            else
            {
                state.Audio = audio;
            }

            if (audio.WarmupLoopSound == null)
            {
                return;
            }

            if (state.LoopSustainer == null || state.LoopSustainer.Ended)
            {
                state.LoopSustainer = audio.WarmupLoopSound.TrySpawnSustainer(
                    SoundInfo.InMap(pawn, MaintenanceType.PerTick));
            }

            state.LoopSustainer?.Maintain();
        }

        /// <summary>
        /// 读取指定侧的音效状态。
        /// </summary>
        private ActivationAudioState GetState(TriggerSide side)
        {
            ActivationAudioState state;
            return states.TryGetValue(side, out state) ? state : null;
        }

        /// <summary>
        /// 单侧激活音效状态，不参与存档；读档后由切换上下文重新同步。
        /// </summary>
        private sealed class ActivationAudioState
        {
            /// <summary>
            /// 当前前摇目标芯片身份。
            /// </summary>
            public string TargetThingId;

            /// <summary>
            /// 当前前摇目标的音效声明。
            /// </summary>
            public ChipActivationAudioContract Audio;

            /// <summary>
            /// 当前前摇持续音效实例。
            /// </summary>
            public Sustainer LoopSustainer;
        }
    }
}
