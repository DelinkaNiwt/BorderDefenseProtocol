using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace BDP.Core.Projectiles.RangedFlightProtocol.Impact
{
    /// <summary>
    /// 原版伤害入口完成后的中性结果。
    /// 它只描述“伤害入口最终发生了什么”，不判断具体是哪一种护盾业务。
    /// </summary>
    public enum DamageResolutionOutcome
    {
        /// <summary>
        /// 当前调用尚未完成。
        /// </summary>
        NotResolved,

        /// <summary>
        /// 原版在进入伤害处理前就因 0 伤害等原因结束。
        /// </summary>
        NoDamage,

        /// <summary>
        /// 伤害前拦截器或护盾吸收了本次伤害。
        /// </summary>
        ShieldBlocked,

        /// <summary>
        /// 模块主动取消了本次伤害，但目标已通过护盾裁决。
        /// </summary>
        ModuleIntercepted,

        /// <summary>
        /// 伤害入口正常完成，后续反馈仍由原版结果决定。
        /// </summary>
        DamageProcessed
    }

    /// <summary>
    /// 一次 Thing.TakeDamage（目标承伤）调用的结果快照。
    /// </summary>
    public sealed class DamageResolution
    {
        /// <summary>
        /// 本次结果对应的目标 Thing。
        /// </summary>
        public Thing TargetThing { get; internal set; }

        /// <summary>
        /// 本次承伤调用的原版结果。
        /// </summary>
        public DamageWorker.DamageResult DamageResult { get; internal set; }

        /// <summary>
        /// 当前结果分类。
        /// </summary>
        public DamageResolutionOutcome Outcome { get; internal set; }

        /// <summary>
        /// 是否是前置护盾/拦截器吸收。
        /// </summary>
        public bool IsShieldBlocked
        {
            get { return Outcome == DamageResolutionOutcome.ShieldBlocked; }
        }

        /// <summary>
        /// 是否已进入原版伤害工作器。
        /// </summary>
        public bool IsDamageProcessed
        {
            get { return Outcome == DamageResolutionOutcome.DamageProcessed; }
        }
    }

    /// <summary>
    /// 记录原版伤害入口的短生命周期结果，并为外部护盾实现提供中性登记口。
    /// </summary>
    public static class DamageResolutionRuntime
    {
        /// <summary>
        /// 当前线程正在执行的 TakeDamage（目标承伤）调用栈。
        /// </summary>
        [System.ThreadStatic]
        private static Stack<Capture> captures;

        /// <summary>
        /// 当前线程最近一次已完成的承伤结果。
        /// </summary>
        [System.ThreadStatic]
        private static DamageResolution lastCompleted;

        /// <summary>
        /// 读取最近一次已完成结果。
        /// </summary>
        public static DamageResolution LastCompleted
        {
            get { return lastCompleted; }
        }

        /// <summary>
        /// 创建“模块主动拦截伤害”的结果，不伪造 TakeDamage（目标承伤）调用。
        /// </summary>
        public static DamageResolution CreateModuleInterception(Thing targetThing)
        {
            return new DamageResolution
            {
                TargetThing = targetThing,
                Outcome = DamageResolutionOutcome.ModuleIntercepted
            };
        }

        /// <summary>
        /// 创建“外部投射物拦截器已挡住”的结果。
        /// </summary>
        public static DamageResolution CreateProjectileInterception(Thing targetThing)
        {
            return new DamageResolution
            {
                TargetThing = targetThing,
                Outcome = DamageResolutionOutcome.ShieldBlocked
            };
        }

        /// <summary>
        /// 只调用一次原版伤害前裁决，用于“模块取消伤害但仍需尊重护盾”的路径。
        /// 这不会进入 DamageWorker（伤害工作器），因此不会造成真实伤害。
        /// </summary>
        public static bool TryProbeDamageInterception(
            Thing targetThing,
            ref DamageInfo damageInfo,
            out bool absorbed)
        {
            absorbed = false;
            if (targetThing == null)
            {
                return false;
            }

            MethodInfo preApplyDamage = ResolvePreApplyDamageMethod(targetThing.GetType());
            if (preApplyDamage == null)
            {
                return false;
            }

            object[] arguments = { damageInfo, false };
            preApplyDamage.Invoke(targetThing, arguments);
            if (arguments[0] is DamageInfo updatedDamageInfo)
            {
                damageInfo = updatedDamageInfo;
            }

            absorbed = arguments[1] is bool value && value;
            return true;
        }

        /// <summary>
        /// 开始记录一次原版承伤调用。
        /// </summary>
        internal static Capture Begin(Thing targetThing, DamageInfo damageInfo)
        {
            if (captures == null)
            {
                captures = new Stack<Capture>();
            }

            Capture capture = new Capture(targetThing, damageInfo);
            captures.Push(capture);
            return capture;
        }

        /// <summary>
        /// 记录伤害前入口已经判定为吸收。
        /// </summary>
        public static void MarkAbsorbed(Thing targetThing)
        {
            Capture capture = FindCapture(targetThing);
            if (capture != null)
            {
                capture.Absorbed = true;
            }
        }

        /// <summary>
        /// 记录原版 PreApplyDamage（承伤前处理）最终的 absorbed（已吸收）值。
        /// </summary>
        internal static void ObservePreApplyDamage(Thing targetThing, bool absorbed)
        {
            if (absorbed)
            {
                MarkAbsorbed(targetThing);
            }
        }

        /// <summary>
        /// 完成一次承伤记录，并把它发布为最近结果供当前外层调用者消费。
        /// </summary>
        internal static void Complete(
            Capture capture,
            DamageWorker.DamageResult damageResult,
            bool completedNormally)
        {
            if (capture == null)
            {
                return;
            }

            capture.Resolution.DamageResult = damageResult;
            capture.Resolution.Outcome = !capture.HasPositiveDamage
                ? DamageResolutionOutcome.NoDamage
                : capture.Absorbed
                    ? DamageResolutionOutcome.ShieldBlocked
                    : completedNormally
                        ? DamageResolutionOutcome.DamageProcessed
                        : DamageResolutionOutcome.NotResolved;
            lastCompleted = capture.Resolution;
            RemoveCapture(capture);
        }

        /// <summary>
        /// 清理异常退出的承伤记录，避免错误状态泄漏到下一次攻击。
        /// </summary>
        internal static void Abort(Capture capture)
        {
            if (capture == null)
            {
                return;
            }

            capture.Resolution.Outcome = DamageResolutionOutcome.NotResolved;
            RemoveCapture(capture);
        }

        /// <summary>
        /// 消费指定目标的最近结果；目标不匹配时返回空，避免串用其它目标的结果。
        /// </summary>
        public static DamageResolution ConsumeLast(Thing targetThing)
        {
            if (lastCompleted == null || lastCompleted.TargetThing != targetThing)
            {
                return null;
            }

            DamageResolution result = lastCompleted;
            lastCompleted = null;
            return result;
        }

        /// <summary>
        /// 查找当前目标最内层的承伤记录。
        /// </summary>
        private static Capture FindCapture(Thing targetThing)
        {
            if (captures == null)
            {
                return null;
            }

            foreach (Capture capture in captures)
            {
                if (capture != null && capture.TargetThing == targetThing)
                {
                    return capture;
                }
            }

            return null;
        }

        /// <summary>
        /// 从当前线程调用栈移除已完成记录。
        /// </summary>
        private static void RemoveCapture(Capture capture)
        {
            if (captures == null || captures.Count == 0 || captures.Peek() == capture)
            {
                if (captures != null && captures.Count > 0)
                {
                    captures.Pop();
                }

                return;
            }

            Stack<Capture> remaining = new Stack<Capture>();
            while (captures.Count > 0)
            {
                Capture current = captures.Pop();
                if (current == capture)
                {
                    break;
                }

                remaining.Push(current);
            }

            while (remaining.Count > 0)
            {
                captures.Push(remaining.Pop());
            }
        }

        /// <summary>
        /// 从目标实际运行时类型解析 protected PreApplyDamage（伤害前处理）覆盖。
        /// </summary>
        private static MethodInfo ResolvePreApplyDamageMethod(System.Type targetType)
        {
            while (targetType != null)
            {
                MethodInfo method = targetType.GetMethod(
                    "PreApplyDamage",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(DamageInfo).MakeByRefType(), typeof(bool).MakeByRefType() },
                    null);
                if (method != null)
                {
                    return method;
                }

                targetType = targetType.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Harmony（运行时补丁）使用的内部记录对象。
        /// </summary>
        public sealed class Capture
        {
            /// <summary>
            /// 创建一次目标承伤记录。
            /// </summary>
            internal Capture(Thing targetThing, DamageInfo damageInfo)
            {
                TargetThing = targetThing;
                HasPositiveDamage = damageInfo.Amount > 0f;
                Resolution = new DamageResolution
                {
                    TargetThing = targetThing,
                    Outcome = DamageResolutionOutcome.NotResolved
                };
            }

            /// <summary>
            /// 当前目标。
            /// </summary>
            internal Thing TargetThing { get; }

            /// <summary>
            /// 原版入口开始时是否携带正伤害量。
            /// </summary>
            internal bool HasPositiveDamage { get; }

            /// <summary>
            /// 是否被伤害前入口吸收。
            /// </summary>
            internal bool Absorbed { get; set; }

            /// <summary>
            /// 对外发布的结果对象。
            /// </summary>
            internal DamageResolution Resolution { get; }
        }
    }
}
