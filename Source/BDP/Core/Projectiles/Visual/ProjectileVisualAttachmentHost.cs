using System;
using System.Collections.Generic;
using BDP.Support.Diagnostics;
using Verse;

namespace BDP.Core.Projectiles.Visual
{
    /// <summary>
    /// 投射物视觉附加宿主。
    /// 它负责扫描投射物定义或来源定义上的视觉附加提供器，并按生命周期顺序广播视觉事件。
    /// </summary>
    internal sealed class ProjectileVisualAttachmentHost
    {
        /// <summary>
        /// 当前投射物实例绑定的全部视觉附加件。
        /// </summary>
        private readonly List<IProjectileVisualAttachment> attachments = new List<IProjectileVisualAttachment>();

        /// <summary>
        /// 当前宿主正在服务的投射物定义名称。
        /// 它只用于诊断输出，不参与业务逻辑。
        /// </summary>
        private string projectileDefName;

        /// <summary>
        /// 用当前投射物定义与可选来源定义重新初始化视觉附加件集合。
        /// </summary>
        /// <param name="projectileDef">当前投射物定义。</param>
        /// <param name="visualAttachmentProviderDefs">当前投射物优先消费的来源视觉提供器定义集合。</param>
        /// <param name="visualAppearanceOverrides">当前投射物冻结的可选视觉外观覆盖。</param>
        internal void Initialize(
            ThingDef projectileDef,
            IReadOnlyList<ThingDef> visualAttachmentProviderDefs = null,
            ProjectileVisualAppearanceOverrides visualAppearanceOverrides = null)
        {
            attachments.Clear();
            projectileDefName = projectileDef != null ? projectileDef.defName : null;
            int sourceAttachmentCount = TryInitializeFromProviderDefs(
                visualAttachmentProviderDefs,
                visualAppearanceOverrides);
            if (sourceAttachmentCount > 0)
            {
                return;
            }

            TryInitializeFromDef(projectileDef, visualAppearanceOverrides);
        }

        /// <summary>
        /// 按顺序从来源定义集合创建视觉附加件。
        /// 只要成功创建过至少一个附加件，来源定义就覆盖投射物默认定义。
        /// </summary>
        /// <param name="providerDefs">当前来源定义集合。</param>
        /// <returns>成功创建的视觉附加件数量。</returns>
        private int TryInitializeFromProviderDefs(
            IReadOnlyList<ThingDef> providerDefs,
            ProjectileVisualAppearanceOverrides visualAppearanceOverrides)
        {
            if (providerDefs == null)
            {
                return 0;
            }

            int createdCount = 0;
            for (int i = 0; i < providerDefs.Count; i++)
            {
                createdCount += TryInitializeFromDef(providerDefs[i], visualAppearanceOverrides);
            }

            return createdCount;
        }

        /// <summary>
        /// 从一个定义的扩展列表创建视觉附加件。
        /// </summary>
        /// <param name="providerDef">当前提供视觉附加件的定义。</param>
        /// <returns>成功创建的视觉附加件数量。</returns>
        private int TryInitializeFromDef(
            ThingDef providerDef,
            ProjectileVisualAppearanceOverrides visualAppearanceOverrides)
        {
            if (providerDef == null || providerDef.modExtensions == null)
            {
                return 0;
            }

            int createdCount = 0;
            for (int i = 0; i < providerDef.modExtensions.Count; i++)
            {
                if (!(providerDef.modExtensions[i] is IProjectileVisualAttachmentProvider provider))
                {
                    continue;
                }

                try
                {
                    IProjectileVisualAttachment attachment = provider.CreateAttachment(visualAppearanceOverrides);
                    if (attachment != null)
                    {
                        attachments.Add(attachment);
                        createdCount++;
                    }
                }
                catch (Exception exception)
                {
                    LogAttachmentFailure("initialize", i, exception);
                }
            }

            return createdCount;
        }

        /// <summary>
        /// 清空当前投射物的视觉附加件集合。
        /// </summary>
        internal void Clear()
        {
            attachments.Clear();
            projectileDefName = null;
        }

        /// <summary>
        /// 向全部视觉附加件广播发射事件。
        /// </summary>
        /// <param name="context">当前发射上下文。</param>
        internal void NotifyLaunch(in ProjectileVisualLaunchContext context)
        {
            for (int i = 0; i < attachments.Count; i++)
            {
                try
                {
                    attachments[i].OnLaunch(in context);
                }
                catch (Exception exception)
                {
                    LogAttachmentFailure("launch", i, exception);
                }
            }
        }

        /// <summary>
        /// 向全部视觉附加件广播飞行样本事件。
        /// </summary>
        /// <param name="context">当前飞行样本上下文。</param>
        internal void NotifyFlightSample(in ProjectileVisualFlightSampleContext context)
        {
            for (int i = 0; i < attachments.Count; i++)
            {
                try
                {
                    attachments[i].OnFlightSample(in context);
                }
                catch (Exception exception)
                {
                    LogAttachmentFailure("flight_sample", i, exception);
                }
            }
        }

        /// <summary>
        /// 向全部视觉附加件广播读档恢复事件。
        /// </summary>
        /// <param name="context">当前恢复上下文。</param>
        internal void NotifyRestored(in ProjectileVisualRestoreContext context)
        {
            for (int i = 0; i < attachments.Count; i++)
            {
                try
                {
                    attachments[i].OnRestored(in context);
                }
                catch (Exception exception)
                {
                    LogAttachmentFailure("restored", i, exception);
                }
            }
        }

        /// <summary>
        /// 向全部视觉附加件广播结束事件。
        /// </summary>
        /// <param name="context">当前结束上下文。</param>
        internal void NotifyTerminate(in ProjectileVisualTerminateContext context)
        {
            for (int i = 0; i < attachments.Count; i++)
            {
                try
                {
                    attachments[i].OnTerminate(in context);
                }
                catch (Exception exception)
                {
                    LogAttachmentFailure("terminate", i, exception);
                }
            }
        }

        /// <summary>
        /// 记录某个视觉附加件的执行失败。
        /// 它只写节流诊断，不允许把异常继续打穿主线逻辑。
        /// </summary>
        /// <param name="stage">当前失败阶段。</param>
        /// <param name="attachmentIndex">当前失败附加件索引。</param>
        /// <param name="exception">当前异常对象。</param>
        private void LogAttachmentFailure(string stage, int attachmentIndex, Exception exception)
        {
            string safeDefName = SafeDiagnosticId(projectileDefName);
            string key = "projectile.visual_attachment.error."
                + stage
                + "."
                + safeDefName
                + "."
                + attachmentIndex;
            BdpDiagnostics.Throttled(
                key,
                "投射物视觉附加件执行失败。stage=" + stage
                + ", projectileDef=" + safeDefName
                + ", index=" + attachmentIndex
                + ", error=" + (exception != null ? exception.Message : "<null>"),
                60);
        }

        /// <summary>
        /// 规避空诊断标识，避免拼接出的节流 key 失真。
        /// </summary>
        /// <param name="value">待规避的原始标识。</param>
        /// <returns>可安全用于诊断的标识文本。</returns>
        private static string SafeDiagnosticId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value;
        }
    }
}
