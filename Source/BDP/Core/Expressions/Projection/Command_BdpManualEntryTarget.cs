using System.Collections.Generic;
using BDP.Core.AttackExecution;
using UnityEngine;
using Verse;

namespace BDP.Core.Expressions
{
    /// <summary>
    /// BDP 手动入口专用目标选择命令。
    /// 它负责显示按钮，并把单选/多选场景统一接入正确的 targetingSource。
    /// </summary>
    internal sealed class Command_BdpManualEntryTarget : Command
    {
        /// <summary>
        /// 当前按钮绑定的单体 targeting 适配源。
        /// </summary>
        private readonly AttackExecutionTargetingSource targetingSource;

        /// <summary>
        /// 当前按钮所属的手动入口组标识。
        /// 它用于让原版 gizmo grouping 按“同攻击入口”聚合，而不是只按文案和图标偶然一致来聚合。
        /// </summary>
        private readonly string manualEntryGroupId;

        /// <summary>
        /// 当前原版 Gizmo 合并到此按钮的其他攻击目标源。
        /// 原版只绘制合并组中的代表按钮，因此悬停预览必须由代表按钮保留全部成员。
        /// </summary>
        private readonly List<AttackExecutionTargetingSource> groupedTargetingSources =
            new List<AttackExecutionTargetingSource>();

        /// <summary>
        /// 用显示信息、单体 targeting 源和入口组标识构造手动入口命令。
        /// </summary>
        public Command_BdpManualEntryTarget(
            string label,
            string description,
            Texture2D iconTexture,
            AttackExecutionTargetingSource targetingSource,
            string manualEntryGroupId)
        {
            defaultLabel = label;
            defaultDesc = description;
            icon = iconTexture;
            this.targetingSource = targetingSource;
            this.manualEntryGroupId = manualEntryGroupId;
            groupKey = !string.IsNullOrWhiteSpace(manualEntryGroupId)
                ? GenText.StableStringHash(manualEntryGroupId)
                : 0;
        }

        /// <summary>
        /// 由按钮自己写入原版保护级禁用状态，并保留玩家可见原因。
        /// </summary>
        internal void DisableForUseRequirements(string reason)
        {
            disabled = true;
            disabledReason = reason;
        }

        /// <summary>
        /// 处理按钮点击。
        /// 当前只保留基础点击反馈，真正的 targeting 启动统一放到组点击入口里。
        /// </summary>
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
        }

        /// <summary>
        /// 鼠标悬停时预览本按钮及已合并同组武器的有效范围。
        /// 这里对齐原版 Command_VerbTarget：不进入 Targeter，直接绘制所有已激活武器的射程圈。
        /// </summary>
        public override void GizmoUpdateOnMouseover()
        {
            targetingSource?.DrawGizmoRangePreview();

            foreach (AttackExecutionTargetingSource groupedSource in groupedTargetingSources)
            {
                groupedSource?.DrawGizmoRangePreview();
            }
        }

        /// <summary>
        /// 在原版 Gizmo 合并阶段保留同组攻击源。
        /// GizmoGridDrawer 只会悬停代表按钮；没有这一步，多武器入口只能预览第一把武器。
        /// </summary>
        public override void MergeWith(Gizmo other)
        {
            base.MergeWith(other);

            Command_BdpManualEntryTarget groupedCommand = other as Command_BdpManualEntryTarget;
            if (groupedCommand == null)
            {
                return;
            }

            AddGroupedTargetingSource(groupedCommand.targetingSource);
            foreach (AttackExecutionTargetingSource groupedSource in groupedCommand.groupedTargetingSources)
            {
                AddGroupedTargetingSource(groupedSource);
            }
        }

        /// <summary>
        /// 聚合命令不再继承组内其他成员的单独点击逻辑。
        /// 否则会在全局单例 Targeter 上反复覆盖 BeginTargeting 状态。
        /// </summary>
        public override bool InheritInteractionsFrom(Gizmo other)
        {
            return false;
        }

        /// <summary>
        /// 处理当前命令所在分组的统一点击逻辑。
        /// 单选时直接开启单体 targeting，多选聚合时开启组级 targeting。
        /// </summary>
        public override void ProcessGroupInput(Event ev, List<Gizmo> group)
        {
            List<AttackExecutionTargetingSource> groupedSources = CollectGroupedSources(group);
            if (groupedSources.Count == 0)
            {
                return;
            }

            if (groupedSources.Count == 1)
            {
                Find.Targeter.BeginTargeting(groupedSources[0]);
                return;
            }

            Find.Targeter.BeginTargeting(new GroupedAttackExecutionTargetingSource(groupedSources));
        }

        /// <summary>
        /// 从原版 gizmo grouping 结果里提取与当前入口同组的单体 targetingSource。
        /// </summary>
        private List<AttackExecutionTargetingSource> CollectGroupedSources(List<Gizmo> group)
        {
            List<AttackExecutionTargetingSource> groupedSources = new List<AttackExecutionTargetingSource>();
            if (group == null || group.Count == 0)
            {
                if (targetingSource != null)
                {
                    groupedSources.Add(targetingSource);
                }

                return groupedSources;
            }

            for (int i = 0; i < group.Count; i++)
            {
                Command_BdpManualEntryTarget groupedCommand = group[i] as Command_BdpManualEntryTarget;
                if (groupedCommand == null
                    || groupedCommand.targetingSource == null
                    || groupedSources.Contains(groupedCommand.targetingSource))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(manualEntryGroupId)
                    && !string.IsNullOrWhiteSpace(groupedCommand.manualEntryGroupId)
                    && groupedCommand.manualEntryGroupId != manualEntryGroupId)
                {
                    continue;
                }

                groupedSources.Add(groupedCommand.targetingSource);
            }

            if (groupedSources.Count == 0 && targetingSource != null)
            {
                groupedSources.Add(targetingSource);
            }

            return groupedSources;
        }

        /// <summary>
        /// 向悬停预览集合加入一个同组攻击源，并排除当前代表源与重复源。
        /// </summary>
        private void AddGroupedTargetingSource(AttackExecutionTargetingSource source)
        {
            if (source == null || source == targetingSource || groupedTargetingSources.Contains(source))
            {
                return;
            }

            groupedTargetingSources.Add(source);
        }
    }
}
