using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BDP.Core.AttackExecution
{
    /// <summary>
    /// BDP 手动入口的组级 targeting 适配源。
    /// 它只负责把一组单体 targetingSource 提升成“一次 targeting、逐成员下单”的最小语义。
    /// </summary>
    internal sealed class GroupedAttackExecutionTargetingSource : ITargetingSource
    {
        /// <summary>
        /// 当前聚合手动入口包含的成员级 targetingSource 集合。
        /// </summary>
        private readonly IReadOnlyList<AttackExecutionTargetingSource> sources;

        /// <summary>
        /// 用成员级 targetingSource 集合构造组级 targeting 源。
        /// </summary>
        public GroupedAttackExecutionTargetingSource(IReadOnlyList<AttackExecutionTargetingSource> sources)
        {
            this.sources = sources ?? new List<AttackExecutionTargetingSource>();
        }

        /// <summary>
        /// 当前组级 targeting 仍然是 Pawn 施法者语义。
        /// </summary>
        public bool CasterIsPawn => ResolveRepresentativeSource()?.CasterIsPawn ?? true;

        /// <summary>
        /// 当前组级 targeting 是否对应近战攻击。
        /// </summary>
        public bool IsMeleeAttack => ResolveRepresentativeSource()?.IsMeleeAttack ?? false;

        /// <summary>
        /// 当前组级 targeting 是否需要目标选择。
        /// </summary>
        public bool Targetable => ResolveRepresentativeSource()?.Targetable ?? false;

        /// <summary>
        /// 当前组级 targeting 不支持 Shift 连续下单。
        /// </summary>
        public bool MultiSelect => false;

        /// <summary>
        /// 当前组级 targeting 是否隐藏 Pawn tooltip。
        /// </summary>
        public bool HidePawnTooltips => ResolveRepresentativeSource()?.HidePawnTooltips ?? false;

        /// <summary>
        /// 当前组级 targeting 的代表施法者。
        /// </summary>
        public Thing Caster => ResolveRepresentativeSource()?.Caster;

        /// <summary>
        /// 当前组级 targeting 的代表 Pawn。
        /// </summary>
        public Pawn CasterPawn => ResolveRepresentativeSource()?.CasterPawn;

        /// <summary>
        /// 当前组级 targeting 要展示的代表 Verb。
        /// </summary>
        public Verb GetVerb => ResolveRepresentativeSource()?.GetVerb;

        /// <summary>
        /// 当前组级 targeting 要展示的代表图标。
        /// </summary>
        public Texture2D UIIcon => ResolveRepresentativeSource()?.UIIcon;

        /// <summary>
        /// 当前组级 targeting 使用的代表目标参数。
        /// 同一攻击入口的成员应共享一致的目标语义，因此这里不额外拼装新参数。
        /// </summary>
        public TargetingParameters targetParams
        {
            get
            {
                AttackExecutionTargetingSource representative = ResolveRepresentativeSource();
                return representative != null ? representative.targetParams : new TargetingParameters();
            }
        }

        /// <summary>
        /// 当前组级 targeting 在任一成员要求继续交互时，继续留在原版 Targeter 主循环。
        /// </summary>
        public ITargetingSource DestinationSelector
        {
            get
            {
                bool hasContinuation = HasActiveContinuation();
                if (hasContinuation)
                {
                    AttackExecutionDiagnostics.LogGroupedManualTargetingContinuation(
                        CasterPawn,
                        sources.Count,
                        DescribeSourceResultIds(),
                        true,
                        "destination_selector");
                    return this;
                }

                return null;
            }
        }

        /// <summary>
        /// 判断组内是否至少有一个成员能命中当前目标。
        /// </summary>
        public bool CanHitTarget(LocalTargetInfo target)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                AttackExecutionTargetingSource source = sources[i];
                if (source != null && source.CanHitTarget(target))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断组内是否至少有一个成员允许确认当前目标。
        /// 全部失败时才回退到代表成员输出拒绝反馈。
        /// </summary>
        public bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                AttackExecutionTargetingSource source = sources[i];
                if (source != null && source.ValidateTarget(target, false))
                {
                    return true;
                }
            }

            AttackExecutionTargetingSource representative = ResolveRepresentativeSource();
            return representative != null && representative.ValidateTarget(target, showMessages);
        }

        /// <summary>
        /// 绘制组级 targeting 的高亮反馈。
        /// 当前直接复用代表成员的绘制逻辑。
        /// </summary>
        public void DrawHighlight(LocalTargetInfo target)
        {
            AttackExecutionTargetingSource representative = ResolveRepresentativeSource();
            if (representative != null)
            {
                representative.DrawHighlight(target);
            }
        }

        /// <summary>
        /// 把已确认的目标逐个回写到组内可执行成员。
        /// 组级适配层不在成员正式确认链之前预筛业务合法性；每个成员仍通过自己的单体 targetingSource 进入正式下单边界并自行裁定。
        /// </summary>
        public void OrderForceTarget(LocalTargetInfo target)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                AttackExecutionTargetingSource source = sources[i];
                if (source == null)
                {
                    continue;
                }

                source.OrderForceTarget(target);
            }

            AttackExecutionDiagnostics.LogGroupedManualTargetingContinuation(
                CasterPawn,
                sources.Count,
                DescribeSourceResultIds(),
                HasActiveContinuation(),
                "after_order_force_target");
        }

        /// <summary>
        /// 绘制组级 targeting 的鼠标附着 UI。
        /// </summary>
        public void OnGUI(LocalTargetInfo target)
        {
            AttackExecutionTargetingSource representative = ResolveRepresentativeSource();
            if (representative != null)
            {
                representative.OnGUI(target);
            }
        }

        /// <summary>
        /// 解析当前用于 UI 和错误反馈的代表成员。
        /// 优先选用能解析出真实 Verb 的成员，否则退回到第一个非空成员。
        /// </summary>
        private AttackExecutionTargetingSource ResolveRepresentativeSource()
        {
            AttackExecutionTargetingSource fallback = null;
            for (int i = 0; i < sources.Count; i++)
            {
                AttackExecutionTargetingSource source = sources[i];
                if (source == null)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = source;
                }

                if (source.GetVerb != null)
                {
                    return source;
                }
            }

            return fallback;
        }

        /// <summary>
        /// 判断组内是否已有成员要求原版 Targeter 继续停留在多步目标选择流程。
        /// </summary>
        private bool HasActiveContinuation()
        {
            for (int i = 0; i < sources.Count; i++)
            {
                AttackExecutionTargetingSource source = sources[i];
                if (source != null && source.DestinationSelector != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 输出组内成员结果标识，服务手动聚合 targeting 的运行态排查。
        /// </summary>
        private string DescribeSourceResultIds()
        {
            List<string> resultIds = new List<string>();
            for (int i = 0; i < sources.Count; i++)
            {
                string sourceResultId = sources[i] != null ? sources[i].DiagnosticResultId : null;
                if (!string.IsNullOrWhiteSpace(sourceResultId))
                {
                    resultIds.Add(sourceResultId);
                }
            }

            return resultIds.Count > 0 ? string.Join("|", resultIds.ToArray()) : null;
        }
    }
}
