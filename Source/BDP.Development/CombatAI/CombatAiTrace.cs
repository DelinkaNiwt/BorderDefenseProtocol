using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BDP.Core.Trigger;
using BDP.Core.Verbs;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace BDP.Development.CombatAI
{
    /// <summary>本轮战斗诊断的只读快照和安全日志入口；移除开发目录即可整体撤销。</summary>
    internal static class CombatAiTrace
    {
        /// <summary>稳定机器标记，所有新增诊断均可按此搜索。</summary>
        internal const string Marker = "[BDP_AI_TRACE]";

        /// <summary>只缓存反射读取口，不缓存或修改正式业务状态。</summary>
        private static readonly Dictionary<string, MemberInfo> Readers = new Dictionary<string, MemberInfo>();

        /// <summary>主菜单尚无游戏对象；必须先保护 Current.Game，不能从 Find.TickManager 开始判空。</summary>
        internal static int CurrentTick => Current.Game?.tickManager?.TicksGame ?? -1;

        /// <summary>仅接纳非玩家 BDP 装备持有者；已经登记的人物由地图继续追踪卸装和崩解。</summary>
        internal static bool IsCandidate(Pawn pawn)
        {
            return pawn != null && pawn.Faction?.IsPlayer != true
                && pawn.equipment?.Primary?.GetComp<CompTriggerBody>() != null;
        }

        /// <summary>诊断失败显式报错但不传播进游戏方法，避免观测改变原版执行结果。</summary>
        internal static void Observe(Pawn pawn, Action<CombatAiPawnTrace> action)
        {
            // 用户授权的一轮诊断随 Development 自动启用，不能因忘开开发模式而得到空日志。
            if (pawn?.Map == null) return;
            try
            {
                MapComponent_CombatAiDiagnostics component = pawn.Map.GetComponent<MapComponent_CombatAiDiagnostics>();
                CombatAiPawnTrace trace = component?.GetTrace(pawn);
                if (trace != null) action(trace);
            }
            catch (Exception exception)
            {
                Error("observe:" + pawn.ThingID, exception);
            }
        }

        /// <summary>中文说明从开发语言包读取；机器字段保持稳定便于比对。</summary>
        internal static void Write(string stage, Pawn pawn, string details)
        {
            Log.Message(Marker + " " + ("BDP_Dev_CombatAI_" + stage).Translate()
                + " | event=" + stage + " | tick=" + CurrentTick
                + " | pawn=" + Thing(pawn) + " | " + details);
        }

        /// <summary>相同诊断错误只报一次，保留完整异常供定位，不用静默吞掉异常。</summary>
        internal static void Error(string stage, Exception exception)
        {
            Log.ErrorOnce(Marker + " " + "BDP_Dev_CombatAI_Error".Translate()
                + " | stage=" + stage + " | " + exception, (Marker + stage).GetHashCode());
        }

        /// <summary>只反射已核实的字段或纯读取属性；找不到成员时明确失败。</summary>
        internal static object Read(object owner, string name)
        {
            if (owner == null) return null;
            Type type = owner.GetType();
            string key = type.FullName + ":" + name;
            MemberInfo member;
            if (!Readers.TryGetValue(key, out member))
            {
                member = (MemberInfo)AccessTools.Field(type, name) ?? AccessTools.Property(type, name);
                if (member == null) throw new MissingMemberException(type.FullName, name);
                Readers.Add(key, member);
            }
            FieldInfo field = member as FieldInfo;
            return field != null ? field.GetValue(owner) : ((PropertyInfo)member).GetValue(owner, null);
        }

        /// <summary>不调用 TryGetAttackVerb、PrimaryVerb 或 Available，避免诊断自己创建起手会话。</summary>
        internal static string Snapshot(Pawn pawn)
        {
            Job job = pawn.CurJob;
            Stance_Busy busy = pawn.stances?.curStance as Stance_Busy;
            var result = new StringBuilder();
            result.Append("map=").Append(pawn.Map?.uniqueID).Append(", pos=").Append(pawn.Position)
                .Append(", faction=").Append(pawn.Faction?.def.defName)
                .Append(", downed=").Append(pawn.Downed).Append(", violentDisabled=").Append(pawn.WorkTagIsDisabled(WorkTags.Violent))
                .Append(", duty=").Append(pawn.mindState?.duty?.def.defName)
                .Append(", lord=").Append(pawn.GetLord()?.LordJob?.GetType().Name)
                .Append(", job={").Append(Job(job)).Append("}, driver=").Append(pawn.jobs?.curDriver?.GetType().Name)
                .Append(", toilIndex=").Append(pawn.jobs?.curDriver?.CurToilIndex)
                .Append(", jobAge=").Append(job == null ? -1 : Find.TickManager.TicksGame - job.startTick)
                .Append(", stance=").Append(pawn.stances?.curStance?.GetType().Name)
                .Append(", fullBodyBusy=").Append(pawn.stances?.FullBodyBusy)
                .Append(", stanceTicks=").Append(busy?.ticksLeft)
                .Append(", stanceVerb=").Append(Verb(busy?.verb))
                .Append(", moving=").Append(pawn.pather?.Moving)
                .Append(", destination=").Append(pawn.pather?.Destination)
                .Append(", enemy=").Append(Target(pawn.mindState?.enemyTarget))
                .Append(", meleeThreat=").Append(Target(pawn.mindState?.meleeThreat));
            if (job?.targetA.HasThing == true)
                result.Append(", jobTarget={").Append(Target(job.targetA.Thing))
                    .Append(", touch=").Append(pawn.CanReachImmediate(job.targetA, PathEndMode.Touch)).Append('}');
            result.Append(", adjacent=[").Append(Adjacent(pawn)).Append("]");
            CompTriggerBody trigger = pawn.equipment?.Primary?.GetComp<CompTriggerBody>();
            result.Append(", trigger=").Append(Thing(trigger?.parent));
            object projection = Read(trigger, "PublishedCombatProjection");
            object snapshot = Read(projection, "Snapshot");
            result.Append(", projection=").Append(Read(projection, "ProjectionVersion"))
                .Append(", primaryRanged=").Append(Read(Read(snapshot, "PrimaryRanged"), "Id"))
                .Append(", primaryMelee=").Append(Read(Read(snapshot, "PrimaryMelee"), "Id"));
            IDictionary bindings = Read(Read(trigger, "VerbHostManager"), "bindings") as IDictionary;
            if (bindings != null)
                foreach (DictionaryEntry binding in bindings)
                {
                    if (!Equals(Read(binding.Value, "IsAvailable"), true)) continue;
                    result.Append(", host[").Append(binding.Key).Append("]={");
                    Verb ranged = Read(binding.Value, "RangedVerb") as Verb;
                    Verb melee = Read(binding.Value, "MeleeVerb") as Verb;
                    result.Append(Host(ranged)).Append(';').Append(Host(melee)).Append('}');
                }
            if (pawn.jobs?.curDriver?.GetType().FullName == "BDP.Core.AttackExecution.JobDriver_BdpMeleeAttackExecution")
                result.Append(", castCount=").Append(Read(pawn.jobs.curDriver, "castCount"));
            return result.ToString();
        }

        /// <summary>任务身份包含来源及结束条件，不只记录显示名称。</summary>
        internal static string Job(Job job)
        {
            if (job == null) return "null";
            return job.def.defName + "#" + RuntimeHelpers.GetHashCode(job)
                + ", targetA=" + Thing(job.targetA.Thing) + ":" + job.targetA.Cell
                + ", giver=" + job.jobGiver?.GetType().Name + ", tree=" + job.jobGiverThinkTree?.defName
                + ", source=" + job.source?.GetType().Name + ", verb=" + Verb(job.verbToUse)
                + ", forced=" + job.playerForced + ", expiry=" + job.expiryInterval
                + ", checkOverride=" + job.checkOverrideOnExpire + ", requiresEnemies=" + job.expireRequiresEnemiesNearby
                + ", expireOnEnemies=" + job.expireOnEnemiesNearby + ", killDowned=" + job.killIncappedTarget
                + ", reacting=" + job.reactingToMeleeThreat + ", maxMelee=" + job.maxNumMeleeAttacks;
        }

        /// <summary>攻击行为身份不触发可用性查询。</summary>
        internal static string Verb(Verb verb)
        {
            return verb == null ? "null" : verb.GetType().Name + "#" + verb.loadID
                + ":melee=" + verb.verbProps?.IsMeleeAttack;
        }

        /// <summary>实体标识同时保留玩家可识别的名字和稳定编号。</summary>
        internal static string Thing(Thing thing) => thing == null ? "null" : thing.LabelShort + "#" + thing.ThingID;

        /// <summary>正式宿主内部状态仅做纯读，保留空闲但有旧会话这一关键区别。</summary>
        internal static string Host(Verb verb)
        {
            if (verb == null) return "null";
            string common = "verb=" + Verb(verb) + ", state=" + verb.state + ", warming=" + verb.WarmingUp
                + ", currentTarget=" + verb.CurrentTarget + ", token=" + Read(verb, "HostSessionToken");
            if (verb is BdpVerb_Shoot)
            {
                object cursor = Read(verb, "emissionCursor");
                return common + ", sessionTarget=" + Read(verb, "sessionTarget")
                    + ", hasModuleSession=" + (Read(verb, "HostModuleSession") != null)
                    + ", hasContext=" + (Read(verb, "HostAttackContextSnapshot") != null)
                    + ", hasEmissionPlan=" + (Read(cursor, "PendingVerbEmissionPlan") != null)
                    + ", emissionWindow=" + Read(cursor, "PendingWindowIndex")
                    + ", consumedEmits=" + Read(cursor, "PendingEmissionConsumedCount");
            }
            if (verb is BdpVerb_MeleeAttackDamage)
                return common + ", nextStep=" + Read(verb, "NextRuntimeStepIndex")
                    + ", dispatch=" + Read(verb, "PlanDispatchIntent");
            return common;
        }

        /// <summary>区分不还手与真正威胁失效，不写入目标状态。</summary>
        private static string Target(Thing thing)
        {
            Pawn target = thing as Pawn;
            return Thing(thing) + "@" + thing?.Position + (target == null ? "" : ":downed=" + target.Downed
                + ":awake=" + target.Awake() + ":threatDisabled=" + target.ThreatDisabled(null)
                + ":drafted=" + target.Drafted + ":job=" + target.CurJob?.def.defName);
        }

        /// <summary>邻格敌人只扫描九格，不重新索敌；没有原版近战调用时仍可知道目标就在旁边。</summary>
        private static string Adjacent(Pawn pawn)
        {
            var result = new StringBuilder();
            for (int i = 0; i < GenAdj.AdjacentCellsAndInside.Length; i++)
            {
                IntVec3 cell = pawn.Position + GenAdj.AdjacentCellsAndInside[i];
                if (!cell.InBounds(pawn.Map)) continue;
                foreach (Thing thing in cell.GetThingList(pawn.Map))
                {
                    Pawn target = thing as Pawn;
                    if (target == null || target == pawn || !pawn.HostileTo(target)) continue;
                    result.Append(Target(target)).Append(":activeThreat=")
                        .Append(GenHostility.IsActiveThreatTo(target, pawn.Faction, ignoreHives: false))
                        .Append(":touch=").Append(pawn.CanReachImmediate(target, PathEndMode.Touch)).Append(';');
                }
            }
            return result.ToString();
        }
    }

    /// <summary>单个人物的诊断计数与去重，仅存于地图组件内，不进入存档。</summary>
    internal sealed class CombatAiPawnTrace
    {
        /// <summary>被观测的人物。</summary>
        internal readonly Pawn Pawn;
        /// <summary>入口累计次数，即使节流也能确认对应分支确实运行。</summary>
        private readonly Dictionary<string, int> counters = new Dictionary<string, int>();
        /// <summary>各阶段上次的稳定信息。</summary>
        private readonly Dictionary<string, string> signatures = new Dictionary<string, string>();
        /// <summary>各阶段最近输出时间。</summary>
        private readonly Dictionary<string, int> emittedTicks = new Dictionary<string, int>();

        /// <summary>登记人物时只绑定引用，不读写正式攻击状态。</summary>
        internal CombatAiPawnTrace(Pawn pawn)
        {
            Pawn = pawn;
            foreach (string stage in new[] { "decision_enter", "ranged_query", "wait_auto", "melee_request", "melee_tick", "cast_request", "melee_shot", "ranged_shot", "ranged_reset" })
                counters.Add(stage, 0);
        }

        /// <summary>每次调用计数，内容变化立即记录；同内容至少三秒再报告一次。</summary>
        internal void Record(string stage, string details, bool force = false, string scope = "")
        {
            int count;
            counters.TryGetValue(stage, out count);
            counters[stage] = count + 1;
            string key = stage + scope;
            string previous;
            int tick = Find.TickManager.TicksGame;
            int lastTick;
            if (!force && signatures.TryGetValue(key, out previous) && previous == details
                && emittedTicks.TryGetValue(key, out lastTick) && tick - lastTick < 180) return;
            signatures[key] = details;
            emittedTicks[key] = tick;
            // 高频正常出手只留短上下文；异常、任务变化和周期快照保留完整宿主，避免大袭击淹没日志。
            bool compact = stage == "cast_request" || stage == "melee_shot" || stage == "ranged_shot"
                || stage == "wait_auto" || stage == "attack_query"
                || (stage == "cast_result" && details.Contains("started=True"))
                || (stage == "session_validation" && details.Contains("valid=True"))
                || (stage == "ranged_query" && details.Contains("bridgeHandled=True"))
                || (stage == "bdp_query_transform" && !details.Contains("afterBdp=null"));
            string snapshot;
            try
            {
                snapshot = compact
                    ? "pos=" + Pawn.Position + ", job=" + Pawn.CurJob?.def.defName
                        + ", driver=" + Pawn.jobs?.curDriver?.GetType().Name
                        + ", stance=" + Pawn.stances?.curStance?.GetType().Name
                        + ", fullBodyBusy=" + Pawn.stances?.FullBodyBusy
                    : CombatAiTrace.Snapshot(Pawn);
            }
            catch (Exception exception)
            {
                CombatAiTrace.Error("snapshot:" + Pawn.ThingID, exception);
                snapshot = "snapshot_error=" + exception.GetType().Name;
            }
            CombatAiTrace.Write(stage, Pawn, details + " | snapshot={" + snapshot + "}");
        }

        /// <summary>没有动作或没有入口调用也每五秒报告计数和状态，避免空反馈。</summary>
        internal void Pulse()
        {
            var result = new StringBuilder();
            foreach (KeyValuePair<string, int> counter in counters)
                result.Append(counter.Key).Append('=').Append(counter.Value).Append(';');
            Record("pawn_pulse", "counts={" + result + "}", true);
        }
    }

    /// <summary>自动观察地图上所有非玩家 BDP 人物，无需选中、点按钮或序列化开关。</summary>
    public sealed class MapComponent_CombatAiDiagnostics : MapComponent
    {
        /// <summary>样本随地图生命周期释放，读档或新档不复用旧计数。</summary>
        private readonly Dictionary<Pawn, CombatAiPawnTrace> traces = new Dictionary<Pawn, CombatAiPawnTrace>();
        /// <summary>地图首次输出标记，不受原版开发模式开关控制。</summary>
        private bool announced;

        /// <summary>原版自动创建地图组件。</summary>
        public MapComponent_CombatAiDiagnostics(Map map) : base(map) { }

        /// <summary>地图生成／读取结束即报告，不依赖第一名敌人到场。</summary>
        public override void FinalizeInit()
        {
            CombatAiTrace.Write("map_ready", null, "map=" + map.uniqueID + ", devMode=" + Prefs.DevMode
                + ", patches=" + CombatAiDiagnosticsHooks.InstalledCount + "/" + CombatAiDiagnosticsHooks.ExpectedCount);
        }

        /// <summary>已登记人物继续观察卸装后的行为；普通原版人物不进入样本。</summary>
        internal CombatAiPawnTrace GetTrace(Pawn pawn)
        {
            CombatAiPawnTrace trace;
            if (traces.TryGetValue(pawn, out trace)) return trace;
            if (!CombatAiTrace.IsCandidate(pawn)) return null;
            trace = new CombatAiPawnTrace(pawn);
            traces.Add(pawn, trace);
            trace.Record("pawn_registered", "equipment=" + CombatAiTrace.Thing(pawn.equipment?.Primary), true);
            return trace;
        }

        /// <summary>每秒采样并报告主要状态变化；五秒人物心跳、十秒地图心跳明确诊断存活。</summary>
        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % 60 != 0) return;
            try
            {
                int tick = Find.TickManager.TicksGame;
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    CombatAiTrace.Observe(pawn, trace =>
                    {
                        Job job = pawn.CurJob;
                        string signature = CombatAiTrace.Job(job) + ", stance=" + pawn.stances?.curStance?.GetType().Name
                            + ", moving=" + pawn.pather?.Moving + ", enemy=" + CombatAiTrace.Thing(pawn.mindState?.enemyTarget);
                        trace.Record("state_sample", signature);
                        if (tick % 300 == 0) trace.Pulse();
                    });
                }
                if (!announced || tick % 600 == 0)
                {
                    announced = true;
                    CombatAiTrace.Write("map_pulse", null, "map=" + map.uniqueID + ", tracked=" + traces.Count
                        + ", patches=" + CombatAiDiagnosticsHooks.InstalledCount + "/" + CombatAiDiagnosticsHooks.ExpectedCount);
                }
            }
            catch (Exception exception) { CombatAiTrace.Error("map_tick", exception); }
        }
    }
}
