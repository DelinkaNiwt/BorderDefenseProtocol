using System;
using System.Collections.Generic;
using Verse;

namespace BDP.Core.Abilities
{
    /// <summary>
    /// 全局 Ability 来源级冷却账本。
    /// 它只保存 Pawn 与配置冷却键之间的结束 tick 和实际总时长，不知道任何具体业务 Ability。
    /// </summary>
    public sealed class GameComponent_AbilityReleaseCooldownLedger : GameComponent
    {
        /// <summary>
        /// 当前游戏内的来源级冷却记录。
        /// </summary>
        private List<AbilityReleaseCooldownRecord> records = new List<AbilityReleaseCooldownRecord>();

        /// <summary>
        /// 供原版 GameComponent 自动构造。
        /// </summary>
        public GameComponent_AbilityReleaseCooldownLedger(Game game)
        {
        }

        /// <summary>
        /// 读取指定 Pawn 和冷却键的剩余 tick，并清理已到期记录。
        /// </summary>
        internal int GetRemaining(string pawnThingId, string cooldownKey)
        {
            int remaining;
            int durationTicks;
            return TryGetCooldown(pawnThingId, cooldownKey, out remaining, out durationTicks)
                ? remaining
                : 0;
        }

        /// <summary>
        /// 读取指定来源级冷却的剩余 tick 和释放时总时长。
        /// </summary>
        internal bool TryGetCooldown(
            string pawnThingId,
            string cooldownKey,
            out int remaining,
            out int durationTicks)
        {
            remaining = 0;
            durationTicks = 0;
            if (string.IsNullOrWhiteSpace(pawnThingId) || string.IsNullOrWhiteSpace(cooldownKey))
            {
                return false;
            }

            int currentTick = CurrentTick;
            for (int index = records.Count - 1; index >= 0; index--)
            {
                AbilityReleaseCooldownRecord record = records[index];
                if (record == null
                    || string.IsNullOrWhiteSpace(record.PawnThingId)
                    || string.IsNullOrWhiteSpace(record.CooldownKey))
                {
                    records.RemoveAt(index);
                    continue;
                }

                if (!string.Equals(record.PawnThingId, pawnThingId, StringComparison.Ordinal)
                    || !string.Equals(record.CooldownKey, cooldownKey, StringComparison.Ordinal))
                {
                    continue;
                }

                remaining = record.CooldownEndTick - currentTick;
                if (remaining <= 0)
                {
                    records.RemoveAt(index);
                    return false;
                }

                durationTicks = record.CooldownDurationTicks > 0
                    ? record.CooldownDurationTicks
                    : remaining;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 从当前 tick 起启动或延长指定来源级冷却，并保存本次释放的总时长。
        /// </summary>
        internal void StartCooldown(
            string pawnThingId,
            string cooldownKey,
            int cooldownTicks,
            int cooldownDurationTicks)
        {
            if (string.IsNullOrWhiteSpace(pawnThingId) || string.IsNullOrWhiteSpace(cooldownKey))
            {
                return;
            }

            int requestedEndTick = CurrentTick + Math.Max(0, cooldownTicks);
            for (int index = 0; index < records.Count; index++)
            {
                AbilityReleaseCooldownRecord record = records[index];
                if (record == null)
                {
                    continue;
                }

                if (string.Equals(record.PawnThingId, pawnThingId, StringComparison.Ordinal)
                    && string.Equals(record.CooldownKey, cooldownKey, StringComparison.Ordinal))
                {
                    if (requestedEndTick > record.CooldownEndTick)
                    {
                        record.CooldownEndTick = requestedEndTick;
                        record.CooldownDurationTicks = Math.Max(
                            record.CooldownDurationTicks,
                            cooldownDurationTicks);
                    }
                    else if (record.CooldownDurationTicks <= 0)
                    {
                        record.CooldownDurationTicks = cooldownDurationTicks;
                    }
                    return;
                }
            }

            records.Add(new AbilityReleaseCooldownRecord
            {
                PawnThingId = pawnThingId,
                CooldownKey = cooldownKey,
                CooldownEndTick = requestedEndTick,
                CooldownDurationTicks = cooldownDurationTicks
            });
        }

        /// <summary>
        /// 保存来源级冷却记录，使读档后仍保留尚未结束的冷却。
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref records, "abilityReleaseCooldownRecords", LookMode.Deep);
            if (records == null)
            {
                records = new List<AbilityReleaseCooldownRecord>();
            }
        }

        /// <summary>
        /// 读取当前游戏 tick；菜单和早期初始化阶段安全返回 0。
        /// </summary>
        private static int CurrentTick
        {
            get { return Find.TickManager != null ? Find.TickManager.TicksGame : 0; }
        }
    }

    /// <summary>
    /// 单条 Ability 来源级冷却记录。
    /// </summary>
    public sealed class AbilityReleaseCooldownRecord : IExposable
    {
        /// <summary>
        /// Pawn 的稳定 ThingID。
        /// </summary>
        public string PawnThingId;

        /// <summary>
        /// AbilityDef 声明的共享冷却键。
        /// </summary>
        public string CooldownKey;

        /// <summary>
        /// 冷却结束的绝对游戏 tick。
        /// </summary>
        public int CooldownEndTick;

        /// <summary>
        /// 释放时 Ability 实际使用的总冷却 tick，用于恢复进度条比例。
        /// </summary>
        public int CooldownDurationTicks;

        /// <summary>
        /// 保存或读取记录字段。
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref PawnThingId, "pawnThingId");
            Scribe_Values.Look(ref CooldownKey, "cooldownKey");
            Scribe_Values.Look(ref CooldownEndTick, "cooldownEndTick", 0);
            Scribe_Values.Look(ref CooldownDurationTicks, "cooldownDurationTicks", 0);
        }
    }
}
