using System;
using System.Reflection;
using BDP.Core.Verbs;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Development.CombatAI
{
    /// <summary>独立注册本轮只读观察补丁；不依赖开发模式在启动时已经开启，不改任何业务返回值。</summary>
    [StaticConstructorOnStartup]
    internal static class CombatAiDiagnosticsHooks
    {
        /// <summary>实际成功注册的方法数，地图心跳会反复报告。</summary>
        internal static int InstalledCount { get; private set; }
        /// <summary>计划注册的方法数，失败也保留计数避免“部分成功”冒充完整覆盖。</summary>
        internal static int ExpectedCount { get; private set; }
        /// <summary>仅属于本次诊断的补丁标识。</summary>
        private static readonly Harmony Observer = new Harmony("niwt.bdp.development.combat_ai_trace");

        /// <summary>逐项注册和报告错误；不被已有 DevelopmentBootstrap 的静默捕获遮住。</summary>
        static CombatAiDiagnosticsHooks()
        {
            string[] decisions =
            {
                "RimWorld.JobGiver_AIFightEnemy", "RimWorld.JobGiver_AITrashColonyClose",
                "RimWorld.JobGiver_AITrashBuildingsDistant", "RimWorld.JobGiver_AIGotoNearestHostile",
                "RimWorld.JobGiver_AISapper", "RimWorld.JobGiver_ReactToCloseMeleeThreat"
            };
            foreach (string type in decisions) Install(type, "TryGiveJob", "DecisionBefore", "DecisionAfter");
            Install("RimWorld.JobGiver_AIFightEnemy", "UpdateEnemyTarget", "TargetBefore", "TargetAfter");
            Install("RimWorld.JobGiver_AIFightEnemies", "TryFindShootingPosition", null, "ShootingPositionAfter");
            Install("Verse.AI.Pawn_JobTracker", "StartJob", "StartJobBefore", "StartJobAfter");
            Install("Verse.AI.Pawn_JobTracker", "CleanupCurrentJob", "CleanupBefore", "CleanupAfter");
            Install("Verse.AI.Pawn_JobTracker", "CheckForJobOverride", "OverrideBefore", "OverrideAfter");
            Install("RimWorld.PawnUtility", "EnemiesAreNearby", null, "NearbyEnemiesAfter");
            Install("Verse.Pawn", "TryGetAttackVerb", null, "AttackQueryAfter");
            Install("BDP.Patches.Patch_Pawn_TryGetAttackVerb", "Postfix", "BdpQueryBefore", "BdpQueryAfter");
            Install("BDP.Core.AttackExecution.AttackExecutionSurfaceAccess", "TryGetAutoRangedVerb", null, "RangedQueryAfter");
            Install("Verse.AI.JobDriver_Wait", "CheckForAutoAttack", "WaitAttackBefore", null);
            Install("RimWorld.Pawn_MeleeVerbs", "TryMeleeAttack", "MeleeBefore", "MeleeAfter");
            Install("BDP.Core.AttackExecution.JobDriver_BdpMeleeAttackExecution", "TryTickExecution", "MeleeTickBefore", "MeleeTickAfter");
            Install("BDP.Core.AttackExecution.AttackExecutionPostLoadRecovery", "IsCurrentAttackSessionValid", null, "SessionValidationAfter");
            Type[] castParameters =
            {
                typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool), typeof(bool)
            };
            Install("BDP.Core.Verbs.BdpVerb_Shoot", "TryStartCastOn", "CastBefore", "CastAfter", castParameters);
            Install("Verse.Verb", "TryStartCastOn", "CastBefore", "CastAfter", castParameters);
            Install("BDP.Core.Verbs.BdpVerb_MeleeAttackDamage", "TryCastShot", null, "MeleeShotAfter");
            Install("BDP.Core.Verbs.BdpVerb_Shoot", "TryCastShot", null, "RangedShotAfter");
            Install("BDP.Core.Verbs.BdpVerb_Shoot", "Reset", "ResetBefore", null);
            CombatAiTrace.Write("hooks_ready", null, "installed=" + InstalledCount + ", expected=" + ExpectedCount
                + ", devMode=" + Prefs.DevMode + ", developmentMvid=" + typeof(CombatAiDiagnosticsHooks).Module.ModuleVersionId
                + ", coreMvid=" + typeof(BdpVerb_Shoot).Module.ModuleVersionId);
        }

        /// <summary>只注册声明于目标类型的方法，避免把派生类型的继承方法重复打补丁。</summary>
        private static void Install(string typeName, string methodName, string before, string after, Type[] parameters = null)
        {
            ExpectedCount++;
            try
            {
                Type type = AccessTools.TypeByName(typeName);
                MethodInfo target = type == null ? null : AccessTools.DeclaredMethod(type, methodName, parameters);
                if (target == null) throw new MissingMethodException(typeName, methodName);
                HarmonyMethod prefix = before == null ? null : new HarmonyMethod(typeof(CombatAiDiagnosticsHooks), before) { priority = Priority.First };
                HarmonyMethod postfix = after == null ? null : new HarmonyMethod(typeof(CombatAiDiagnosticsHooks), after) { priority = Priority.Last };
                Observer.Patch(target, prefix, postfix);
                InstalledCount++;
                CombatAiTrace.Write("hook_installed", null, "method=" + typeName + "." + methodName);
            }
            catch (Exception exception) { CombatAiTrace.Error("install:" + typeName + "." + methodName, exception); }
        }

        /// <summary>派工入口记录旧目标和当前任务，与返回结果组成同一次尝试。</summary>
        private static void DecisionBefore(Pawn pawn, MethodBase __originalMethod, out string __state)
        {
            __state = CombatAiTrace.Thing(pawn?.mindState?.enemyTarget);
            string prior = __state;
            CombatAiTrace.Observe(pawn, trace => trace.Record("decision_enter", "node=" + __originalMethod.DeclaringType.Name
                + ", enemyBefore=" + prior, scope: __originalMethod.DeclaringType.Name));
        }

        /// <summary>无任务也输出；不能把未派工当作没有观测。</summary>
        private static void DecisionAfter(Pawn pawn, Job __result, MethodBase __originalMethod, string __state)
        {
            CombatAiTrace.Observe(pawn, trace => trace.Record("decision_result", "node=" + __originalMethod.DeclaringType.Name
                + ", enemyBefore=" + __state + ", enemyAfter=" + CombatAiTrace.Thing(pawn.mindState?.enemyTarget)
                + ", result={" + CombatAiTrace.Job(__result) + "}", scope: __originalMethod.DeclaringType.Name));
        }

        /// <summary>索敌前身份快照。</summary>
        private static void TargetBefore(Pawn pawn, out string __state) { __state = CombatAiTrace.Thing(pawn?.mindState?.enemyTarget); }

        /// <summary>记录保留、丢失或更换目标，不重新执行索敌。</summary>
        private static void TargetAfter(Pawn pawn, string __state)
        {
            CombatAiTrace.Observe(pawn, trace => trace.Record("target_update", "before=" + __state
                + ", after=" + CombatAiTrace.Thing(pawn.mindState?.enemyTarget)));
        }

        /// <summary>区分攻击行为为空和有远程行为但找不到射击位置。</summary>
        private static void ShootingPositionAfter(Pawn pawn, IntVec3 dest, Verb verbToUse, bool __result)
        {
            CombatAiTrace.Observe(pawn, trace => trace.Record("shoot_position", "found=" + __result
                + ", destination=" + dest + ", suppliedVerb=" + CombatAiTrace.Verb(verbToUse)));
        }

        /// <summary>保存启动前任务，不持有可能被回收重用的旧 Job 引用。</summary>
        private static void StartJobBefore(Pawn ___pawn, Job newJob, JobCondition lastJobEndCondition,
            ThinkNode jobGiver, bool cancelBusyStances, out string __state)
        {
            __state = null;
            string saved = null;
            CombatAiTrace.Observe(___pawn, trace =>
            {
                saved = CombatAiTrace.Job(___pawn.CurJob);
                trace.Record("job_start_attempt", "old={" + saved + "}, requested={" + CombatAiTrace.Job(newJob)
                    + "}, reason=" + lastJobEndCondition + ", suppliedGiver=" + jobGiver?.GetType().Name
                    + ", cancelBusy=" + cancelBusyStances, true);
            });
            __state = saved;
        }

        /// <summary>确认实际启动结果，发现请求任务与最终任务不一致的嵌套替换。</summary>
        private static void StartJobAfter(Pawn ___pawn, Job newJob, string __state)
        {
            CombatAiTrace.Observe(___pawn, trace => trace.Record("job_started", "old={" + __state
                + "}, requestedIsCurrent=" + ReferenceEquals(newJob, ___pawn.CurJob), true));
        }

        /// <summary>记录所有退出入口，包括成功、不可完成、强制打断及可选替换。</summary>
        private static void CleanupBefore(Pawn ___pawn, JobCondition condition, bool cancelBusyStancesSoft, out string __state)
        {
            __state = null;
            string saved = null;
            CombatAiTrace.Observe(___pawn, trace =>
            {
                saved = CombatAiTrace.Job(___pawn.CurJob);
                trace.Record("job_cleanup", "ending={" + saved + "}, condition=" + condition
                    + ", cancelBusySoft=" + cancelBusyStancesSoft, true);
            });
            __state = saved;
        }

        /// <summary>对照任务清理后旧远程会话是否仍驻留。</summary>
        private static void CleanupAfter(Pawn ___pawn, string __state)
        {
            CombatAiTrace.Observe(___pawn, trace => trace.Record("job_cleaned", "ended={" + __state + "}", true));
        }

        /// <summary>记录到期／其他原因触发的重决策入口，不重新运行职责树。</summary>
        private static void OverrideBefore(Pawn ___pawn, out string __state)
        {
            __state = null;
            string saved = null;
            CombatAiTrace.Observe(___pawn, trace =>
            {
                saved = CombatAiTrace.Job(___pawn.CurJob);
                trace.Record("override_enter", "old={" + saved + "}");
            });
            __state = saved;
        }

        /// <summary>重新决策之后是否真的替换任务，由当前快照对照旧任务。</summary>
        private static void OverrideAfter(Pawn ___pawn, string __state)
        {
            CombatAiTrace.Observe(___pawn, trace => trace.Record("override_result", "old={" + __state + "}"));
        }

        /// <summary>只记录原版已经做过的附近敌人判断，解释期限已过但未复查的情况。</summary>
        private static void NearbyEnemiesAfter(Pawn pawn, int regionsToScan, float maxDistance, bool __result)
        {
            CombatAiTrace.Observe(pawn, trace => trace.Record("nearby_enemies", "result=" + __result
                + ", regions=" + regionsToScan + ", distance=" + maxDistance));
        }

        /// <summary>观察原版入口最终返回值，覆盖 BDP 以外补丁再次改变结果的情况。</summary>
        private static void AttackQueryAfter(Pawn __instance, Thing target, Verb __result)
        {
            CombatAiTrace.Observe(__instance, trace => trace.Record("attack_query", "target=" + CombatAiTrace.Thing(target)
                + ", final=" + CombatAiTrace.Verb(__result), scope: target?.ThingID ?? "null"));
        }

        /// <summary>按位置只读 BDP 后置补丁的输入候选；__0 对应其 ref Verb 参数的当前值。</summary>
        private static void BdpQueryBefore(Verb __0, out string __state)
        {
            // 禁用 __args：当前 Harmony 会把入口数组回写到 ref 参数，覆盖业务刚写出的候选。
            // 被观察方法的 __result／__instance 又与 Harmony 保留名冲突，必须用位置而非名字注入。
            __state = CombatAiTrace.Verb(__0);
        }

        /// <summary>直接证明是 BDP 将已有候选清空，而不是推测原版本来就没有武器。</summary>
        private static void BdpQueryAfter(Verb __0, Pawn __1, Thing __2, string __state)
        {
            Pawn pawn = __1;
            Thing target = __2;
            CombatAiTrace.Observe(pawn, trace => trace.Record("bdp_query_transform", "target=" + CombatAiTrace.Thing(target)
                + ", original=" + __state + ", afterBdp=" + CombatAiTrace.Verb(__0), scope: target?.ThingID ?? "null"));
        }

        /// <summary>只观察默认远程候选的选择结果；查询不再准备或阻断攻击会话。</summary>
        private static void RangedQueryAfter(Pawn pawn, Thing target, Verb verb, bool __result)
        {
            CombatAiTrace.Observe(pawn, trace => trace.Record("ranged_query", "target=" + CombatAiTrace.Thing(target)
                + ", bridgeHandled=" + __result
                + ", provided=" + CombatAiTrace.Verb(verb), scope: target?.ThingID ?? "null"));
        }

        /// <summary>等待任务的自动攻击循环只计数并节流，邻格快照解释未进入近战的原因。</summary>
        private static void WaitAttackBefore(JobDriver __instance)
        {
            CombatAiTrace.Observe(__instance.GetActor(), trace => trace.Record("wait_auto", "driver=" + __instance.GetType().Name));
        }

        /// <summary>优先于 BDP 近战接管，保证即使原方法被跳过仍能看到原版请求。</summary>
        private static void MeleeBefore(Pawn_MeleeVerbs __instance, Thing target, Verb verbToUse, out string __state)
        {
            Pawn pawn = __instance.Pawn;
            __state = null;
            string saved = null;
            CombatAiTrace.Observe(pawn, trace =>
            {
                saved = CombatAiTrace.Job(pawn.CurJob);
                trace.Record("melee_request", "target=" + CombatAiTrace.Thing(target)
                    + ", explicitVerb=" + CombatAiTrace.Verb(verbToUse), true);
            });
            __state = saved;
        }

        /// <summary>返回成功不一定已出刀，所以同时记录任务是否被换掉。</summary>
        private static void MeleeAfter(Pawn_MeleeVerbs __instance, Thing target, bool __result, string __state)
        {
            Pawn pawn = __instance.Pawn;
            CombatAiTrace.Observe(pawn, trace => trace.Record("melee_result", "target=" + CombatAiTrace.Thing(target)
                + ", accepted=" + __result + ", oldJob={" + __state + "}", true));
        }

        /// <summary>持续近战每刻经过此处，但只有门槛变化或三秒间隔才输出。</summary>
        private static void MeleeTickBefore(JobDriver __instance)
        {
            Pawn pawn = __instance.GetActor();
            CombatAiTrace.Observe(pawn, trace =>
            {
                Verb verb = pawn.CurJob?.verbToUse;
                Thing target = pawn.CurJob?.targetA.Thing;
                trace.Record("melee_tick", "warming=" + verb?.WarmingUp + ", state=" + verb?.state
                    + ", busy=" + pawn.stances?.FullBodyBusy
                    + ", touch=" + (target != null && pawn.CanReachImmediate(target, PathEndMode.Touch))
                    + ", target=" + CombatAiTrace.Thing(target));
            });
        }

        /// <summary>推进失败必须明确记录，不能仅等原版之后更换任务。</summary>
        private static void MeleeTickAfter(JobDriver __instance, bool __result)
        {
            if (!__result) CombatAiTrace.Observe(__instance.GetActor(), trace => trace.Record("melee_tick_failed", "result=false", true));
        }

        /// <summary>原逻辑已执行的会话校验结果，不在观察器中再次调用校验。</summary>
        private static void SessionValidationAfter(Verb verb, bool __result)
        {
            CombatAiTrace.Observe(verb?.CasterPawn, trace => trace.Record("session_validation",
                "valid=" + __result + ", verb=" + CombatAiTrace.Verb(verb), scope: verb?.loadID ?? "null"));
        }

        /// <summary>远程外层与原版真正起手分层记录；只有自有 BDP 行为才被观察。</summary>
        private static void CastBefore(Verb __instance, LocalTargetInfo castTarg, MethodBase __originalMethod)
        {
            if (!(__instance is BdpVerb_Shoot) && !(__instance is BdpVerb_MeleeAttackDamage)) return;
            CombatAiTrace.Observe(__instance.CasterPawn, trace => trace.Record("cast_request", "layer=" + __originalMethod.DeclaringType.Name
                + ", target=" + castTarg + ", verb=" + CombatAiTrace.Verb(__instance), true));
        }

        /// <summary>实际起手成功与失败均保留，不把接单成功等同于出手成功。</summary>
        private static void CastAfter(Verb __instance, LocalTargetInfo castTarg, MethodBase __originalMethod, bool __result)
        {
            if (!(__instance is BdpVerb_Shoot) && !(__instance is BdpVerb_MeleeAttackDamage)) return;
            CombatAiTrace.Observe(__instance.CasterPawn, trace => trace.Record("cast_result", "layer=" + __originalMethod.DeclaringType.Name
                + ", target=" + castTarg + ", started=" + __result + ", verb=" + CombatAiTrace.Verb(__instance), true));
        }

        /// <summary>短剑实际单击已执行；命中和动作完成分别读取，区分闪避与没有挥刀。</summary>
        private static void MeleeShotAfter(BdpVerb_MeleeAttackDamage __instance, bool __result)
        {
            CombatAiTrace.Observe(__instance.CasterPawn, trace => trace.Record("melee_shot", "verbReturned=" + __result
                + ", attackId=" + __instance.AttackInstanceId
                + ", completed=" + CombatAiTrace.Read(__instance, "LastMeleeBurstShotCompleted")
                + ", hit=" + CombatAiTrace.Read(__instance, "LastMeleeBurstShotHit"), true));
        }

        /// <summary>远程实际发射执行结果，与起手成功及后续会话残留分开统计。</summary>
        private static void RangedShotAfter(BdpVerb_Shoot __instance, bool __result)
        {
            CombatAiTrace.Observe(__instance.CasterPawn, trace => trace.Record("ranged_shot", "emitted=" + __result
                + ", attackId=" + __instance.AttackInstanceId, true));
        }

        /// <summary>远程会话真正重置时记录旧身份；没有重置也能由周期快照看出残留。</summary>
        private static void ResetBefore(BdpVerb_Shoot __instance)
        {
            CombatAiTrace.Observe(__instance.CasterPawn, trace => trace.Record("ranged_reset", "oldHost={" + CombatAiTrace.Host(__instance) + "}", true));
        }
    }
}
