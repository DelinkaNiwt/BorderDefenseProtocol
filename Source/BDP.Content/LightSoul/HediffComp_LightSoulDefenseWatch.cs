using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BDP.Content.LightSoul
{
    /// <summary>
    /// 光魂防护“注视警戒”组件。
    /// 原版 VerbTracker（行为追踪器）持有唯一警戒 Verb，本组件只负责防护状态生命周期和按钮投影。
    /// </summary>
    public sealed class HediffComp_LightSoulDefenseWatch : HediffComp_VerbGiver
    {
        /// <summary>
        /// 读取本组件持有的正式注视警戒 Verb。
        /// </summary>
        internal Verb_LightSoulDefenseWatch WatchVerb =>
            verbTracker?.PrimaryVerb as Verb_LightSoulDefenseWatch;

        /// <summary>
        /// 进入防护形态时退出远程攻击、远程瞄准和排队远程命令，保留近战。
        /// </summary>
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            LightSoulDefenseWatchUtility.CancelRangedAttackState(Pawn);
        }

        /// <summary>
        /// 离开防护形态时清理自动目标，并结束本 Verb 下达的注视警戒作业。
        /// </summary>
        public override void CompPostPostRemoved()
        {
            WatchVerb?.ClearWatchTarget();
            LightSoulDefenseWatchUtility.EndManualWatchJob(Pawn, WatchVerb);
            base.CompPostPostRemoved();
        }

        /// <summary>
        /// 为玩家控制的人物提供原版 Command_VerbTarget（Verb 目标命令）。
        /// 此命令不是远程武器，因此不受防护形态的远程限制影响。
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            Verb_LightSoulDefenseWatch verb = WatchVerb;
            Pawn pawn = Pawn;
            if (verb == null || pawn == null || pawn.Dead || !pawn.IsPlayerControlled)
            {
                yield break;
            }

            Command_VerbTarget command = new Command_VerbTarget
            {
                defaultLabel = "BDP_Command_LightSoulDefenseWatch".Translate(),
                defaultDesc = "BDP_Command_LightSoulDefenseWatch_Description".Translate(),
                icon = verb.UIIcon,
                verb = verb,
                drawRadius = true,
                requiresAvailableVerb = true,
                // Hediff Verb 不在原版装备聚合查询里；禁止自动合并，避免多选时只给代表人物下令。
                groupable = false,
                tutorTag = "VerbTarget"
            };

            AcceptanceReport allowsDrafting = pawn.GetLord()?.AllowsDrafting(pawn) ?? (AcceptanceReport)true;
            if (!allowsDrafting)
            {
                command.Disable(allowsDrafting.Reason);
            }
            else if (!pawn.Drafted && !DebugSettings.ShowDevGizmos)
            {
                command.Disable("IsNotDrafted".Translate(pawn.LabelShort, pawn));
            }

            yield return command;
        }
    }
}
