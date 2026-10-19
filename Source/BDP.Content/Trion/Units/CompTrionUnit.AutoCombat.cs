using BDP.Support.Diagnostics;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Content.Trion.Units
{
    /// <summary>只承载自主战斗意图与接管边界，具体决策留给独立原版行为子树。</summary>
    public sealed partial class CompTrionUnit
    {
        /// <summary>敌情读取原版缓存，每秒核对一次，不逐刻重开战斗作业。</summary>
        private const int AutoCombatCheckInterval = 60;
        /// <summary>独立战斗子树的抢占叶节点须声明同等或更高的原版优先级。</summary>
        internal const float AutoCombatOverridePriority = 1f;
        /// <summary>玩家可保存的意图；不是当前行动状态，也不覆盖征召或运行条件。</summary>
        private bool autoCombatEnabled;

        /// <summary>原版切换按钮和读取者只访问这一份保存真值。</summary>
        public bool AutoCombatEnabled => autoCombatEnabled;
        /// <summary>身体和玩家控制资格成立后才允许自主接管，敌情另行判定。</summary>
        internal bool CanAutoCombat => autoCombatEnabled && IsOperational
            && Pawn.Faction?.IsPlayer == true && Pawn.Spawned
            && !Pawn.Drafted && !Pawn.Downed && !Pawn.InMentalState;
        /// <summary>动态消费敌情，不保存第二份全地图敌人名单或战斗相位。</summary>
        internal bool ShouldAutoCombat => CanAutoCombat && HasAutoCombatEnemy();

        /// <summary>按原版攻击目标缓存读取仍在本地图上的有效敌对目标。</summary>
        private bool HasAutoCombatEnemy()
        {
            foreach (IAttackTarget target in Pawn.Map.attackTargetsCache.TargetsHostileToFaction(Pawn.Faction))
            {
                Thing thing = target.Thing;
                if (thing != null && thing.Spawned && thing.Map == Pawn.Map
                    && !target.ThreatDisabled(Pawn)) return true;
            }
            return false;
        }

        /// <summary>只改保存意图；征召期间或停机时切换不会夺取当前命令。</summary>
        public void SetAutoCombatEnabled(bool enabled)
        {
            if (autoCombatEnabled == enabled) return;
            autoCombatEnabled = enabled;
            RefreshAutoCombat();
            BdpDiagnostics.Trace("Trion兵自动战斗开关 | pawn=" + Pawn?.ThingID + " | enabled=" + enabled);
        }

        /// <summary>用原版保存的作业来源祖先识别本分支，不按攻击／移动作业类型一刀切。</summary>
        internal static bool IsAutoCombatJob(Job job)
        {
            for (ThinkNode node = job?.jobGiver; node != null; node = node.parent)
                if (node is ThinkNode_ConditionalTrionUnitAutoCombat) return true;
            return false;
        }

        /// <summary>先卸下旧自动作业再清动作，最后按调用场景决定是否寻找新作业。</summary>
        internal void EndAutoCombatJob(bool startNewJob)
        {
            Job job = Pawn?.CurJob;
            if (!IsAutoCombatJob(job)) return;
            Verb jobVerb = job.verbToUse;
            Verb stanceVerb = (Pawn.stances?.curStance as Stance_Busy)?.verb;
            Pawn.pather?.StopDead();
            // SetStance（改变姿态）会同步通知旧Wait驱动器重新攻击，必须先让原版卸下驱动器。
            Pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
            // 结束作业只软取消预热；用此前捕获的实际动作收口宿主剩余连射，不碰后续新动作。
            jobVerb?.Reset();
            if (!ReferenceEquals(stanceVerb, jobVerb)) stanceVerb?.Reset();
            Pawn.stances?.CancelBusyStanceHard();
            if (startNewJob && Pawn.Spawned && !Pawn.Dead) Pawn.jobs.CheckForJobOverride();
        }

        /// <summary>退出只结束本分支；接管只允许有明确战斗优先级的新作业抢占日常行为。</summary>
        private void RefreshAutoCombat()
        {
            if (Pawn?.jobs == null) return;
            bool ownsCurrentJob = IsAutoCombatJob(Pawn.CurJob);
            if (!ShouldAutoCombat)
            {
                if (ownsCurrentJob)
                {
                    EndAutoCombatJob(startNewJob: true);
                }
                return;
            }
            // 子树找不到战斗作业时，不允许主树回退的闲逛／工作被这次核对强制启动。
            if (!ownsCurrentJob) Pawn.jobs.CheckForJobOverride(AutoCombatOverridePriority);
        }

    }
}
