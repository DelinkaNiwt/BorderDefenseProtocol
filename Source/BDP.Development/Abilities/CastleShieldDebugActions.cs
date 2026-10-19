using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BDP.Content.Abilities;
using BDP.Content.Assembly.ChipManufacturing.Defs;
using BDP.Content.Assembly.ChipManufacturing.Production;
using BDP.Core.Abilities;
using BDP.Core.Chips;
using BDP.Core.Trion;
using LudeonTK;
using RimWorld;
using Verse;

namespace BDP.Development.Abilities
{
    /// <summary>护城盾真实芯片的可移除开发入口；不绕过正式装载或直接授予能力。</summary>
    public static class CastleShieldDebugActions
    {
        /// <summary>同角色同定义共用的测试能力身份。</summary>
        internal const string AbilityDefName = "BDP_Ability_CastleShield";
        /// <summary>现有芯片工厂读取的单功能预设身份。</summary>
        private const string FunctionDefName = "BDP_ChipFunction_CastleShield";

        /// <summary>全部玩家可见菜单文案读取开发语言包。</summary>
        [DebugAction("BDP", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static DebugActionNode CastleShieldMenu()
        {
            var root = new DebugActionNode("BDP_Dev_CastleShield_Menu".Translate());
            root.AddChild(new DebugActionNode("BDP_Dev_CastleShield_Spawn".Translate(), action: () => Selected(SpawnChip)));
            root.AddChild(new DebugActionNode("BDP_Dev_CastleShield_Validate".Translate(), action: ValidateDefinitions));
            root.AddChild(new DebugActionNode("BDP_Dev_CastleShield_Output".Translate(), action: () => Selected(Output)));
            root.AddChild(new DebugActionNode("BDP_Dev_CastleShield_PlacementTool".Translate(), DebugActionType.ToolMap,
                () => Selected(pawn => ComparePlacement(pawn, Verse.UI.MouseCell()))));
            root.AddChild(new DebugActionNode("BDP_Dev_CastleShield_Observe".Translate(), action: () => Selected(pawn =>
            {
                var observer = pawn.Map.GetComponent<MapComponent_CastleShieldDiagnostics>();
                observer.Enabled = !observer.Enabled;
                Log.Message((observer.Enabled ? "BDP_Dev_CastleShield_ObserveOn" : "BDP_Dev_CastleShield_ObserveOff").Translate());
            })));
            return root;
        }

        /// <summary>操作仅针对地图上明确选中的人物。</summary>
        private static void Selected(Action<Pawn> action)
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null || !pawn.Spawned)
            {
                Messages.Message("BDP_Dev_CastleShield_SelectPawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            action(pawn);
        }

        /// <summary>走已有动态芯片工厂；返回物品后由玩家按正常流程装入触发体。</summary>
        private static void SpawnChip(Pawn pawn)
        {
            Thing chip;
            string reason;
            if (!ChipProductCreationService.TryCreateSingleFunctionProduct(FunctionDefName, out chip, out reason))
            {
                Messages.Message("BDP_Dev_CastleShield_CreateFailed".Translate(reason), MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!GenPlace.TryPlaceThing(chip, pawn.Position, pawn.Map, ThingPlaceMode.Near))
            {
                if (!chip.Destroyed) chip.Destroy(DestroyMode.Vanish);
                Messages.Message("BDP_Dev_CastleShield_PlaceFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            Messages.Message("BDP_Dev_CastleShield_Created".Translate(), chip, MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>检查游戏实际加载后的定义与跨定义引用，不用文件文本假装运行时验证。</summary>
        private static void ValidateDefinitions()
        {
            AbilityDef ability = DefDatabase<AbilityDef>.GetNamedSilentFail(AbilityDefName);
            var function = DefDatabase<ChipManufacturingPresetDef>.GetNamedSilentFail(FunctionDefName);
            var errors = new List<string>();
            if (ability == null || ability.abilityClass != typeof(BdpBatchAbility)
                || ability.gizmoClass != typeof(Command_BdpBatchAbility)
                || ability.verbProperties?.verbClass != typeof(BdpVerb_CastBatchAbility)
                || ability.charges != 10 || !ability.cooldownPerCharge
                || ability.cooldownTicksRange.min != 600 || ability.cooldownTicksRange.max != 600
                || ability.verbProperties.range != 15f || ability.verbProperties.warmupTime != 1f
                || !ability.verbProperties.requireLineOfSight || ability.verbProperties.burstShotCount != 1
                || ability.verbProperties.defaultCooldownTime != 0f
                || ability.GetModExtension<BdpBatchTargetingExtension>()?.skipInvalidInteriorTargets != true
                || ability.groupDef != null || ability.waitForJobEnd || ability.targetWorldCell
                || (ability.jobDef != null && ability.jobDef != JobDefOf.CastAbilityOnThing))
                errors.Add("BDP_Dev_CastleShield_InvalidAbility".Translate());
            var buildings = ability?.comps?.OfType<CompProperties_AbilityEffect_BatchBuildingSpawn>().ToList();
            var building = buildings?.FirstOrDefault();
            if (buildings?.Count != 1 || building?.buildingDef?.defName != "Wall" || building.stuffDef?.defName != "Plasteel"
                || building.buildingDef.size.x != 1 || building.buildingDef.size.z != 1
                || !building.stuffDef.stuffProps.CanMake(building.buildingDef))
                errors.Add("BDP_Dev_CastleShield_InvalidBuilding".Translate());
            if (ability?.comps == null
                || !ability.comps.OfType<CompProperties_AbilityEffect_BdpExpressionLifecycle>().Any(p => p.retainWhenInactive)
                || !ability.comps.OfType<CompProperties_AbilityEffect_BdpTrionCost>().Any()
                || !ability.comps.OfType<CompProperties_CastleShieldPresentation>().Any())
                errors.Add("BDP_Dev_CastleShield_InvalidComponents".Translate());
            var entries = function?.config?.Expression?.Entries;
            var entry = entries?.FirstOrDefault();
            if (function == null || !function.showInManufacturing
                || function.config?.Profile?.Category?.defName != "BDP_ChipCategory_Defense"
                || function.config?.Loadout?.SlotRegion != ChipSlotRegion.MainSub
                || function.config.Loadout.SlotOccupancy != ChipSlotOccupancy.Single
                || function.config.Loadout.ActivationDelayTicks != 60 || function.config.Loadout.DeactivationDelayTicks != 30
                || function.config.Trion?.CapacityCost != 50f || function.config.Trion.ActivationCost != 15f
                || entries?.Count != 1 || entry?.AbilityDefName != AbilityDefName || entry.Trion?.UseCost != 20f)
                errors.Add("BDP_Dev_CastleShield_InvalidFunction".Translate());
            Log.Message(errors.Count == 0 ? "BDP_Dev_CastleShield_ConfigValid".Translate().ToString()
                : "BDP_Dev_CastleShield_ConfigInvalid".Translate(string.Join("\n", errors)).ToString());
        }

        /// <summary>手动快照便于比较支付、充能、开关和读档前后。</summary>
        private static void Output(Pawn pawn) => Log.Message(BuildSnapshot(pawn));

        /// <summary>点击地图格只读对照原版、建筑效果和能力目标判定，区分占用与射程／视线问题。</summary>
        private static void ComparePlacement(Pawn pawn, IntVec3 cell)
        {
            BdpBatchAbility ability = FindAbility(pawn);
            var properties = ability?.def?.comps?.OfType<CompProperties_AbilityEffect_BatchBuildingSpawn>().FirstOrDefault();
            var effect = ability?.CompOfType<CompAbilityEffect_BatchBuildingSpawn>();
            if (properties == null || effect == null || !cell.InBounds(pawn.Map))
            {
                Messages.Message("BDP_Dev_CastleShield_NoAbility".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            var target = new LocalTargetInfo(cell);
            AcceptanceReport native = GenConstruct.CanPlaceBlueprintAt(properties.buildingDef, cell,
                Rot4.North, pawn.Map, false, null, null, properties.stuffDef);
            string occupants = string.Join("，", cell.GetThingList(pawn.Map).Select(thing => thing.LabelCap + " (" + thing.def.defName + ")"));
            if (occupants.Length == 0) occupants = "BDP_Dev_CastleShield_NoOccupants".Translate();
            Log.Message("BDP_Dev_CastleShield_PlacementReport".Translate(cell,
                native.Accepted ? "BDP_Dev_CastleShield_PlacementAllowed".Translate()
                    : "BDP_Dev_CastleShield_PlacementDenied".Translate(native.Reason),
                (effect.Valid(target) ? "BDP_Dev_CastleShield_PlacementAllowed" : "BDP_Dev_CastleShield_PlacementRejected").Translate(),
                (ability.verb.ValidateTarget(target, false) ? "BDP_Dev_CastleShield_PlacementAllowed" : "BDP_Dev_CastleShield_PlacementRejected").Translate(),
                occupants));
        }

        /// <summary>读取保留的原版能力对象，包括来源关闭时的次数与计时。</summary>
        internal static BdpBatchAbility FindAbility(Pawn pawn)
        {
            AbilityDef def = DefDatabase<AbilityDef>.GetNamedSilentFail(AbilityDefName);
            return def != null ? pawn.abilities?.GetAbility(def, true) as BdpBatchAbility : null;
        }

        /// <summary>只展示原版与正式资源读面，不修改任何玩法状态。</summary>
        internal static string BuildSnapshot(Pawn pawn)
        {
            BdpBatchAbility ability = FindAbility(pawn);
            ITrionReader reader = TrionSurfaceAccess.ResolveReader(pawn);
            string text = "BDP_Dev_CastleShield_Header".Translate(pawn.LabelShortCap, pawn.ThingID, Find.TickManager.TicksGame);
            text += "\n" + "BDP_Dev_CastleShield_Resource".Translate(Number(reader?.Cur ?? 0f), Number(reader?.Available ?? 0f),
                Number(reader?.Allocated ?? 0f), Number(reader?.Reserved ?? 0f),
                (TrionResourceRuleRegistry.GetRules(pawn).FunctionCostsEnabled
                    ? "BDP_Dev_CastleShield_CostsOn" : "BDP_Dev_CastleShield_CostsOff").Translate());
            if (ability == null) return text + "\n" + "BDP_Dev_CastleShield_NoAbility".Translate();
            bool active = ability.CompOfType<CompAbilityEffect_BdpExpressionLifecycle>()?.SourceActive == true;
            text += "\n" + "BDP_Dev_CastleShield_Ability".Translate(ability.Id,
                (active ? "BDP_Dev_CastleShield_SourceOn" : "BDP_Dev_CastleShield_SourceOff").Translate(),
                ability.RemainingCharges, ability.maxCharges, Number(ability.CooldownTicksRemaining / 60f),
                ability.CurrentBatchTargets.Count, ability.lastCastTick);
            int walls = pawn.Map.listerThings.AllThings.Count(thing => thing.def.defName == "Wall" && thing.Stuff?.defName == "Plasteel");
            return text + "\n" + "BDP_Dev_CastleShield_Walls".Translate(walls);
        }

        /// <summary>统一小数格式，方便人工比较日志。</summary>
        private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    /// <summary>显式开启后每秒观察一次选中人物，只在能力状态改变时输出；不保存到存档。</summary>
    public sealed class MapComponent_CastleShieldDiagnostics : MapComponent
    {
        /// <summary>默认关闭，可随开发目录一起移除。</summary>
        public bool Enabled;
        /// <summary>最近一次被观察的角色。</summary>
        private Pawn lastPawn;
        /// <summary>排除连续倒计时的变化键，避免每秒刷同一恢复状态。</summary>
        private string lastState;
        /// <summary>原版地图创建入口。</summary>
        public MapComponent_CastleShieldDiagnostics(Map map) : base(map) { }

        /// <summary>仅在开发模式、开关开启且选择同地图角色时读取。</summary>
        public override void MapComponentTick()
        {
            if (!Prefs.DevMode || !Enabled || Find.TickManager.TicksGame % 60 != 0) return;
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null || pawn.Map != map) { lastPawn = null; lastState = null; return; }
            BdpBatchAbility ability = CastleShieldDebugActions.FindAbility(pawn);
            string state = ability == null ? "none" : ability.Id + "/" + ability.RemainingCharges + "/"
                + ability.CompOfType<CompAbilityEffect_BdpExpressionLifecycle>()?.SourceActive + "/"
                + ability.CurrentBatchTargets.Count + "/" + ability.OnCooldown + "/" + ability.lastCastTick;
            if (pawn == lastPawn && state == lastState) return;
            lastPawn = pawn;
            lastState = state;
            Log.Message(CastleShieldDebugActions.BuildSnapshot(pawn));
        }
    }
}
