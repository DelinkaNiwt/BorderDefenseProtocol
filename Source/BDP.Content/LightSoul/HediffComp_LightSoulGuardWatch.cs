using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BDP.Content.LightSoul
{
    /// <summary>
    /// 光魂举盾“注视警戒”组件。
    /// 原版 VerbTracker（行为追踪器）持有唯一警戒 Verb，本组件只负责姿态生命周期和按钮投影。
    /// </summary>
    public sealed class HediffComp_LightSoulGuardWatch : HediffComp_VerbGiver
    {
        /// <summary>
        /// 读取本组件持有的正式注视警戒 Verb。
        /// </summary>
        internal Verb_LightSoulGuardWatch WatchVerb =>
            verbTracker?.PrimaryVerb as Verb_LightSoulGuardWatch;

        /// <summary>
        /// 进入举盾姿态时立即退出攻击、攻击瞄准和排队攻击命令。
        /// </summary>
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            LightSoulGuardWatchUtility.CancelAttackState(Pawn);
        }

        /// <summary>
        /// 离开举盾姿态时清理自动目标，并结束本 Verb 下达的注视警戒作业。
        /// </summary>
        public override void CompPostPostRemoved()
        {
            WatchVerb?.ClearWatchTarget();
            LightSoulGuardWatchUtility.EndManualWatchJob(Pawn, WatchVerb);
            base.CompPostPostRemoved();
        }

        /// <summary>
        /// 为玩家控制的人物提供原版 Command_VerbTarget（Verb 目标命令）。
        /// 此命令是非暴力行为，因此举盾造成的“禁止暴力”不会禁用它。
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            Verb_LightSoulGuardWatch verb = WatchVerb;
            Pawn pawn = Pawn;
            if (verb == null || pawn == null || pawn.Dead || pawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            Command_VerbTarget command = new Command_VerbTarget
            {
                defaultLabel = "BDP_Command_LightSoulGuardWatch".Translate(),
                defaultDesc = "BDP_Command_LightSoulGuardWatch_Description".Translate(),
                icon = verb.UIIcon,
                verb = verb,
                drawRadius = true,
                requiresAvailableVerb = true,
                // Hediff Verb 不在原版装备聚合查询里；禁止自动合并，避免多选时只给代表人物下令。
                groupable = false,
                tutorTag = "VerbTarget"
            };

            if (!pawn.Drafted && !DebugSettings.ShowDevGizmos)
            {
                command.Disable("IsNotDrafted".Translate(pawn.LabelShort, pawn));
            }

            yield return command;
        }
    }
}
