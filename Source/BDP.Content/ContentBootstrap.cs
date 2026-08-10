using HarmonyLib;
using BDP.Content.CombatBody.Escape;
using BDP.Content.CombatBody.Transform;
using BDP.Content.CombatBody.Wounds.Visuals;
using BDP.Content.Assembly.ChipManufacturing.Validation;
using BDP.Content.Assembly.ChipManufacturing.Recipe;
using BDP.Content.Trion.Talent;
using BDP.Content.Trigger.UI;
using BDP.Content.Trigger.UI.ChipModes;
using BDP.Core.CombatBody.External;
using BDP.Core.CombatBody.Presentation;
using BDP.Core.CombatBody.Wounds.Presentation;
using BDP.Core.Trigger;
using BDP.Core.Trion.External;
using Verse;

namespace BDP.Content
{
    /// <summary>
    /// 内容程序集的正式启动入口。
    /// 只负责补丁扫描与已确认内容提供器的正式接线。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ContentBootstrap
    {
        /// <summary>
        /// 模组装载时自动运行。
        /// 不承载具体业务判断，避免启动代码膨胀。
        /// </summary>
        static ContentBootstrap()
        {
            new Harmony("niwt.bdp.content").PatchAll();
            // 启动期一次性建立扫描裁切网格，避免首次战斗体变换承担 Unity 网格创建开销。
            CombatBodyScanMeshCache.WarmUp();
            PawnTrionTalentAssessmentInjector.Apply();
            PawnCombatBodyEmergencyEscapeStateInjector.Apply();
            ChipManufacturingDefValidator.ValidateAll();
            ChipRecipeIngredientUniverse.InitializeAll();
            CombatBodyCollapseExtensionRegistry.Register(new CombatBodyEmergencyEscapeExtensionProvider());
            CombatBodyTransformPresentationRegistry.Register(new CombatBodyTransformScanPresentationProvider());
            CombatBodyWoundPresentationRegistry.Register(new CombatBodyWoundSprayPresentationProvider());
            TrionGizmoExtensionRegistry.RegisterPanel(new TriggerLoadoutPanelProvider());
            TriggerExternalGizmoRegistry.Register(new ChipModeGizmoProvider());
            TrionGizmoExtensionRegistry.Register(new CombatBodyEmergencyEscapeGizmoExtensionProvider());
        }
    }
}
