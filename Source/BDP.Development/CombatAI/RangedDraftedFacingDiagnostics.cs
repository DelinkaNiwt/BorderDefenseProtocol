#if BDP_RIMWORLD_1_7
using System;
using System.Collections.Generic;
using System.Reflection;
using BDP.Core.Trigger;
using BDP.Core.Verbs;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Development.CombatAI
{
    /// <summary>临时记录征召远程强制攻击的起手与朝南回退；整体位于 Development，可直接拔除。</summary>
    internal static class RangedDraftedFacingDiagnostics
    {
        /// <summary>稳定日志标记，方便从 Player.log 搜索本次诊断。</summary>
        private const string Marker = "[BDP_RANGED_TURN_TRACE]";

        /// <summary>BDP 连续远程强制攻击使用的 JobDef 名称。</summary>
        private const string RangedExecutionJobDefName = "BDP_RangedAttackExecution";

        /// <summary>独立 Harmony 标识；移除此类即可撤销全部观察补丁。</summary>
        private static readonly Harmony Observer = new Harmony("niwt.bdp.development.ranged_turn_trace");

        /// <summary>人物最近一次连续起手失败，用于压缩重复日志并关联朝向变化。</summary>
        private static readonly Dictionary<string, FailedAttemptSpan> Failures = new Dictionary<string, FailedAttemptSpan>();

        /// <summary>已安装的观察补丁数，仅用于启动时确认覆盖范围。</summary>
        private static int installedCount;

        /// <summary>防止启动入口重复调用时重复挂接观察补丁。</summary>
        private static bool initialized;

        /// <summary>由 DevelopmentBootstrap 显式启动，确保诊断类型随开发程序集启动入口一同运行。</summary>
        internal static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            Install(typeof(Pawn), "TryStartAttack", "AttackStartBefore", "AttackStartAfter", new[] { typeof(LocalTargetInfo) });
            Install(typeof(BdpVerb_FormalHostShoot), "TryPrepareCastSession", "SessionPrepareBefore", "SessionPrepareAfter", new[] { typeof(LocalTargetInfo) });
            Install(typeof(Pawn_RotationTracker), "UpdateRotation", "RotationBefore", "RotationAfter", Type.EmptyTypes);
            Log("hooks_ready", "installed=" + installedCount + "/3");
        }

        /// <summary>按精确签名安装只读 Prefix/Postfix；单点失败不阻止 Development 加载。</summary>
        private static void Install(Type type, string methodName, string prefixName, string postfixName, Type[] parameters)
        {
            try
            {
                MethodInfo target = AccessTools.DeclaredMethod(type, methodName, parameters);
                if (target == null) throw new MissingMethodException(type.FullName, methodName);
                HarmonyMethod prefix = prefixName == null ? null : new HarmonyMethod(typeof(RangedDraftedFacingDiagnostics), prefixName);
                HarmonyMethod postfix = postfixName == null ? null : new HarmonyMethod(typeof(RangedDraftedFacingDiagnostics), postfixName);
                Observer.Patch(target, prefix, postfix);
                installedCount++;
            }
            catch (Exception exception)
            {
                Log("hook_failed", "method=" + type.FullName + "." + methodName + ", error=" + exception.GetType().Name);
            }
        }

        /// <summary>只跟踪玩家阵营中持有 BDP 触发体、正在执行原版或 BDP 远程强制攻击的人物。</summary>
        private static bool IsObservedPawn(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && pawn.Drafted
                && pawn.Faction != null
                && pawn.Faction.IsPlayer
                && pawn.CurJob != null
                && (pawn.CurJob.def == JobDefOf.AttackStatic
                    || pawn.CurJob.def.defName == RangedExecutionJobDefName)
                && pawn.equipment != null
                && pawn.equipment.Primary != null
                && pawn.equipment.Primary.GetComp<CompTriggerBody>() != null;
        }

        /// <summary>捕获原版每次强制攻击的目标与人物身份，避免任务结束后的 Postfix 丢失筛选条件。</summary>
        private static void AttackStartBefore(Pawn __instance, LocalTargetInfo __0, out AttackAttemptSnapshot __state)
        {
            __state = null;
            try
            {
                if (IsObservedPawn(__instance)) __state = new AttackAttemptSnapshot(__instance, __0, __instance.CurJob);
            }
            catch (Exception exception) { Log("observer_error", "stage=attack_start_before, error=" + exception.GetType().Name); }
        }

        /// <summary>首次失败时记录上下文；成功重试后输出失败跨度并清理暂存状态。</summary>
        private static void AttackStartAfter(Pawn __instance, LocalTargetInfo __0, bool __result, AttackAttemptSnapshot __state)
        {
            if (__state == null) return;
            try
            {
                int tick = CurrentTick;
                FailedAttemptSpan span;
                if (!__result)
                {
                    if (!Failures.TryGetValue(__state.Pawn.ThingID, out span)
                        || !ReferenceEquals(span.Job, __state.Job)
                        || span.LastTarget != __0
                        || (tick >= 0 && span.LastTick >= 0 && tick - span.LastTick > 120))
                    {
                        span = new FailedAttemptSpan(tick, __state.Target, __state.Job);
                        Failures[__state.Pawn.ThingID] = span;
                        Log("attack_start_false", "target=" + Target(__state.Target) + ", snapshot={" + Snapshot(__state.Pawn) + "}");
                    }
                    span.Count++;
                    span.LastTick = tick;
                    span.LastTarget = __0;
                    return;
                }

                if (Failures.TryGetValue(__state.Pawn.ThingID, out span)
                    && ReferenceEquals(span.Job, __state.Job))
                {
                    Log("attack_start_recovered", "firstFailTick=" + span.FirstTick
                        + ", recoveredTick=" + tick
                        + ", failedAttempts=" + span.Count
                        + ", target=" + Target(__0)
                        + ", snapshot={" + Snapshot(__state.Pawn) + "}");
                    Failures.Remove(__state.Pawn.ThingID);
                }
            }
            catch (Exception exception) { Log("observer_error", "stage=attack_start_after, error=" + exception.GetType().Name); }
        }

        /// <summary>保存会话准入调用前的状态；只在原方法拒绝时输出，避免正常射击刷屏。</summary>
        private static void SessionPrepareBefore(BdpVerb_FormalHostShoot __instance, LocalTargetInfo __0, out PrepareSnapshot __state)
        {
            __state = null;
            try
            {
                Pawn pawn = __instance != null ? __instance.CasterPawn : null;
                if (IsObservedPawn(pawn)) __state = new PrepareSnapshot(pawn, __instance, __0);
            }
            catch (Exception exception) { Log("observer_error", "stage=session_prepare_before, error=" + exception.GetType().Name); }
        }

        /// <summary>拒绝时报告可区分的门槛与完整会话快照，不重试或改写正式判断。</summary>
        private static void SessionPrepareAfter(BdpVerb_FormalHostShoot __instance, LocalTargetInfo __0, bool __result, PrepareSnapshot __state)
        {
            if (__state == null || __result) return;
            try
            {
                bool runtimeRequired = ReadBool(__instance, "RequiresFormalHostRuntimeTick");
                bool targetMatches = ReadTargetMatch(__instance, __0);
                string likelyGate;
                if (__instance.WarmingUp || __instance.Bursting || __state.Pawn.stances?.FullBodyBusy == true)
                    likelyGate = "busy_guard";
                else if (runtimeRequired)
                    likelyGate = targetMatches ? "active_runtime_unexpected_reject" : "active_runtime_target_mismatch";
                else
                    likelyGate = "binding_projection_result_or_module_session";

                string signature = likelyGate + "|runtimeRequired=" + runtimeRequired + "|targetMatches=" + targetMatches + "|target=" + Target(__0);
                string details = "target=" + Target(__0)
                    + ", likelyGate=" + likelyGate
                    + ", runtimeRequired=" + runtimeRequired
                    + ", targetMatchesSession=" + targetMatches
                    + ", entry={" + __state.EntrySnapshot + "}"
                    + ", current={" + Snapshot(__state.Pawn) + "}"
                    + ", host={" + SafeHost(__instance) + "}";
                RecordPrepareFailure(__state.Pawn, __0, __state.Job, signature, details);
            }
            catch (Exception exception) { Log("observer_error", "stage=session_prepare_after, error=" + exception.GetType().Name); }
        }

        /// <summary>缓存朝向更新前的姿态与作业，避免只看到结果而丢失轮间边界。</summary>
        private static void RotationBefore(Pawn_RotationTracker __instance, out RotationSnapshot __state)
        {
            __state = null;
            try
            {
                Pawn pawn = ReadPawn(__instance);
                if (IsObservedPawn(pawn)) __state = new RotationSnapshot(pawn, pawn.Rotation);
            }
            catch (Exception exception) { Log("observer_error", "stage=rotation_before, error=" + exception.GetType().Name); }
        }

        /// <summary>只在非南朝向变成南朝向时输出前后姿态，直接关联轮间朝向回退。</summary>
        private static void RotationAfter(Pawn_RotationTracker __instance, RotationSnapshot __state)
        {
            if (__state == null || __state.Before == Rot4.South || __state.Pawn.Rotation != Rot4.South || __state.Pawn.pather?.Moving == true)
                return;

            try
            {
                FailedAttemptSpan span;
                string failure = Failures.TryGetValue(__state.Pawn.ThingID, out span)
                    && ReferenceEquals(span.Job, __state.Pawn.CurJob)
                    ? ", latestFailure={firstTick:" + span.FirstTick + ", lastTick:" + span.LastTick
                        + ", count:" + span.Count + ", prepareCount:" + span.PrepareFailureCount
                        + ", target:" + Target(span.LastTarget) + "}"
                    : ", latestFailure=none";
                Log("rotation_to_south", "before=" + __state.Before
                    + ", after=" + __state.Pawn.Rotation
                    + ", beforeStance=" + __state.BeforeStance
                    + ", beforeBusy=" + __state.BeforeBusy
                    + ", beforeJob=" + __state.BeforeJob
                    + ", beforeDriver=" + __state.BeforeDriver
                    + failure
                    + ", snapshot={" + Snapshot(__state.Pawn) + "}");
            }
            catch (Exception exception) { Log("observer_error", "stage=rotation_after, error=" + exception.GetType().Name); }
        }

        /// <summary>对同一连续失败只首记一次；会话拒绝状态变化或间隔一分钟后允许再记。</summary>
        private static void RecordPrepareFailure(Pawn pawn, LocalTargetInfo target, Job job, string signature, string details)
        {
            int tick = CurrentTick;
            FailedAttemptSpan span;
            if (!Failures.TryGetValue(pawn.ThingID, out span)
                || !ReferenceEquals(span.Job, job)
                || span.LastTarget != target
                || (tick >= 0 && span.LastTick >= 0 && tick - span.LastTick > 120))
            {
                span = new FailedAttemptSpan(tick, target, job);
                Failures[pawn.ThingID] = span;
            }

            span.PrepareFailureCount++;
            if (span.LastPrepareDetails == signature && tick >= 0 && span.LastPrepareLogTick >= 0
                && tick - span.LastPrepareLogTick < 60)
                return;

            span.LastPrepareDetails = signature;
            span.LastPrepareLogTick = tick;
            Log("session_prepare_false", details);
        }

        /// <summary>读取原版朝向跟踪器持有的人物引用；读取失败仅影响诊断，不传播到游戏逻辑。</summary>
        private static Pawn ReadPawn(Pawn_RotationTracker tracker)
        {
            try { return CombatAiTrace.Read(tracker, "pawn") as Pawn; }
            catch { return null; }
        }

        /// <summary>只调用纯读取的正式宿主运行态判断，用于把拒绝归到对应分支。</summary>
        private static bool ReadBool(object owner, string methodName)
        {
            try
            {
                MethodInfo method = AccessTools.Method(owner.GetType(), methodName, Type.EmptyTypes);
                return method != null && (bool)method.Invoke(owner, null);
            }
            catch { return false; }
        }

        /// <summary>复用正式目标匹配只读判断，记录活跃会话是否拒绝了当前攻击目标。</summary>
        private static bool ReadTargetMatch(BdpVerb_FormalHostShoot verb, LocalTargetInfo target)
        {
            try
            {
                MethodInfo method = AccessTools.Method(typeof(BdpVerb_Shoot), "MatchesAutomaticCastTarget", new[] { typeof(LocalTargetInfo) });
                return method != null && (bool)method.Invoke(verb, new object[] { target });
            }
            catch { return false; }
        }

        /// <summary>安全格式化正式宿主状态；反射异常不会影响被观察的游戏方法。</summary>
        private static string SafeHost(Verb verb)
        {
            try { return CombatAiTrace.Host(verb); }
            catch (Exception exception) { return "snapshot_error=" + exception.GetType().Name; }
        }

        /// <summary>读取人物与 BDP 宿主当前只读状态，不调用攻击查询或武器可用性入口。</summary>
        private static string Snapshot(Pawn pawn)
        {
            try
            {
                return "rotation=" + pawn.Rotation
                    + ", stance=" + pawn.stances?.curStance?.GetType().Name
                    + ", fullBodyBusy=" + pawn.stances?.FullBodyBusy
                    + ", moving=" + pawn.pather?.Moving
                    + ", job=" + CombatAiTrace.Job(pawn.CurJob)
                    + ", driver=" + pawn.jobs?.curDriver?.GetType().Name
                    + ", projectionAndHosts={" + CombatAiTrace.Snapshot(pawn) + "}";
            }
            catch (Exception exception) { return "snapshot_error=" + exception.GetType().Name; }
        }

        /// <summary>输出稳定机器标记、阶段、游戏 tick 与结构化字段，便于从 Player.log 筛选。</summary>
        private static void Log(string stage, string details)
        {
            try { Verse.Log.Message(Marker + " event=" + stage + " | tick=" + CurrentTick + " | " + details); }
            catch { }
        }

        /// <summary>安全读取当前游戏 tick；游戏尚未启动时使用 -1。</summary>
        private static int CurrentTick => Current.Game != null && Current.Game.tickManager != null
            ? Current.Game.tickManager.TicksGame
            : -1;

        /// <summary>格式化目标实体或地格，不查询或修改目标状态。</summary>
        private static string Target(LocalTargetInfo target)
        {
            return target.HasThing
                ? target.Thing.LabelShort + "#" + target.Thing.ThingID + "@" + target.Thing.Position
                : (target.IsValid ? "cell@" + target.Cell : "invalid");
        }

        /// <summary>一次原版攻击尝试的前置快照。</summary>
        private sealed class AttackAttemptSnapshot
        {
            /// <summary>本次攻击人物。</summary>
            internal readonly Pawn Pawn;
            /// <summary>原版任务请求的目标。</summary>
            internal readonly LocalTargetInfo Target;
            /// <summary>任务实例用于区分两次连续强制攻击。</summary>
            internal readonly Job Job;

            /// <summary>保存本次观察的对象与目标。</summary>
            internal AttackAttemptSnapshot(Pawn pawn, LocalTargetInfo target, Job job)
            {
                Pawn = pawn;
                Target = target;
                Job = job;
            }
        }

        /// <summary>会话准入调用前的状态快照。</summary>
        private sealed class PrepareSnapshot
        {
            /// <summary>发起攻击的人物。</summary>
            internal readonly Pawn Pawn;
            /// <summary>本次会话准入所属的原版任务实例。</summary>
            internal readonly Job Job;
            /// <summary>调用前人物、任务与姿态信息。</summary>
            internal readonly string EntrySnapshot;

            /// <summary>保存原始状态，供拒绝后的日志关联。</summary>
            internal PrepareSnapshot(Pawn pawn, Verb verb, LocalTargetInfo target)
            {
                Pawn = pawn;
                Job = pawn.CurJob;
                EntrySnapshot = "target=" + Target(target)
                    + ", rotation=" + pawn.Rotation
                    + ", stance=" + pawn.stances?.curStance?.GetType().Name
                    + ", fullBodyBusy=" + pawn.stances?.FullBodyBusy
                    + ", job=" + CombatAiTrace.Job(pawn.CurJob)
                    + ", host={" + SafeHost(verb) + "}";
            }
        }

        /// <summary>朝向更新前后的同一人物状态。</summary>
        private sealed class RotationSnapshot
        {
            /// <summary>正在更新朝向的人物。</summary>
            internal readonly Pawn Pawn;
            /// <summary>原版更新前的朝向。</summary>
            internal readonly Rot4 Before;
            /// <summary>原版更新前的人物姿态类型。</summary>
            internal readonly string BeforeStance;
            /// <summary>原版更新前是否处于占用全身的姿态。</summary>
            internal readonly bool BeforeBusy;
            /// <summary>原版更新前的当前作业定义。</summary>
            internal readonly string BeforeJob;
            /// <summary>原版更新前的当前作业驱动类型。</summary>
            internal readonly string BeforeDriver;

            /// <summary>保存人物与旧朝向。</summary>
            internal RotationSnapshot(Pawn pawn, Rot4 before)
            {
                Pawn = pawn;
                Before = before;
                BeforeStance = pawn.stances?.curStance?.GetType().Name ?? "none";
                BeforeBusy = pawn.stances?.FullBodyBusy ?? false;
                BeforeJob = CombatAiTrace.Job(pawn.CurJob);
                BeforeDriver = pawn.jobs?.curDriver?.GetType().Name ?? "none";
            }
        }

        /// <summary>连续失败期间的压缩统计，避免持续不可命中时每刻输出一行。</summary>
        private sealed class FailedAttemptSpan
        {
            /// <summary>首次失败的游戏 tick。</summary>
            internal readonly int FirstTick;
            /// <summary>最近一次失败 tick。</summary>
            internal int LastTick;
            /// <summary>原版起手失败总次数。</summary>
            internal int Count;
            /// <summary>正式会话拒绝次数。</summary>
            internal int PrepareFailureCount;
            /// <summary>上次输出的会话拒绝内容。</summary>
            internal string LastPrepareDetails;
            /// <summary>上次输出会话拒绝的 tick。</summary>
            internal int LastPrepareLogTick = -1;
            /// <summary>最近一次失败目标。</summary>
            internal LocalTargetInfo LastTarget;
            /// <summary>失败跨度对应的原版任务实例。</summary>
            internal readonly Job Job;

            /// <summary>开始一次失败跨度。</summary>
            internal FailedAttemptSpan(int tick, LocalTargetInfo target, Job job)
            {
                FirstTick = tick;
                LastTick = tick;
                LastTarget = target;
                Job = job;
            }
        }
    }
}
#endif
