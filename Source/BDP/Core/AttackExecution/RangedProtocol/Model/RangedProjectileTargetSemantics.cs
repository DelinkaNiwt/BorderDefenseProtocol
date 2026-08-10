using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 单发投射物的目标语义快照。
    /// 它把目标引用与真实空间坐标分开，避免路径、追踪和最终目标互相覆盖。
    /// </summary>
    internal sealed class RangedProjectileTargetSemantics : IExposable
    {
        /// <summary>
        /// 玩家操作结束时冻结的最终目标引用。
        /// 该引用保留原版语义，可以指实体，也可以指原版地格目标。
        /// </summary>
        public LocalTargetInfo IntentFinalTarget { get; set; }

        /// <summary>
        /// 玩家操作结束时冻结的最终真实空间坐标。
        /// 它不把地图格当成权威语义，只在需要时由调用方临时投影。
        /// </summary>
        public Vector3 IntentFinalPoint { get; set; }

        /// <summary>
        /// 玩家操作结束时冻结的第一段目标引用。
        /// 路径模块应把首个锚点或首段目标落在这里。
        /// </summary>
        public LocalTargetInfo IntentFirstTarget { get; set; }

        /// <summary>
        /// 玩家操作结束时冻结的第一段真实空间坐标。
        /// 普通直射时它与最终真实坐标相同。
        /// </summary>
        public Vector3 IntentFirstPoint { get; set; }

        /// <summary>
        /// 飞行运行期的真实最终目标引用。
        /// 追踪等模块可用它保持“最终要找谁”的实时事实。
        /// </summary>
        public LocalTargetInfo LiveFinalTarget { get; set; }

        /// <summary>
        /// 飞行运行期的真实最终空间坐标。
        /// 目标移动或业务刷新时可更新这一层，而不改冻结意图层。
        /// </summary>
        public Vector3 LiveFinalPoint { get; set; }

        /// <summary>
        /// 飞行运行期的下一目标引用。
        /// 路径续段和追踪转向只应改这一层的实时目标。
        /// </summary>
        public LocalTargetInfo LiveNextTarget { get; set; }

        /// <summary>
        /// 飞行运行期的下一真实空间坐标。
        /// 它回答投射物此刻真正正往哪个点飞。
        /// </summary>
        public Vector3 LiveNextPoint { get; set; }

        /// <summary>
        /// 按最终目标和第一段目标创建默认目标语义。
        /// </summary>
        /// <param name="finalTarget">最终目标引用。</param>
        /// <param name="firstTarget">第一段目标引用。</param>
        /// <returns>新的目标语义快照。</returns>
        public static RangedProjectileTargetSemantics CreateFromTargets(
            LocalTargetInfo finalTarget,
            LocalTargetInfo firstTarget)
        {
            LocalTargetInfo resolvedFinalTarget = finalTarget.IsValid ? finalTarget : firstTarget;
            LocalTargetInfo resolvedFirstTarget = firstTarget.IsValid ? firstTarget : resolvedFinalTarget;
            Vector3 finalPoint = ResolvePoint(resolvedFinalTarget);
            Vector3 firstPoint = ResolvePoint(resolvedFirstTarget);
            return new RangedProjectileTargetSemantics
            {
                IntentFinalTarget = resolvedFinalTarget,
                IntentFinalPoint = finalPoint,
                IntentFirstTarget = resolvedFirstTarget,
                IntentFirstPoint = firstPoint,
                LiveFinalTarget = resolvedFinalTarget,
                LiveFinalPoint = finalPoint,
                LiveNextTarget = resolvedFirstTarget,
                LiveNextPoint = firstPoint
            };
        }

        /// <summary>
        /// 复制当前目标语义，保证每发投射物持有独立对象。
        /// </summary>
        /// <returns>目标语义副本。</returns>
        public RangedProjectileTargetSemantics Clone()
        {
            return new RangedProjectileTargetSemantics
            {
                IntentFinalTarget = IntentFinalTarget,
                IntentFinalPoint = IntentFinalPoint,
                IntentFirstTarget = IntentFirstTarget,
                IntentFirstPoint = IntentFirstPoint,
                LiveFinalTarget = LiveFinalTarget,
                LiveFinalPoint = LiveFinalPoint,
                LiveNextTarget = LiveNextTarget,
                LiveNextPoint = LiveNextPoint
            };
        }

        /// <summary>
        /// 统一序列化当前目标语义快照。
        /// </summary>
        public void ExposeData()
        {
            LocalTargetInfo intentFinalTarget = IntentFinalTarget;
            Vector3 intentFinalPoint = IntentFinalPoint;
            LocalTargetInfo intentFirstTarget = IntentFirstTarget;
            Vector3 intentFirstPoint = IntentFirstPoint;
            LocalTargetInfo liveFinalTarget = LiveFinalTarget;
            Vector3 liveFinalPoint = LiveFinalPoint;
            LocalTargetInfo liveNextTarget = LiveNextTarget;
            Vector3 liveNextPoint = LiveNextPoint;

            Scribe_TargetInfo.Look(ref intentFinalTarget, "intentFinalTarget");
            Scribe_Values.Look(ref intentFinalPoint, "intentFinalPoint");
            Scribe_TargetInfo.Look(ref intentFirstTarget, "intentFirstTarget");
            Scribe_Values.Look(ref intentFirstPoint, "intentFirstPoint");
            Scribe_TargetInfo.Look(ref liveFinalTarget, "liveFinalTarget");
            Scribe_Values.Look(ref liveFinalPoint, "liveFinalPoint");
            Scribe_TargetInfo.Look(ref liveNextTarget, "liveNextTarget");
            Scribe_Values.Look(ref liveNextPoint, "liveNextPoint");

            IntentFinalTarget = intentFinalTarget;
            IntentFinalPoint = intentFinalPoint;
            IntentFirstTarget = intentFirstTarget;
            IntentFirstPoint = intentFirstPoint;
            LiveFinalTarget = liveFinalTarget;
            LiveFinalPoint = liveFinalPoint;
            LiveNextTarget = liveNextTarget;
            LiveNextPoint = liveNextPoint;
        }

        /// <summary>
        /// 从原版目标引用解析当前可用的真实空间坐标。
        /// 这里只读取原版已给出的中心点，不把格子语义写入目标语义。
        /// </summary>
        /// <param name="target">待解析目标引用。</param>
        /// <returns>目标当前对应的真实空间坐标。</returns>
        private static Vector3 ResolvePoint(LocalTargetInfo target)
        {
            return target.IsValid ? target.CenterVector3 : Vector3.zero;
        }
    }
}
