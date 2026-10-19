using System;
using System.Collections.Generic;
using System.Linq;
using BDP.Content.Royal.Quest;
using BDP.Core.CombatBody;
using BDP.Core.Genes;
using BDP.Core.Trigger;
using BDP.Core.Trion;
using LudeonTK;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BDP.Development.CombatBody.HostileResource
{
    /// <summary>敌方公共资源实测入口，可随整个开发程序集移除。</summary>
    public static class HostileTrionDebugActions
    {
        /// <summary>原型测试直接装备任务入门原型，复用其固定预载，不另发一把战利品。</summary>
        private const string PrototypeDefName = "BDP_TriggerBody_NeighborPrototype";

        /// <summary>原型没有特殊槽，护盾公共测试另用标准触发体。</summary>
        private const string TriggerDefName = "BDP_TriggerBody_BorderStandard";

        /// <summary>复用正式完整芯片成品工厂。</summary>
        private const string ChipDefName = "BDP_Chip_Custom";

        /// <summary>提供本地化的原版调试菜单节点；属性只承担分类和准入。</summary>
        [DebugAction("BDP", requiresBiotech = true, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static DebugActionNode ResourceTestMenu()
        {
            var root = new DebugActionNode("BDP_Dev_HostileTrion_Menu".Translate());
            root.AddChild(new DebugActionNode("BDP_Dev_HostileTrion_Spawn".Translate(),
                DebugActionType.ToolMap, () => SpawnAtMouse(false)));
            root.AddChild(new DebugActionNode("BDP_Dev_HostileTrion_SpawnShield".Translate(),
                DebugActionType.ToolMap, () => SpawnAtMouse(true)));
            root.AddChild(new DebugActionNode("BDP_Dev_HostileTrion_SpawnPursuer".Translate(),
                DebugActionType.ToolMap, () => SpawnAtMouse(false, true)));
            root.AddChild(new DebugActionNode("BDP_Dev_HostileTrion_Output".Translate(),
                action: () => WithSelectedPawn(MapComponent_HostileTrionDiagnostics.Output)));
            root.AddChild(new DebugActionNode("BDP_Dev_HostileTrion_Observe".Translate(),
                action: ToggleObservation));
            root.AddChild(new DebugActionNode("BDP_Dev_HostileTrion_Full".Translate(),
                action: () => SetSelectedResource(1f)));
            root.AddChild(new DebugActionNode("BDP_Dev_HostileTrion_Low".Translate(),
                action: () => SetSelectedResource(0.01f)));
            root.AddChild(new DebugActionNode("BDP_Dev_HostileTrion_Zero".Translate(),
                action: () => SetSelectedResource(0f)));
            root.AddChild(new DebugActionNode("BDP_Dev_HostileTrion_Wound".Translate(),
                action: () => WithSelectedPawn(ApplyTestWound)));
            return root;
        }

        /// <summary>先在地图外完成正式会话，再让人物首次以战斗体状态出现。</summary>
        private static void SpawnAtMouse(bool withShield, bool asPursuer = false)
        {
            Map map = Find.CurrentMap;
            IntVec3 cell = Verse.UI.MouseCell();
            if (map == null || !cell.InBounds(map) || !cell.Standable(map)
                || cell.GetThingList(map).Any(thing => thing is Pawn))
            {
                Reject("BDP_Dev_HostileTrion_BadCell");
                return;
            }

            Faction faction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(candidate =>
                !candidate.IsPlayer && candidate.def.humanlikeFaction && !candidate.defeated
                && candidate.HostileTo(Faction.OfPlayer));
            if (faction == null)
            {
                Reject("BDP_Dev_HostileTrion_NoFaction");
                return;
            }

            Pawn pawn = null;
            ThingWithComps trigger = null;
            try
            {
                GeneDef gland = DefDatabase<GeneDef>.GetNamed("BDP_Gene_TrionGland");
                var request = new PawnGenerationRequest(PawnKindDefOf.Colonist, faction,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: true, fixedBiologicalAge: 25f, dontGiveWeapon: true,
                    forcedEndogenes: new List<GeneDef> { gland });
                pawn = PawnGenerator.GeneratePawn(request);
                if (!TrionGlandEligibility.HasActiveTrionGland(pawn))
                    throw new InvalidOperationException("gland_unavailable");

                CompTrion resource = pawn.GetComp<CompTrion>();
                if (resource == null) throw new InvalidOperationException("resource_missing");
                // 此组件的初始化无地图依赖；先建立真实首次资源，再激活战斗体。
                // 正式落地图时 initialized 已为真，不会重置冻结、余量及占用。
                resource.PostSpawnSetup(false);
                trigger = (ThingWithComps)ThingMaker.MakeThing(
                    DefDatabase<ThingDef>.GetNamed(withShield ? TriggerDefName : PrototypeDefName));
                pawn.equipment.AddEquipment(trigger);
                if (withShield)
                {
                    ITriggerLoadoutCommands loadout = TriggerSurfaceAccess.ResolveLoadoutCommands(pawn);
                    LoadChip(loadout, TriggerSide.Main, "BDP_ChipFunction_Kogetsu");
                    LoadChip(loadout, TriggerSide.Sub, "BDP_ChipFunction_Asteroid");
                    LoadChip(loadout, TriggerSide.Special, "BDP_ChipFunction_EnergyShield_Auto");
                }

                ICombatBodyCommands body = CombatBodySurfaceAccess.ResolveCommands(pawn);
                if (body == null || !body.TryActivate()
                    || CombatBodySurfaceAccess.ResolveReader(pawn)?.Phase != CombatBodyPhase.Active)
                    throw new InvalidOperationException("activation_failed");

                // 在这一行之前人物从未落地图，原身快照已由正式激活事务捕获。
                GenSpawn.Spawn(pawn, cell, map);
                // 公共资源样本仍用普通队伍；专用追兵样本验证正式的崩解保留身份逻辑。
                LordJob job = asPursuer ? (LordJob)new LordJob_RoyalPursuers(faction)
                    : new LordJob_AssaultColony(faction,
                        canKidnap: false, canTimeoutOrFlee: false, canSteal: false);
                LordMaker.MakeNewLord(faction, job, map, new[] { pawn });
                Find.Selector.ClearSelection();
                Find.Selector.Select(pawn);
                MapComponent_HostileTrionDiagnostics.Output(pawn);
                Messages.Message("BDP_Dev_HostileTrion_Spawned".Translate(pawn.LabelShortCap),
                    pawn, MessageTypeDefOf.PositiveEvent, false);
            }
            catch (Exception exception)
            {
                // 只清理本入口新建对象，不改任何既有人物或派系关系。
                if (pawn != null && !pawn.Destroyed) pawn.Destroy(DestroyMode.Vanish);
                if (trigger != null && !trigger.Destroyed) trigger.Destroy(DestroyMode.Vanish);
                Log.Error("BDP_Dev_HostileTrion_Failed".Translate(exception.ToString()));
            }
        }

        /// <summary>按正式功能预设创建完整实体芯片，失败时回收未装入的成品。</summary>
        private static void LoadChip(ITriggerLoadoutCommands loadout, TriggerSide side, string preset)
        {
            Thing chip;
            if (loadout == null || !TriggerFixedLoadoutFactoryRegistry.TryCreate(
                DefDatabase<ThingDef>.GetNamed(ChipDefName), preset, out chip))
                throw new InvalidOperationException("chip_creation_failed:" + preset);
            if (!loadout.TryLoadChip(side, 0, chip))
            {
                if (!chip.Destroyed) chip.Destroy(DestroyMode.Vanish);
                throw new InvalidOperationException("chip_load_failed:" + preset);
            }
        }

        /// <summary>读取玩家当前选中的真实资源宿主，兼容普通殖民者对照测试。</summary>
        private static void WithSelectedPawn(Action<Pawn> action)
        {
            Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null || TrionSurfaceAccess.ResolveReader(pawn) == null)
            {
                Reject("BDP_Dev_HostileTrion_NoPawn");
                return;
            }
            action(pawn);
        }

        /// <summary>通过既有调试写入口设置比例；不改容量或芯片占用参数。</summary>
        private static void SetSelectedResource(float fraction)
        {
            WithSelectedPawn(pawn =>
            {
                ITrionReader reader = TrionSurfaceAccess.ResolveReader(pawn);
                TrionCurrentWriteResult result = TrionSurfaceAccess.ResolveCommands(pawn)
                    .TrySetCurrent(reader.Max * fraction);
                if (!result.Succeeded || result.WasClamped)
                    Messages.Message(result.Message ?? "BDP_Dev_HostileTrion_WriteRejected".Translate(),
                        MessageTypeDefOf.RejectInput, false);
                MapComponent_HostileTrionDiagnostics.Output(pawn);
            });
        }

        /// <summary>切换当前地图的选中人物观测；关闭后不持续采样，读档默认关闭。</summary>
        private static void ToggleObservation()
        {
            MapComponent_HostileTrionDiagnostics observer = Find.CurrentMap
                .GetComponent<MapComponent_HostileTrionDiagnostics>();
            observer.Enabled = !observer.Enabled;
            Messages.Message((observer.Enabled ? "BDP_Dev_HostileTrion_ObserveOn"
                : "BDP_Dev_HostileTrion_ObserveOff").Translate(), MessageTypeDefOf.NeutralEvent, false);
        }

        /// <summary>经正常受伤流程命中完整外部非核心部位；护盾仍按真实规则决定拦截。</summary>
        private static void ApplyTestWound(Pawn pawn)
        {
            ICombatBodyReader body = CombatBodySurfaceAccess.ResolveReader(pawn);
            if (body?.Phase != CombatBodyPhase.Active)
            {
                Reject("BDP_Dev_HostileTrion_NotActive");
                return;
            }
            BodyPartRecord part = pawn.health.hediffSet.GetNotMissingParts()
                .FirstOrDefault(candidate => candidate.depth == BodyPartDepth.Outside
                    && candidate != pawn.RaceProps.body.corePart
                    && pawn.health.hediffSet.GetPartHealth(candidate) > 10f);
            if (part == null)
            {
                Reject("BDP_Dev_HostileTrion_NoPart");
                return;
            }
            pawn.TakeDamage(new DamageInfo(DamageDefOf.Cut, 5f, hitPart: part));
            MapComponent_HostileTrionDiagnostics.Output(pawn);
        }

        /// <summary>所有新增提示从开发语言包读取。</summary>
        private static void Reject(string key)
        {
            Messages.Message(key.Translate(), MessageTypeDefOf.RejectInput, false);
        }
    }
}
