using BDP.Development.Diagnostics;
using BDP.Development.Trigger.Diagnostics;
using BDP.Development.FloatingFlight;
using BDP.Development.Information;
using BDP.Development.CombatAI;
using BDP.Content.FloatingFlight;
using BDP.Support.Diagnostics;
using BDP.Core.Trigger;
using HarmonyLib;
using Verse;

namespace BDP.Development
{
    /// <summary>
    /// 开发辅助程序集的唯一启动入口。
    /// 当前仅证明程序集可独立加载，具体开发设施由后续任务显式接入。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class DevelopmentBootstrap
    {
        /// <summary>
        /// Development 生命周期内唯一的日志接收器实例。
        /// </summary>
        private static readonly VerseLogDiagnosticSink DiagnosticSink = new VerseLogDiagnosticSink();

        /// <summary>
        /// 模组装载时由 RimWorld 自动运行。
        /// </summary>
        static DevelopmentBootstrap()
        {
            try
            {
                BdpDiagnosticSinkRegistry.Register(DiagnosticSink);
                BdpDiagnostics.BindAttackExecutionSwitchResolver(IsDeveloperModeEnabled);
                BdpDiagnostics.Once(
                    "development.diagnostics.sink_registered",
                    "Development 诊断接收器已注册；风刃流程诊断可用。"
                );
                BdpDiagnostics.Once(
                    "development.chip-feature-diagnostics.available",
                    "芯片功能语义诊断已注册；可从开发者菜单捕获声明、来源和裁定结果。"
                );
#if BDP_RIMWORLD_1_7
                // 朝向诊断本身仅在 1.7 存在，1.6 不注册不存在的入口。
                RangedDraftedFacingDiagnostics.Initialize();
#endif
                if (IsDeveloperModeEnabled())
                {
                    new Harmony("niwt.bdp.development").PatchAll();
                    TriggerExternalGizmoRegistry.Register(new TriggerVisualMarkerGizmoProvider());
                    FloatingFlightNonPlayerModeProviderRegistry.Register(
                        FloatingFlightNonPlayerModeProvider.Instance);
                }
            }
            catch
            {
                // 开发辅助设施失败不得阻断 Core 或 Content 的正式加载。
            }
        }

        /// <summary>
        /// 安全读取 RimWorld 开发者模式，初始化早期读取失败时按关闭处理。
        /// </summary>
        private static bool IsDeveloperModeEnabled()
        {
            try
            {
                return Prefs.DevMode;
            }
            catch
            {
                return false;
            }
        }
    }
}
