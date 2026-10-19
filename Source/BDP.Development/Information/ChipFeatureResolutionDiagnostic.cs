using System;
using System.Collections.Generic;
using BDP.Core.Information;
using BDP.Support.Diagnostics;
using LudeonTK;
using Verse;

namespace BDP.Development.Information
{
    /// <summary>Development 专用功能解析诊断入口，按模型摘要去重输出。</summary>
    public static class ChipFeatureResolutionDiagnostic
    {
        /// <summary>在开发者模式下从当前选中 Thing 捕获功能解析摘要。</summary>
        [DebugAction(
            "Mods",
            "BDP：诊断芯片功能语义",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing,
            displayPriority = -95)]
        public static void RunForSelectedThing()
        {
            Thing selectedThing = Find.Selector?.SingleSelectedThing;
            ChipInformationRequest request;
            if (!ChipInformationInstanceRequestAdapter.TryCreate(
                selectedThing,
                ChipInformationRuntimeInputAdapter.CreateStatic(),
                null,
                null,
                out request))
            {
                Log.Warning("[BDP诊断] 当前选中 Thing 没有可读取的芯片定义。");
                return;
            }

            ChipInformationModel model = ChipInformationSurfaceAccess.Read(request);
            WriteOnce(request, model);
            ChipFeatureRawFactReport report = ChipFeatureRawFactDiagnostic.Capture(model);
            for (int index = 0; index < report.Lines.Count; index++)
            {
                Log.Message("[BDP诊断] " + report.Lines[index]);
            }
        }

        /// <summary>输出解析诊断和统一节点事实；同一事实只输出一次。</summary>
        public static void WriteOnce(
            ChipInformationRequest request,
            ChipInformationModel model)
        {
            if (model == null)
            {
                return;
            }

            IReadOnlyList<ChipInformationDiagnostic> diagnostics =
                request == null
                    ? null
                    : ChipInformationSurfaceAccess.ReadDiagnostics(request);
            if (diagnostics != null)
            {
                for (int index = 0; index < diagnostics.Count; index++)
                {
                    ChipInformationDiagnostic diagnostic = diagnostics[index];
                    if (diagnostic != null)
                    {
                        BdpDiagnostics.Once(
                            "chip-feature-diagnostic:" + diagnostic.Code + ":" + diagnostic.BlockKey + ":" + diagnostic.FieldKey,
                            "芯片功能解析诊断 " + diagnostic.Code + "：" + diagnostic.Detail);
                    }
                }
            }

            ChipFeatureRawFactReport report = ChipFeatureRawFactDiagnostic.Capture(model);
            for (int index = 0; index < report.Lines.Count; index++)
            {
                BdpDiagnostics.Once(
                    "chip-feature-fact:" + report.Lines[index],
                    report.Lines[index]);
            }
        }
    }
}
