using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution.RangedProtocol.Model
{
    /// <summary>
    /// 投射物发射时冻结的统一结果。
    /// 它把首发分支、目标命中资格和偏射几何放在同一份事实中，供后续路线、碰撞和视觉阶段读取。
    /// </summary>
    public sealed class ProjectileLaunchResolution : IExposable
    {
        /// <summary>当前投射物的首发分支。</summary>
        public ProjectileLaunchDisposition Disposition { get; set; } = ProjectileLaunchDisposition.Unknown;

        /// <summary>当前投射物是否允许命中原定目标。</summary>
        public bool CanHitIntendedTarget { get; set; }

        /// <summary>当前首发结果允许采用的路线续飞模式。</summary>
        public ProjectileLaunchContinuationMode ContinuationMode { get; set; } = ProjectileLaunchContinuationMode.None;

        /// <summary>原版发射时实际使用的首发目标。</summary>
        public LocalTargetInfo ActualLaunchTarget { get; set; } = LocalTargetInfo.Invalid;

        /// <summary>原版发射时实际使用的首发目的地。</summary>
        public Vector3 ActualLaunchDestination { get; set; }

        /// <summary>独立于首段导航的末端裁定目标；无覆盖时为无效目标。</summary>
        public LocalTargetInfo TerminalTarget { get; set; } = LocalTargetInfo.Invalid;

        /// <summary>发射时冻结的末端裁定坐标，不随目标移动或后续散布重新计算。</summary>
        public Vector3 TerminalDestination { get; set; }

        /// <summary>相对名义首发路线冻结的偏射向量。</summary>
        public Vector3 WildMissOffset { get; set; }

        /// <summary>原版掩体拦截分支最终选择的掩体定义。</summary>
        public ThingDef TargetCoverDef { get; set; }

        /// <summary>导航切换后仍保留被禁止命中的原意图目标，防止它被重分类为可命中的非目标。</summary>
        public LocalTargetInfo ResolveIntendedTarget(LocalTargetInfo originalTarget, LocalTargetInfo navigationTarget)
        {
            return Disposition != ProjectileLaunchDisposition.Unknown
                && !CanHitIntendedTarget && originalTarget.IsValid
                ? originalTarget
                : navigationTarget;
        }

        /// <summary>
        /// 持久化发射结果。
        /// 旧存档缺少该节点时保留 Unknown，后续模块必须走安全默认路径。
        /// </summary>
        public void ExposeData()
        {
            ProjectileLaunchDisposition disposition = Disposition;
            bool canHitIntendedTarget = CanHitIntendedTarget;
            ProjectileLaunchContinuationMode continuationMode = ContinuationMode;
            LocalTargetInfo actualLaunchTarget = ActualLaunchTarget;
            Vector3 actualLaunchDestination = ActualLaunchDestination;
            LocalTargetInfo terminalTarget = TerminalTarget;
            Vector3 terminalDestination = TerminalDestination;
            Vector3 wildMissOffset = WildMissOffset;
            ThingDef targetCoverDef = TargetCoverDef;

            Scribe_Values.Look(ref disposition, "disposition", ProjectileLaunchDisposition.Unknown);
            Scribe_Values.Look(ref canHitIntendedTarget, "canHitIntendedTarget", false);
            Scribe_Values.Look(ref continuationMode, "continuationMode", ProjectileLaunchContinuationMode.None);
            Scribe_TargetInfo.Look(ref actualLaunchTarget, "actualLaunchTarget");
            Scribe_Values.Look(ref actualLaunchDestination, "actualLaunchDestination");
            Scribe_TargetInfo.Look(ref terminalTarget, "terminalTarget");
            Scribe_Values.Look(ref terminalDestination, "terminalDestination");
            Scribe_Values.Look(ref wildMissOffset, "wildMissOffset");
            Scribe_Defs.Look(ref targetCoverDef, "targetCoverDef");

            Disposition = disposition;
            CanHitIntendedTarget = canHitIntendedTarget;
            ContinuationMode = continuationMode;
            ActualLaunchTarget = actualLaunchTarget;
            ActualLaunchDestination = actualLaunchDestination;
            TerminalTarget = terminalTarget;
            TerminalDestination = terminalDestination;
            WildMissOffset = wildMissOffset;
            TargetCoverDef = targetCoverDef;
        }
    }

    /// <summary>
    /// 首发结果允许后续轨迹使用的中性续飞模式。
    /// </summary>
    public enum ProjectileLaunchContinuationMode
    {
        /// <summary>不由路线模块继续接管。</summary>
        None,

        /// <summary>沿名义路径继续；末端可由独立冻结结果覆盖，不代表允许命中原目标。</summary>
        IntendedRoute,

        /// <summary>沿不重新吸附目标的偏移路线继续。</summary>
        WildMissOffsetRoute
    }
}
