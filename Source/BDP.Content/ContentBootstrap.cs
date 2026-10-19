using HarmonyLib;
using BDP.Core.CombatLog;
using BDP.Content.PlayerLog;
using BDP.Content.PlayerLog.CombatBody;
using BDP.Core.AttackExecution;
using BDP.Content.CombatBody.Apparel;
using BDP.Content.CombatBody.Appearance;
using BDP.Content.CombatBody.Escape;
using BDP.Content.CombatBody.Transform;
using BDP.Content.CombatBody.Wounds.Visuals;
using BDP.Content.Assembly.ChipManufacturing.Validation;
using BDP.Content.Assembly.ChipManufacturing.Recipe;
using BDP.Content.Assembly.ChipManufacturing.Production;
using BDP.Content.Assembly.ChipManufacturing.Resolution;
using BDP.Content.Trion.Talent;
using BDP.Content.Trigger.UI;
using BDP.Content.Trigger.UI.ChipModes;
using BDP.Content.Trigger.UI.ChipStances;
using BDP.Content.FloatingFlight.UI;
using BDP.Content.Shield.UI;
using BDP.Content.FloatingFlight;
using BDP.Content.Trigger.Black;
using BDP.Content.Trigger.Black.Formation;
using BDP.Core.CombatBody.External;
using BDP.Core.CombatBody.Presentation;
using BDP.Core.CombatBody.Wounds.Presentation;
using BDP.Core.Trigger;
using BDP.Content.Trigger.Classification;
using BDP.Content.Trigger.NonPlayer;
using BDP.Core.Trion.External;
using BDP.Core.Trion;
using BDP.Core.Trion.Capacity;
using BDP.Core.Trigger.Runtime;
using BDP.Core.Expressions.External;
using BDP.Core.Information;
using BDP.Core.Projectiles.RangedFlightProtocol.Effects;
using BDP.Content.RangedModules.Debuff;
using BDP.Content.RangedModules.Debuff.Effects;
using BDP.Content.RangedModules.ImpactVisual;
using BDP.Content.HitPartPolicy;
using BDP.Content.Information;
using BDP.Content.Trion.Facility;
using BDP.Content.Trion.Resource;
using BDP.Content.OperatorConsole;
using BDP.Core.Permissions;
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
        /// Content 对 Core 恢复许可的唯一正式提供器实例。
        /// </summary>
        private static readonly TrionRecoveryFacilityGate TrionRecoveryGate =
            new TrionRecoveryFacilityGate();

        /// <summary>
        /// 黑触发器容量业务的唯一正式提供器实例。
        /// </summary>
        private static readonly BlackTriggerTrionCapacityBinding BlackTriggerTrionCapacity =
            new BlackTriggerTrionCapacityBinding();

        /// <summary>远程目标直视许可业务接收器实例。</summary>
        private static readonly DirectTargetLineOfSightPermissionReceiver DirectTargetLineOfSightReceiver =
            new DirectTargetLineOfSightPermissionReceiver();

        /// <summary>浮空自身许可的远程间接攻击接收器实例。</summary>
        private static readonly FloatingFlightIndirectAttackPermissionReceiver FloatingFlightIndirectAttackReceiver =
            new FloatingFlightIndirectAttackPermissionReceiver();

        /// <summary>
        /// Content 命中部位策略解析器适配器实例。
        /// </summary>
        private static readonly HitPartPolicyResolverAdapter HitPartPolicyResolver =
            new HitPartPolicyResolverAdapter();

        /// <summary>
        /// Content 个人服装模板的 Core 外观提供器实例。
        /// </summary>
        private static readonly CombatBodyAppearanceProvider CombatBodyAppearanceProvider =
            new CombatBodyAppearanceProvider();

        /// <summary>
        /// 模组装载时自动运行。
        /// 不承载具体业务判断，避免启动代码膨胀。
        /// </summary>
        static ContentBootstrap()
        {
            BattleLogEntryFactoryAccess.Register(new PlayerBattleLogEntryFactory());
            new Harmony("niwt.bdp.content").PatchAll();
            AttackAdmissionAccess.Register(new BDP.Content.LightSoul.LightSoulDefenseAttackAdmissionRule());
            AttackAdmissionAccess.Register(new BDP.Content.Trion.Units.Patches.TrionUnitAttackAdmissionRule());
            TrionCapacityContributorRegistry.Register(BlackTriggerTrionCapacity);
            TriggerTrionCapacityBindingRegistry.Register(BlackTriggerTrionCapacity);
            TrionRecoveryGateRegistry.Register(TrionRecoveryGate);
            TrionResourceRuleRegistry.Register(new NonPlayerTrionResourceRuleProvider());
            TriggerSwitchDelayPolicyRegistry.Register(new NonPlayerTriggerSwitchDelayPolicy());
            ChipFunctionPermissionReceiverRegistry.Register(DirectTargetLineOfSightReceiver);
            DirectTargetLineOfSightPermissionResolverRegistry.Register(DirectTargetLineOfSightReceiver);
            IndirectRangedAttackPermissionResolverRegistry.Register(FloatingFlightIndirectAttackReceiver);
            IndirectRangedAttackPermissionResolverRegistry.Register(DirectTargetLineOfSightReceiver);
            GroundTargetingPermissionResolverRegistry.Register(DirectTargetLineOfSightReceiver);
            HitPartPolicyResolverRegistry.Register(HitPartPolicyResolver);
            TriggerFixedLoadoutFactoryRegistry.Register(
                new ManufacturedChipTriggerFixedLoadoutFactory());
            ExtraEffectPlanExecutorRegistry.TryRegister(new RangedDebuffEffectExecutor());
            ExtraEffectPlanExecutorRegistry.TryRegister(new ImpactVisualEffectExecutor());
            // 启动期一次性建立扫描裁切网格，避免首次战斗体变换承担 Unity 网格创建开销。
            CombatBodyScanMeshCache.WarmUp();
            PawnTrionTalentAssessmentInjector.Apply();
            PawnCombatBodyEmergencyEscapeStateInjector.Apply();
            PawnCombatBodyPlayerLogInjector.Apply();
            BDP.Content.Combos.TomahawkProjectileDefinitionResolver.Resolve();
            ChipManufacturingDefValidator.ValidateAll();
            BlackTriggerFormationDefValidator.ValidateAll();
            ChipRecipeIngredientUniverse.InitializeAll();
            ComboExpressionVariantModifierRegistry.Register(
                new ChipArmamentFormComboExpressionModifier());
            TriggerChipActivationControlRegistry.Register(
                new AccessoryActivationControlProvider());
            CombatBodyFrontApparelRegistry.Register(new CombatBodyFrontApparelPolicy());
            CombatBodyFrontAppearanceRegistry.Register(CombatBodyAppearanceProvider);
            CombatBodyCollapseExtensionRegistry.Register(new CombatBodyEmergencyEscapeExtensionProvider());
            CombatBodyTransformPresentationRegistry.Register(new CombatBodyTransformScanPresentationProvider());
            CombatBodyWoundPresentationRegistry.Register(new CombatBodyWoundSprayPresentationProvider());
            TrionGizmoExtensionRegistry.RegisterPanel(new TriggerLoadoutPanelProvider());
            TriggerExternalGizmoRegistry.Register(new ChipModeGizmoProvider());
            TriggerExternalGizmoRegistry.Register(new ChipStanceGizmoProvider());
            TriggerExternalGizmoRegistry.Register(new FloatingFlightGizmoProvider());
            TriggerExternalGizmoRegistry.Register(new EnergyShieldStatusGizmoProvider());
            TrionGizmoExtensionRegistry.Register(new CombatBodyEmergencyEscapeGizmoExtensionProvider());
        }
    }
}
