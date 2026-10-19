using BDP.Core.Abilities;
using BDP.Support.Diagnostics;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BDP.Development.Abilities
{
    /// <summary>
    /// 风刃从玩家点击到原版 Ability 工作执行之间的大方向诊断补丁。
    /// 只观察关键事件，不进入 GUI 绘制、目标器每帧更新或投射物循环。
    /// </summary>
    internal static class FujinRangedStrikeCastFlowDiagnostics
    {
        /// <summary>
        /// 风刃 AbilityDef 的稳定标识；诊断只关注这一项，不影响其它 Ability。
        /// </summary>
        private const string FujinRangedStrikeAbilityDefName = "BDP_Ability_FujinRangedStrike";

        /// <summary>
        /// 观察玩家点击 Ability 按钮的入口。
        /// 原版随后会启动目标器，因此该日志早于目标确认。
        /// </summary>
        [HarmonyPatch(typeof(Command_Ability), nameof(Command_Ability.ProcessInput))]
        private static class Patch_CommandAbility_ProcessInput
        {
            /// <summary>
            /// 记录玩家实际触发了风刃 Ability 按钮。
            /// </summary>
            private static void Prefix(Command_Ability __instance)
            {
                Ability ability = __instance?.Ability;
                if (!IsFujinRangedStrike(ability))
                {
                    return;
                }

                Log(
                    __instance.Pawn,
                    "ui_button_click",
                    "targetRequired=" + ability.def.targetRequired
                    + ", verb=" + DescribeType(ability.verb));
            }
        }

        /// <summary>
        /// 观察玩家在目标器确认目标后进入 Ability 工作排队入口。
        /// </summary>
        [HarmonyPatch(
            typeof(Ability),
            nameof(Ability.QueueCastingJob),
            new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
        private static class Patch_Ability_QueueCastingJob
        {
            /// <summary>
            /// 记录目标确认后的工作排队尝试；是否真正创建工作由下一阶段日志确认。
            /// </summary>
            private static void Prefix(
                Ability __instance,
                LocalTargetInfo target,
                LocalTargetInfo destination)
            {
                if (!IsFujinRangedStrike(__instance))
                {
                    return;
                }

                Log(
                    __instance.pawn,
                    "target_confirmed_queue_attempt",
                    "target=" + DescribeTarget(target)
                    + ", destination=" + DescribeTarget(destination));
            }
        }

        /// <summary>
        /// 观察未走 Verb_CastAbility 专用重定向时的原版 Verb 目标确认入口。
        /// BdpVerb_AbilityShoot 当前继承 Verb_AbilityShoot；原版会在这里直接创建通用 Verb 作业。
        /// </summary>
        [HarmonyPatch(
            typeof(Verb),
            nameof(Verb.OrderForceTarget),
            new[] { typeof(LocalTargetInfo) })]
        private static class Patch_Verb_OrderForceTarget
        {
            /// <summary>
            /// 记录风刃是否进入 Verse.Verb 的通用目标确认路径。
            /// </summary>
            private static void Prefix(Verb __instance, LocalTargetInfo target)
            {
                BdpVerb_AbilityShoot abilityShoot = __instance as BdpVerb_AbilityShoot;
                if (!IsFujinRangedStrike(abilityShoot?.Ability))
                {
                    return;
                }

                Log(
                    abilityShoot.CasterPawn,
                    "target_confirmed_verb_order",
                    "route=Verse.Verb.OrderForceTarget"
                    + ", target=" + DescribeTarget(target)
                    + ", ability=" + abilityShoot.Ability.def.defName);
            }

            /// <summary>
            /// 记录通用目标确认方法返回，便于区分提前返回和后续作业排队。
            /// </summary>
            private static void Postfix(Verb __instance, LocalTargetInfo target)
            {
                BdpVerb_AbilityShoot abilityShoot = __instance as BdpVerb_AbilityShoot;
                if (!IsFujinRangedStrike(abilityShoot?.Ability))
                {
                    return;
                }

                Log(
                    abilityShoot.CasterPawn,
                    "target_confirmed_verb_order_returned",
                    "target=" + DescribeTarget(target)
                    + ", casterJob=" + DescribeJob(abilityShoot.CasterPawn?.CurJob));
            }
        }

        /// <summary>
        /// 观察原版 Verb.OrderForceTarget 创建的通用作业是否进入 Pawn 作业队列。
        /// </summary>
        [HarmonyPatch(
            typeof(Pawn_JobTracker),
            nameof(Pawn_JobTracker.TryTakeOrderedJob),
            new[] { typeof(Job), typeof(JobTag?), typeof(bool) })]
        private static class Patch_PawnJobTracker_TryTakeOrderedJob
        {
            /// <summary>
            /// 记录通用风刃作业提交到原版作业管理器的入口。
            /// </summary>
            private static void Prefix(Job job, JobTag? tag, bool requestQueueing)
            {
                BdpVerb_AbilityShoot abilityShoot = job?.verbToUse as BdpVerb_AbilityShoot;
                if (!IsFujinRangedStrike(abilityShoot?.Ability))
                {
                    return;
                }

                Log(
                    abilityShoot.CasterPawn,
                    "generic_job_queue_attempt",
                    "job=" + DescribeJob(job)
                    + ", tag=" + (tag.HasValue ? tag.Value.ToString() : "null")
                    + ", requestQueueing=" + requestQueueing);
            }

            /// <summary>
            /// 记录原版作业管理器是否接受通用风刃作业。
            /// </summary>
            private static void Postfix(Job job, bool __result)
            {
                BdpVerb_AbilityShoot abilityShoot = job?.verbToUse as BdpVerb_AbilityShoot;
                if (!IsFujinRangedStrike(abilityShoot?.Ability))
                {
                    return;
                }

                Log(
                    abilityShoot.CasterPawn,
                    "generic_job_queue_result",
                    "job=" + DescribeJob(job)
                    + ", accepted=" + __result);
            }
        }

        /// <summary>
        /// 观察通用风刃作业从队列转入 Pawn_JobTracker.StartJob 的入口。
        /// </summary>
        [HarmonyPatch(
            typeof(Pawn_JobTracker),
            nameof(Pawn_JobTracker.StartJob),
            new[]
            {
                typeof(Job),
                typeof(JobCondition),
                typeof(ThinkNode),
                typeof(bool),
                typeof(bool),
                typeof(ThinkTreeDef),
                typeof(JobTag?),
                typeof(bool),
                typeof(bool),
                typeof(bool?),
                typeof(bool),
                typeof(bool),
                typeof(bool)
            })]
        private static class Patch_PawnJobTracker_StartJob
        {
            /// <summary>
            /// 记录原版真正尝试启动通用风刃作业的边界。
            /// </summary>
            private static void Prefix(Job newJob, bool fromQueue)
            {
                BdpVerb_AbilityShoot abilityShoot = newJob?.verbToUse as BdpVerb_AbilityShoot;
                if (!IsFujinRangedStrike(abilityShoot?.Ability))
                {
                    return;
                }

                Log(
                    abilityShoot.CasterPawn,
                    "generic_job_start_attempt",
                    "job=" + DescribeJob(newJob)
                    + ", fromQueue=" + fromQueue);
            }
        }

        /// <summary>
        /// 观察通用 CastVerbOnce 作业驱动器开始、建造工作步骤和准备进入首个工作步骤。
        /// </summary>
        [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.Notify_Starting))]
        private static class Patch_JobDriver_NotifyStarting
        {
            /// <summary>
            /// 记录通用作业驱动器已开始执行。
            /// </summary>
            private static void Prefix(JobDriver __instance)
            {
                Job job = GetJob(__instance);
                if (!IsFujinRangedStrikeGenericVerbJob(job))
                {
                    return;
                }

                Log(
                    __instance.GetActor(),
                    "generic_job_driver_started",
                    "driver=" + DescribeType(__instance)
                    + ", job=" + DescribeJob(job));
            }
        }

        /// <summary>
        /// 观察通用 JobDriver.SetupToils（构建工作步骤）是否完成。
        /// </summary>
        [HarmonyPatch(typeof(JobDriver), "SetupToils")]
        private static class Patch_JobDriver_SetupToils
        {
            /// <summary>
            /// 记录通用作业开始构建工作步骤。
            /// </summary>
            private static void Prefix(JobDriver __instance)
            {
                Job job = GetJob(__instance);
                if (!IsFujinRangedStrikeGenericVerbJob(job))
                {
                    return;
                }

                Log(
                    __instance.GetActor(),
                    "generic_toils_setup_enter",
                    "driver=" + DescribeType(__instance)
                    + ", job=" + DescribeJob(job));
            }

            /// <summary>
            /// 记录通用作业完成工作步骤构建并返回原版启动流程。
            /// </summary>
            private static void Postfix(JobDriver __instance)
            {
                Job job = GetJob(__instance);
                if (!IsFujinRangedStrikeGenericVerbJob(job))
                {
                    return;
                }

                Log(
                    __instance.GetActor(),
                    "generic_toils_setup_returned",
                    "driver=" + DescribeType(__instance)
                    + ", job=" + DescribeJob(job));
            }
        }

        /// <summary>
        /// 观察 JobDriver 准备启动下一个工作步骤的边界；施放 Toil 的 initAction 随后会调用风刃 Verb。
        /// </summary>
        [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.ReadyForNextToil))]
        private static class Patch_JobDriver_ReadyForNextToil
        {
            /// <summary>
            /// 记录通用作业向下一个工作步骤推进的尝试。
            /// </summary>
            private static void Prefix(JobDriver __instance)
            {
                Job job = GetJob(__instance);
                if (!IsFujinRangedStrikeGenericVerbJob(job))
                {
                    return;
                }

                Log(
                    __instance.GetActor(),
                    "generic_toil_ready_attempt",
                    "driver=" + DescribeType(__instance)
                    + ", job=" + DescribeJob(job)
                    + ", toilIndex=" + __instance.CurToilIndex);
            }
        }

        /// <summary>
        /// 观察原版 Ability 创建施放工作。
        /// </summary>
        [HarmonyPatch(
            typeof(Ability),
            nameof(Ability.GetJob),
            new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
        private static class Patch_Ability_GetJob
        {
            /// <summary>
            /// 记录工作对象是否成功生成。
            /// </summary>
            private static void Postfix(
                Ability __instance,
                LocalTargetInfo target,
                LocalTargetInfo destination,
                Job __result)
            {
                if (!IsFujinRangedStrike(__instance))
                {
                    return;
                }

                Log(
                    __instance.pawn,
                    "job_created",
                    "job=" + (__result != null ? __result.def?.defName ?? "null" : "null")
                    + ", verb=" + DescribeType(__result?.verbToUse)
                    + ", target=" + DescribeTarget(target)
                    + ", destination=" + DescribeTarget(destination));
            }
        }

        /// <summary>
        /// 观察原版施放 Ability 工作真正开始执行。
        /// </summary>
        [HarmonyPatch(typeof(JobDriver_CastAbility), nameof(JobDriver_CastAbility.Notify_Starting))]
        private static class Patch_JobDriverCastAbility_NotifyStarting
        {
            /// <summary>
            /// 记录工作启动；之后原版会进入 CastVerb Toil 并调用 Verb 的施放入口。
            /// </summary>
            private static void Prefix(JobDriver_CastAbility __instance)
            {
                Job job = Traverse.Create(__instance).Field("job").GetValue<Job>();
                if (!IsFujinRangedStrike(job?.ability))
                {
                    return;
                }

                Log(
                    __instance.GetActor(),
                    "job_started",
                    "job=" + (job.def?.defName ?? "null")
                    + ", verb=" + DescribeType(job.verbToUse)
                    + ", target=" + DescribeTarget(job.targetA));
            }
        }

        /// <summary>
        /// 判断 Ability 是否为风刃。
        /// </summary>
        private static bool IsFujinRangedStrike(Ability ability)
        {
            return ability?.def?.defName == FujinRangedStrikeAbilityDefName;
        }

        /// <summary>
        /// 输出一条风刃大方向流程诊断，并按阶段限流。
        /// </summary>
        private static void Log(Pawn pawn, string stage, string details)
        {
            string pawnId = pawn != null ? pawn.ThingID : "null";
            string key = "fujin.rangedStrike.flow." + pawnId + "." + stage;
            BdpDiagnostics.AttackExecutionThrottled(
                key,
                "风刃流程诊断 | stage=" + stage
                + " | pawn=" + pawnId
                + " | " + (details ?? "none"),
                60);
        }

        /// <summary>
        /// 将目标压缩成一行文本，不展开目标 Thing 的全部状态。
        /// </summary>
        private static string DescribeTarget(LocalTargetInfo target)
        {
            if (!target.IsValid)
            {
                return "invalid";
            }

            if (target.Cell.IsValid)
            {
                return target.Cell.ToString();
            }

            return target.Thing != null ? target.Thing.ThingID : "valid";
        }

        /// <summary>
        /// 获取对象运行时类型名，避免把对象完整 ToString 写入日志。
        /// </summary>
        private static string DescribeType(object value)
        {
            return value != null ? value.GetType().FullName : "null";
        }

        /// <summary>
        /// 读取 JobDriver 当前持有的原版作业对象。
        /// </summary>
        private static Job GetJob(JobDriver driver)
        {
            return driver != null
                ? Traverse.Create(driver).Field("job").GetValue<Job>()
                : null;
        }

        /// <summary>
        /// 判断作业是否是当前风刃 Verb 走出的原版通用 UseVerbOnThing 作业。
        /// </summary>
        private static bool IsFujinRangedStrikeGenericVerbJob(Job job)
        {
            BdpVerb_AbilityShoot abilityShoot = job?.verbToUse as BdpVerb_AbilityShoot;
            return job?.def?.defName == "UseVerbOnThing"
                && IsFujinRangedStrike(abilityShoot?.Ability);
        }

        /// <summary>
        /// 将作业压缩成一行文本，避免把完整作业对象展开进日志。
        /// </summary>
        private static string DescribeJob(Job job)
        {
            if (job == null)
            {
                return "null";
            }

            return (job.def?.defName ?? "null")
                + ", verb=" + DescribeType(job.verbToUse)
                + ", target=" + DescribeTarget(job.targetA);
        }
    }
}
